# Contributing

!!! info

    Some useful guidance for anyone wishing to contribute to library.

## Public API Analyzer

!!! tip

    This project uses [Public API Analyzer](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) to ensure API stability.  
    Before submitting a PR, please fix any analyzer warnings created by adding new APIs.

Note: The analyzer quick fix only fixes it for one target framework.  
For convenience, a `Powershell` script `FixUndeclaredAPIs.ps1` is included in repo root to work around this.  

## Rider Bug: Tests not Recompiled After Main Library Changes

!!! failure

    In some cases, making changes to the main library will not correctly trigger recompilation of test project; leading
    you to debug old code. This happens in 2023.1.1; unsure if affects other versions. Make sure to hit `Ctrl+Shift+B` to 
    force a recompliation of whole project just in case.
