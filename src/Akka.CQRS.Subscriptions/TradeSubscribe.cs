namespace Akka.CQRS.Subscriptions
{
    /// <summary>
    /// Subscribe to trade events for the specified ticker symbol.
    /// </summary>
    public sealed class TradeSubscribe
    {
        public TradeSubscribe(string tickerSymbol, TradeEventType[] events)
        {
            TickerSymbol = tickerSymbol;
            Events = events;
        }

        public string TickerSymbol { get; }

        public TradeEventType[] Events { get; }
    }
}