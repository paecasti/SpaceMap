using FluentAssertions;

namespace SpaceMap.Integration.Tests.NativeActions;

public sealed class NativeActionServiceTests
{
    [Fact]
    public async Task OpenPathAsync_WithMissingPath_ReturnsFailureCode()
    {
        var host = TestHostBuilder.Build(Path.Combine(Path.GetTempPath(), "SpaceMap.Tests", Guid.NewGuid().ToString("N")));
        var result = await host.DiskScanService.OpenPathAsync(@"C:\definitely-missing-path-for-tests");
        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be("PATH_NOT_FOUND");
    }

    [Fact]
    public async Task CopyPathAsync_WithEmptyPath_ReturnsFailureCode()
    {
        var host = TestHostBuilder.Build(Path.Combine(Path.GetTempPath(), "SpaceMap.Tests", Guid.NewGuid().ToString("N")));
        var result = await host.DiskScanService.CopyPathAsync(string.Empty);
        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_PATH");
    }
}
