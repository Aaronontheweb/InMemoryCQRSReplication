using Akka.Cluster.Hosting;
using Akka.Persistence.Hosting;
using Akka.Remote.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Akka.CQRS.Hosting.Tests
{
    internal class AkkaSettings
    {
        public string ActorSystemName { get; set; } = "AkkaWeb";

        public bool UseClustering { get; set; } = true;

        public bool LogConfigOnStart { get; set; } = false;

        public RemoteOptions RemoteOptions { get; set; } = new()
        {
            // can be overridden via config, but is dynamic by default
            PublicHostName = Dns.GetHostName()
        };

        public ClusterOptions ClusterOptions { get; set; } = new ClusterOptions()
        {
            // use our dynamic local host name by default
            SeedNodes = new[] { $"akka.tcp://AkkaWebApi@{Dns.GetHostName()}:8081" }
        };

        public ShardOptions ShardOptions { get; set; } = new ShardOptions()
        {

        };

        public PersistenceMode PersistenceMode { get; set; } = PersistenceMode.InMemory;
    }
    public enum PersistenceMode
    {
        InMemory,
        Azure
    }
}
