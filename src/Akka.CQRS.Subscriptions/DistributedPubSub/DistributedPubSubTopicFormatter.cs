using Akka.Cluster.Tools.PublishSubscribe;

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
    }
}