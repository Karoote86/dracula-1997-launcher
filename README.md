# Vlad Tepes Dracula (1997) – Launcher for Windows 10/11

Builds a modernized version of the strategy game *Vlad Tepes Dracula* (Computer House, 1997, English CD
version 1.0) from **your own CD** (an `.mds/.mdf` image or an installed original):

- runs on Windows 10/11 without the CD, with correct colours ([cnc-ddraw](https://github.com/FunkyFr3sh/cnc-ddraw))
  and the CD music as OGG files ([ogg-winmm](https://github.com/ayuanx/ogg-winmm))
- 16:9 battlefield with mouse-wheel zoom (7 levels), a fixed side panel at every level,
  unit-type and control-group buttons
- menus and strategy map with blurred side bars instead of black, videos without black lines
- a built-in game guide (`data/guide*.html`); an image install also copies the original manual (`MANUAL.PDF`)
- launcher and guide in English, German, French, Spanish, Italian, Polish, Russian and Portuguese
  (picked from the Windows language, switchable in the launcher; translations in `data/lang/`)
- detects the screen size; display modes 1600x900 to 5120x2880 incl. 16:10 and 21:9; uninstall keeps save games if wanted

**This repository contains no game data.** The launcher reads every game file (EXE, graphics, videos,
music) from your CD. The patches contain only new code; wherever they need bytes of the original, they store
just a reference (offset, length) into your own EXE. Files are verified with SHA-256 checksums
(`data/original_files.tsv`).

## Usage
1. Start `DraculaLauncher.exe`.
2. Choose the source: `Dracula.mds` (the `.mdf` must be next to it) or the `Dracula.exe` of an installed original.
3. Choose a target folder and the settings, click **Install**, then **Play**.
4. To change settings later: choose the target folder, change them, click **Apply settings**.

Music is only available when installing from the image (the CD audio tracks are converted to OGG with the
bundled encoder `encoder/dracenc.exe`).

## Building
- Launcher: `build.cmd` (uses the C# compiler that ships with Windows, .NET Framework 4).
- Encoder: `encoder\build.cmd <path\to\w64devkit\bin>` (gcc, libogg + libvorbis from `third_party`).
- Patches (`data/patches/*.drp`):
  `py -I tools/make_patchset.py <original Dracula.exe> <Art\Council.art> tools/patches/hires_patch.py <outdir>`.
  `tools/patches/hires_patch.py` is the full patch generator (x86 code for zoom, side panel, buttons, menu
  compositing). It contains no game bytes: it reads the bytes it checks or reuses from your EXE and verifies them
  by checksum (made from the development version with `tools/redact_patch_script.py`).

## Licences
See `THIRD_PARTY.md`. *Vlad Tepes Dracula* © 1997 Computer House; this project is not affiliated with the
rights holders.
