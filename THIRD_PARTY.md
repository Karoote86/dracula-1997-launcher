# Third-party components

| Component | Version | Licence | Where | Source |
|---|---|---|---|---|
| cnc-ddraw | v7.1.0.0 | MIT (`third_party/cnc-ddraw-7.1.0.0/LICENSE`) | `bundle/game/ddraw.dll`, `cnc-ddraw config.exe`, `Shaders/`, `ddraw.ini` | https://github.com/FunkyFr3sh/cnc-ddraw/tree/v7.1.0.0 |
| ogg-winmm | v2025.01.16 | GPL-2.0 (`third_party/ogg-winmm-2025.01.16/LICENSE.txt`) | `bundle/game/winmm.dll`, `winmm.ini` | complete source in `third_party/ogg-winmm-2025.01.16` (tag v2025.01.16 of https://github.com/ayuanx/ogg-winmm) |
| libogg | 1.3.6 | BSD-3-Clause (`third_party/libogg-1.3.6/COPYING`) | statically linked into `encoder/dracenc.exe` | `third_party/libogg-1.3.6`, https://xiph.org/downloads/ |
| libvorbis | 1.3.7 | BSD-3-Clause (`third_party/libvorbis-1.3.7/COPYING`) | statically linked into `encoder/dracenc.exe` | `third_party/libvorbis-1.3.7`, https://xiph.org/downloads/ |

The bundled cnc-ddraw and ogg-winmm files are the unmodified files of the official release archives
(`cnc-ddraw.zip` of v7.1.0.0, `ogg-winmm_binary.zip` of v2025.01.16; SHA-256 verified 2026-11-26), except
`ddraw.ini`, which carries this project's settings.

`encoder/dracenc.c` follows libvorbis' `examples/encoder_example.c` (BSD, Xiph.Org Foundation).
`third_party/libogg-1.3.6/include/ogg/config_types.h` was generated from `config_types.h.in` for Windows.

Libogg / libvorbis copyright notice: Copyright (c) 2002-2020 Xiph.org Foundation. Redistribution and use in
source and binary forms, with or without modification, are permitted under the conditions in their COPYING files.
