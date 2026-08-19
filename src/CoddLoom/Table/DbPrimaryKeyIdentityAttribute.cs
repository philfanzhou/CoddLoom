using CoddLoom.Table.Base;
using System;

namespace CoddLoom.Table;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class DbPrimaryKeyIdentityAttribute : DbPrimaryKeyBaseAttribute
{
}