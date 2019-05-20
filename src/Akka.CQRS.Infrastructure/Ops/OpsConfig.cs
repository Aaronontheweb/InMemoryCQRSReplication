using Akka.Configuration;

namespace Akka.CQRS.Infrastructure.Ops
{
    /// <summary>
    /// Helper class for ensuring that certain parts of Akka.NET's
    /// configuration are applied consistently across all services.
    /// </summary>
    public class OpsConfig
    {
        public static Akka.Configuration.Config GetOpsConfig()
        {
            return ConfigurationFactory.FromResource<OpsConfig>("Akka.CQRS.Infrastructure.Ops.ops.conf");
        }
    }
}
