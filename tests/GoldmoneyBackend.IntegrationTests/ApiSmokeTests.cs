using FluentAssertions;

namespace GoldmoneyBackend.IntegrationTests;

public sealed class ApiSmokeTests
{
    [Fact]
    public void Placeholder_Should_Compile_Integration_Project()
    {
        true.Should().BeTrue();
    }
}
