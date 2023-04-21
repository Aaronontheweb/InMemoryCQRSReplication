using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using static Akka.CQRS.Subscriptions.DistributedPubSub.DistributedPubSubTopicFormatter;

namespace Akka.CQRS.Subscriptions.DistributedPubSub
{
    /// <summary>
    /// <see cref="ITradeEventPublisher"/> used for distributing events over the <see cref="DistributedPubSub.Mediator"/>.
    /// </summary>
    public sealed class DistributedPubSubTradeEventPublisher : ITradeEventPublisher
    {
        private readonly IActorRef _mediator;

        public DistributedPubSubTradeEventPublisher(IActorRef mediator)
        {
            _mediator = mediator;
        }

        public void Publish(string tickerSymbol, ITradeEvent @event)
        {
            var eventType = @event.ToTradeEventType();
            var topic = ToTopic(tickerSymbol, eventType);
            _mediator.Tell(new Publish(topic, @event));
        }

        public static DistributedPubSubTradeEventPublisher For(ActorSystem sys)
        {
            var mediator = Cluster.Tools.PublishSubscribe.DistributedPubSub.Get(sys).Mediator;
            return new DistributedPubSubTradeEventPublisher(mediator);
        }
    }
}
