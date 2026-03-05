namespace ThermoDust
{
    partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblJavaPath        = new System.Windows.Forms.Label();
            this.txtJavaPath        = new System.Windows.Forms.TextBox();
            this.btnBrowseJava      = new System.Windows.Forms.Button();

            this.lblMSFraggerJar    = new System.Windows.Forms.Label();
            this.txtMSFraggerJar    = new System.Windows.Forms.TextBox();
            this.btnBrowseMSFragger = new System.Windows.Forms.Button();

            this.lblFraggerParams   = new System.Windows.Forms.Label();
            this.txtFraggerParams   = new System.Windows.Forms.TextBox();
            this.btnBrowseParams    = new System.Windows.Forms.Button();

            this.lblOutputDirectory = new System.Windows.Forms.Label();
            this.txtOutputDirectory = new System.Windows.Forms.TextBox();
            this.btnBrowseOutput    = new System.Windows.Forms.Button();

            this.btnDetect          = new System.Windows.Forms.Button();
            this.btnSave            = new System.Windows.Forms.Button();
            this.btnCancel          = new System.Windows.Forms.Button();

            this.SuspendLayout();

            int lw = 140;   // label width
            int tw = 320;   // textbox width
            int bw = 90;    // browse button width
            int bh = 26;
            int margin = 16;
            int rowH = 52;
            int col1 = margin;
            int col2 = col1 + lw + 6;
            int col3 = col2 + tw + 6;

            // ── Row 0: Java ─────────────────────────────────────────────────
            int y = margin;
            this.lblJavaPath.Text     = "Java executable:";
            this.lblJavaPath.Location = new System.Drawing.Point(col1, y + 5);
            this.lblJavaPath.Size     = new System.Drawing.Size(lw, 20);

            this.txtJavaPath.Location = new System.Drawing.Point(col2, y);
            this.txtJavaPath.Size     = new System.Drawing.Size(tw, bh);

            this.btnBrowseJava.Text     = "Browse…";
            this.btnBrowseJava.Location = new System.Drawing.Point(col3, y);
            this.btnBrowseJava.Size     = new System.Drawing.Size(bw, bh);
            this.btnBrowseJava.Click   += new System.EventHandler(this.btnBrowseJava_Click);

            // ── Row 1: MSFragger JAR ────────────────────────────────────────
            y += rowH;
            this.lblMSFraggerJar.Text     = "MSFragger JAR:";
            this.lblMSFraggerJar.Location = new System.Drawing.Point(col1, y + 5);
            this.lblMSFraggerJar.Size     = new System.Drawing.Size(lw, 20);

            this.txtMSFraggerJar.Location = new System.Drawing.Point(col2, y);
            this.txtMSFraggerJar.Size     = new System.Drawing.Size(tw, bh);

            this.btnBrowseMSFragger.Text     = "Browse…";
            this.btnBrowseMSFragger.Location = new System.Drawing.Point(col3, y);
            this.btnBrowseMSFragger.Size     = new System.Drawing.Size(bw, bh);
            this.btnBrowseMSFragger.Click   += new System.EventHandler(this.btnBrowseMSFragger_Click);

            // ── Row 2: fragger.params ───────────────────────────────────────
            y += rowH;
            this.lblFraggerParams.Text     = "fragger.params:";
            this.lblFraggerParams.Location = new System.Drawing.Point(col1, y + 5);
            this.lblFraggerParams.Size     = new System.Drawing.Size(lw, 20);

            this.txtFraggerParams.Location = new System.Drawing.Point(col2, y);
            this.txtFraggerParams.Size     = new System.Drawing.Size(tw, bh);

            this.btnBrowseParams.Text     = "Browse…";
            this.btnBrowseParams.Location = new System.Drawing.Point(col3, y);
            this.btnBrowseParams.Size     = new System.Drawing.Size(bw, bh);
            this.btnBrowseParams.Click   += new System.EventHandler(this.btnBrowseParams_Click);

            // ── Row 3: Output Directory ─────────────────────────────────────
            y += rowH;
            this.lblOutputDirectory.Text     = "Output directory:";
            this.lblOutputDirectory.Location = new System.Drawing.Point(col1, y + 5);
            this.lblOutputDirectory.Size     = new System.Drawing.Size(lw, 20);

            this.txtOutputDirectory.Location = new System.Drawing.Point(col2, y);
            this.txtOutputDirectory.Size     = new System.Drawing.Size(tw, bh);

            this.btnBrowseOutput.Text     = "Browse…";
            this.btnBrowseOutput.Location = new System.Drawing.Point(col3, y);
            this.btnBrowseOutput.Size     = new System.Drawing.Size(bw, bh);
            this.btnBrowseOutput.Click   += new System.EventHandler(this.btnBrowseOutput_Click);

            // ── Bottom buttons ──────────────────────────────────────────────
            y += rowH + 10;
            this.btnDetect.Text     = "Detect Defaults";
            this.btnDetect.Location = new System.Drawing.Point(col1, y);
            this.btnDetect.Size     = new System.Drawing.Size(130, bh);
            this.btnDetect.Click   += new System.EventHandler(this.btnDetect_Click);

            this.btnSave.Text     = "Save";
            this.btnSave.Location = new System.Drawing.Point(col3 - 100, y);
            this.btnSave.Size     = new System.Drawing.Size(90, bh);
            this.btnSave.Click   += new System.EventHandler(this.btnSave_Click);

            this.btnCancel.Text     = "Cancel";
            this.btnCancel.Location = new System.Drawing.Point(col3, y);
            this.btnCancel.Size     = new System.Drawing.Size(bw, bh);
            this.btnCancel.Click   += new System.EventHandler(this.btnCancel_Click);

            // ── Form ────────────────────────────────────────────────────────
            this.ClientSize = new System.Drawing.Size(col3 + bw + margin, y + bh + margin);
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                lblJavaPath, txtJavaPath, btnBrowseJava,
                lblMSFraggerJar, txtMSFraggerJar, btnBrowseMSFragger,
                lblFraggerParams, txtFraggerParams, btnBrowseParams,
                lblOutputDirectory, txtOutputDirectory, btnBrowseOutput,
                btnDetect, btnSave, btnCancel
            });
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox    = false;
            this.MinimizeBox    = false;
            this.StartPosition  = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text           = "QCactus — Settings";
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label   lblJavaPath;
        private System.Windows.Forms.TextBox txtJavaPath;
        private System.Windows.Forms.Button  btnBrowseJava;

        private System.Windows.Forms.Label   lblMSFraggerJar;
        private System.Windows.Forms.TextBox txtMSFraggerJar;
        private System.Windows.Forms.Button  btnBrowseMSFragger;

        private System.Windows.Forms.Label   lblFraggerParams;
        private System.Windows.Forms.TextBox txtFraggerParams;
        private System.Windows.Forms.Button  btnBrowseParams;

        private System.Windows.Forms.Label   lblOutputDirectory;
        private System.Windows.Forms.TextBox txtOutputDirectory;
        private System.Windows.Forms.Button  btnBrowseOutput;

        private System.Windows.Forms.Button  btnDetect;
        private System.Windows.Forms.Button  btnSave;
        private System.Windows.Forms.Button  btnCancel;
    }
}
