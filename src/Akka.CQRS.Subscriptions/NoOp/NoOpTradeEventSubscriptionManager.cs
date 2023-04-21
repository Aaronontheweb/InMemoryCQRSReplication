using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.CQRS.Subscriptions.NoOp
{
    /// <summary>
    /// Used to ignore subscription management events.
    /// </summary>
    public sealed class NoOpTradeEventSubscriptionManager : ITradeEventSubscriptionManager
    {
        public static readonly NoOpTradeEventSubscriptionManager Instance = new NoOpTradeEventSubscriptionManager();
        private NoOpTradeEventSubscriptionManager() { }

        public Task<TradeSubscribeAck> Subscribe(string tickerSymbol, IActorRef subscriber)
        {
            return Subscribe(tickerSymbol, TradeEventHelpers.AllTradeEventTypes, subscriber);
        }

        public Task<TradeSubscribeAck> Subscribe(string tickerSymbol, TradeEventType @event, IActorRef subscriber)
        {
            return Subscribe(tickerSymbol, new []{ @event }, subscriber);
        }

        public Task<TradeSubscribeAck> Subscribe(string tickerSymbol, TradeEventType[] events, IActorRef subscriber)
        {
            return Task.FromResult(new TradeSubscribeAck(tickerSymbol, TradeEventHelpers.AllTradeEventTypes));
        }

        public Task<TradeUnsubscribeAck> Unsubscribe(string tickerSymbol, TradeEventType[] events, IActorRef subscriber)
        {
            return Task.FromResult(new TradeUnsubscribeAck(tickerSymbol, events));
        }

        public Task<TradeUnsubscribeAck> Unsubscribe(string tickerSymbol, TradeEventType @event, IActorRef subscriber)
        {
            return Unsubscribe(tickerSymbol, new[] {@event}, subscriber);
        }

        public Task<TradeUnsubscribeAck> Unsubscribe(string tickerSymbol, IActorRef subscriber)
        {
            return Unsubscribe(tickerSymbol, TradeEventHelpers.AllTradeEventTypes, subscriber);
        }
    }
}
