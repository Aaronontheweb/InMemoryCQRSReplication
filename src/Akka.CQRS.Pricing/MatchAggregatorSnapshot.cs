using System.Collections.Generic;
using Akka.CQRS.Pricing.Events;

namespace Akka.CQRS.Pricing
{
    /// <summary>
    /// Represents the point-in-time state of the match aggregator at any given time.
    /// </summary>
    public sealed class MatchAggregatorSnapshot
    {
        public MatchAggregatorSnapshot(long queryOffset, decimal avgPrice, double avgVolume, 
            IReadOnlyList<IPriceUpdate> recentPriceUpdates, IReadOnlyList<IVolumeUpdate> recentVolumeUpdates)
        {
            QueryOffset = queryOffset;
            AvgPrice = avgPrice;
            AvgVolume = avgVolume;
            RecentPriceUpdates = recentPriceUpdates;
            RecentVolumeUpdates = recentVolumeUpdates;
        }

        /// <summary>
        /// The sequence number of the Akka.Persistence.Query object to begin reading from.
        /// </summary>
        public long QueryOffset { get; }

        /// <summary>
        /// The most recently saved average price.
        /// </summary>
        public decimal AvgPrice { get; }

        /// <summary>
        /// The most recently saved average volume.
        /// </summary>
        public double AvgVolume { get; }

        public IReadOnlyList<IPriceUpdate> RecentPriceUpdates { get; }

        public IReadOnlyList<IVolumeUpdate> RecentVolumeUpdates { get; }
    }
}
