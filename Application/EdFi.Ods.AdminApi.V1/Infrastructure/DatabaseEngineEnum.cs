namespace EdFi.Ods.AdminApi.V1.Infrastructure;

public static class DatabaseEngineEnum
{
    public const string SqlServer = "SqlServer";
    public const string PostgreSql = "PostgreSql";

    public static string Parse(string value)
    {
        if (value.Equals(SqlServer, StringComparison.InvariantCultureIgnoreCase))
        {
            return SqlServer;
        }

        if (value.Equals(PostgreSql, StringComparison.InvariantCultureIgnoreCase))
        {
            return PostgreSql;
        }

        throw new NotSupportedException("Not supported DatabaseEngine \"" + value + "\". Supported engines: SqlServer, and PostgreSql.");
    }
}
