namespace Oddify.IntegrationTests;

#pragma warning disable CA1515
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<OddifyWebAppFactory>
{
    public const string Name = "Integration";
}
#pragma warning restore CA1515
