// -----------------------------------------------------------------------
// <copyright file="PriceCmdRouter.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.CQRS.Pricing.Commands;
using Akka.CQRS.Pricing.Views;
using Petabridge.Cmd;
using Petabridge.Cmd.Host;

namespace Akka.CQRS.Pricing.Cli
{
    /// <summary>
    /// Actor responsible for carrying out <see cref="PricingCmd.PricingCommandPalette"/> commands.
    /// </summary>
    public sealed class PriceCmdRouter : CommandHandlerActor
    {
        private IActorRef _priceViewMaster;

        public PriceCmdRouter(IActorRef priceViewMaster) : base(PricingCmd.PricingCommandPalette)
        {
            _priceViewMaster = priceViewMaster;

            Process(PricingCmd.TrackPrice.Name, (command, arguments) =>
            {
                var tickerSymbol = arguments.ArgumentValues("symbol").Single();

                // the tracker actor will start automatically recording price information on its own. No further action needed.
                var trackerActor =
                    Context.ActorOf(Props.Create(() => new PriceTrackingActor(tickerSymbol, _priceViewMaster, Sender)));
            });

            Process(PricingCmd.PriceHistory.Name, (command, arguments) =>
            {
                var tickerSymbol = arguments.ArgumentValues("symbol").Single();
                var getPriceTask = _priceViewMaster.Ask<PriceHistory>(new GetPriceHistory(tickerSymbol), TimeSpan.FromSeconds(5));
                var sender = Sender;

                // pipe happy results back to the sender only on successful Ask
                getPriceTask.ContinueWith(tr =>
                {
                    return Enumerable.Select(tr.Result.HistoricalPrices, x => new CommandResponse(x.ToString(), false))
                        .Concat(new []{ CommandResponse.Empty });
                }, TaskContinuationOptions.OnlyOnRanToCompletion).PipeTo(sender);

                // pipe unhappy results back to sender on failure
                getPriceTask.ContinueWith(tr => 
                        new ErroredCommandResponse($"Error while fetching price history for {tickerSymbol} - " +
                                                   $"timed out after 5s"), TaskContinuationOptions.NotOnRanToCompletion)
                    .PipeTo(sender); ;
            });
        }
    }
}