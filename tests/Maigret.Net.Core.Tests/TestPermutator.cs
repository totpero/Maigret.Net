using Shouldly;

namespace Maigret.Net.Core.Tests;

public class TestPermutator
{
    [Fact]
    public void Gather_Strict_MatchesPython()
    {
        var elements = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
        var permute = new Permute<int>(elements);
        var result = permute.Gather(PermuteMode.Strict);

        var expected = new Dictionary<string, int>
        {
            ["a_b"] = 1,
            ["b_a"] = 2,
            ["a-b"] = 1,
            ["b-a"] = 2,
            ["a.b"] = 1,
            ["b.a"] = 2,
            ["ab"] = 1,
            ["ba"] = 2,
            ["_ab"] = 1,
            ["ab_"] = 1,
            ["_ba"] = 2,
            ["ba_"] = 2,
        };

        result.Count.ShouldBe(expected.Count);
        foreach (var (k, v) in expected)
        {
            result.ShouldContainKeyAndValue(k, v);
        }
    }

    [Fact]
    public void Gather_All_IncludesSingleElements()
    {
        var elements = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
        var permute = new Permute<int>(elements);
        var result = permute.Gather(PermuteMode.All);

        // Single-element members (added by "all" only)
        result.ShouldContainKeyAndValue("a", 1);
        result.ShouldContainKeyAndValue("_a", 1);
        result.ShouldContainKeyAndValue("a_", 1);
        result.ShouldContainKeyAndValue("b", 2);
        result.ShouldContainKeyAndValue("_b", 2);
        result.ShouldContainKeyAndValue("b_", 2);

        // Multi-element members are still present
        result.ShouldContainKeyAndValue("a_b", 1);
        result.ShouldContainKeyAndValue("ab", 1);

        // 6 single + 12 multi = 18
        result.Count.ShouldBe(18);
    }

    [Fact]
    public void Gather_EmptyInput_ReturnsEmpty()
    {
        var permute = new Permute<int>(new Dictionary<string, int>());
        permute.Gather().ShouldBeEmpty();
    }

    [Fact]
    public void Permutations_Simple_MatchesItertools()
    {
        var perms = Permute<int>.Permutations([1, 2, 3], 2)
            .Select(list => string.Join(",", list))
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();
        perms.ShouldBe(["1,2", "1,3", "2,1", "2,3", "3,1", "3,2"]);
    }
}
