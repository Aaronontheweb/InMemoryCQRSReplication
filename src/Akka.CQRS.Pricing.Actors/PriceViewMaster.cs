using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Event;

namespace Akka.CQRS.Pricing.Actors
{
    /// <summary>
    /// Parent actor to all <see cref="PriceVolumeViewActor"/> instances.
    /// </summary>
    public sealed class PriceViewMaster : ReceiveActor, IWithUnboundedStash
    {
        public class BeginTrackPrices
        {
            public BeginTrackPrices(IActorRef shardRegion)
            {
                ShardRegion = shardRegion;
            }

            public IActorRef ShardRegion { get; }
        }

        private readonly ILoggingAdapter _log = Context.GetLogger();

        public PriceViewMaster()
        {
            WaitingForSharding();
        }

        public IStash Stash { get; set; }

        private void WaitingForSharding()
        {
            Receive<BeginTrackPrices>(b =>
            {
                _log.Info("Received access to stock price mediator... Starting pricing views...");
                var mediator = DistributedPubSub.Get(Context.System).Mediator;

                foreach (var stock in AvailableTickerSymbols.Symbols)
                {
                    Context.ActorOf(Props.Create(() => new PriceVolumeViewActor(stock, b.ShardRegion, mediator)),
                        stock);
                }

                Become(Running);
                Stash.UnstashAll();
            });

            ReceiveAny(_ => Stash.Stash());
        }

        private void Running()
        {
            Receive<IWithStockId>(w =>
            {
                var child = Context.Child(w.StockId);
                if (child.IsNobody())
                {
                    _log.Warning("Message received for unknown ticker symbol [{0}] - sending to dead letters.", w.StockId);
                }

                // Goes to deadletters if stock ticker symbol does not exist.
                child.Forward(w);
            });
        }
    }
}
