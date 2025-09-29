using Qz.Infra.Database.Input;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Qz.Infra.Database;

partial class DbEngine
{
    private readonly struct BatchInsertContext(string table, int batchSize, IDbTransaction transaction)
    {
        public string Table { get; } = table ?? throw new ArgumentNullException(nameof(table));
        public int BatchSize { get; } = batchSize < 1 ? throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than 0.") : batchSize;
        public IDbTransaction Transaction { get; } = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    private int InsertWithTransaction(BatchInsertContext context, IEnumerable<InputValues> inputs)
    {
        if (inputs == null) throw new ArgumentNullException(nameof(inputs));

        var affected = 0;
        var batchIndex = 0;

        foreach (var batch in Chunk(inputs, context.BatchSize))
        {
            try
            {
                affected += InsertChunk(batch, context, batchIndex * context.BatchSize);
            }
            catch (Exception ex)
            {
                var isLastBatch = batch.Count < context.BatchSize;
                var batchLabel = isLastBatch ? "final batch" : $"batch {batchIndex + 1}";
                throw BuildBatchException(ex, batch, batchIndex * context.BatchSize, batchLabel);
            }

            batchIndex++;
        }

        return affected;
    }

    private int InsertChunk(IReadOnlyList<InputValues> chunk, BatchInsertContext context, int startIndex)
    {
        try
        {
            return ExecuteChunk(chunk, context);
        }
        catch
        {
            return ResolveFaulty(chunk.ToList(), context, startIndex);
        }
    }

    private int ResolveFaulty(List<InputValues> chunk, BatchInsertContext context, int startIndex)
    {
        if (chunk.Count == 1)
        {
            try
            {
                return ExecuteChunk(chunk, context);
            }
            catch (Exception ex)
            {
                throw BuildRowException(ex, chunk[0], startIndex);
            }
        }

        var mid = chunk.Count / 2;
        var leftHalf = chunk.GetRange(0, mid);
        var rightHalf = chunk.GetRange(mid, chunk.Count - mid);

        var affected = 0;

        try
        {
            affected += ExecuteChunk(leftHalf, context);
        }
        catch
        {
            return ResolveFaulty(leftHalf, context, startIndex);
        }

        try
        {
            affected += ExecuteChunk(rightHalf, context);
        }
        catch
        {
            return ResolveFaulty(rightHalf, context, startIndex + mid);
        }

        return affected;
    }

    private static InvalidOperationException BuildBatchException(Exception ex, IReadOnlyList<InputValues> batch, int startIndex, string batchLabel)
    {
        var failingIndexInfo = string.Join(", ", Enumerable.Range(0, batch.Count).Select(i => startIndex + i));
        return new InvalidOperationException(
            $"Batch insert failed at {batchLabel}. Affected row indexes: [{failingIndexInfo}]. Original error: {ex.Message}", ex);
    }

    private static InvalidOperationException BuildRowException(Exception ex, InputValues row, int startIndex)
    {
        var sampleValues = string.Join(", ", row.Items.Select(item => $"{item.Column}={FormatValue(item.Value)}"));
        return new InvalidOperationException(
            $"Failed to insert data at index {startIndex}. Values: {sampleValues}. Original error: {ex.Message}", ex);
    }

    private static string FormatValue(object value)
    {
        return value switch
        {
            null => "<null>",
            byte[] bytes => $"0x{BitConverter.ToString(bytes).Replace("-", string.Empty)}",
            string str when str.Length > 64 => str.Substring(0, 61) + "...",
            _ => value.ToString()
        };
    }

    private int ExecuteChunk(IReadOnlyList<InputValues> chunk, BatchInsertContext context)
    {
        if (chunk.Count == 0)
        {
            return 0;
        }

        var valuesCount = CalculateParameterCount(chunk);
        var forceUseParameter = valuesCount < 2100; // sqlserver default parameter count limit.

        var sql = Executor.SqlBuilder.Insert(context.Table, chunk, out var dbParams, forceUseParameter);
        return Executor.NonQuery(sql, dbParams, null, context.Transaction);
    }

    private static int CalculateParameterCount(IReadOnlyList<InputValues> chunk)
    {
        var firstItemCount = chunk[0].Items.Count;
        var allSameCount = chunk.All(input => input.Items.Count == firstItemCount);

        return allSameCount
            ? chunk.Count * firstItemCount
            : chunk.Sum(input => input.Items.Count);
    }

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