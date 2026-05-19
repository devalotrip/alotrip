namespace Shared.Common.Configurations;

public sealed class ConnectionStringsCfg
{
    public const string Section  = "ConnectionStrings";
    public const string Database = "Database";
    public const string DbType   = "DbType";
}

public static class DatabaseType
{
    public const string PostgreSql = "PostgreSql";
    public const string SqlServer  = "SqlServer";
    public const string MySql      = "MySql";
}
