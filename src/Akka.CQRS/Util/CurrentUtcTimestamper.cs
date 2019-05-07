// -----------------------------------------------------------------------
// <copyright file="CurrentUtcTimestamper.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Akka.CQRS.Util
{
    /// <inheritdoc />
    /// <summary>
    /// Uses <see cref="P:System.DateTimeOffset.UtcNow" /> to provide timestamp signatures.
    /// </summary>
    public sealed class CurrentUtcTimestamper : ITimestamper
    {
        public static readonly CurrentUtcTimestamper Instance = new CurrentUtcTimestamper();
        private CurrentUtcTimestamper() { }
        public DateTimeOffset Now => DateTimeOffset.UtcNow;
    }
}