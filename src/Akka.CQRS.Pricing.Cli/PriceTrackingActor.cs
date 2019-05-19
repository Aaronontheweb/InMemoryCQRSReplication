// -----------------------------------------------------------------------
// <copyright file="PriceTrackingActor.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using Akka.Actor;
using Akka.CQRS.Pricing.Commands;
using Akka.CQRS.Pricing.Events;
using Akka.CQRS.Pricing.Views;
using Petabridge.Cmd;

namespace Akka.CQRS.Pricing.Cli
{
    /// <summary>
    /// Actor responsible for populating the output for the <see cref="PricingCmd.TrackPrice"/> command.
    /// </summary>
    public sealed class PriceTrackingActor : ReceiveActor, IWithUnboundedStash
    {
        private readonly string _tickerSymbol;
        private readonly IActorRef _priceViewActor;
        private readonly IActorRef _commandHandlerActor;
        private ICancelable _priceCheckInterval;

        private IPriceUpdate _currentPrice;

        public PriceTrackingActor(string tickerSymbol, IActorRef priceViewActor, IActorRef commandHandlerActor)
        {
            _priceViewActor = priceViewActor;
            _commandHandlerActor = commandHandlerActor;
            _tickerSymbol = tickerSymbol;

            WaitingForPriceHistory();
        }

        private void WaitingForPriceHistory()
        {
            Receive<PriceHistory>(p =>
            {
                if (p.HistoricalPrices.IsEmpty)
                {
                    _commandHandlerActor.Tell(new CommandResponse($"No historical price data for [{_tickerSymbol}] - waiting for updates.", false));
                    BecomePriceUpdates();
                    return;
                }

                _currentPrice = p.CurrentPriceUpdate;
                foreach (var e in p.HistoricalPrices)
                {
                    _commandHandlerActor.Tell(new CommandResponse(e.ToString(), false));
                }

                BecomePriceUpdates();
            });

            Receive<ReceiveTimeout>(t =>
            {
                _commandHandlerActor.Tell(new CommandResponse($"No historical price data for [{_tickerSymbol}] - waiting for updates.", false));
                BecomePriceUpdates();
            });

            ReceiveAny(_ => Stash.Stash());
        }

        private void BecomePriceUpdates()
        {
            Context.SetReceiveTimeout(null);
            Become(PriceUpdates);
            Stash.UnstashAll();
        }

        private void PriceUpdates()
        {
            Receive<IPriceUpdate>(p =>
            {
                _currentPrice = p;
                _commandHandlerActor.Tell(new CommandResponse(p.ToString(), false));
            });

            Receive<Terminated>(t =>
            {
                _commandHandlerActor.Tell(new CommandResponse("Price View Actor terminated."));
                Context.Stop(Self);
            });
        }

        protected override void PreStart()
        {
            var getlatestPrice = new GetLatestPrice(_tickerSymbol);

            // get the historical price
            _priceViewActor.Tell(new GetPriceHistory(_tickerSymbol));
            _priceCheckInterval = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(3), _priceViewActor, getlatestPrice, Self);

            Context.SetReceiveTimeout(TimeSpan.FromSeconds(1));
            Context.Watch(_priceViewActor);
        }

        protected override void PostStop()
        {
            _priceCheckInterval.Cancel();
        }

        public IStash Stash { get; set; }
    }
}