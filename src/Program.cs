// Dracula launcher: install from the user's own CD image / original install, settings, start the game.
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace DraculaLauncher
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            L.Init(AppDomain.CurrentDomain.BaseDirectory);
            Application.Run(new MainForm());
        }
    }

    sealed class MainForm : Form
    {
        readonly string home = AppDomain.CurrentDomain.BaseDirectory;
        readonly TextBox src = new TextBox(), dst = new TextBox(), log = new TextBox();
        readonly ComboBox level = new ComboBox(), disp = new ComboBox(), lang = new ComboBox();
        readonly CheckBox blur = new CheckBox(), full = new CheckBox(), smooth = new CheckBox(), reg = new CheckBox();
        readonly TrackBar vol = new TrackBar();
        readonly Button bInstall = new Button(), bApply = new Button(), bPlay = new Button(), bGuide = new Button(), bOpen = new Button(), bRemove = new Button();
        readonly ProgressBar bar = new ProgressBar();
        readonly Label screenInfo = new Label();

        public MainForm()
        {
            Text = "Vlad Tepes Dracula - Launcher";
            ClientSize = new Size(640, 676);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 9f);

            int y = 12;
            var ll = new Label { Text = L.T("Language:"), AutoSize = true };
            ll.Location = new Point(400, y + 4); Controls.Add(ll);
            lang.DropDownStyle = ComboBoxStyle.DropDownList; lang.SetBounds(478, y, 150, 24);
            foreach (var l in L.Languages) lang.Items.Add(l[1]);
            lang.SelectedIndex = Array.FindIndex(L.Languages, l => l[0] == L.Code);
            lang.SelectedIndexChanged += (s, e) =>
            {
                string code = L.Languages[lang.SelectedIndex][0];
                if (code == L.Code) return;
                L.Choose(home, code);
                Application.Restart();
            };
            Controls.Add(lang);
            y += 34;
            var g1 = Group(L.T("1. Installation (from your own CD)"), ref y, 150);
            Row(g1, 24, L.T("Source:"), src, L.T("CD image (.mds) or folder with an installed original game"), BrowseSource);
            Row(g1, 74, L.T("Target:"), dst, L.T("Folder for the modernized game"), BrowseTarget);
            reg.Text = L.T("Create registry entry and Start menu shortcut (asks for admin rights)");
            reg.Checked = true; reg.SetBounds(12, 120, 600, 22); g1.Controls.Add(reg);

            var g2 = Group(L.T("2. Settings"), ref y, 200);
            Label(g2, L.T("Start zoom level (battlefield):"), 12, 28); Combo(g2, level, Settings.Levels, 300, 24);
            Label(g2, L.T("Display resolution:"), 12, 60); Combo(g2, disp, Settings.Displays, 300, 56);
            screenInfo.AutoSize = true; screenInfo.ForeColor = SystemColors.GrayText; screenInfo.Location = new Point(450, 60); g2.Controls.Add(screenInfo);
            disp.SelectedIndexChanged += (s, e) => ShowScreenInfo();
            blur.Text = L.T("Blurred side bars in menus and on the strategy map (otherwise black)"); blur.SetBounds(12, 88, 600, 22);
            full.Text = L.T("Fullscreen (otherwise window)"); full.SetBounds(12, 112, 600, 22);
            smooth.Text = L.T("Smooth picture (filter); off = sharp pixels"); smooth.SetBounds(12, 136, 600, 22);
            g2.Controls.AddRange(new Control[] { blur, full, smooth });
            Label(g2, L.T("Music volume:"), 12, 166);
            vol.SetBounds(296, 158, 300, 30); vol.Minimum = 0; vol.Maximum = 100; vol.TickFrequency = 10; g2.Controls.Add(vol);

            bInstall.Text = L.T("Install"); bInstall.SetBounds(12, y, 148, 32); bInstall.Click += (s, e) => Work(true);
            bApply.Text = L.T("Apply settings"); bApply.SetBounds(168, y, 148, 32); bApply.Click += (s, e) => Work(false);
            bGuide.Text = L.T("Game guide"); bGuide.SetBounds(324, y, 148, 32); bGuide.Click += (s, e) => Guide();
            bPlay.Text = L.T("Play"); bPlay.SetBounds(480, y, 148, 32); bPlay.Click += (s, e) => Play();
            Controls.AddRange(new Control[] { bInstall, bApply, bGuide, bPlay });
            y += 40;
            bOpen.Text = L.T("Open game folder"); bOpen.SetBounds(12, y, 148, 28); bOpen.Click += (s, e) => OpenFolder();
            bRemove.Text = L.T("Uninstall..."); bRemove.SetBounds(480, y, 148, 28); bRemove.Click += (s, e) => Remove();
            bar.SetBounds(168, y + 4, 304, 20); bar.Minimum = 0; bar.Maximum = 100;
            Controls.AddRange(new Control[] { bOpen, bRemove, bar });
            y += 38;
            log.Multiline = true; log.ReadOnly = true; log.ScrollBars = ScrollBars.Vertical;
            log.SetBounds(12, y, 616, ClientSize.Height - y - 12); Controls.Add(log);

            dst.Text = @"C:\Games\Dracula";
            dst.TextChanged += (s, e) => LoadSettings();
            LoadSettings();
        }

        GroupBox Group(string t, ref int y, int h)
        {
            var g = new GroupBox { Text = t };
            g.SetBounds(12, y, 616, h); Controls.Add(g); y += h + 10; return g;
        }

        void Row(GroupBox g, int y, string label, TextBox tb, string hint, EventHandler browse)
        {
            Label(g, label, 12, y + 4);
            tb.SetBounds(100, y, 398, 24); g.Controls.Add(tb);
            var b = new Button { Text = L.T("Browse...") }; b.SetBounds(504, y - 1, 100, 26); b.Click += browse; g.Controls.Add(b);
            var h = new Label { Text = hint, ForeColor = SystemColors.GrayText, AutoSize = true };
            h.Location = new Point(100, y + 26); g.Controls.Add(h);
        }

        static void Label(Control p, string t, int x, int y)
        {
            p.Controls.Add(new Label { Text = t, AutoSize = true, Location = new Point(x, y) });
        }

        static void Combo(Control p, ComboBox c, string[] items, int x, int y)
        {
            c.DropDownStyle = ComboBoxStyle.DropDownList; c.Items.AddRange(items); c.SetBounds(x, y, 140, 24); p.Controls.Add(c);
        }

        void BrowseSource(object s, EventArgs e)
        {
            using (var d = new OpenFileDialog { Filter = L.T("CD image (*.mds)|*.mds|Original game (Dracula.exe)|Dracula.exe"), Title = L.T("CD image or Dracula.exe of the original game") })
                if (d.ShowDialog(this) == DialogResult.OK)
                    src.Text = d.FileName.EndsWith(".mds", StringComparison.OrdinalIgnoreCase) ? d.FileName : Path.GetDirectoryName(d.FileName);
        }

        void BrowseTarget(object s, EventArgs e)
        {
            using (var d = new FolderBrowserDialog { Description = L.T("Target folder for the game") })
                if (d.ShowDialog(this) == DialogResult.OK) dst.Text = d.SelectedPath;
        }

        Settings Current()
        {
            return new Settings
            {
                Level = (string)level.SelectedItem, Display = (string)disp.SelectedItem, BlurBars = blur.Checked,
                Fullscreen = full.Checked, Smooth = smooth.Checked, MusicVolume = vol.Value
            };
        }

        void LoadSettings()
        {
            var st = Settings.Load(Path.Combine(dst.Text, Installer.SettingsName));
            level.SelectedItem = st.Level; disp.SelectedItem = st.Display; blur.Checked = st.BlurBars;
            full.Checked = st.Fullscreen; smooth.Checked = st.Smooth; vol.Value = st.MusicVolume;
            ShowScreenInfo();
        }

        void Say(string t) { BeginInvoke((Action)(() => log.AppendText(t + Environment.NewLine))); }
        void SetProgress(int p) { BeginInvoke((Action)(() => bar.Value = Math.Max(0, Math.Min(100, p)))); }

        void ShowScreenInfo()
        {
            string scr = Settings.ScreenSize();
            string sel = (string)disp.SelectedItem;
            screenInfo.Text = scr == null ? "" : L.T("Your screen: {0}", scr) + (scr == sel ? "" : L.T(" (scaled to fit)"));
        }

        void Work(bool install)
        {
            string source = src.Text.Trim(), target = dst.Text.Trim();
            if (install && source.Length == 0) { MessageBox.Show(this, L.T("Please choose the source first.")); return; }
            if (target.Length == 0) { MessageBox.Show(this, L.T("Please choose a target folder.")); return; }
            if (install && File.Exists(Path.Combine(target, "Dracula.exe")) &&
                MessageBox.Show(this, L.T("The target folder already contains the game. Overwrite the game files? (Save games are kept.)"),
                                Text, MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            var st = Current();
            bool doReg = reg.Checked;
            SetBusy(true);
            new Thread(() =>
            {
                try
                {
                    var ins = new Installer(home) { Log = Say, Progress = SetProgress };
                    if (install)
                    {
                        ins.Install(source, target, st);
                        if (doReg)
                        {
                            Say(L.T("Registry entry and shortcut ..."));
                            Installer.WriteRegistry(target);
                            Installer.CreateShortcut(target, "Vlad Tepes Dracula");
                        }
                    }
                    else ins.Apply(target, st);
                    Say(install ? L.T("Installation complete. Have fun!") : L.T("Settings applied."));
                }
                catch (Exception ex) { Say(L.T("ERROR: {0}", ex.Message)); }
                finally { BeginInvoke((Action)(() => SetBusy(false))); }
            }) { IsBackground = true }.Start();
        }

        void SetBusy(bool b) { bInstall.Enabled = bApply.Enabled = bPlay.Enabled = bRemove.Enabled = !b; UseWaitCursor = b; if (b) bar.Value = 0; }

        void OpenFolder()
        {
            string t = dst.Text.Trim();
            if (!Directory.Exists(t)) { MessageBox.Show(this, L.T("The target folder does not exist yet.")); return; }
            System.Diagnostics.Process.Start("explorer.exe", "\"" + t + "\"");
        }

        void Remove()
        {
            string t = dst.Text.Trim();
            if (!Installer.IsInstall(t)) { MessageBox.Show(this, L.T("No game installed by this launcher was found in the target folder.")); return; }
            if (MessageBox.Show(this, L.T("Remove the game from\n{0}\n(including the Start menu shortcut and registry entry)?", t), Text,
                                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            SetBusy(true);
            new Thread(() =>
            {
                try
                {
                    new Installer(home) { Log = Say }.Uninstall(t);
                    Say(L.T("Removing the registry entry ..."));
                    Installer.DeleteRegistry();
                    Say(L.T("Game removed. Your save games stay in {0}", Installer.SavesHome));
                }
                catch (Exception ex) { Say(L.T("ERROR: {0}", ex.Message)); }
                finally { BeginInvoke((Action)(() => SetBusy(false))); }
            }) { IsBackground = true }.Start();
        }

        // Own guide (always available); the original manual is in the game folder after an image install.
        void Guide()
        {
            string html = L.GuidePath(home);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(html) { UseShellExecute = true });
            string pdf = Path.Combine(dst.Text.Trim(), "MANUAL.PDF");
            if (File.Exists(pdf)) Say(L.T("Original manual: {0}", pdf));
        }

        void Play()
        {
            string exe = Path.Combine(dst.Text.Trim(), "Dracula.exe");
            if (!File.Exists(exe)) { MessageBox.Show(this, L.T("No game found in the target folder.")); return; }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe) { WorkingDirectory = Path.GetDirectoryName(exe) });
        }
    }
}
