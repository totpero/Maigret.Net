# CLI Reference

`maigret <USERNAMES…> [OPTIONS]`

Mirrors the relevant flags of `maigret.py setup_arguments_parser`. Run `maigret --help` for the full inventory at the version you have installed.

## HTTP behavior

| Flag | Default | Description |
|---|---|---|
| `--timeout <SECONDS>` | `30` | Per-request HTTP timeout |
| `--retries <N>` | `0` | Retries on transient failures |
| `-n`, `--max-connections <N>` | `100` | Maximum concurrent requests |
| `--proxy <URL>` | — | Generic HTTP / SOCKS5 proxy |
| `--tor-proxy <URL>` | `socks5://127.0.0.1:9050` (when set) | Tor proxy override |
| `--i2p-proxy <URL>` | `http://127.0.0.1:4444` (when set) | I2P proxy override |
| `--cookies-jar-file <FILE>` | — | Mozilla-format cookies for authenticated sites |

## Search behavior

| Flag | Default | Description |
|---|---|---|
| `--no-recursion` | off | Disable recursive search by extracted IDs |
| `--no-extracting` | off | Disable profile-data extraction |
| `--id-type <TYPE>` | `username` | Identifier type (`gaia_id`, `vk_id`, …) |
| `--permute` | off | Permute multi-username inputs with `_ - .` separators |
| `--db <PATH_OR_URL>` | embedded | Custom `data.json` file or URL |
| `--with-domains` | off | Probe domain names via DNS in addition to HTTP |

## Site selection

| Flag | Default | Description |
|---|---|---|
| `-a`, `--all-sites` | off | Scan every site in the database |
| `--top-sites <N>` | `500` | Cap to the top N sites by Alexa rank |
| `--tags <CSV>` | — | Comma-separated whitelist (`social,photo,us`) |
| `--ignore <CSV>` | — | Comma-separated blacklist of site names |

## Output

| Flag | Description |
|---|---|
| `-o`, `--folderoutput <DIR>` | Folder for report files (default `./reports`) |
| `--txt` | Generate plain-text report |
| `--csv` | Generate CSV report |
| `--json` | Generate JSON report (simple object) |
| `--html` | Generate HTML report (via Scriban template) |
| `--markdown` | Generate Markdown report |
| `--print-not-found` | Also print sites where the username was not found |
| `--print-errors` | Also print sites that returned errors |
| `--no-color` | Disable colored output |

## Exit codes

| Code | Meaning |
|---|---|
| `0` | Success |
| `2` | No usernames provided |
| `1` | Unhandled error |

## Examples

```bash
# Quick lookup
maigret johndoe

# Multiple usernames + permutations
maigret first last --permute

# Limit + tags + ignore list
maigret johndoe --top-sites 100 --tags social --ignore Twitter,Reddit

# All-format reports into a folder
maigret johndoe --top-sites 50 --txt --csv --json --markdown --html -o ./out

# Use a custom local database
maigret johndoe --db ./my-data.json --all-sites
```
