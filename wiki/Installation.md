# Installation

## Prerequisites

- [.NET 8.0](https://dotnet.microsoft.com/download/dotnet/8.0), [.NET 9.0](https://dotnet.microsoft.com/download/dotnet/9.0), or [.NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) SDK
- Cross-platform: Windows, Linux, macOS

## Install as a `dotnet tool` (recommended)

```bash
dotnet tool install --global Maigret.Net.Cli
```

Then run anywhere:

```bash
maigret zuck
maigret --help
```

Make sure `~/.dotnet/tools` (Linux/macOS) or `%USERPROFILE%\.dotnet\tools` (Windows) is on your `PATH`. The .NET SDK adds it on first tool install — restart your terminal if needed.

### Update

```bash
dotnet tool update --global Maigret.Net.Cli
```

### Uninstall

```bash
dotnet tool uninstall --global Maigret.Net.Cli
```

## Install as a library

Pick the packages your project needs:

```bash
# Core engine (sites, checkers, search, recursive ID following).
dotnet add package Maigret.Net

# Optional: report writers (TXT/CSV/JSON/Markdown + ITemplateEngine hook).
dotnet add package Maigret.Net.Reports

# Optional: default Scriban-based HTML template.
dotnet add package Maigret.Net.Reports.Scriban
```

## Build from source

```bash
git clone --recurse-submodules https://github.com/totpero/Maigret.Net.git
cd Maigret.Net
dotnet build Maigret.Net.slnx
dotnet test  Maigret.Net.slnx --filter "Category!=Integration"
```

Run the CLI without installing:

```bash
dotnet run --project src/Maigret.Net.Cli -- octocat --top-sites 25
```

Pack and install your local build globally:

```bash
dotnet pack Maigret.Net.slnx -c Release -o ./artifacts
dotnet tool install --global --add-source ./artifacts Maigret.Net.Cli --version 0.1.0
```

## Verification

```bash
maigret --version
# 0.1.0
```
