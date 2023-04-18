namespace Akka.CQRS.Hosting.Tests.Fixture
{
    [CollectionDefinition(nameof(TestCollection), DisableParallelization = true)]
    public class TestCollection : ICollectionFixture<TestFixture>
    {
    }
}
