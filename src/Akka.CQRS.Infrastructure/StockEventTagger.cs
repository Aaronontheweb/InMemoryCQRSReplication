using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Akka.CQRS.Events;
using Akka.Persistence.Journal;

namespace Akka.CQRS.Infrastructure
{
    /// <summary>
    /// Used to tag trade events so they can be consumed inside Akka.Persistence.Query
    /// </summary>
    public sealed class StockEventTagger : IWriteEventAdapter
    {
        public string Manifest(object evt)
        {
            return string.Empty;
        }

        public object ToJournal(object evt)
        {
            switch (evt)
            {
                case Ask ask:
                    return new Tagged(evt, ImmutableHashSet<string>.Empty.Add(ask.StockId).Add("Ask"));
                case Bid bid:
                    return new Tagged(evt, ImmutableHashSet<string>.Empty.Add(bid.StockId).Add("Bid"));
                case Fill fill:
                    return new Tagged(evt, ImmutableHashSet<string>.Empty.Add(fill.StockId).Add("Fill"));
                case Match match:
                    return new Tagged(evt, ImmutableHashSet<string>.Empty.Add(match.StockId).Add("Match"));
                default:
                    return evt;
            }
        }
    }
}
