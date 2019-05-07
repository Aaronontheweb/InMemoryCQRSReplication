// -----------------------------------------------------------------------
// <copyright file="IWithOrderId.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
namespace Akka.CQRS
{
    /// <summary>
    /// Marker interface for routing messages with specific trade IDs
    /// </summary>
    public interface IWithOrderId
    {
        /// <summary>
        /// Unique identifier for a specific order
        /// </summary>
        string OrderId { get; }
    }
}