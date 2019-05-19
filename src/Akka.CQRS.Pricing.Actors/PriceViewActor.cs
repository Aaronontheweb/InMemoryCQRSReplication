using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.CQRS.Pricing.Commands;
using Akka.CQRS.Pricing.Events;
using Akka.CQRS.Pricing.Views;
using Akka.Event;

namespace Akka.CQRS.Pricing.Actors
{
    /// <summary>
    /// In-memory, replicated view of the current price and volume for a specific stock.
    /// </summary>
    public sealed class PriceVolumeViewActor : ReceiveActor, IWithUnboundedStash
    {
        private readonly string _tickerSymbol;
        private ICancelable _pruneTimer;
        private readonly ILoggingAdapter _log = Context.GetLogger();

        // the Cluster.Sharding proxy
        private readonly IActorRef _priceActorGateway;

        // the DistributedPubSub mediator
        private readonly IActorRef _mediator;

        private IActorRef _tickerEntity;
        private PriceHistory _history;
        private readonly string _priceTopic;

        private sealed class Prune
        {
            public static readonly Prune Instance = new Prune();
            private Prune() { }
        }

        public PriceVolumeViewActor(string tickerSymbol, IActorRef priceActorGateway, IActorRef mediator)
        {
            _tickerSymbol = tickerSymbol;
            _priceActorGateway = priceActorGateway;
            _priceTopic = PriceTopicHelpers.PriceUpdateTopic(_tickerSymbol);
            _mediator = mediator;
            _history = new PriceHistory(_tickerSymbol, ImmutableSortedSet<IPriceUpdate>.Empty);

            WaitingForPriceAndVolume();
        }

        public IStash Stash { get; set; }

        private void WaitingForPriceAndVolume()
        {
            Receive<PriceAndVolumeSnapshot>(s =>
            {
                if (s.PriceUpdates.Length == 0) // empty set - no price data yet
                {
                    _history = new PriceHistory(_tickerSymbol, ImmutableSortedSet<IPriceUpdate>.Empty);
                    _log.Info("Received empty price history for [{0}]", _history.StockId);
                }
                else
                {
                    _history = new PriceHistory(_tickerSymbol, s.PriceUpdates.ToImmutableSortedSet());
                    _log.Info("Received recent price history for [{0}] - current price is [{1}] as of [{2}]", _history.StockId, _history.CurrentPrice, _history.Until);
                }
                
                _tickerEntity = Sender;
                _mediator.Tell(new Subscribe(_priceTopic, Self));
            });

            Receive<SubscribeAck>(ack =>
            {
                _log.Info("Subscribed to {0} - ready for real-time processing.", _priceTopic);
                Become(Processing);
                Context.Watch(_tickerEntity);
                Context.SetReceiveTimeout(null);
            });

            Receive<ReceiveTimeout>(_ =>
            {
                _log.Warning("Received no initial price values for [{0}] from source of truth after 5s. Retrying..", _tickerSymbol);
                _priceActorGateway.Tell(new FetchPriceAndVolume(_tickerSymbol));
            });
        }

        private void Processing()
        {
            Receive<IPriceUpdate>(p =>
            {
                _history = _history.WithPrice(p);
                _log.Info("[{0}] - current price is [{1}] as of [{2}]", _history.StockId, p.CurrentAvgPrice, p.Timestamp);

            });

            Receive<GetPriceHistory>(h =>
            {
                Sender.Tell(_history);
            });

            Receive<GetLatestPrice>(_ =>
            {
                Sender.Tell(_history.CurrentPriceUpdate);
            });

            Receive<PriceAndVolumeSnapshot>(_ => { }); // ignore

            // purge older price update entries.
            Receive<Prune>(_ => { _history = _history.Prune(DateTimeOffset.UtcNow.AddMinutes(-5)); });

            Receive<Terminated>(t =>
            {
                if (t.ActorRef.Equals(_tickerEntity))
                {
                    _log.Info("Source of truth entity terminated. Re-acquiring...");
                    Context.SetReceiveTimeout(TimeSpan.FromSeconds(5));
                    _priceActorGateway.Tell(new FetchPriceAndVolume(_tickerSymbol));
                    _mediator.Tell(new Unsubscribe(_priceTopic, Self)); // unsubscribe until we acquire new source of truth pricing
                    Become(WaitingForPriceAndVolume);
                }
            });
        }

        protected override void PreStart()
        {
            Context.SetReceiveTimeout(TimeSpan.FromSeconds(5.0));
            _priceActorGateway.Tell(new FetchPriceAndVolume(_tickerSymbol));
            _pruneTimer = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(5), Self, Prune.Instance, ActorRefs.NoSender);
        }

        protected override void PostStop()
        {
            _pruneTimer.Cancel();
        }
    }
}
