using System;

namespace Akka.CQRS
{
    /// <summary>
    /// Produces <see cref="DateTimeOffset"/> records for trades and orders.
    /// </summary>
    public interface ITimestamper
    {
        DateTimeOffset Now { get; }
    }
}
