using Qz.Infra.Database.Table;
using System.Data;

namespace TestProject.DbCode.Tables
{
    internal static class UserTable
    {
        [DbTableName]
        internal const string TableName = "UserTable";

        [DbPrimaryKey(Type = DbType.Int32)]
        internal const string Id = "id";

        [DbColumnString(AllowEmpty = false)]
        internal const string UnionId = "unionId";

        [DbColumn(Type = DbType.DateTime, AllowEmpty = true)]
        public const string RegistrationDate = "registrationDate";

        [DbColumnBinary]
        public const string Data = "data";

        [DbColumn(Type = DbType.Double)]
        public const string DoubleData = "doubleData";

        [DbColumnDecimal(Length = 10, PointLength = 5)]
        public const string DecimalData = "decimalData";

        [DbColumn(Type = DbType.Int16)]
        public const string ShortData = "shortData";

        [DbColumn(Type = DbType.Int32)]
        public const string IntData = "intData";

        [DbColumn(Type = DbType.Boolean)]
        public const string BoolData = "boolData";

        [DbColumnString(AllowUnicode = true)]
        public const string SpecialString = "specialString";
    }
}
