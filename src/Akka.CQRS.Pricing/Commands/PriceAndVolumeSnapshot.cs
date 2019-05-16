// -----------------------------------------------------------------------
// <copyright file="PriceAndVolumeSnapshot.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.CQRS.Pricing.Events;

namespace Akka.CQRS.Pricing.Commands
{
    /// <summary>
    /// The response to a <see cref="FetchPriceAndVolume"/> command.
    /// </summary>
    public sealed class PriceAndVolumeSnapshot : IWithStockId
    {
        public PriceAndVolumeSnapshot(string stockId, IPriceUpdate[] priceUpdates, IVolumeUpdate[] volumeUpdates)
        {
            StockId = stockId;
            PriceUpdates = priceUpdates;
            VolumeUpdates = volumeUpdates;
        }

        public string StockId { get; }

        public IPriceUpdate[] PriceUpdates { get; }

        public IVolumeUpdate[] VolumeUpdates { get; }

        public static readonly IPriceUpdate[] EmptyPrices = new IPriceUpdate[0];
        public static readonly IVolumeUpdate[] EmptyVolumes = new IVolumeUpdate[0];

        public static PriceAndVolumeSnapshot Empty(string stockId)
        {
            return new PriceAndVolumeSnapshot(stockId, EmptyPrices, EmptyVolumes);
        }
    }
}