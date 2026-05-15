# Running Tests

The solution has two test projects, both based on `xUnit` + `NSubstitute` + `Shouldly`:

| Project | What it covers |
|---|---|
| `tests/Maigret.Net.Core.Tests` | Site model, checkers, search orchestration, recursive search, activation, ID extraction, permutator, executors, utils, settings |
| `tests/Maigret.Net.Reports.Tests` | Built-in writers (TXT/CSV/JSON/Markdown), Scriban HTML, DI registration |

Tests are organized as ports of the upstream Python suite (`maigret/tests/test_*.py`) plus a few extras (DI smoke tests, `MaigretClient` shape tests).

## Unit tests

The default sweep — fast, no network:

```bash
dotnet test Maigret.Net.slnx --filter "Category!=Integration"
```

Pass `-f net8.0` / `-f net9.0` / `-f net10.0` to target one specific framework.

## Integration tests

Tests under `Maigret.Net.Core.Tests/Integration/` are flagged with `[Trait("Category", "Integration")]` and probe real sites (GitHub, DeviantArt). They are **excluded** from the default run and from CI. Invoke explicitly:

```bash
dotnet test Maigret.Net.slnx --filter "Category=Integration"
```

The integration suite expects a working internet connection, and some tests treat WAF challenges as soft-skips so they do not flake during a Cloudflare or DDoS-Guard incident.

## CI

GitHub Actions (`.github/workflows/build.yml`) runs the unit tests on a 3×3 matrix:

- `ubuntu-latest`, `windows-latest`, `macos-latest`
- `.NET 8.0`, `.NET 9.0`, `.NET 10.0`

A green build means all 9 cells pass the `Category!=Integration` filter. Integration tests are excluded from CI because they depend on third-party site availability.

## Adding a test

Mirror the Python file naming (`test_*.py` → `Test*.cs`). Use the same xUnit conventions as the existing tests:

```csharp
public class TestMyFeature
{
    [Fact]
    public async Task MyFeature_HappyPath_ReturnsExpected()
    {
        // Arrange, Act, Assert
    }
}
```

Mock HTTP via a custom `HttpMessageHandler` — see `TestChecking.cs` and `TestMaigret.cs` for the `StubHandler` pattern.

## Code coverage

`coverlet.collector` is wired into both test projects. To produce a coverage report:

```bash
dotnet test Maigret.Net.slnx --collect:"XPlat Code Coverage"
```

Reports land in `tests/<project>/TestResults/<guid>/coverage.cobertura.xml`. Combine with ReportGenerator for HTML rendering.
