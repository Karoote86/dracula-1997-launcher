// Install / rebuild logic (no UI). All game data comes from the user's own disc image or install.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace DraculaLauncher
{
    public sealed class Settings
    {
        public string Level = "1280x720";      // zoom start level (battlefield resolution)
        public string Display = "2560x1440";   // display mode used for battle + menus
        public bool BlurBars = true;           // menu side bars: blurred copy (true) or black
        public bool Fullscreen = true;
        public bool Smooth = true;             // cnc-ddraw shader (catmull-rom) vs sharp
        public int MusicVolume = 100;

        public static readonly string[] Levels = { "920x480", "1040x560", "1160x640", "1280x720", "1520x880", "1840x1080", "2080x1240" };
        public static readonly string[] Displays = { "1920x1080", "2560x1440", "3840x2160" };

        public string PatchName { get { return "z" + Level + "_d" + Display + "_" + (BlurBars ? "blur" : "black") + ".drp"; } }

        public void Save(string path)
        {
            File.WriteAllLines(path, new[] {
                "; Dracula launcher settings",
                "Level=" + Level, "Display=" + Display, "BlurBars=" + BlurBars, "Fullscreen=" + Fullscreen,
                "Smooth=" + Smooth, "MusicVolume=" + MusicVolume });
        }

        public static Settings Load(string path)
        {
            var s = new Settings();
            if (!File.Exists(path)) return s;
            foreach (var line in File.ReadAllLines(path))
            {
                int eq = line.IndexOf('=');
                if (eq < 0 || line.StartsWith(";")) continue;
                string k = line.Substring(0, eq).Trim(), v = line.Substring(eq + 1).Trim();
                bool b;
                switch (k)
                {
                    case "Level": if (Levels.Contains(v)) s.Level = v; break;
                    case "Display": if (Displays.Contains(v)) s.Display = v; break;
                    case "BlurBars": if (bool.TryParse(v, out b)) s.BlurBars = b; break;
                    case "Fullscreen": if (bool.TryParse(v, out b)) s.Fullscreen = b; break;
                    case "Smooth": if (bool.TryParse(v, out b)) s.Smooth = b; break;
                    case "MusicVolume": int n; if (int.TryParse(v, out n)) s.MusicVolume = Math.Max(0, Math.Min(100, n)); break;
                }
            }
            return s;
        }
    }

    public sealed class Installer
    {
        public const string OrigExeName = "Dracula_Original.exe";   // kept in the game dir for later rebuilds
        public const string SettingsName = "launcher.ini";
        public readonly string Home;          // launcher folder (data\, bundle\, encoder\)
        public Action<string> Log = s => { };

        public Installer(string home) { Home = home; }

        sealed class Entry { public string Path; public long Size; public string Sha; }

        List<Entry> Manifest()
        {
            var list = new List<Entry>();
            foreach (var line in File.ReadAllLines(System.IO.Path.Combine(Home, "data", "original_files.tsv")))
            {
                if (line.Length == 0 || line[0] == '#') continue;
                var p = line.Split('\t');
                list.Add(new Entry { Path = p[0], Size = long.Parse(p[1]), Sha = p[2] });
            }
            return list;
        }

        static string Sha(string file)
        {
            using (var h = System.Security.Cryptography.SHA256.Create())
            using (var f = File.OpenRead(file))
                return BitConverter.ToString(h.ComputeHash(f)).Replace("-", "").ToLowerInvariant();
        }

        // Full install: source = .mds image or a folder with an installed original game.
        public void Install(string source, string target, Settings s)
        {
            Directory.CreateDirectory(target);
            var man = Manifest();
            string tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DraculaLauncher_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            try
            {
                bool image = source.EndsWith(".mds", StringComparison.OrdinalIgnoreCase);
                string musicSrc = null;
                if (image)
                {
                    string mdf = System.IO.Path.ChangeExtension(source, ".mdf");
                    if (!File.Exists(mdf)) throw new FileNotFoundException("The .mdf file next to the .mds is missing: " + mdf);
                    Log("Reading the CD data from the image ...");
                    using (var d = new DiscImage(source, mdf))
                    {
                        var files = d.ListFiles().Where(f => !f.IsDir)
                                     .ToDictionary(f => f.Path.ToUpperInvariant(), f => f);
                        foreach (var e in man)
                        {
                            IsoFile f;
                            if (!files.TryGetValue(e.Path.ToUpperInvariant(), out f))
                                throw new InvalidDataException("File missing on the CD: " + e.Path);
                            d.ExtractFile(f, System.IO.Path.Combine(tmp, "game", e.Path));
                        }
                        IsoFile manual;                                  // original English manual (optional)
                        if (files.TryGetValue("MANUAL.PDF", out manual))
                        {
                            Directory.CreateDirectory(target);
                            d.ExtractFile(manual, System.IO.Path.Combine(target, "MANUAL.PDF"));
                        }
                        Log("Converting the CD music to OGG ...");
                        musicSrc = System.IO.Path.Combine(tmp, "music");
                        Directory.CreateDirectory(musicSrc);
                        string enc = System.IO.Path.Combine(Home, "encoder", "dracenc.exe");
                        foreach (var t in d.Tracks.Where(t => t.IsAudio))
                        {
                            string pcm = System.IO.Path.Combine(tmp, "track.pcm");
                            using (var o = File.Create(pcm)) d.CopyAudio(t, o);
                            string ogg = System.IO.Path.Combine(musicSrc, string.Format("Track{0:00}.ogg", t.Number));
                            Run(enc, Quote(pcm) + " " + Quote(ogg) + " 0.7");
                            File.Delete(pcm);
                            Log(string.Format("  Track{0:00}.ogg", t.Number));
                        }
                    }
                }
                else
                {
                    foreach (var e in man)
                    {
                        string p = System.IO.Path.Combine(source, e.Path);
                        if (!File.Exists(p)) throw new FileNotFoundException("File missing in the source folder: " + e.Path);
                        string dst = System.IO.Path.Combine(tmp, "game", e.Path);
                        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dst));
                        File.Copy(p, dst);
                    }
                    string m = System.IO.Path.Combine(source, "Music");
                    if (Directory.Exists(m) && Directory.GetFiles(m, "Track*.ogg").Length > 0) musicSrc = m;
                }

                Log("Checking the game files ...");
                foreach (var e in man)
                {
                    string p = System.IO.Path.Combine(tmp, "game", e.Path);
                    if (new FileInfo(p).Length != e.Size || Sha(p) != e.Sha)
                        throw new InvalidDataException("File does not match the known original version: " + e.Path +
                            "\n(Only the English CD version 1.0 from 1997 is supported.)");
                }

                Log("Copying the game files ...");
                foreach (var e in man)
                {
                    string dst = System.IO.Path.Combine(target, e.Path);
                    bool save = e.Path.StartsWith("Saves\\", StringComparison.OrdinalIgnoreCase);
                    if (save && File.Exists(dst)) continue;              // never overwrite the player's saves
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dst));
                    File.Copy(System.IO.Path.Combine(tmp, "game", e.Path), dst, true);
                }
                File.Copy(System.IO.Path.Combine(target, "Dracula.exe"), System.IO.Path.Combine(target, OrigExeName), true);

                Log("Removing the black lines from the videos ...");
                foreach (var v in Directory.GetFiles(System.IO.Path.Combine(target, "Cinema"), "*.cin"))
                    if (!Patcher.PatchVideo(v)) Log("  skipped: " + System.IO.Path.GetFileName(v));

                if (musicSrc != null)
                {
                    string md = System.IO.Path.Combine(target, "Music");
                    Directory.CreateDirectory(md);
                    foreach (var f in Directory.GetFiles(musicSrc, "Track*.ogg"))
                        File.Copy(f, System.IO.Path.Combine(md, System.IO.Path.GetFileName(f)), true);
                }
                else Log("Note: no music found (only possible from the image) - the game runs without music.");

                Log("Copying cnc-ddraw and ogg-winmm ...");
                CopyDir(System.IO.Path.Combine(Home, "bundle", "game"), target);
                File.Copy(System.IO.Path.Combine(Home, "data", "guide.html"), System.IO.Path.Combine(target, "Guide.html"), true);

                Apply(target, s);
            }
            finally
            {
                try { Directory.Delete(tmp, true); } catch { }
            }
        }

        // Rebuild Dracula.exe + configs of an existing install with new settings.
        public void Apply(string target, Settings s)
        {
            string orig = System.IO.Path.Combine(target, OrigExeName);
            if (!File.Exists(orig)) throw new FileNotFoundException("Original EXE missing (" + OrigExeName + "). Please install again.");
            var man = Manifest();
            var exeEntry = man.First(e => e.Path.Equals("Dracula.exe", StringComparison.OrdinalIgnoreCase));
            if (Sha(orig) != exeEntry.Sha) throw new InvalidDataException(OrigExeName + " is not the original EXE.");
            Log("Building Dracula.exe (" + s.Level + ", display " + s.Display + ") ...");
            byte[] drp = File.ReadAllBytes(System.IO.Path.Combine(Home, "data", "patches", s.PatchName));
            byte[] exe = Patcher.ApplyExePatch(drp, File.ReadAllBytes(orig),
                                               File.ReadAllBytes(System.IO.Path.Combine(target, "Art", "Council.art")));
            File.WriteAllBytes(System.IO.Path.Combine(target, "Dracula.exe"), exe);

            string dd = System.IO.Path.Combine(target, "ddraw.ini");
            if (File.Exists(dd))
            {
                IniSet(dd, "fullscreen", s.Fullscreen ? "true" : "false");
                IniSet(dd, "shader", s.Smooth ? @"Shaders\interpolation\catmull-rom-bilinear.glsl" : "");
            }
            string wm = System.IO.Path.Combine(target, "winmm.ini");
            if (File.Exists(wm)) IniSet(wm, "CDDAVolume", s.MusicVolume.ToString());
            s.Save(System.IO.Path.Combine(target, SettingsName));
            Log("Done.");
        }

        // Sets the first "key=value" line with this key (the [ddraw] section comes first in cnc-ddraw's ini).
        static void IniSet(string path, string key, string value)
        {
            var lines = File.ReadAllLines(path);
            bool found = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string t = lines[i].TrimStart();
                int eq = t.IndexOf('=');
                if (eq > 0 && !t.StartsWith(";") && t.Substring(0, eq).Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                { lines[i] = key + (lines[i].Contains(" = ") ? " = " : "=") + value; found = true; break; }
            }
            if (!found) throw new InvalidDataException("Setting " + key + " not found in " + System.IO.Path.GetFileName(path));
            File.WriteAllLines(path, lines);
        }

        static void CopyDir(string src, string dst)
        {
            foreach (var f in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
            {
                string rel = f.Substring(src.Length).TrimStart('\\');
                string d = System.IO.Path.Combine(dst, rel);
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(d));
                File.Copy(f, d, true);
            }
        }

        static string Quote(string s) { return "\"" + s + "\""; }

        static void Run(string exe, string args)
        {
            var psi = new ProcessStartInfo(exe, args) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
            using (var p = Process.Start(psi))
            {
                string err = p.StandardError.ReadToEnd();
                p.WaitForExit();
                if (p.ExitCode != 0) throw new Exception(System.IO.Path.GetFileName(exe) + " failed: " + err);
            }
        }

        // Registry entry like the original installer (HKLM, needs admin: runs reg.exe elevated).
        public static void WriteRegistry(string target)
        {
            string dir = target.TrimEnd('\\') + "\\";
            string reg = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dracula_launcher.reg");
            string esc = dir.Replace("\\", "\\\\");
            File.WriteAllText(reg, "Windows Registry Editor Version 5.00\r\n\r\n" +
                "[HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\ComputerHouse\\Dracula]\r\n" +
                "\"InstallPath\"=\"" + esc + "\"\r\n\"SourcePath\"=\"" + esc + "\"\r\n", Encoding.Unicode);
            var psi = new ProcessStartInfo("reg.exe", "import \"" + reg + "\"") { UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden };
            using (var p = Process.Start(psi)) { p.WaitForExit(); if (p.ExitCode != 0) throw new Exception("Registry entry failed"); }
            File.Delete(reg);
        }

        public static void CreateShortcut(string target, string name)
        {
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            Type t = Type.GetTypeFromProgID("WScript.Shell");
            dynamic sh = Activator.CreateInstance(t);
            dynamic lnk = sh.CreateShortcut(System.IO.Path.Combine(dir, name + ".lnk"));
            lnk.TargetPath = System.IO.Path.Combine(target, "Dracula.exe");
            lnk.WorkingDirectory = target;
            lnk.Save();
        }
    }
}
