using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProjectionSetup
{
    internal sealed class MainForm : Form
    {
        private readonly CliArgs _cli;
        private readonly TextBox _dirBox;
        private readonly Button _browse;
        private readonly Button _start;
        private readonly Button _close;
        private readonly ProgressBar _bar;
        private readonly Label _status;
        private readonly TextBox _logBox;
        private bool _busy;

        public int ExitCode { get; private set; } = (int)ExitCodes.Cancelled;

        public MainForm(CliArgs cli)
        {
            _cli = cli ?? new CliArgs();

            Text = Installer.AppName + " v" + Installer.Version;
            Font = new Font("Microsoft YaHei UI", 9F);
            ClientSize = new Size(720, 544);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { }

            Label dirLabel = new Label();
            dirLabel.Text = "游戏目录（包含 Terraria.exe 的文件夹）：";
            dirLabel.Location = new Point(14, 14);
            dirLabel.AutoSize = true;

            _dirBox = new TextBox();
            _dirBox.Location = new Point(14, 38);
            _dirBox.Width = 580;

            _browse = new Button();
            _browse.Text = "浏览…";
            _browse.Location = new Point(600, 36);
            _browse.Size = new Size(76, 26);
            _browse.Click += OnBrowse;

            _bar = new ProgressBar();
            _bar.Location = new Point(14, 76);
            _bar.Size = new Size(692, 16);
            _bar.Minimum = 0;
            _bar.Maximum = 100;

            _status = new Label();
            _status.Text = "准备就绪。";
            _status.Location = new Point(14, 98);
            _status.Size = new Size(692, 20);
            _status.AutoEllipsis = true;

            _logBox = new TextBox();
            _logBox.Location = new Point(14, 122);
            _logBox.Size = new Size(692, 348);
            _logBox.Multiline = true;
            _logBox.ReadOnly = true;
            _logBox.ScrollBars = ScrollBars.Both;
            _logBox.WordWrap = false;
            _logBox.BackColor = Color.FromArgb(30, 30, 30);
            _logBox.ForeColor = Color.FromArgb(220, 220, 220);
            _logBox.Font = new Font("Consolas", 8.5F);

            _start = new Button();
            _start.Text = "开始安装";
            _start.Location = new Point(494, 482);
            _start.Size = new Size(110, 32);
            _start.Click += OnStart;

            _close = new Button();
            _close.Text = "关闭";
            _close.Location = new Point(612, 482);
            _close.Size = new Size(94, 32);
            _close.Click += delegate { Close(); };

            Controls.AddRange(new Control[] { dirLabel, _dirBox, _browse, _bar, _status, _logBox, _start, _close });

            Log.Written += OnLogWritten;
            FormClosed += delegate { Log.Written -= OnLogWritten; };
            FormClosing += OnFormClosing;

            AcceptButton = _start;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (!string.IsNullOrWhiteSpace(_cli.GameDir))
            {
                _dirBox.Text = _cli.GameDir;
                _status.Text = "游戏目录来自命令行参数，点「开始安装」继续。";
                return;
            }
            try
            {
                string how;
                string dir = Installer.DetectOnly(null, out how);
                if (!string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, "Terraria.exe")))
                {
                    _dirBox.Text = dir;
                    _status.Text = "已自动找到游戏目录（" + how + "），点「开始安装」。";
                }
                else
                {
                    _status.Text = "没有自动找到游戏，请点「浏览…」选择包含 Terraria.exe 的文件夹。";
                }
            }
            catch (Exception ex)
            {
                Log.Exception("界面自动检测游戏目录失败", ex);
                _status.Text = "自动检测失败，请手动选择游戏目录。";
            }
        }

        private void OnBrowse(object sender, EventArgs e)
        {
            string picked = ShowFolderDialog();
            if (!string.IsNullOrEmpty(picked)) _dirBox.Text = picked;
        }

        private void OnStart(object sender, EventArgs e)
        {
            if (_busy) return;
            string dir = _dirBox.Text.Trim();

            if (string.IsNullOrEmpty(dir) || !File.Exists(Path.Combine(dir, "Terraria.exe")))
            {
                MessageBox.Show(this, "请选择包含 Terraria.exe 的游戏目录。", Installer.AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                string picked = ShowFolderDialog();
                if (string.IsNullOrEmpty(picked)) return;
                _dirBox.Text = picked;
                dir = picked;
                if (!File.Exists(Path.Combine(dir, "Terraria.exe")))
                {
                    MessageBox.Show(this, "这个目录里没有 Terraria.exe。", Installer.AppName,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            RunInstall(dir);
        }

        private void RunInstall(string gameDir)
        {
            _busy = true;
            _start.Enabled = false;
            _browse.Enabled = false;
            _dirBox.Enabled = false;
            _bar.Value = 0;
            _logBox.Clear();
            _status.Text = "开始安装…";

            InstallOptions opt = new InstallOptions();
            opt.GameDir = gameDir;
            opt.Silent = false;
            opt.NoShortcut = _cli.NoShortcut;
            opt.ShortcutName = _cli.ShortcutName;
            opt.ShortcutDir = _cli.ShortcutDir;
            opt.AskGameDir = AskGameDir;
            opt.Progress = OnProgress;

            Task.Run(delegate
            {
                InstallResult r = Installer.Run(opt);
                try { BeginInvoke(new Action(delegate { FinishInstall(r); })); }
                catch { }
            });
        }

        private void OnProgress(int pct, string message)
        {
            if (IsDisposed) return;
            try
            {
                if (!IsHandleCreated) return;
                BeginInvoke(new Action(delegate
                {
                    if (IsDisposed) return;
                    int v = Math.Max(_bar.Minimum, Math.Min(_bar.Maximum, pct));
                    _bar.Value = v;
                    if (!string.IsNullOrEmpty(message)) _status.Text = message;
                }));
            }
            catch { }
        }

        private void FinishInstall(InstallResult r)
        {
            _busy = false;
            _start.Enabled = true;
            _browse.Enabled = true;
            _dirBox.Enabled = true;
            ExitCode = r.Ok ? (int)ExitCodes.Ok : r.ExitCode;
            Log.Close();

            if (r.Ok)
            {
                _bar.Value = 100;
                _status.Text = "安装完成。";
                string extra = r.NoCleanBackup
                    ? "\r\n\r\n[重要] 本版本没有干净的原版备份，卸载时将无法还原 exe。\r\n" +
                      string.Join("\r\n", BackupStore.SteamVerifyHints(r.VersionTag))
                    : "";
                MessageBox.Show(this,
                    "安装完成！\r\n\r\n" +
                    "• 启动：桌面「泰拉瑞亚投影MOD」或 Steam 正常启动\r\n" +
                    "• 卸载：游戏目录里的 卸载.bat（只认同版本的原版备份）\r\n" +
                    "• 原版备份：" + (string.IsNullOrEmpty(r.BackupPath) ? "(没有)" : System.IO.Path.GetFileName(r.BackupPath)) + "\r\n" +
                    "• 游戏更新后需要重跑本安装器\r\n" + extra +
                    "\r\n日志：" + (Log.Path ?? "(无法写入日志)"),
                    Installer.AppName, MessageBoxButtons.OK,
                    r.NoCleanBackup ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            else
            {
                _status.Text = "安装失败：" + r.Message;
                MessageBox.Show(this,
                    "安装失败（退出码 " + r.ExitCode + "）：\r\n\r\n" + r.Message + "\r\n\r\n日志：" + (Log.Path ?? "(无法写入日志)"),
                    Installer.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string AskGameDir()
        {
            try
            {
                return (string)Invoke(new Func<string>(delegate { return ShowFolderDialog(); }));
            }
            catch (Exception e)
            {
                Log.Exception("打开目录选择框失败", e);
                return null;
            }
        }

        private string ShowFolderDialog()
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "选择包含 Terraria.exe 的游戏文件夹";
                dlg.UseDescriptionForTitle = true;
                dlg.ShowNewFolderButton = false;
                string guess = _dirBox.Text.Trim();
                if (!string.IsNullOrEmpty(guess) && Directory.Exists(guess)) dlg.SelectedPath = guess;
                return dlg.ShowDialog(this) == DialogResult.OK ? dlg.SelectedPath : null;
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_busy) return;
            e.Cancel = true;
            MessageBox.Show(this, "正在安装，请等安装结束再关闭。", Installer.AppName,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnLogWritten(string line)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action<string>(OnLogWritten), line); }
                catch { }
                return;
            }
            if (!IsHandleCreated) return;
            if (_logBox.TextLength > 400000) _logBox.Clear();
            _logBox.AppendText(line + "\r\n");
        }
    }
}
