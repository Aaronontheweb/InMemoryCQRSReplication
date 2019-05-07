// -----------------------------------------------------------------------
// <copyright file="IWithTradeId.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
namespace Akka.CQRS
{
    /// <summary>
    /// Marker interface for routing messages with specific trade IDs
    /// </summary>
    public interface IWithTradeId
    {
        /// <summary>
        /// Unique identifier for a specific trade
        /// </summary>
        string TradeId { get; }
    }
}