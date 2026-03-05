using System;
using System.IO;
using System.Windows.Forms;

namespace ThermoDust
{
    public partial class SettingsForm : Form
    {
        // Theme colors (shared with Form1)
        internal static readonly System.Drawing.Color BgDark    = System.Drawing.Color.FromArgb(24, 26, 38);
        internal static readonly System.Drawing.Color BgPanel   = System.Drawing.Color.FromArgb(36, 39, 56);
        internal static readonly System.Drawing.Color BgInput   = System.Drawing.Color.FromArgb(18, 20, 30);
        internal static readonly System.Drawing.Color Accent    = System.Drawing.Color.FromArgb(82, 196, 217);
        internal static readonly System.Drawing.Color TextLight = System.Drawing.Color.FromArgb(220, 228, 240);

        public SettingsForm()
        {
            InitializeComponent();
            LoadSettings();
            ApplyTheme();
        }

        private void LoadSettings()
        {
            txtJavaPath.Text        = Properties.Settings.Default.JavaPath;
            txtMSFraggerJar.Text    = Properties.Settings.Default.MSFraggerJarPath;
            txtFraggerParams.Text   = Properties.Settings.Default.FraggerParamsPath;
            txtOutputDirectory.Text = Properties.Settings.Default.OutputDirectory;
        }

        private void ApplyTheme()
        {
            this.BackColor = BgDark;
            this.ForeColor = TextLight;
            this.Font = new System.Drawing.Font("Segoe UI", 9.5f);

            foreach (Control c in this.Controls)
                ApplyThemeRecursive(c);
        }

        private void ApplyThemeRecursive(Control c)
        {
            c.BackColor = BgPanel;
            c.ForeColor = TextLight;

            if (c is TextBox tb)
            {
                tb.BackColor = BgInput;
                tb.ForeColor = TextLight;
                tb.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (c is Button b)
            {
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderColor = Accent;
                b.BackColor = System.Drawing.Color.FromArgb(44, 48, 70);
                b.ForeColor = TextLight;
                b.Cursor = Cursors.Hand;
            }
            else if (c is Label lbl)
            {
                lbl.BackColor = BgPanel;
                lbl.ForeColor = TextLight;
            }
            else if (c is Panel pnl)
            {
                pnl.BackColor = BgDark;
            }

            foreach (Control child in c.Controls)
                ApplyThemeRecursive(child);
        }

        // ── Browse buttons ──────────────────────────────────────────────────

        private void btnBrowseJava_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title  = "Select java.exe",
                Filter = "Executable|java.exe|All files|*.*"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                txtJavaPath.Text = dlg.FileName;
        }

        private void btnBrowseMSFragger_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title  = "Select MSFragger JAR",
                Filter = "JAR files|*.jar|All files|*.*"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                txtMSFraggerJar.Text = dlg.FileName;
        }

        private void btnBrowseParams_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title  = "Select fragger.params",
                Filter = "Params files|*.params|All files|*.*"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                txtFraggerParams.Text = dlg.FileName;
        }

        private void btnBrowseOutput_Click(object sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description         = "Select output directory for .pin files",
                UseDescriptionForTitle = true
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                txtOutputDirectory.Text = dlg.SelectedPath;
        }

        // ── Detect Defaults button ──────────────────────────────────────────

        private void btnDetect_Click(object sender, EventArgs e)
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;

            string jarCandidate    = Path.Combine(appDir, "msfragger", "MSFragger-3.8.jar");
            string paramsCandidate = Path.Combine(appDir, "msfragger", "fragger.params");
            string defaultOutput   = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QCactus");

            txtJavaPath.Text        = "java";   // rely on PATH; user can override if needed
            txtMSFraggerJar.Text    = File.Exists(jarCandidate)    ? jarCandidate    : txtMSFraggerJar.Text;
            txtFraggerParams.Text   = File.Exists(paramsCandidate) ? paramsCandidate : txtFraggerParams.Text;
            txtOutputDirectory.Text = defaultOutput;

            MessageBox.Show(
                $"Detected:\n  Java: java (PATH)\n  JAR: {txtMSFraggerJar.Text}\n  Params: {txtFraggerParams.Text}\n  Output: {txtOutputDirectory.Text}",
                "Detect Defaults", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ── Save / Cancel ───────────────────────────────────────────────────

        private void btnSave_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.JavaPath        = txtJavaPath.Text.Trim();
            Properties.Settings.Default.MSFraggerJarPath  = txtMSFraggerJar.Text.Trim();
            Properties.Settings.Default.FraggerParamsPath = txtFraggerParams.Text.Trim();
            Properties.Settings.Default.OutputDirectory  = txtOutputDirectory.Text.Trim();
            Properties.Settings.Default.Save();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
