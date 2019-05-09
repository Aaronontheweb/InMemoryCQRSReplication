namespace Akka.CQRS.Subscriptions
{
    /// <summary>
    /// Subscription to a specific ticker was not successful.
    /// </summary>
    public sealed class TradeSubscribeNack
    {
        public TradeSubscribeNack(string tickerSymbol, TradeEventType[] events, string reason)
        {
            TickerSymbol = tickerSymbol;
            Events = events;
            Reason = reason;
        }

        public string TickerSymbol { get; }

        public TradeEventType[] Events { get; }

        public string Reason { get; }
    }
}