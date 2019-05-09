namespace Akka.CQRS.Subscriptions
{
    /// <summary>
    /// Unsubscription to a specific ticker has been successful.
    /// </summary>
    public sealed class TradeUnsubscribeAck
    {
        public TradeUnsubscribeAck(string tickerSymbol, TradeEventType[] events)
        {
            TickerSymbol = tickerSymbol;
            Events = events;
        }

        public string TickerSymbol { get; }

        public TradeEventType[] Events { get; }
    }
}