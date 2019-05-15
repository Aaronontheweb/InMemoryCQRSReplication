// -----------------------------------------------------------------------
// <copyright file="VolumeChanged.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Akka.CQRS.Pricing.Events
{
    /// <summary>
    /// Concrete <see cref="IVolumeUpdate"/> implementation.
    /// </summary>
    public sealed class VolumeChanged : IVolumeUpdate, IComparable<VolumeChanged>
    {
        public VolumeChanged(string stockId, double currentVolume, DateTimeOffset timestamp)
        {
            StockId = stockId;
            CurrentVolume = currentVolume;
            Timestamp = timestamp;
        }

        public string StockId { get; }

        public DateTimeOffset Timestamp { get; }
        public double CurrentVolume { get; }

        public int CompareTo(IVolumeUpdate other)
        {
            if (other is VolumeChanged c)
            {
                return CompareTo(c);
            }
            throw new ArgumentException();
        }

        public int CompareTo(VolumeChanged other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return Timestamp.CompareTo(other.Timestamp);
        }
    }
}