using System;

namespace Akka.CQRS.Subscriptions.DistributedPubSub
{
    /// <summary>
    /// Formats <see cref="ITradeEvent"/> messages into <see cref="DistributedPubSub"/>-friendly topic names.
    /// </summary>
    public static class DistributedPubSubTopicFormatter
    {
        public static string ToTopic(string tickerSymbol, TradeEventType tradeEventType)
        {
            string ToStr(TradeEventType e)
            {
                switch (e)
                {
                    case TradeEventType.Ask:
                        return "Ask";
                    case TradeEventType.Bid:
                        return "Bid";
                    case TradeEventType.Fill:
                        return "Fill";
                    case TradeEventType.Match:
                        return "Match";
                    default:
                        throw new ArgumentOutOfRangeException(nameof(e));
                }
            }
            return $"{tickerSymbol}-{ToStr(tradeEventType)}";
        }
    }
}