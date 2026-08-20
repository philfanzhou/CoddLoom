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
    /// <exception cref="InvalidOperationException">Thrown when a batch cannot be inserted; includes its row-index range and error details.</exception>
    private int InsertWithTransaction(string table, IEnumerable<InputValues> inputs, int batchSize, IDbTransaction transaction)
    {
        if (table == null) throw new ArgumentNullException(nameof(table));
        if (batchSize < 1) throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than 0.");
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (inputs == null) throw new ArgumentNullException(nameof(inputs));

        var affected = 0;
        var batchIndex = 0;
        var startIndex = 0;

        foreach (var batch in Chunk(inputs, batchSize, Executor.MaxParametersPerCommand))
        {
            try
            {
                affected += ExecuteChunk(batch, table, transaction);
            }
            catch (Exception ex)
            {
                throw BuildBatchException(ex, batch, startIndex, $"batch {batchIndex + 1}");
            }

            startIndex += batch.Count;
            batchIndex++;
        }

        return affected;
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
    /// Executes a batch insert.
    /// </summary>
    private int ExecuteChunk(IReadOnlyList<InputValues> chunk, string table, IDbTransaction transaction)
    {
        if (chunk.Count == 0)
        {
            return 0;
        }

        var sql = Executor.SqlBuilder.Insert(table, chunk, out var dbParams);
        return Executor.NonQuery(sql, dbParams, null, transaction);
    }

    /// <summary>
    /// Splits input rows by both the requested batch size and the provider parameter limit.
    /// </summary>
    private static IEnumerable<IReadOnlyList<InputValues>> Chunk(
        IEnumerable<InputValues> source, int size, int maxParameters)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (size < 1) throw new ArgumentOutOfRangeException(nameof(size), "Batch size must be greater than 0.");
        if (maxParameters < 1)
        {
            throw new InvalidOperationException("The provider parameter limit must be greater than 0.");
        }

        var batch = new List<InputValues>(size);
        var parameterCount = 0;

        foreach (var input in source)
        {
            if (input == null) throw new ArgumentNullException(nameof(source), "Input rows cannot contain null values.");

            var rowParameterCount = input.Items.Count;
            if (rowParameterCount > maxParameters)
            {
                throw new InvalidOperationException(
                    $"One input row requires {rowParameterCount} parameters, exceeding the provider limit of {maxParameters}.");
            }

            if (batch.Count > 0
                && (batch.Count >= size || parameterCount > maxParameters - rowParameterCount))
            {
                yield return batch;
                batch = new List<InputValues>(size);
                parameterCount = 0;
            }

            batch.Add(input);
            parameterCount += rowParameterCount;
        }

        if (batch.Count > 0)
        {
            yield return batch;
        }
    }
}
