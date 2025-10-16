using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qz.Infra.Database;
using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Params;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TestProject.DbCode;
using TestProject.DbCode.Entity;
using TestProject.DbCode.Tables;
using TestProject.DbTest;

namespace TestProject.DbTest
{
    /// <summary>
    /// DbEngine分页查询测试类
    /// 测试DbEngine的分页查询功能
    /// </summary>
    [TestClass]
    public class DbEnginePaginationTest : TestBase
    {
        

        /// <summary>
        /// 测试基础分页查询
        /// </summary>
        [TestMethod]
        public void PageSelect_BasicPagination_Should_Succeed()
        {

                // 准备测试数据 - 插入10条记录
                var entities = new List<User>();
                for (int i = 1; i <= 10; i++)
                {
                    entities.Add(new User
                    {
                        Id = i.ToString(),
                        UnionId = $"PaginationUser{i}",
                        RegistrationDate = DateTime.Now.AddDays(-i),
                        IntData = i,
                        Data = new byte[] { (byte)i, (byte)(i + 1), (byte)(i + 2) },
                        DoubleData = i * 10.5,
                        DecimalData = i * 1.23m,
                        ShortData = (short)i,
                        BoolData = i % 2 == 0,
                        SpecialString = $"Pagination{i}"
                    });
                }
                DbEngine.Insert(entities, 5); // 批量插入

                // 测试分页查询 - 第1页，每页3条
                var pageParam = new PageParam { PageNumber = 1, PageSize = 3 };
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "PaginationUser%", WhereOperator.Like);
                var orderBy = new OrderByCondition(UserTable.IntData, false); // ASC

                var result = DbEngine.PageSelect<User>(where, orderBy, pageParam, out var totalPages, out var totalCount);

                // 验证结果
                Assert.AreEqual(10, totalCount, "总记录数应该是10");
                Assert.AreEqual(4, totalPages, "总页数应该是4（10/3=3余1，所以是4页）");
                Assert.AreEqual(3, result.Count, "第1页应该返回3条记录");
                Assert.AreEqual("PaginationUser1", result[0].UnionId, "第1条记录应该是PaginationUser1");
                Assert.AreEqual("PaginationUser2", result[1].UnionId, "第2条记录应该是PaginationUser2");
                Assert.AreEqual("PaginationUser3", result[2].UnionId, "第3条记录应该是PaginationUser3");
        }

        /// <summary>
        /// 测试分页查询 - 第2页
        /// </summary>
        [TestMethod]
        public void PageSelect_SecondPage_Should_Succeed()
        {

                // 准备测试数据 - 插入10条记录
                var entities = new List<User>();
                for (int i = 1; i <= 10; i++)
                {
                    entities.Add(new User
                    {
                        Id = i.ToString(),
                        UnionId = $"SecondPageUser{i}",
                        RegistrationDate = DateTime.Now.AddDays(-i),
                        IntData = i,
                        Data = new byte[] { (byte)i, (byte)(i + 1), (byte)(i + 2) },
                        DoubleData = i * 10.5,
                        DecimalData = i * 1.23m,
                        ShortData = (short)i,
                        BoolData = i % 2 == 0,
                        SpecialString = $"SecondPage{i}"
                    });
                }
                DbEngine.Insert(entities, 5);

                // 测试分页查询 - 第2页，每页3条
                var pageParam = new PageParam { PageNumber = 2, PageSize = 3 };
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "SecondPageUser%", WhereOperator.Like);
                var orderBy = new OrderByCondition(UserTable.IntData, false); // ASC

                var result = DbEngine.PageSelect<User>(where, orderBy, pageParam, out var totalPages, out var totalCount);

                // 验证结果
                Assert.AreEqual(10, totalCount, "总记录数应该是10");
                Assert.AreEqual(4, totalPages, "总页数应该是4");
                Assert.AreEqual(3, result.Count, "第2页应该返回3条记录");
                Assert.AreEqual("SecondPageUser4", result[0].UnionId, "第1条记录应该是SecondPageUser4");
                Assert.AreEqual("SecondPageUser5", result[1].UnionId, "第2条记录应该是SecondPageUser5");
                Assert.AreEqual("SecondPageUser6", result[2].UnionId, "第3条记录应该是SecondPageUser6");
        }

        /// <summary>
        /// 测试分页查询 - 最后一页
        /// </summary>
        [TestMethod]
        public void PageSelect_LastPage_Should_Succeed()
        {

                // 准备测试数据 - 插入10条记录
                var entities = new List<User>();
                for (int i = 1; i <= 10; i++)
                {
                    entities.Add(new User
                    {
                        Id = i.ToString(),
                        UnionId = $"LastPageUser{i}",
                        RegistrationDate = DateTime.Now.AddDays(-i),
                        IntData = i,
                        Data = new byte[] { (byte)i, (byte)(i + 1), (byte)(i + 2) },
                        DoubleData = i * 10.5,
                        DecimalData = i * 1.23m,
                        ShortData = (short)i,
                        BoolData = i % 2 == 0,
                        SpecialString = $"LastPage{i}"
                    });
                }
                DbEngine.Insert(entities, 5);

                // 测试分页查询 - 第4页（最后一页），每页3条
                var pageParam = new PageParam { PageNumber = 4, PageSize = 3 };
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "LastPageUser%", WhereOperator.Like);
                var orderBy = new OrderByCondition(UserTable.IntData, false); // ASC

                var result = DbEngine.PageSelect<User>(where, orderBy, pageParam, out var totalPages, out var totalCount);

                // 验证结果
                Assert.AreEqual(10, totalCount, "总记录数应该是10");
                Assert.AreEqual(4, totalPages, "总页数应该是4");
                Assert.AreEqual(1, result.Count, "最后一页应该返回1条记录");
                Assert.AreEqual("LastPageUser10", result[0].UnionId, "最后一条记录应该是LastPageUser10");
        }

        /// <summary>
        /// 测试分页查询 - 空结果
        /// </summary>
        [TestMethod]
        public void PageSelect_EmptyResult_Should_Succeed()
        {

                // 不插入任何数据，测试空结果的分页查询
                var pageParam = new PageParam { PageNumber = 1, PageSize = 5 };
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "NonExistentUser");
                var orderBy = new OrderByCondition(UserTable.IntData, false);

                var result = DbEngine.PageSelect<User>(where, orderBy, pageParam, out var totalPages, out var totalCount);

                // 验证结果
                Assert.AreEqual(0, totalCount, "总记录数应该是0");
                Assert.AreEqual(0, totalPages, "总页数应该是0");
                Assert.AreEqual(0, result.Count, "结果应该为空");
        }

        /// <summary>
        /// 测试分页查询 - 大页面大小
        /// </summary>
        [TestMethod]
        public void PageSelect_LargePageSize_Should_Succeed()
        {

                // 准备测试数据 - 插入5条记录
                var entities = new List<User>();
                for (int i = 1; i <= 5; i++)
                {
                    entities.Add(new User
                    {
                        Id = i.ToString(),
                        UnionId = $"LargePageUser{i}",
                        RegistrationDate = DateTime.Now.AddDays(-i),
                        IntData = i,
                        Data = new byte[] { (byte)i, (byte)(i + 1), (byte)(i + 2) },
                        DoubleData = i * 10.5,
                        DecimalData = i * 1.23m,
                        ShortData = (short)i,
                        BoolData = i % 2 == 0,
                        SpecialString = $"LargePage{i}"
                    });
                }
                DbEngine.Insert(entities, 5);

                // 测试分页查询 - 页面大小大于总记录数
                var pageParam = new PageParam { PageNumber = 1, PageSize = 10 };
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "LargePageUser%", WhereOperator.Like);
                var orderBy = new OrderByCondition(UserTable.IntData, false);

                var result = DbEngine.PageSelect<User>(where, orderBy, pageParam, out var totalPages, out var totalCount);

                // 验证结果
                Assert.AreEqual(5, totalCount, "总记录数应该是5");
                Assert.AreEqual(1, totalPages, "总页数应该是1");
                Assert.AreEqual(5, result.Count, "应该返回所有5条记录");
        }

        /// <summary>
        /// 测试分页查询 - 降序排列
        /// </summary>
        [TestMethod]
        public void PageSelect_DescendingOrder_Should_Succeed()
        {

                // 准备测试数据 - 插入5条记录
                var entities = new List<User>();
                for (int i = 1; i <= 5; i++)
                {
                    entities.Add(new User
                    {
                        Id = i.ToString(),
                        UnionId = $"DescUser{i}",
                        RegistrationDate = DateTime.Now.AddDays(-i),
                        IntData = i,
                        Data = new byte[] { (byte)i, (byte)(i + 1), (byte)(i + 2) },
                        DoubleData = i * 10.5,
                        DecimalData = i * 1.23m,
                        ShortData = (short)i,
                        BoolData = i % 2 == 0,
                        SpecialString = $"Desc{i}"
                    });
                }
                DbEngine.Insert(entities, 5);

                // 测试分页查询 - 降序排列
                var pageParam = new PageParam { PageNumber = 1, PageSize = 3 };
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "DescUser%", WhereOperator.Like);
                var orderBy = new OrderByCondition(UserTable.IntData, true); // DESC

                var result = DbEngine.PageSelect<User>(where, orderBy, pageParam, out var totalPages, out var totalCount);

                // 验证结果
                Assert.AreEqual(5, totalCount, "总记录数应该是5");
                Assert.AreEqual(2, totalPages, "总页数应该是2");
                Assert.AreEqual(3, result.Count, "第1页应该返回3条记录");
                Assert.AreEqual("DescUser5", result[0].UnionId, "第1条记录应该是DescUser5（降序）");
                Assert.AreEqual("DescUser4", result[1].UnionId, "第2条记录应该是DescUser4");
                Assert.AreEqual("DescUser3", result[2].UnionId, "第3条记录应该是DescUser3");
        }

        /// <summary>
        /// 测试分页查询 - 带列选择
        /// </summary>
        [TestMethod]
        public void PageSelect_WithColumnSelection_Should_Succeed()
        {

                // 准备测试数据 - 插入3条记录
                var entities = new List<User>();
                for (int i = 1; i <= 3; i++)
                {
                    entities.Add(new User
                    {
                        Id = i.ToString(),
                        UnionId = $"ColumnUser{i}",
                        RegistrationDate = DateTime.Now.AddDays(-i),
                        IntData = i,
                        Data = new byte[] { (byte)i, (byte)(i + 1), (byte)(i + 2) },
                        DoubleData = i * 10.5,
                        DecimalData = i * 1.23m,
                        ShortData = (short)i,
                        BoolData = i % 2 == 0,
                        SpecialString = $"Column{i}"
                    });
                }
                DbEngine.Insert(entities, 5);

                // 测试分页查询 - 只选择特定列
                var pageParam = new PageParam { PageNumber = 1, PageSize = 2 };
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "ColumnUser%", WhereOperator.Like);
                var orderBy = new OrderByCondition(UserTable.IntData, false);
                var columns = new ColumnParam();
                columns.AddSelect(UserTable.UnionId);
                columns.AddSelect(UserTable.IntData);

                var result = DbEngine.PageSelect<User>(where, orderBy, columns, pageParam, out var totalPages, out var totalCount);

                // 验证结果
                Assert.AreEqual(3, totalCount, "总记录数应该是3");
                Assert.AreEqual(2, totalPages, "总页数应该是2");
                Assert.AreEqual(2, result.Count, "第1页应该返回2条记录");
                Assert.AreEqual("ColumnUser1", result[0].UnionId, "第1条记录的UnionId应该是ColumnUser1");
                Assert.AreEqual(1, result[0].IntData, "第1条记录的IntData应该是1");
                Assert.AreEqual("ColumnUser2", result[1].UnionId, "第2条记录的UnionId应该是ColumnUser2");
                Assert.AreEqual(2, result[1].IntData, "第2条记录的IntData应该是2");
        }
    }
}
