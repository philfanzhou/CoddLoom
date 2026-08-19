using CoddLoom.Entity;
using System;
using CoddLoom.Tests.DbCode.Tables;

namespace CoddLoom.Tests.DbCode.Entity
{
    [MapTable(Name = UserTable.TableName)]
    public class User
    {
        [MapColumn(Name = UserTable.Id)]
        public string Id { get; set; }

        [MapColumn(Name = UserTable.UnionId)]
        public string UnionId { get; set; }

        [MapColumn(Name = UserTable.RegistrationDate)]
        public DateTime RegistrationDate { get; set; }

        [MapColumn(Name = UserTable.Data)]
        public byte[] Data { get; set; }

        [MapColumn(Name = UserTable.DoubleData)]
        public double DoubleData { get; set; }

        [MapColumn(Name = UserTable.DecimalData)]
        public decimal DecimalData { get; set; }

        [MapColumn(Name = UserTable.ShortData)]
        public short ShortData { get; set; }

        [MapColumn(Name = UserTable.IntData)]
        public int IntData { get; set; }

        [MapColumn(Name = UserTable.BoolData)]
        public bool BoolData { get; set; }

        [MapColumn(Name = UserTable.SpecialString)]
        public string SpecialString { get; set; }
    }
}