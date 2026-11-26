ogg-winmm: Windows Multimedia API (winmm.dll) CD Audio Emulator 

By AyuanX (https://github.com/ayuanx/ogg-winmm)


HOW TO USE:

1. Rip the audio tracks from the CD and encode them to ogg files, following naming convention: Track02.ogg, Track03.ogg, Track04.ogg, ...
   Note that the file names can not contain any whitespace, and the track numbers must be contiguous without gap in the middle.
   Also note the numbering usually starts from 02 since on mixed mode CDs the first track is a data track.
   In rare cases some games may use a pure audio CD with no data tracks in which case you should start numbering them from "Track01.ogg".

2. Extract the dll file and the ini file to the game folder where the game's main executable file is located.

3. Create a subfolder called "Music" there and put the ogg files in it.

4. (Optional) Override the volume for CDDA/MIDI/WAVE in "winmm.ini" file if desired.

5. Run the game and enjoy the emulated CD audio.
