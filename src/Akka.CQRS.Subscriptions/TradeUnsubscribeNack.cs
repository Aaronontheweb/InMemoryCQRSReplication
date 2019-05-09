namespace Akka.CQRS.Subscriptions
{
    /// <summary>
    /// Unsubscribe from a specific ticker was not successful.
    /// </summary>
    public sealed class TradeUnsubscribeNack
    {
        public TradeUnsubscribeNack(string tickerSymbol, TradeEventType[] events, string reason)
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