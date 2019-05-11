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
            var mongoHocon = @"akka.persistence.journal.mongodb.connection-string = """ + connectionStr + @"""
                                akka.persistence.journal.mongodb.connection-string = """ + connectionStr + @"""";
            return mongoHocon;
        }
    }
}
