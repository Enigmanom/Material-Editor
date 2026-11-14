## Material-Editor by [ousnius](https://github.com/ousnius)
A small UI application to edit BGSM/BGEM material files.

[![.NET](https://github.com/ousnius/Material-Editor/actions/workflows/dotnet.yml/badge.svg)](https://github.com/ousnius/Material-Editor/actions/workflows/dotnet.yml)

Original decoding of version 2 files by [gibbed](https://github.com/gibbed) (rick 'at' gibbed 'dot' us).  
Additional work by [jonwd7](https://github.com/jonwd7) on version 20 and more.

### TruePBR (BGSM version 100)
Fallout 4 BGSM files can carry a Community Shaders TruePBR section, edited on the TruePBR tab and saved as version 100 (a version 2 material followed by `CSPB`/`CSTS`/`CSFZ` blocks). Saving is refused while the section is invalid; JSON uses a nested `TruePBR` object.
