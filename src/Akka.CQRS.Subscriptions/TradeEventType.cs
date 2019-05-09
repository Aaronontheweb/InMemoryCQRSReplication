using System;

namespace Akka.CQRS.Subscriptions
{
    /// <summary>
    /// The type of trade event we're interested in.
    /// </summary>
    public enum TradeEventType
    {
        Bid,
        Ask,
        Fill,
        Match
    }
}
