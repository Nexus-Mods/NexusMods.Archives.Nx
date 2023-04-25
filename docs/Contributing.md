# Contributing

!!! info

    Some useful guidance for anyone wishing to contribute to library.

## Public API Analyzer in Rider

!!! tip

    [Rider does not have a way to apply Roslyn code fixes in a larger scope](https://youtrack.jetbrains.com/issue/RIDER-18372),
    so working with [Public API Analyzer](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) might be painful.

To work around this, apply the fixes from `dotnet` CLI:

```pwsh
# Fix 'Not Declared in Public API' & 'nullable enable not defined'
# For some reason, RS0016 can only handle 1 target framework per run (it's pretty silly)
# So you have to run once for every TFM.

dotnet format analyzers ./NexusMods.Archives.Nx.sln --diagnostics RS0037 RS0016 # first time
dotnet format analyzers ./NexusMods.Archives.Nx.sln --diagnostics RS0037 RS0016 # second time
dotnet format analyzers ./NexusMods.Archives.Nx.sln --diagnostics RS0037 RS0016 # third time
# etc...
```

For convenience, a `Powershell` script `FixUndeclaredAPIs.ps1` is included in repo root. 