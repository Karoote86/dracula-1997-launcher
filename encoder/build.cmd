@echo off
rem Builds dracenc.exe with w64devkit (gcc). Usage: build.cmd [path\to\w64devkit\bin]
setlocal
if not "%~1"=="" set "PATH=%~1;%PATH%"
set T=%~dp0..\third_party
set OGG=%T%\libogg-1.3.6
set VOR=%T%\libvorbis-1.3.7
set L=%VOR%\lib
gcc -O2 -s -static -I"%OGG%\include" -I"%VOR%\include" -I"%L%" -o "%~dp0dracenc.exe" "%~dp0dracenc.c" ^
  "%OGG%\src\bitwise.c" "%OGG%\src\framing.c" ^
  "%L%\analysis.c" "%L%\bitrate.c" "%L%\block.c" "%L%\codebook.c" "%L%\envelope.c" "%L%\floor0.c" ^
  "%L%\floor1.c" "%L%\info.c" "%L%\lookup.c" "%L%\lpc.c" "%L%\lsp.c" "%L%\mapping0.c" "%L%\mdct.c" ^
  "%L%\psy.c" "%L%\registry.c" "%L%\res0.c" "%L%\sharedbook.c" "%L%\smallft.c" "%L%\synthesis.c" ^
  "%L%\vorbisenc.c" "%L%\window.c" -lm
exit /b %errorlevel%
