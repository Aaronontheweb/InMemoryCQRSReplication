// -----------------------------------------------------------------------
// <copyright file="IVolumeUpdate.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Akka.CQRS.Pricing.Events
{
    /// <summary>
    /// Used to signal a change in volume for a specific stock.
    /// </summary>
    public interface IVolumeUpdate : IWithStockId, IComparable<IVolumeUpdate>
    {
        /// <summary>
        /// The time of this price update.
        /// </summary>
        DateTimeOffset Timestamp { get; }

        /// <summary>
        /// The current trade volume.
        /// </summary>
        double CurrentVolume { get; }
    }
}