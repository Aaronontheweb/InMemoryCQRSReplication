using System;
using System.Collections.Generic;
using System.Text;
using Petabridge.Cmd;
using Petabridge.Cmd.Host;

namespace Akka.CQRS.Pricing.Cli
{
    public sealed class PriceCmdRouter : CommandHandlerActor
    {
        public PriceCmdRouter(CommandPalette commands) : base(commands)
        {
        }
    }
}
