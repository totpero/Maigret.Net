# Recursive Search

Maigret's signature feature: after a claimed account is found, the engine **harvests identifiers from the response** (display name, linked usernames, internal IDs) and **re-runs the search** with the new identifiers as inputs. Results from depth ≥ 1 still stream through the same `IAsyncEnumerable`.

## Enabling and disabling

Recursive search is **on by default**. Disable with `--no-recursion` on the CLI or `NoRecursion = true` on `MaigretSearchOptions`.

```csharp
var summary = await client.SearchAsync("johndoe", new MaigretSearchOptions
{
    NoRecursion = false,    // default — recursion on
    RecursionDepth = 3,     // hard cap (default 3)
});
```

## How it works

1. `MaigretClient.StreamAsync` wraps the request in `IRecursiveSearchEngine.SearchAsync`.
2. After each `Claimed` result, the engine calls `RecursiveSearchEngine.ParseUsernames(result.IdsData)` to derive `(id, type)` candidates.
3. Candidates are deduped against an `IdComparer` (`(id, type)` set, case-insensitive on the id).
4. New candidates are batched by `type` (`username`, `vk_id`, …) and re-fed into `MaigretSearchEngine.SearchAsync`.
5. The loop stops when:
   - The queue is empty, **or**
   - Depth reaches `RecursionDepth`, **or**
   - The cancellation token fires.

## ID parsing rules

Ported from `checking.parse_usernames`:

| Key shape | Output type |
|---|---|
| `username`, `*username*` (not `usernames`), `screen_name`, `login` | `username` |
| `usernames` containing a Python-style list (`['a','b']`) | one `username` per item |
| Any of `Checking.SupportedIds` (`gaia_id`, `vk_id`, `ok_id`, …) | the key itself |

Empty values are skipped. Already-probed `(id, type)` pairs are filtered out.

## Limits

- `RecursiveSearchOptions.MaxDepth` — hard cap on recursion depth (default `3`).
- `RecursiveSearchOptions.MaxIdsPerResult` — caps the number of candidates harvested from one result (default `16`). Guards against pathological extractor output.

## Example flow

```
seed = ["alice"]                          (depth 0)
  └─ alice on GitHub → IdsData["fullname"] = "Alice Aim"
                       IdsData["bio"] = "@alice2"
  └─ alice on Instagram → IdsData["screen_name"] = "alice_real"

extracted = [("alice_real", "username")]  (depth 1)
  └─ alice_real on …

stop when depth == MaxDepth.
```

## When to disable

Recursion can multiply the number of HTTP calls dramatically. Disable it when:

- You only need a quick existence check across the top-N sites.
- You're feeding Maigret into a larger pipeline that does its own ID following.
- You're hitting rate limits.

## Plugging in a custom engine

`IRecursiveSearchEngine` is a single-method interface. Register an alternative implementation via DI:

```csharp
services.AddSingleton<IRecursiveSearchEngine, MyDistributedRecursiveEngine>();
```

Common reasons to replace it:

- Persist the queue across process restarts.
- Distribute the second level to a worker pool.
- Add a deduplication layer that survives multiple runs.
