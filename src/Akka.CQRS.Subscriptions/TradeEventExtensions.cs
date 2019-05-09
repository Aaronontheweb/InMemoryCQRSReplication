using System;
using Akka.CQRS.Events;

namespace Akka.CQRS.Subscriptions
{
    /// <summary>
    /// Extension methods for working with <see cref="ITradeEvent"/>
    /// </summary>
    public static class TradeEventExtensions
    {
        public static TradeEventType ToTradeEventType(this ITradeEvent @event)
        {
            switch (@event)
            {
                case Bid b:
                    return TradeEventType.Bid;
                case Ask a:
                    return TradeEventType.Ask;
                case Fill f:
                    return TradeEventType.Fill;
                case Match m:
                    return TradeEventType.Match;
                default:
                    throw new ArgumentOutOfRangeException($"[{@event}] is not a supported trade event type.", nameof(@event));
            }
        }
    }
}