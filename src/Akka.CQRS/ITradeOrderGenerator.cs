namespace Akka.CQRS
{
    /// <summary>
    /// Generates unique trade order Ids.
    /// </summary>
    public interface ITradeOrderIdGenerator
    {
        string NextId();
    }
}
