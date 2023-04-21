using Akka.Actor;
using Petabridge.Cmd.Host;
using static Akka.CQRS.Pricing.Cli.PricingCmd;

namespace Akka.CQRS.Pricing.Cli
{
    /// <summary>
    /// The <see cref="PetabridgeCmd"/> command palette handelr for <see cref="PricingCmd.PricingCommandPalette"/>.
    /// </summary>
    public sealed class PriceCommands : CommandPaletteHandler
    {
        private IActorRef _priceViewMaster;

        public PriceCommands(IActorRef priceViewMaster) : base(PricingCommandPalette)
        {
            _priceViewMaster = priceViewMaster;
            HandlerProps = Props.Create(() => new PriceCmdRouter(_priceViewMaster));
        }

        public override Props HandlerProps { get; }
    }
}
