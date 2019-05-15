// -----------------------------------------------------------------------
// <copyright file="UnexpectedEndOfStream.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
namespace Akka.CQRS.Pricing.Actors
{
    /// <summary>
    /// Send this to ourselves in the event that our Akka.Persistence.Query stream completes, which it shouldn't.
    /// </summary>
    public sealed class UnexpectedEndOfStream
    {
        public static readonly UnexpectedEndOfStream Instance = new UnexpectedEndOfStream();
        private UnexpectedEndOfStream() { }
    }
}