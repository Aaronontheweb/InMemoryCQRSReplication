namespace Akka.CQRS.Subscriptions
{
    /// <summary>
    /// Subscription to a specific ticker has been successful.
    /// </summary>
    public sealed class TradeSubscribeAck
    {
        public TradeSubscribeAck(string tickerSymbol, TradeEventType[] events)
        {
            TickerSymbol = tickerSymbol;
            Events = events;
        }

        public string TickerSymbol { get; }

        public TradeEventType[] Events { get; }
    }
}