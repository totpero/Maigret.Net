using Maigret.Net.Core;
using Shouldly;

namespace Maigret.Net.Core.Tests;

public class SmokeTests
{
    [Fact]
    public void MaigretDefaults_Version_IsNotEmpty()
    {
        MaigretDefaults.Version.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void MaigretDefaults_HttpClientName_IsNotEmpty()
    {
        MaigretDefaults.HttpClientName.ShouldNotBeNullOrWhiteSpace();
    }
}
