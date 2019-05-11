using System;
using Akka.Configuration;

namespace Akka.CQRS.Infrastructure
{
    /// <summary>
    /// Shared utility class for formatting MongoDb connection strings into the required
    /// Akka.Persistence.MongoDb HOCON <see cref="Akka.Configuration.Config"/>.
    /// </summary>
    public static class MongoDbHoconHelper
    {
        public static Config GetMongoHocon(string connectionStr)
        {
            return ConfigurationFactory.ParseString($"akka.persistence.journal.mongodb.connection-string = \"{connectionStr}\"" + Environment.NewLine
                + $"akka.persistence.journal.mongodb.connection-string = \"{connectionStr}\"");
        }
    }
}
