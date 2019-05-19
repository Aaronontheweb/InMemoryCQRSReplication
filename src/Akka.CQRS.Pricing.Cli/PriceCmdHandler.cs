using System.Collections.Generic;
using System.Text;
using Akka.Actor;
using Petabridge.Cmd.Host;
using static Akka.CQRS.Pricing.Cli.PricingCmd;

namespace Akka.CQRS.Pricing.Cli
{
    /// <summary>
    /// The <see cref="PetabridgeCmd"/> command palette handelr for <see cref="PricingCmd.PricingCommandPalette"/>.
    /// </summary>
    public sealed class PriceCmdHandler : CommandPaletteHandler
    {
        private IActorRef _priceViewMaster;

        public PriceCmdHandler(IActorRef priceViewMaster) : base(PricingCommandPalette)
        {
            _priceViewMaster = priceViewMaster;
            HandlerProps = Props.Create(() => new PriceCmdRouter(_priceViewMaster));
        }

        public override Props HandlerProps { get; }
    }
}
