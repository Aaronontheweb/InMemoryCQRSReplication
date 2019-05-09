using System;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.CQRS.Events;

namespace Akka.CQRS.Subscriptions.DistributedPubSub
{
    /// <summary>
    /// Formats <see cref="ITradeEvent"/> messages into <see cref="DistributedPubSub"/>-friendly topic names.
    /// </summary>
    public static class DistributedPubSubTopicFormatter
    {
        public static string ToTopic(string tickerSymbol, TradeEventType tradeEventType)
        {
            return $"{tickerSymbol}-{nameof(tradeEventType)}";
        }

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