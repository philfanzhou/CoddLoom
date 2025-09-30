using Qz.Infra.Database.Input;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Qz.Infra.Database;

partial class DbEngine
{
    /// <summary>
    /// 批量插入数据，如果任何一条数据插入失败，则抛出异常
    /// </summary>
    /// <param name="table">表名</param>
    /// <param name="inputs">要插入的数据</param>
    /// <param name="batchSize">批次大小</param>
    /// <param name="transaction">事务</param>
    /// <returns>成功插入的记录数</returns>
    /// <exception cref="InvalidOperationException">当任何一条数据插入失败时抛出，包含具体的失败索引和错误信息</exception>
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
                affected += InsertChunk(batch, table, batchSize, transaction, batchIndex * batchSize);
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
    /// 插入单个批次，如果失败则使用二分查找确定具体失败的记录
    /// </summary>
    private int InsertChunk(IReadOnlyList<InputValues> chunk, string table, int batchSize, IDbTransaction transaction, int startIndex)
    {
        try
        {
            return ExecuteChunk(chunk, table, transaction);
        }
        catch (Exception)
        {
            // 如果批次失败，使用二分查找确定具体失败的记录
            var failedIndex = BinarySearchFailedRecord(chunk, table, batchSize, transaction, 0, chunk.Count - 1);
            throw BuildRowException(new Exception("Record insertion failed"), chunk[failedIndex], startIndex + failedIndex);
        }
    }

    /// <summary>
    /// 使用二分查找定位失败的记录
    /// </summary>
    private int BinarySearchFailedRecord(IReadOnlyList<InputValues> chunk, string table, int batchSize, IDbTransaction transaction, int left, int right)
    {
        // 如果只有一个元素，直接返回
        if (left == right)
        {
            return left;
        }

        // 如果只有两个元素，先尝试第一个
        if (right - left == 1)
        {
            try
            {
                ExecuteChunk(new List<InputValues> { chunk[left] }, table, transaction);
                return right; // 第一个成功，第二个失败
            }
            catch
            {
                return left; // 第一个失败
            }
        }

        // 二分查找
        var mid = (left + right) / 2;
        
        try
        {
            // 尝试插入左半部分
            var leftChunk = chunk.Skip(left).Take(mid - left + 1).ToList();
            ExecuteChunk(leftChunk, table, transaction);
            
            // 左半部分成功，问题在右半部分
            return BinarySearchFailedRecord(chunk, table, batchSize, transaction, mid + 1, right);
        }
        catch
        {
            // 左半部分失败，问题在左半部分
            return BinarySearchFailedRecord(chunk, table, batchSize, transaction, left, mid);
        }
    }


    /// <summary>
    /// 构建批次失败的异常
    /// </summary>
    private static InvalidOperationException BuildBatchException(Exception ex, IReadOnlyList<InputValues> batch, int startIndex, string batchLabel)
    {
        var failingIndexInfo = string.Join(", ", Enumerable.Range(0, batch.Count).Select(i => startIndex + i));
        return new InvalidOperationException(
            $"Batch insert failed at {batchLabel}. Affected row indexes: [{failingIndexInfo}]. Original error: {ex.Message}", ex);
    }

    /// <summary>
    /// 构建单条记录失败的异常
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
    /// 执行批次插入
    /// </summary>
    private int ExecuteChunk(IReadOnlyList<InputValues> chunk, string table, IDbTransaction transaction)
    {
        if (chunk.Count == 0)
        {
            return 0;
        }

        // 计算参数数量，用于判断是否使用参数化查询
        // 如果所有记录都有相同的列数，使用乘法优化计算
        // 否则需要遍历所有记录求和
        var firstItemCount = chunk[0].Items.Count;
        var allSameCount = chunk.All(input => input.Items.Count == firstItemCount);
        var valuesCount = allSameCount
            ? chunk.Count * firstItemCount
            : chunk.Sum(input => input.Items.Count);

        // SQL Server 默认参数限制为 2100 个，超过则使用字符串拼接
        var forceUseParameter = valuesCount < 2100;

        var sql = Executor.SqlBuilder.Insert(table, chunk, out var dbParams, forceUseParameter);
        return Executor.NonQuery(sql, dbParams, null, transaction);
    }

    /// <summary>
    /// 将集合分块
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