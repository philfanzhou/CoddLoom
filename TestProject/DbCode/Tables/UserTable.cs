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

        [DbColumn(Type = DbType.String, AllowEmpty = false)]
        internal const string UnionId = "unionId";

        [DbColumn(Type = DbType.DateTime, AllowEmpty = true)]
        public const string RegistrationDate = "registrationDate";

        [DbColumn(Type = DbType.Binary)]
        public const string Data = "data";

        [DbColumn(Type = DbType.Double)]
        public const string DoubleData = "doubleData";

        [DbColumn(Type = DbType.Decimal, FixedLength = true, Length = 10, PointLength = 5)]
        public const string DecimalData = "decimalData";

        [DbColumn(Type = DbType.Int16)]
        public const string ShortData = "shortData";

        [DbColumn(Type = DbType.Int32)]
        public const string IntData = "intData";

        [DbColumn(Type = DbType.Boolean)]
        public const string BoolData = "boolData";
    }
}
