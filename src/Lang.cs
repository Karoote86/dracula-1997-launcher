// Translations. The English text is the key; data\lang\<code>.txt holds "English<TAB>translation" lines
// (\n written as backslash-n). Missing entries fall back to English.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DraculaLauncher
{
    public static class L
    {
        public static readonly string[][] Languages = {
            new[] { "en", "English" }, new[] { "de", "Deutsch" }, new[] { "fr", "Français" }, new[] { "es", "Español" },
            new[] { "it", "Italiano" }, new[] { "pl", "Polski" }, new[] { "ru", "Русский" }, new[] { "pt", "Português" } };

        public static string Code = "en";
        static Dictionary<string, string> map = new Dictionary<string, string>();

        static string ChoiceFile
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DraculaLauncher", "language.txt"); }
        }

        public static void Init(string home)
        {
            string code = null;
            try { if (File.Exists(ChoiceFile)) code = File.ReadAllText(ChoiceFile).Trim(); } catch { }
            if (code == null || !Languages.Any(l => l[0] == code))
            {
                string sys = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                code = Languages.Any(l => l[0] == sys) ? sys : "en";
            }
            Load(home, code);
        }

        public static void Choose(string home, string code)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ChoiceFile));
                File.WriteAllText(ChoiceFile, code);
            }
            catch { }
            Load(home, code);
        }

        static void Load(string home, string code)
        {
            Code = code;
            map = new Dictionary<string, string>();
            string f = Path.Combine(home, "data", "lang", code + ".txt");
            if (code == "en" || !File.Exists(f)) return;
            foreach (var line in File.ReadAllLines(f, System.Text.Encoding.UTF8))
            {
                int tab = line.IndexOf('\t');
                if (tab <= 0 || line.StartsWith("#")) continue;
                map[Unescape(line.Substring(0, tab))] = Unescape(line.Substring(tab + 1));
            }
        }

        static string Unescape(string s) { return s.Replace("\\n", "\n"); }

        public static string T(string english, params object[] args)
        {
            string s;
            if (!map.TryGetValue(english, out s)) s = english;
            return args.Length > 0 ? string.Format(s, args) : s;
        }

        // Guide in the current language (data\guide.<code>.html), else the English one.
        public static string GuidePath(string home)
        {
            string p = Path.Combine(home, "data", "guide." + Code + ".html");
            return File.Exists(p) ? p : Path.Combine(home, "data", "guide.html");
        }
    }
}
