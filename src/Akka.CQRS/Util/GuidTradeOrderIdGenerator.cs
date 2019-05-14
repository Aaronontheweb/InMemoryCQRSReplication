// -----------------------------------------------------------------------
// <copyright file="GuidTradeOrderIdGenerator.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Akka.CQRS.Util
{
    /// <summary>
    /// Creates trade order ids using <see cref="Guid"/>s.
    /// </summary>
    public sealed class GuidTradeOrderIdGenerator : ITradeOrderIdGenerator
    {
        public static readonly GuidTradeOrderIdGenerator Instance = new GuidTradeOrderIdGenerator();
        private GuidTradeOrderIdGenerator() { }

        public string NextId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}