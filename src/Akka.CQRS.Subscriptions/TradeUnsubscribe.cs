using Akka.Actor;

namespace Akka.CQRS.Subscriptions
{
    /// <summary>
    /// Unsubscribe to trade events for the specified ticker symbol.
    /// </summary>
    public sealed class TradeUnsubscribe
    {
        public TradeUnsubscribe(string tickerSymbol, TradeEventType[] events, IActorRef subscriber)
        {
            TickerSymbol = tickerSymbol;
            Events = events;
            Subscriber = subscriber;
        }

        public string TickerSymbol { get; }

        public TradeEventType[] Events { get; }

        public IActorRef Subscriber { get; }
    }
}