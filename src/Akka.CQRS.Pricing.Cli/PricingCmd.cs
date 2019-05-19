using System;
using Petabridge.Cmd;

namespace Akka.CQRS.Pricing.Cli
{
    /// <summary>
    /// Defines all of the Petabridge.Cmd commands for tracking the price.
    /// </summary>
    public static class PricingCmd
    {
        public static readonly CommandDefinition TrackPrice = new CommandDefinitionBuilder()
            .WithName("track").WithDescription("Track the live changes in price for a given ticker symbol continuously. Press Control + C to exit.")
            .WithArgument(b => b.IsMandatory(true).WithName("symbol").WithSwitch("-s").WithSwitch("-S").WithSwitch("--symbol").WithDefaultValues(AvailableTickerSymbols.Symbols))
            .Build();

        public static readonly CommandDefinition PriceHistory = new CommandDefinitionBuilder()
            .WithName("history").WithDescription("Get the historical price history for the specified ticker symbol")
            .WithArgument(b => b.IsMandatory(true).WithName("symbol").WithSwitch("-s").WithSwitch("-S").WithSwitch("--symbol").WithDefaultValues(AvailableTickerSymbols.Symbols))
            .Build();

        public static readonly CommandPalette PricingCommandPalette = new CommandPalette("price", new []{ TrackPrice, PriceHistory });
    }
}
