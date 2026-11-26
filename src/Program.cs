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
            Application.Run(new MainForm());
        }
    }

    sealed class MainForm : Form
    {
        readonly string home = AppDomain.CurrentDomain.BaseDirectory;
        readonly TextBox src = new TextBox(), dst = new TextBox(), log = new TextBox();
        readonly ComboBox level = new ComboBox(), disp = new ComboBox();
        readonly CheckBox blur = new CheckBox(), full = new CheckBox(), smooth = new CheckBox(), reg = new CheckBox();
        readonly TrackBar vol = new TrackBar();
        readonly Button bInstall = new Button(), bApply = new Button(), bPlay = new Button(), bGuide = new Button(), bOpen = new Button(), bRemove = new Button();
        readonly ProgressBar bar = new ProgressBar();
        readonly Label screenInfo = new Label();

        public MainForm()
        {
            Text = "Vlad Tepes Dracula - Launcher";
            ClientSize = new Size(640, 640);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 9f);

            int y = 12;
            var g1 = Group("1. Installation (from your own CD)", ref y, 150);
            Row(g1, 24, "Source:", src, "CD image (.mds) or folder with an installed original game", BrowseSource);
            Row(g1, 74, "Target:", dst, "Folder for the modernized game", BrowseTarget);
            reg.Text = "Create registry entry and Start menu shortcut (asks for admin rights)";
            reg.Checked = true; reg.SetBounds(12, 120, 600, 22); g1.Controls.Add(reg);

            var g2 = Group("2. Settings", ref y, 200);
            Label(g2, "Start zoom level (battlefield):", 12, 28); Combo(g2, level, Settings.Levels, 230, 24);
            Label(g2, "Display resolution:", 12, 60); Combo(g2, disp, Settings.Displays, 230, 56);
            screenInfo.AutoSize = true; screenInfo.ForeColor = SystemColors.GrayText; screenInfo.Location = new Point(400, 60); g2.Controls.Add(screenInfo);
            disp.SelectedIndexChanged += (s, e) => ShowScreenInfo();
            blur.Text = "Blurred side bars in menus and on the strategy map (otherwise black)"; blur.SetBounds(12, 88, 600, 22);
            full.Text = "Fullscreen (otherwise window)"; full.SetBounds(12, 112, 600, 22);
            smooth.Text = "Smooth picture (filter); off = sharp pixels"; smooth.SetBounds(12, 136, 600, 22);
            g2.Controls.AddRange(new Control[] { blur, full, smooth });
            Label(g2, "Music volume:", 12, 166);
            vol.SetBounds(226, 158, 300, 30); vol.Minimum = 0; vol.Maximum = 100; vol.TickFrequency = 10; g2.Controls.Add(vol);

            bInstall.Text = "Install"; bInstall.SetBounds(12, y, 148, 32); bInstall.Click += (s, e) => Work(true);
            bApply.Text = "Apply settings"; bApply.SetBounds(168, y, 148, 32); bApply.Click += (s, e) => Work(false);
            bGuide.Text = "Game guide"; bGuide.SetBounds(324, y, 148, 32); bGuide.Click += (s, e) => Guide();
            bPlay.Text = "Play"; bPlay.SetBounds(480, y, 148, 32); bPlay.Click += (s, e) => Play();
            Controls.AddRange(new Control[] { bInstall, bApply, bGuide, bPlay });
            y += 40;
            bOpen.Text = "Open game folder"; bOpen.SetBounds(12, y, 148, 28); bOpen.Click += (s, e) => OpenFolder();
            bRemove.Text = "Uninstall..."; bRemove.SetBounds(480, y, 148, 28); bRemove.Click += (s, e) => Remove();
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
            tb.SetBounds(70, y, 440, 24); g.Controls.Add(tb);
            var b = new Button { Text = "Browse..." }; b.SetBounds(518, y - 1, 86, 26); b.Click += browse; g.Controls.Add(b);
            var h = new Label { Text = hint, ForeColor = SystemColors.GrayText, AutoSize = true };
            h.Location = new Point(70, y + 26); g.Controls.Add(h);
        }

        static void Label(Control p, string t, int x, int y)
        {
            p.Controls.Add(new Label { Text = t, AutoSize = true, Location = new Point(x, y) });
        }

        static void Combo(Control p, ComboBox c, string[] items, int x, int y)
        {
            c.DropDownStyle = ComboBoxStyle.DropDownList; c.Items.AddRange(items); c.SetBounds(x, y, 160, 24); p.Controls.Add(c);
        }

        void BrowseSource(object s, EventArgs e)
        {
            using (var d = new OpenFileDialog { Filter = "CD image (*.mds)|*.mds|Original game (Dracula.exe)|Dracula.exe", Title = "CD image or Dracula.exe of the original game" })
                if (d.ShowDialog(this) == DialogResult.OK)
                    src.Text = d.FileName.EndsWith(".mds", StringComparison.OrdinalIgnoreCase) ? d.FileName : Path.GetDirectoryName(d.FileName);
        }

        void BrowseTarget(object s, EventArgs e)
        {
            using (var d = new FolderBrowserDialog { Description = "Target folder for the game" })
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
            screenInfo.Text = scr == null ? "" : "Your screen: " + scr + (scr == sel ? "" : " (scaled to fit)");
        }

        void Work(bool install)
        {
            string source = src.Text.Trim(), target = dst.Text.Trim();
            if (install && source.Length == 0) { MessageBox.Show(this, "Please choose the source first."); return; }
            if (target.Length == 0) { MessageBox.Show(this, "Please choose a target folder."); return; }
            if (install && File.Exists(Path.Combine(target, "Dracula.exe")) &&
                MessageBox.Show(this, "The target folder already contains the game. Overwrite the game files? (Save games are kept.)",
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
                            Say("Registry entry and shortcut ...");
                            Installer.WriteRegistry(target);
                            Installer.CreateShortcut(target, "Vlad Tepes Dracula");
                        }
                    }
                    else ins.Apply(target, st);
                    Say(install ? "Installation complete. Have fun!" : "Settings applied.");
                }
                catch (Exception ex) { Say("ERROR: " + ex.Message); }
                finally { BeginInvoke((Action)(() => SetBusy(false))); }
            }) { IsBackground = true }.Start();
        }

        void SetBusy(bool b) { bInstall.Enabled = bApply.Enabled = bPlay.Enabled = bRemove.Enabled = !b; UseWaitCursor = b; if (b) bar.Value = 0; }

        void OpenFolder()
        {
            string t = dst.Text.Trim();
            if (!Directory.Exists(t)) { MessageBox.Show(this, "The target folder does not exist yet."); return; }
            System.Diagnostics.Process.Start("explorer.exe", "\"" + t + "\"");
        }

        void Remove()
        {
            string t = dst.Text.Trim();
            if (!Installer.IsInstall(t)) { MessageBox.Show(this, "No game installed by this launcher was found in the target folder."); return; }
            if (MessageBox.Show(this, "Remove the game from\n" + t + "\n(including the Start menu shortcut and registry entry)?", Text,
                                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            bool keep = MessageBox.Show(this, "Keep your save games (the Saves folder)?", Text, MessageBoxButtons.YesNo) == DialogResult.Yes;
            SetBusy(true);
            new Thread(() =>
            {
                try
                {
                    new Installer(home) { Log = Say }.Uninstall(t, keep);
                    Say("Removing the registry entry ...");
                    Installer.DeleteRegistry();
                    Say(keep ? "Game removed; save games kept in " + Path.Combine(t, "Saves") : "Game removed.");
                }
                catch (Exception ex) { Say("ERROR: " + ex.Message); }
                finally { BeginInvoke((Action)(() => SetBusy(false))); }
            }) { IsBackground = true }.Start();
        }

        // Own guide (always available); the original manual is in the game folder after an image install.
        void Guide()
        {
            string html = Path.Combine(home, "data", "guide.html");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(html) { UseShellExecute = true });
            string pdf = Path.Combine(dst.Text.Trim(), "MANUAL.PDF");
            if (File.Exists(pdf)) Say("Original manual: " + pdf);
        }

        void Play()
        {
            string exe = Path.Combine(dst.Text.Trim(), "Dracula.exe");
            if (!File.Exists(exe)) { MessageBox.Show(this, "No game found in the target folder."); return; }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe) { WorkingDirectory = Path.GetDirectoryName(exe) });
        }
    }
}
