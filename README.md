<div align="center">
	<h1>The Nexus (.nx) Archive Format</h1>
	<img src="./docs/Images/Nexus-Icon.png" width="150" align="center" />
	<br/> <br/>
    A Quite OK Archive Format.
    <br/>
    For <i>Storing</i> and <i>Sharing</i> Mods.<br/>
    <a href="https://codecov.io/gh/Nexus-Mods/NexusMods.Archives.Nx" > 
      <img src="https://codecov.io/gh/Nexus-Mods/NexusMods.Archives.Nx/branch/main/graph/badge.svg?token=ZRKYAL4EF2"/> 
    </a>
    <img alt="GitHub Workflow Status" src="https://img.shields.io/github/actions/workflow/status/Nexus-Mods/NexusMods.Archives.Nx/BuildAndTest.yml">
</div>

## About

The Nexus (`.nx`) format is a semi-SOLID archive format, using *modern* compression technologies such as
[ZStandard](http://facebook.github.io/zstd/) and [LZ4](http://lz4.github.io/lz4/) under the hood.

```mermaid
flowchart TD
    subgraph Block 2
        BigFile1.bin
    end

    subgraph Block 1
        BigFile0.bin
    end

    subgraph Block 0
        ModConfig.json -.-> Updates.json 
        Updates.json -.-> more["... more .json files"]        
    end
```

Between size optimized SOLID archives like `.7z` w/ `LZMA` and non-SOLID archives like `.zip` w/ `Deflate`, the Nexus
(`.nx`) format bridges the gap; providing a tradeoff with most of the benefit of both worlds.

We aim to create a simple format, appropriate for both local storage of mods and for downloading from the web.  
By using modern compression techniques, we provide both competitive file size and compression speeds.  

To learn more, have a look at the [dedicated documentation page](https://nexus-mods.github.io/NexusMods.Archives.Nx/) 🧡.