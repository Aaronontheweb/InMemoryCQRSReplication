namespace Akka.CQRS.Infrastructure
{
    /// <summary>
    /// Shared utility class for formatting SQL connection strings into the required
    /// Akka.Persistence.Sql HOCON <see cref="Akka.Configuration.Config"/>.
    /// </summary>
    public static class SqlDbHoconHelper
    {
        public static Configuration.Config GetSqlHocon(string connectionStr, string providerName)
        {
            var hocon = $@"
akka.persistence.journal.sql {{
    connection-string = ""{connectionStr}""
    provider-name = ""{providerName}""
}}
akka.persistence.query.journal.sql {{
    connection-string = ""{connectionStr}""
    provider-name = ""{providerName}""
}}
akka.persistence.snapshot-store.sql{{
    connection-string = ""{connectionStr}""
    provider-name = ""{providerName}""
}}";
            return hocon;
        }
    }
}
