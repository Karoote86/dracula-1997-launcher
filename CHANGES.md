# Changes compared to the original game

This page lists every way the modernized *Vlad Tepes Dracula* differs from the original 1997 English CD
version 1.0. Everything below is applied by the launcher to **your own** copy of the game; this repository
contains no game data.

Status: **in launcher** = part of the current launcher patches; **in testing** = built and being playtested,
coming with the next launcher update.

## 1. Running on Windows 10/11 (in launcher)
- **No CD needed.** The CD check (`FUN_0041d5a0`, file offset 0x1C9A0) always reports "CD found".
- **Graphics:** [cnc-ddraw](https://github.com/FunkyFr3sh/cnc-ddraw) replaces DirectDraw: correct colours, borderless
  fullscreen or window (Alt+Enter), aspect ratio kept, optional smoothing shader (Catmull-Rom).
- **CD music:** [ogg-winmm](https://github.com/ayuanx/ogg-winmm) plays the CD audio tracks from OGG files. The
  launcher converts the tracks of your CD image with its own small Vorbis encoder (libvorbis, quality 0.7).
- **Cleaner exit:** the game stops CD audio before closing ("stop cdaudio" instead of "stop cdaudio wait", which hung
  with ogg-winmm). Quitting with the menu's Exit is clean; Alt+F4 can still leave a background process for a while.
- **Registry:** the launcher creates the same entries as the original installer (`InstallPath`, `SourcePath`).
- **Save games** live in `Documents\Vlad Tepes Dracula\Saves`. The game folder's `Saves` is a link to it, so saves
  survive reinstalling or uninstalling.

## 2. Videos (in launcher)
- The cinematics were stored interlaced (every other line black). One header flag per video file is changed so the
  video player doubles the lines instead.

## 3. Battle screen (in launcher)
- **16:9 high resolution:** the battlefield is drawn at a much larger size (e.g. 1120x720 view at the 1280x720 level
  instead of 480x480). All size-dependent constants of the battle code were adjusted (scrolling, visible tiles,
  minimap box, fog, positional sound, selection, etc.).
- **Mouse-wheel zoom** with 7 levels (battlefield resolutions 920x480 up to 2080x1240). The point under the cursor
  stays in place; the start level is a launcher setting.
- **Fixed side panel:** the battle runs at a fixed display resolution (launcher setting, e.g. 2560x1440); the side
  panel is always drawn at the same size (2x), the battlefield is scaled to the rest of the screen, and the mouse
  cursor keeps a constant size.
- **Taller side panel:** the info box stays where it was; below it the panel is extended with plain stone texture from
  the original art and closes with the original bottom edge.
- **Unit-type buttons** below the info box (swordsmen, crossbowmen, knights, juggernauts, catapults, cannons):
  click = select all living units of that type on the whole map, Shift+click = add them. The number shows how many
  are still alive. (The keyboard shortcuts for this already existed in the original: Ctrl+S/C/K/J/T/N.)

## 4. Menus and strategy map (in launcher)
- The 640x480 menus and the strategy map are shown sharp at 3x in the centre of the screen. The side bars show a
  blurred, darkened copy of the picture instead of black (launcher option: black). The black torn edges of the
  strategy-map parchment are filled the same way.
- Main menu: a visible sword cursor, and the highlighted menu entry follows the mouse exactly.
- Videos keep black side bars.

## 5. Unit behaviour in battle (in testing)
These fix original behaviour that made large battles slow and frustrating.
- **No endless path searching:** a blocked unit whose path search fails now waits briefly (about half a second)
  before trying again, instead of repeating the same expensive search every frame. In the original, a few blocked
  units used up the shared per-frame search budget, so all other units stood still and the game slowed down.
- **No wandering targets:** when a unit's target cell was occupied, the original moved the target randomly by 1-2
  cells on every retry, so units drifted away from their group. Now a unit within 3 cells (4 for big units) of an
  occupied target simply stops there.
- **Making way:** an idle own unit that blocks another unit's way (not on Guard, without an order of its own) steps
  aside to a free neighbouring spot. Works for all unit types, including the 2x2 units (knights, juggernauts,
  catapults, cannons). The original had no yielding at all, so narrow passages got blocked.
- **No detours around your own army:** when a unit is blocked by terrain or by its own troops, its new route only
  avoids terrain instead of treating every own unit as a wall (which sent units on long detours around big groups).
  Enemy units are still walked around.
- **Attacking while marching:** swordsmen, crossbowmen and knights on a plain move order now react to enemies within
  their sight range like idle units do: ranged units stop and shoot, melee units attack. Siege units (juggernauts,
  catapults, cannons) keep marching. Explicit attack orders and patrols are unchanged.
- **Resuming the march:** a unit that left its march to fight continues to its original destination once no enemy
  is in range any more. A new move order or Stop (S) cancels this.
- **Enemy groups react together:** when an enemy soldier attacks, idle enemies within 8 cells join in on the same
  target. In the original only the edge of an enemy formation charged, one soldier at a time.
- **Hidden stragglers:** the original already lets an enemy army that is down to fewer than 10 % (at least 5) of its
  strength lose one soldier hidden in the fog every ~10 seconds, but the rule stopped working as soon as the first
  candidate stood near a visible area. It now tries the next hidden soldier instead.
- **Last stand:** below that same threshold, the remaining idle enemies attack your nearest unit instead of standing
  somewhere in the fog, so you no longer have to search the map for them.
- **Patrol bug fixed:** the Patrol command in the original set *all* own units to patrol, not only the selected ones.
- **Group buttons removed:** an earlier version of this project added control-group buttons to the side panel; they
  were dropped again. Control groups work as in the original via the keys (Ctrl+0..9 store, 0..9 recall,
  Shift+0..9 add).

## 6. Not changed
- Game rules, unit values, the campaign, the enemy's strategic AI, texts and voices (the game stays in English),
  the videos themselves, the original keyboard shortcuts (see the in-game help, F1 / Ctrl+H, and `data/guide.html`).

## Technical details
- Patch generator: `tools/patches/hires_patch.py` (x86 code caves in an added `.hires` section of the EXE; every patch
  site verifies the original bytes first). Launcher patch files: `data/patches/*.drp` (see `tools/make_patchset.py`).
- Third-party components and licences: `THIRD_PARTY.md`.
- Report problems: [Issues](https://github.com/Karoote86/dracula-1997-launcher/issues).
