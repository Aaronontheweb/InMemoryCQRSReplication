using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;

namespace Akka.CQRS.Subscriptions.DistributedPubSub
{
    /// <summary>
    /// <see cref="ITradeEventSubscriptionManager"/> that uses the <see cref="DistributedPubSub.Mediator"/> under the hood.
    /// </summary>
    public sealed class DistributedPubSubTradeEventSubscriptionManager : ITradeEventSubscriptionManager
    {
        private readonly IActorRef _mediator;

        public DistributedPubSubTradeEventSubscriptionManager(IActorRef mediator)
        {
            _mediator = mediator;
        }

        public async Task<TradeSubscribeAck> Subscribe(string tickerSymbol, TradeEventType[] events, IActorRef subscriber)
        {
            var tasks = ToTopics(tickerSymbol, events).Select(x =>
                _mediator.Ask<SubscribeAck>(new Subscribe(x, subscriber), TimeSpan.FromSeconds(3)));

            await Task.WhenAll(tasks).ConfigureAwait(false);
            
            return new TradeSubscribeAck(tickerSymbol, events);
        }

        public async Task<TradeUnsubscribeAck> Unsubscribe(string tickerSymbol, TradeEventType[] events, IActorRef subscriber)
        {
            var tasks = ToTopics(tickerSymbol, events).Select(x =>
                _mediator.Ask<UnsubscribeAck>(new Unsubscribe(x, subscriber), TimeSpan.FromSeconds(3)));

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return new TradeUnsubscribeAck(tickerSymbol, events);
        }

        internal static string[] ToTopics(string tickerSymbol, TradeEventType[] events)
        {
            return events.Select(x => DistributedPubSubTopicFormatter.ToTopic(tickerSymbol, x)).ToArray();
        }

        public static DistributedPubSubTradeEventSubscriptionManager For(ActorSystem sys)
        {
            var mediator = Cluster.Tools.PublishSubscribe.DistributedPubSub.Get(sys).Mediator;
            return new DistributedPubSubTradeEventSubscriptionManager(mediator);
        }
    }
}