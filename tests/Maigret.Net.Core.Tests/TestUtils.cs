using Shouldly;

namespace Maigret.Net.Core.Tests;

public class TestUtils
{
    [Fact]
    public void CaseConverter_CamelToSnake() => CaseConverter.CamelToSnake("SnakeCasedString").ShouldBe("snake_cased_string");

    [Fact]
    public void CaseConverter_SnakeToCamel() => CaseConverter.SnakeToCamel("camel_cased_string").ShouldBe("camelCasedString");

    [Fact]
    public void CaseConverter_CamelWithDigits_NoChange() => CaseConverter.CamelToSnake("ignore403").ShouldBe("ignore403");

    [Theory]
    [InlineData("ru", true)]
    [InlineData("FR", true)]
    [InlineData("global", true)]
    [InlineData("a1", false)]
    [InlineData("dating", false)]
    [InlineData("", false)]
    public void TagUtils_IsCountryTag(string tag, bool expected) => TagUtils.IsCountryTag(tag).ShouldBe(expected);

    [Fact]
    public void TagUtils_EnrichLinkStr_Plain() => TagUtils.EnrichLinkStr("test").ShouldBe("test");

    [Fact]
    public void TagUtils_EnrichLinkStr_WrapsUrl()
    {
        TagUtils.EnrichLinkStr(" www.flickr.com/photos/x/")
            .ShouldBe("<a class=\"auto-link\" href=\"www.flickr.com/photos/x/\">www.flickr.com/photos/x/</a>");
    }

    [Theory]
    [InlineData("None", "")]
    [InlineData("https://flickr.com/photos/foo", "flickr.com/photos/foo")]
    [InlineData("https://www.flickr.com/photos/foo/", "flickr.com/photos/foo")]
    [InlineData("http://m.flickr.com/photos/foo", "flickr.com/photos/foo")]
    public void UrlMatcher_ExtractMainPart(string input, string expected) => UrlMatcher.ExtractMainPart(input).ShouldBe(expected);

    [Theory]
    [InlineData("http://flickr.com/photos/{username}")]
    [InlineData("https://flickr.com/photos/{username}")]
    [InlineData("https://www.flickr.com/photos/{username}")]
    [InlineData("https://m.flickr.com/photos/{username}/")]
    public void UrlMatcher_MakeProfileUrlRegexp_MatchesAllVariants(string template)
    {
        var regex = UrlMatcher.MakeProfileUrlRegexp(template, string.Empty);
        regex.ShouldNotBeNull();

        // Cross-check: the produced regex matches every shape Maigret accepts.
        foreach (var scheme in new[] { "http://", "https://" })
        {
            foreach (var prefix in new[] { string.Empty, "www.", "m." })
            {
                foreach (var trail in new[] { string.Empty, "/" })
                {
                    var url = $"{scheme}{prefix}flickr.com/photos/alex{trail}";
                    regex.IsMatch(url).ShouldBeTrue($"failed to match: {url}");
                }
            }
        }
    }

    [Fact]
    public void Utils_GetRandomUserAgent_ReturnsKnown()
    {
        var ua = MaigretUtilities.GetRandomUserAgent();
        ua.ShouldNotBeNullOrWhiteSpace();
        MaigretUtilities.DefaultUserAgents.ShouldContain(ua);
    }

    [Fact]
    public void Utils_GenerateRandomUsername_RespectsLength()
    {
        var u = MaigretUtilities.GenerateRandomUsername(12);
        u.Length.ShouldBe(12);
        u.ShouldMatch("^[a-z]+$");
    }

    [Fact]
    public void Utils_GetDictAsciiTree_FormatsLastBranch()
    {
        var items = new[]
        {
            ("uid", "abc"),
            ("name", "Alex"),
        };
        var tree = MaigretUtilities.GetDictAsciiTree(items, prepend: " ");

        tree.ShouldContain("├─uid: abc");
        tree.ShouldContain("└─name: Alex");
    }
}
