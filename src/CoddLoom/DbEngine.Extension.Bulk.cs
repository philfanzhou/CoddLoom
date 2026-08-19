using CoddLoom.Input;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace CoddLoom;

partial class DbEngine
{
    /// <summary>
    /// Inserts data in batches and throws when any record cannot be inserted.
    /// </summary>
    /// <param name="table">The table name.</param>
    /// <param name="inputs">The records to insert.</param>
    /// <param name="batchSize">The batch size.</param>
    /// <param name="transaction">The transaction.</param>
    /// <returns>The number of successfully inserted records.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a record cannot be inserted; includes its index and error details.</exception>
    private int InsertWithTransaction(string table, IEnumerable<InputValues> inputs, int batchSize, IDbTransaction transaction)
    {
        if (table == null) throw new ArgumentNullException(nameof(table));
        if (batchSize < 1) throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than 0.");
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (inputs == null) throw new ArgumentNullException(nameof(inputs));

        var affected = 0;
        var batchIndex = 0;

        foreach (var batch in Chunk(inputs, batchSize))
        {
            try
            {
                affected += InsertChunk(batch, table, transaction, batchIndex * batchSize);
            }
            catch (Exception ex)
            {
                var isLastBatch = batch.Count < batchSize;
                var batchLabel = isLastBatch ? "final batch" : $"batch {batchIndex + 1}";
                throw BuildBatchException(ex, batch, batchIndex * batchSize, batchLabel);
            }

            batchIndex++;
        }

        return affected;
    }

    /// <summary>
    /// Inserts one batch and uses binary search to identify a failed record.
    /// </summary>
    private int InsertChunk(IReadOnlyList<InputValues> chunk, string table, IDbTransaction transaction, int startIndex)
    {
        try
        {
            return ExecuteChunk(chunk, table, transaction);
        }
        catch (Exception)
        {
            // Use binary search to identify the failed record when the batch fails.
            var failedIndex = BinarySearchFailedRecord(chunk, table, transaction, 0, chunk.Count - 1);
            throw BuildRowException(new Exception("Record insertion failed"), chunk[failedIndex], startIndex + failedIndex);
        }
    }

    /// <summary>
    /// Locates a failed record with binary search.
    /// </summary>
    private int BinarySearchFailedRecord(IReadOnlyList<InputValues> chunk, string table, IDbTransaction transaction, int left, int right)
    {
        // Return immediately when only one element remains.
        if (left == right)
        {
            return left;
        }

        // When two elements remain, try the first one.
        if (right - left == 1)
        {
            try
            {
                ExecuteChunk(new List<InputValues> { chunk[left] }, table, transaction);
                return right; // The first succeeded, so the second failed.
            }
            catch
            {
                return left; // The first record failed.
            }
        }

        // Binary search.
        var mid = (left + right) / 2;
        
        try
        {
            // Try to insert the left half.
            var leftChunk = chunk.Skip(left).Take(mid - left + 1).ToList();
            ExecuteChunk(leftChunk, table, transaction);
            
            // The left half succeeded, so the problem is in the right half.
            return BinarySearchFailedRecord(chunk, table, transaction, mid + 1, right);
        }
        catch
        {
            // The left half failed, so the problem is there.
            return BinarySearchFailedRecord(chunk, table, transaction, left, mid);
        }
    }


    /// <summary>
    /// Builds an exception for a failed batch.
    /// </summary>
    private static InvalidOperationException BuildBatchException(Exception ex, IReadOnlyList<InputValues> batch, int startIndex, string batchLabel)
    {
        var failingIndexInfo = string.Join(", ", Enumerable.Range(0, batch.Count).Select(i => startIndex + i));
        return new InvalidOperationException(
            $"Batch insert failed at {batchLabel}. Affected row indexes: [{failingIndexInfo}]. Original error: {ex.Message}", ex);
    }

    /// <summary>
    /// Builds an exception for a failed record.
    /// </summary>
    private static InvalidOperationException BuildRowException(Exception ex, InputValues row, int index)
    {
        var sampleValues = string.Join(", ", row.Items.Select(item => $"{item.Column}={FormatValue(item.Value)}"));
        return new InvalidOperationException(
            $"Failed to insert data at index {index}. Values: {sampleValues}. Original error: {ex.Message}", ex);

        static string FormatValue(object value)
        {
            return value switch
            {
                null => "<null>",
                byte[] bytes => $"0x{BitConverter.ToString(bytes).Replace("-", string.Empty)}",
                string str when str.Length > 64 => str.Substring(0, 61) + "...",
                _ => value.ToString()
            };
        }
    }

    /// <summary>
    /// Executes a batch insert.
    /// </summary>
    private int ExecuteChunk(IReadOnlyList<InputValues> chunk, string table, IDbTransaction transaction)
    {
        if (chunk.Count == 0)
        {
            return 0;
        }

        // Count parameters to decide whether to use a parameterized query.
        // When every record has the same number of columns, multiplication avoids an extra traversal;
        // otherwise, sum the column counts for all records.
        var firstItemCount = chunk[0].Items.Count;
        var allSameCount = chunk.All(input => input.Items.Count == firstItemCount);
        var valuesCount = allSameCount
            ? chunk.Count * firstItemCount
            : chunk.Sum(input => input.Items.Count);

        // SQL Server has a default limit of 2,100 parameters; use literal values above that limit.
        var forceUseParameter = valuesCount < 2100;

        var sql = Executor.SqlBuilder.Insert(table, chunk, out var dbParams, forceUseParameter);
        return Executor.NonQuery(sql, dbParams, null, transaction);
    }

    /// <summary>
    /// Splits a collection into chunks.
    /// </summary>
    private static IEnumerable<IReadOnlyList<T>> Chunk<T>(IEnumerable<T> source, int size)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (size < 1) throw new ArgumentOutOfRangeException(nameof(size), "Batch size must be greater than 0.");

        using var enumerator = source.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var batch = new List<T>(size) { enumerator.Current };
            for (var i = 1; i < size && enumerator.MoveNext(); i++)
            {
                batch.Add(enumerator.Current);
            }

            yield return batch;
        }
    }
}
