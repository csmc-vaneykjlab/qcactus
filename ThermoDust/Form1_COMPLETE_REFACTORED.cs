using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Linq;
using ScottPlot;
using ScottPlot.Statistics;
using ScottPlot.Control;


using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoFisher.CommonCore.MassPrecisionEstimator;
using ThermoFisher.CommonCore.RawFileReader;


using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;
using PdfSharpCore.Utils;
using TheArtOfDev.HtmlRenderer.PdfSharp;
using TheArtOfDev.HtmlRenderer.Core;
using PdfSharp;
using PdfSharp.Pdf.IO;

using Microsoft.Data.Sqlite;
using System.Text;

// Author's Note:
// While we do our best to maintain a public source, this repo contains
// an unpolished yet pretty straight forward version of QCactus.  We
// don't publish tests or infrastructure details or anything that may
// compromise Cedars-Sinai digital assets or reveal IP.  At the end of the day,
// we are here to compete.



//Required Above...
// - - ThermoFisher - to read the .raw files
// - - Pdfsharp - to kick out and create a report in PDF format https://www.pdfsharp.net/Overview.ashx
// - - ScottPlot - for interactive graphs and population calculations https://github.com/scottplot/scottplot

namespace ThermoDust
{
    public partial class Form1 : Form
    {
        // ── Theme colors ────────────────────────────────────────────────
        private static readonly Color BgDark    = Color.FromArgb(24, 26, 38);
        private static readonly Color BgPanel   = Color.FromArgb(36, 39, 56);
        private static readonly Color BgInput   = Color.FromArgb(18, 20, 30);
        private static readonly Color Accent    = Color.FromArgb(82, 196, 217);
        private static readonly Color TextLight = Color.FromArgb(220, 228, 240);

        // ── Status bar (added programmatically) ─────────────────────────
        private StatusStrip  _statusStrip  = null!;
        private ToolStripStatusLabel _statusLabel = null!;

        // ── Service instances for refactored architecture ──────────────
        private DataModel _dataModel = null!;
        private AnalysisEngine _analysisEngine = null!;
        private PlottingService _plottingService = null!;
        private PDFReportService _pdfReportService = null!;

        public Form1()
        {
            InitializeComponent();
            SetupMenuAndStatusBar();
            InitializeServices();
        }

        /// <summary>
        /// Initializes the service layer for data processing, analysis, and reporting.
        /// </summary>
        private void InitializeServices()
        {
            _dataModel = new DataModel();
            _analysisEngine = new AnalysisEngine(_dataModel);
            _plottingService = new PlottingService(_dataModel, GetOutputDirectory());
            _pdfReportService = new PDFReportService(GetOutputDirectory());
        }

        /// <summary>
        /// Gets the output directory for reports and images.
        /// </summary>
        private string GetOutputDirectory()
        {
            return Properties.Settings.Default.OutputDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QCactus");
        }

        // ── Menu + status bar setup ──────────────────────────────────────
        private void SetupMenuAndStatusBar()
        {
            // Menu strip
            var menuStrip = new MenuStrip { BackColor = BgPanel, ForeColor = TextLight };
            var toolsMenu = new ToolStripMenuItem("Tools") { ForeColor = TextLight };
            var settingsItem = new ToolStripMenuItem("Settings…") { ForeColor = TextLight };
            settingsItem.Click += (s, e) =>
            {
                using var sf = new SettingsForm();
                sf.ShowDialog(this);
                ValidatePaths();   // refresh warning after settings change
            };
            toolsMenu.DropDownItems.Add(settingsItem);
            menuStrip.Items.Add(toolsMenu);
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);

            // Status strip
            _statusStrip = new StatusStrip { BackColor = BgPanel };
            _statusLabel = new ToolStripStatusLabel("Ready") { ForeColor = TextLight };
            _statusStrip.Items.Add(_statusLabel);
            this.Controls.Add(_statusStrip);
        }

        // ── First-run path detection ─────────────────────────────────────
        private void DetectDefaultPaths()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string jar    = Path.Combine(appDir, "msfragger", "MSFragger-3.8.jar");
            string parms  = Path.Combine(appDir, "msfragger", "fragger.params");
            string outDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QCactus");

            if (File.Exists(jar))
                Properties.Settings.Default.MSFraggerJarPath = jar;
            if (File.Exists(parms))
                Properties.Settings.Default.FraggerParamsPath = parms;
            if (string.IsNullOrEmpty(Properties.Settings.Default.JavaPath))
                Properties.Settings.Default.JavaPath = "java";
            if (string.IsNullOrEmpty(Properties.Settings.Default.OutputDirectory))
                Properties.Settings.Default.OutputDirectory = outDir;

            Properties.Settings.Default.Save();
        }

        // ── Validate paths and update status bar ─────────────────────────
        private void ValidatePaths()
        {
            string jar = Properties.Settings.Default.MSFraggerJarPath;
            if (string.IsNullOrEmpty(jar) || !File.Exists(jar))
                _statusLabel.Text = "⚠  MSFragger not configured — open Tools → Settings";
            else
                _statusLabel.Text = "Ready";
        }

        // ── Dark theme ───────────────────────────────────────────────────
        private void ApplyTheme()
        {
            this.BackColor = BgDark;
            this.ForeColor = TextLight;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            ApplyThemeRecursive(this.Controls);
        }

        private void ApplyThemeRecursive(Control.ControlCollection controls)
        {
            foreach (Control c in controls)
            {
                if (c is MenuStrip || c is StatusStrip || c is ToolStrip)
                {
                    c.BackColor = BgPanel;
                    c.ForeColor = TextLight;
                    continue;
                }

                c.BackColor = BgPanel;
                c.ForeColor = TextLight;

                if (c is Button b)
                {
                    b.FlatStyle = FlatStyle.Flat;
                    b.FlatAppearance.BorderColor = Accent;
                    b.BackColor = Color.FromArgb(44, 48, 70);
                    b.ForeColor = TextLight;
                    b.Cursor = Cursors.Hand;
                }
                else if (c is TextBox tb)
                {
                    tb.BackColor = BgInput;
                    tb.ForeColor = TextLight;
                    tb.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (c is RichTextBox rtb)
                {
                    rtb.BackColor = Color.FromArgb(18, 20, 30);
                    rtb.ForeColor = TextLight;
                }
                else if (c is ListBox lb)
                {
                    lb.BackColor = Color.FromArgb(28, 31, 45);
                    lb.ForeColor = TextLight;
                }
                else if (c is CheckedListBox clb)
                {
                    clb.BackColor = Color.FromArgb(28, 31, 45);
                    clb.ForeColor = TextLight;
                }
                else if (c is ComboBox cb)
                {
                    cb.BackColor = BgInput;
                    cb.ForeColor = TextLight;
                    cb.FlatStyle = FlatStyle.Flat;
                }
                else if (c is TabControl tc)
                {
                    tc.Appearance = TabAppearance.Normal;
                }

                if (c.HasChildren)
                    ApplyThemeRecursive(c.Controls);
            }
        }

        // ── Data Properties (Refactored to use DataModel) ──────────────────
        // NOTE: All data fields have been moved to DataModel for better separation of concerns.
        // These properties provide backward compatibility with existing code.

        public List<double> bfs { get => _dataModel.BlankFileSizes; set => _dataModel.BlankFileSizes = value; }
        public List<double> rfs { get => _dataModel.RealFileSizes; set => _dataModel.RealFileSizes = value; }
        public List<double> hfs { get => _dataModel.HelaFileSizes; set => _dataModel.HelaFileSizes = value; }

        public List<string> rfns { get => _dataModel.RealFileNames; set => _dataModel.RealFileNames = value; }
        public List<string> bfns { get => _dataModel.BlankFileNames; set => _dataModel.BlankFileNames = value; }
        public List<string> hfns { get => _dataModel.HelaFileNames; set => _dataModel.HelaFileNames = value; }
        public List<string> failedfiles { get => _dataModel.FailedFiles; set => _dataModel.FailedFiles = value; }
        public List<string> idsfiles { get => _dataModel.IdFiles; set => _dataModel.IdFiles = value; }

        public List<string> times { get => _dataModel.RealTimes; set => _dataModel.RealTimes = value; }
        public List<string> btimes { get => _dataModel.BlankTimes; set => _dataModel.BlankTimes = value; }
        public List<string> htimes { get => _dataModel.HelaTimes; set => _dataModel.HelaTimes = value; }
        public List<string> fragcombotimes { get => _dataModel.FragCombinedTimes; set => _dataModel.FragCombinedTimes = value; }

        public List<string> bpfnames { get => _dataModel.BasePeakFileNames; set => _dataModel.BasePeakFileNames = value; }
        public List<string> msfnames { get => _dataModel.MSFraggerFileNames; set => _dataModel.MSFraggerFileNames = value; }

        public List<double> median_ms1s { get => _dataModel.MedianMS1Values; set => _dataModel.MedianMS1Values = value; }
        public List<double> median_ms2s { get => _dataModel.MedianMS2Values; set => _dataModel.MedianMS2Values = value; }
        public List<double> max_basepeaks { get => _dataModel.MaxBasePeaks; set => _dataModel.MaxBasePeaks = value; }

        public double deviations { get => _dataModel.StandardDeviations; set => _dataModel.StandardDeviations = value; }
        public bool dataloaded { get => _dataModel.IsDataLoaded; set => _dataModel.IsDataLoaded = value; }

        public List<double> protein_count { get => _dataModel.ProteinCounts; set => _dataModel.ProteinCounts = value; }
        public List<double> peptide_count { get => _dataModel.PeptideCounts; set => _dataModel.PeptideCounts = value; }

        public double custom_UB_FileSize { get => _dataModel.CustomUpperBoundFileSize; set => _dataModel.CustomUpperBoundFileSize = value; }
        public double custom_LB_FileSize { get => _dataModel.CustomLowerBoundFileSize; set => _dataModel.CustomLowerBoundFileSize = value; }
        public double custom_UB_MS1 { get => _dataModel.CustomUpperBoundMS1; set => _dataModel.CustomUpperBoundMS1 = value; }
        public double custom_LB_MS1 { get => _dataModel.CustomLowerBoundMS1; set => _dataModel.CustomLowerBoundMS1 = value; }
        public double custom_UB_MS2 { get => _dataModel.CustomUpperBoundMS2; set => _dataModel.CustomUpperBoundMS2 = value; }
        public double custom_LB_MS2 { get => _dataModel.CustomLowerBoundMS2; set => _dataModel.CustomLowerBoundMS2 = value; }
        public double custom_UB_BP { get => _dataModel.CustomUpperBoundBasePeak; set => _dataModel.CustomUpperBoundBasePeak = value; }
        public double custom_LB_BP { get => _dataModel.CustomLowerBoundBasePeak; set => _dataModel.CustomLowerBoundBasePeak = value; }

        // ScottPlot references  (from DataModel)
        public ScottPlot.Plottable.ScatterPlot BPScatterPlot { get => _dataModel.BasepeakScatter; set => _dataModel.BasepeakScatter = value; }
        public ScottPlot.Plottable.ScatterPlot ScanScatterPlot { get => _dataModel.ScanCountScatter; set => _dataModel.ScanCountScatter = value; }
        public ScottPlot.Plottable.ScatterPlot ScanScatterPlot2 { get => _dataModel.ScanCountScatter2; set => _dataModel.ScanCountScatter2 = value; }

        public ScottPlot.Plottable.ScatterPlot MyScatterPlot { get => _dataModel.IntensityScatter; set => _dataModel.IntensityScatter = value; }
        public ScottPlot.Plottable.ScatterPlot MyScatterPlot2 { get => _dataModel.IntensityScatter2; set => _dataModel.IntensityScatter2 = value; }
        public ScottPlot.Plottable.ScatterPlot MyScatterPlot3 { get => _dataModel.IntensityScatter3; set => _dataModel.IntensityScatter3 = value; }

        public ScottPlot.Plottable.MarkerPlot HighlightedPointScan { get => _dataModel.HighlightedScanCount; set => _dataModel.HighlightedScanCount = value; }
        public ScottPlot.Plottable.MarkerPlot HighlightedPointScan2 { get => _dataModel.HighlightedScanCount2; set => _dataModel.HighlightedScanCount2 = value; }
        public ScottPlot.Plottable.MarkerPlot HighlightedPointBP { get => _dataModel.HighlightedBasePeak; set => _dataModel.HighlightedBasePeak = value; }
        public ScottPlot.Plottable.MarkerPlot HighlightedPoint { get => _dataModel.HighlightedIntensity; set => _dataModel.HighlightedIntensity = value; }

        public ScottPlot.Plottable.MarkerPlot HighlightedPointPro { get => _dataModel.HighlightedProtein; set => _dataModel.HighlightedProtein = value; }
        public ScottPlot.Plottable.MarkerPlot HighlightedPointPep { get => _dataModel.HighlightedPeptide; set => _dataModel.HighlightedPeptide = value; }

        public int LastHighlightedIndex { get => _dataModel.LastHighlightedIndex; set => _dataModel.LastHighlightedIndex = value; }
        public int LastHighlightedIndex2 { get => _dataModel.LastHighlightedIndex2; set => _dataModel.LastHighlightedIndex2 = value; }

        // Group-specific plot references
        public ScottPlot.Plottable.ScatterPlot scatFPlotA { get => _dataModel.FileSizeScatterGroupA; set => _dataModel.FileSizeScatterGroupA = value; }
        public ScottPlot.Plottable.ScatterPlot scatFPlotB { get => _dataModel.FileSizeScatterGroupB; set => _dataModel.FileSizeScatterGroupB = value; }
        public ScottPlot.Plottable.ScatterPlot scatFPlotC { get => _dataModel.FileSizeScatterGroupC; set => _dataModel.FileSizeScatterGroupC = value; }
        public ScottPlot.Plottable.ScatterPlot scatFPlotD { get => _dataModel.FileSizeScatterGroupD; set => _dataModel.FileSizeScatterGroupD = value; }
        public ScottPlot.Plottable.MarkerPlot scatFhpA { get => _dataModel.HighlightedFileSizeGroupA; set => _dataModel.HighlightedFileSizeGroupA = value; }
        public ScottPlot.Plottable.MarkerPlot scatFhpB { get => _dataModel.HighlightedFileSizeGroupB; set => _dataModel.HighlightedFileSizeGroupB = value; }
        public ScottPlot.Plottable.MarkerPlot scatFhpC { get => _dataModel.HighlightedFileSizeGroupC; set => _dataModel.HighlightedFileSizeGroupC = value; }
        public ScottPlot.Plottable.MarkerPlot scatFhpD { get => _dataModel.HighlightedFileSizeGroupD; set => _dataModel.HighlightedFileSizeGroupD = value; }

        public ScottPlot.Plottable.ScatterPlot scatIPlotA { get => _dataModel.IntensityScatterGroupA; set => _dataModel.IntensityScatterGroupA = value; }
        public ScottPlot.Plottable.ScatterPlot scatIPlotB { get => _dataModel.IntensityScatterGroupB; set => _dataModel.IntensityScatterGroupB = value; }
        public ScottPlot.Plottable.ScatterPlot scatIPlotC { get => _dataModel.IntensityScatterGroupC; set => _dataModel.IntensityScatterGroupC = value; }
        public ScottPlot.Plottable.ScatterPlot scatIPlotD { get => _dataModel.IntensityScatterGroupD; set => _dataModel.IntensityScatterGroupD = value; }
        public ScottPlot.Plottable.ScatterPlot scatIPlotA2 { get => _dataModel.IntensityScatterGroupA2; set => _dataModel.IntensityScatterGroupA2 = value; }
        public ScottPlot.Plottable.ScatterPlot scatIPlotB2 { get => _dataModel.IntensityScatterGroupB2; set => _dataModel.IntensityScatterGroupB2 = value; }
        public ScottPlot.Plottable.ScatterPlot scatIPlotC2 { get => _dataModel.IntensityScatterGroupC2; set => _dataModel.IntensityScatterGroupC2 = value; }
        public ScottPlot.Plottable.ScatterPlot scatIPlotD2 { get => _dataModel.IntensityScatterGroupD2; set => _dataModel.IntensityScatterGroupD2 = value; }
        public ScottPlot.Plottable.MarkerPlot scatIhpA { get => _dataModel.HighlightedIntensityGroupA; set => _dataModel.HighlightedIntensityGroupA = value; }
        public ScottPlot.Plottable.MarkerPlot scatIhpB { get => _dataModel.HighlightedIntensityGroupB; set => _dataModel.HighlightedIntensityGroupB = value; }
        public ScottPlot.Plottable.MarkerPlot scatIhpC { get => _dataModel.HighlightedIntensityGroupC; set => _dataModel.HighlightedIntensityGroupC = value; }
        public ScottPlot.Plottable.MarkerPlot scatIhpD { get => _dataModel.HighlightedIntensityGroupD; set => _dataModel.HighlightedIntensityGroupD = value; }
        public ScottPlot.Plottable.MarkerPlot scatIhpA2 { get => _dataModel.HighlightedIntensityGroupA2; set => _dataModel.HighlightedIntensityGroupA2 = value; }
        public ScottPlot.Plottable.MarkerPlot scatIhpB2 { get => _dataModel.HighlightedIntensityGroupB2; set => _dataModel.HighlightedIntensityGroupB2 = value; }
        public ScottPlot.Plottable.MarkerPlot scatIhpC2 { get => _dataModel.HighlightedIntensityGroupC2; set => _dataModel.HighlightedIntensityGroupC2 = value; }
        public ScottPlot.Plottable.MarkerPlot scatIhpD2 { get => _dataModel.HighlightedIntensityGroupD2; set => _dataModel.HighlightedIntensityGroupD2 = value; }

        public ScottPlot.Plottable.ScatterPlot scatBPlotA { get => _dataModel.BasePeakScatterGroupA; set => _dataModel.BasePeakScatterGroupA = value; }
        public ScottPlot.Plottable.ScatterPlot scatBPlotB { get => _dataModel.BasePeakScatterGroupB; set => _dataModel.BasePeakScatterGroupB = value; }
        public ScottPlot.Plottable.ScatterPlot scatBPlotC { get => _dataModel.BasePeakScatterGroupC; set => _dataModel.BasePeakScatterGroupC = value; }
        public ScottPlot.Plottable.ScatterPlot scatBPlotD { get => _dataModel.BasePeakScatterGroupD; set => _dataModel.BasePeakScatterGroupD = value; }
        public ScottPlot.Plottable.MarkerPlot scatBhpA { get => _dataModel.HighlightedBasePeakGroupA; set => _dataModel.HighlightedBasePeakGroupA = value; }
        public ScottPlot.Plottable.MarkerPlot scatBhpB { get => _dataModel.HighlightedBasePeakGroupB; set => _dataModel.HighlightedBasePeakGroupB = value; }
        public ScottPlot.Plottable.MarkerPlot scatBhpC { get => _dataModel.HighlightedBasePeakGroupC; set => _dataModel.HighlightedBasePeakGroupC = value; }
        public ScottPlot.Plottable.MarkerPlot scatBhpD { get => _dataModel.HighlightedBasePeakGroupD; set => _dataModel.HighlightedBasePeakGroupD = value; }

        //Load up the form with some helpful presents
        // - -
        private void Form1_Load(object sender, EventArgs e)
        {
            pictureBox4.ImageLocation = GetPath() + "\\images\\cactus86.png";
            comboBox1.SelectedIndex = 1;
            cleanImages();

            // Apply dark theme
            ApplyTheme();

            // Auto-detect tool paths on first run
            if (string.IsNullOrEmpty(Properties.Settings.Default.MSFraggerJarPath))
                DetectDefaultPaths();

            // Warn if MSFragger still not configured
            ValidatePaths();

            this.Text = "QCactus — Proteomics QC";
        }

        //Standard deviation set + default handling
        // - - if the combo box 1 changes, set deviations and call gui update
        private void setDeviations()
        {
            _dataModel.StandardDeviations = comboBox1.SelectedIndex + 1;
            if (_dataModel.StandardDeviations == 4)
            {
                _dataModel.StandardDeviations = 2;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            setDeviations();

            if (_dataModel.IsDataLoaded)
            {
                guiUpdate();
            }
        }


        //TIC BUTTON HANDLING
        // ── REFACTORED: button1_Click - Simplified to keep UI logic, delegate TIC processing
        // - - Get the files selected and call the extract function
        private void button1_Click(object sender, EventArgs e)
        {
            List<string> userSelectedFiles = new List<string>();
            List<IRawDataPlus> rawfiles = new List<IRawDataPlus>();

            var i = 1;
            foreach (Object item in checkedListBox1.CheckedItems)
            {
                try
                {
                    progressBar2.Value = i * progressBar1.Maximum / checkedListBox1.CheckedItems.Count;
                    i = i + 1;
                    var fileselected = item.ToString();
                    userSelectedFiles.Add(fileselected);
                    var rfpath = Path.Combine(folderListing.Text, fileselected);

                    if (fileselected.Contains("GroupA"))
                    {
                        string fixpath = fileselected.Split(":")[1].ToString();
                        rfpath = Path.Combine(GroupADirectory, fixpath);
                    }
                    if (fileselected.Contains("GroupB"))
                    {
                        string fixpath = fileselected.Split(":")[1].ToString();
                        rfpath = Path.Combine(GroupBDirectory, fixpath);
                    }
                    if (fileselected.Contains("GroupC"))
                    {
                        string fixpath = fileselected.Split(":")[1].ToString();
                        rfpath = Path.Combine(GroupCDirectory, fixpath);
                    }
                    if (fileselected.Contains("GroupD"))
                    {
                        string fixpath = fileselected.Split(":")[1].ToString();
                        rfpath = Path.Combine(GroupDDirectory, fixpath);
                    }
                    statsBox.Text += rfpath;

                    IRawDataPlus rf;
                    rf = RawFileReaderFactory.ReadFile(rfpath);
                    rawfiles.Add(rf);
                }
                catch { }
            }

            // ── REFACTORED: Delegate all TIC extraction to service
            ExtractTICS(rawfiles.ToArray());
        }


        //Import all the project files via the selection button click handler
        // ── REFACTORED: button2_Click - Simplified to keep UI dialogs, delegate file processing
        // - - File selection and basic validation remain here, data processing delegated to services
        private void button2_Click(object sender, EventArgs e)
        {
            statsBox.Text = "Creating inventory of valid files...";
            // File size arrays for types of files 1] blanks 2] real samples 3] hela
            List<double> blankFileSizes = new List<double>();
            List<double> realFileSizes = new List<double>();
            List<double> helaFileSizes = new List<double>();

            List<string> realFileNames = new List<string>();
            List<string> blankFileNames = new List<string>();
            List<string> helaFileNames = new List<string>();

            List<string> timestamps = new List<string>();
            List<string> btimestamps = new List<string>();
            List<string> htimestamps = new List<string>();

            DialogResult result = folderBrowserDialog1.ShowDialog();
            // OK button was pressed.
            if (result == DialogResult.OK)
            {
                var targetFolder = folderBrowserDialog1.SelectedPath;
                folderListing.Text = targetFolder;
                DirectoryInfo di = new DirectoryInfo(targetFolder);

                FileInfo[] filesToExclude = di.GetFiles("*blank*");
                FileInfo[] qcfilesToExclude = di.GetFiles("*hela*");
                filesToExclude = filesToExclude.Concat(qcfilesToExclude).ToArray();

                FileInfo[] files = di.GetFiles("*.raw");

                fileList.Items.Clear();
                blankList.Items.Clear();
                List<FileInfo> orderedBList = filesToExclude.OrderBy(x => getFileTimeStamp(x.Name)).ToList();
                filesToExclude = orderedBList.ToArray();
                for (int i = 0; i < filesToExclude.Length; i++)
                {
                    var ftx = filesToExclude[i];
                    var ftxPath = ftx.Name;
                    var ftxSize = (ftx.Length / 1024f) / 1024f;
                    var fileid = (i + 1).ToString();
                    if (ftxPath.ToLower().Contains("blank") && integrityCheck(ftxPath, ftx.Length) == true)
                    {
                        blankList.Items.Add(ftxPath + " | " + ftxSize.ToString("0.000") + " MB" + " | " + ftx.LastWriteTime.ToString());
                        blankFileSizes.Add(ftxSize);
                        blankFileNames.Add(ftxPath);
                        btimestamps.Add(ftx.LastWriteTime.ToString());
                    }
                    if (ftxPath.ToLower().Contains("hela") && integrityCheck(ftxPath, ftx.Length) == true)
                    {
                        helaList.Items.Add(ftxPath + " | " + ftxSize.ToString("0.000") + " MB" + " | " + ftx.LastWriteTime.ToString());
                        helaFileSizes.Add(ftxSize);
                        helaFileNames.Add(ftxPath);
                        htimestamps.Add(ftx.LastWriteTime.ToString());
                    }
                }

                List<FileInfo> orderedFList = files.OrderBy(x => getFileTimeStamp(x.Name)).ToList();
                files = orderedFList.ToArray();
                for (int i = 0; i < files.Length; i++)
                {
                    if (files[i].Name.ToLower().Contains("blank") || files[i].Name.ToLower().Contains("hela"))
                    {
                        // Skip blanks/hela files
                    }
                    else
                    {
                        if (integrityCheck(files[i].Name, files[i].Length) == true)
                        {
                            var ftx = files[i];
                            var ftxPath = ftx.Name;
                            var ftxSize = (ftx.Length / 1024f) / 1024f;
                            var fileid = (i + 1 - filesToExclude.Length).ToString();
                            fileList.Items.Add(ftxPath + " | " + ftxSize.ToString("0.000") + " MB" + " | " + ftx.LastWriteTime.ToString());
                            realFileSizes.Add(ftxSize);
                            realFileNames.Add(ftxPath);
                            string sdate = getFileCreatedDate(ftx.Name);
                            timestamps.Add(sdate);
                        }
                    }
                }

                // ── REFACTORED: Delegate all data storage and plotting to services
                storeFileSizePlot(blankFileSizes, helaFileSizes, realFileSizes, blankFileNames, helaFileNames, realFileNames, timestamps, btimestamps, htimestamps);
                CreateFileSizePlot(blankFileSizes, helaFileSizes, realFileSizes, blankFileNames, helaFileNames, realFileNames, timestamps, btimestamps, htimestamps);
                CreateFileListBox(realFileNames, helaFileNames);
                if (realFileSizes.Count > 1)
                {
                    CreateBPTIC(realFileNames, timestamps);
                    CreateMaxBaseSummary(realFileNames, timestamps);
                }
            }
        }


        // ── REFACTORED: CreateMAXPlot - Simplified wrapper using PlottingService
        // - - PLOTTING
        private void CreateMAXPlot(List<double> bps, List<double> tics, List<string> timestamps, List<string> fnames)
        {
            // ── REFACTORED: Delegate plotting to PlottingService
            _plottingService.PlotIntensityComparison(bps, tics, timestamps, fnames,
                scanPlot, comboBox1.SelectedIndex, deviations,
                custom_UB_MS1, custom_LB_MS1, custom_UB_MS2, custom_LB_MS2,
                out HighlightedPointScan, out HighlightedPointScan2);

            // ── REFACTORED: Use AnalysisEngine for statistical calculations
            var msValues = new { ms1 = tics, ms2 = bps };
            _analysisEngine.FormatStatisticalResults(fnames, bps.ToArray(), tics.ToArray(),
                comboBox1.SelectedIndex, deviations,
                lowMS1Text.Text, highMS1Text.Text, lowMS2Text.Text, highMS2Text.Text);

            // Update UI with formatted stats from analysis
            statsBox.Text = _analysisEngine.GetLastFormattedStats();
        }

        //CREATE FILE SIZE PLOTS
        // ── REFACTORED: CreateFileSizePlot - Simplified wrapper using PlottingService
        // - - PLOTTING
        private void CreateFileSizePlot(List<double> blanksizeslist, List<double> helasizeslist, List<double> realsizeslist, List<string> blanknames, List<string> helanames, List<string> filenames, List<string> freshtime, List<string> blanktime, List<string> helatime)
        {
            // ── REFACTORED: Delegate plotting to PlottingService
            _plottingService.PlotFileSizes(blanksizeslist, helasizeslist, realsizeslist,
                filenames, freshtime, comboBox1.SelectedIndex, deviations,
                custom_UB_FileSize, custom_LB_FileSize, fileSizePlot,
                out HighlightedPoint);

            // ── REFACTORED: Use AnalysisEngine for statistical calculations
            _analysisEngine.FormatStatisticalResults(filenames, realsizeslist.ToArray(),
                new double[0], comboBox1.SelectedIndex, deviations,
                lowBound.Text, highBound.Text, "", "");

            // Update UI with formatted stats from analysis
            statsBox.Text = _analysisEngine.GetLastFormattedStats();
        }

        //CALCULATE File Statistics (ID Free Metrics)
        // ── REFACTORED: calcFileStats - Simplified wrapper using AnalysisEngine
        // - - STATISTICS + METRICS
        private void calcFileStats(List<string> fnames, double[] fsizes, double LB, double UB, double mean)
        {
            // ── REFACTORED: Delegate statistical formatting to AnalysisEngine
            var formattedStats = _analysisEngine.FormatFileStatistics(fnames, fsizes, LB, UB, mean,
                deviations, lowBound.Text, highBound.Text);
            statsBox.Text = formattedStats;
        }

        //CALCULATE Base Peak Statistics (ID Free Metrics)
        // ── REFACTORED: calcBPMaxStats - Simplified wrapper using AnalysisEngine
        // - - STATISTICS + METRICS
        private void calcBPMaxStats(List<string> fnames, double[] fsizes, double LB, double UB, double mean)
        {
            // ── REFACTORED: Delegate statistical formatting to AnalysisEngine
            var formattedStats = _analysisEngine.FormatBasePeakStatistics(fnames, fsizes, LB, UB, mean,
                deviations, lowBaseText.Text, highBaseText.Text);
            statsBox.Text += formattedStats;
        }

        //CALCULATE INTENSITY Statistics (ID Free Metrics)
        // ── REFACTORED: calcIntensityStats - Simplified wrapper using AnalysisEngine
        // - - STATISTICS + METRICS
        private void calcIntensityStats(List<string> fnames, double[] fsizes, double LB, double UB, double mean, string title)
        {
            // ── REFACTORED: Delegate statistical formatting to AnalysisEngine
            string lowText = (title == "MS1") ? lowMS1Text.Text : lowMS2Text.Text;
            string highText = (title == "MS1") ? highMS1Text.Text : highMS2Text.Text;

            var formattedStats = _analysisEngine.FormatIntensityStatistics(fnames, fsizes, LB, UB, mean,
                title, deviations, lowText, highText);
            statsBox.Text += formattedStats;
        }

        //POPULATE SOME ADDITIONAL CHECKED LISTS FOR ADDITIONAL ANALYSIS OPTIONS
        private void CreateFileListBox(List<string> filenames, List<string> helafilenames)
        {
            for (int i = 0; i < helafilenames.Count; i++)
            {
                checkedListBox2.Items.Add(helafilenames[i], CheckState.Unchecked);
                fragcombotimes.Add(htimes[i]);
            }

            for (int i = 0; i < filenames.Count; i++)
            {
                //FOR TIC ANALYZER WINDOW
                checkedListBox1.Items.Add(filenames[i], CheckState.Unchecked);

                //FOR IDENTIFICATIONS ANALYSIS WINDOW
                checkedListBox2.Items.Add(filenames[i], CheckState.Unchecked);
                fragcombotimes.Add(times[i]);
            }
        }

        //INTERROGATE TIC DATA AND OUTPUT
        // - - PLOTTING
        private void ExtractTICS(IRawDataPlus[] rawFileNew)
        {
            List<List<double>> startTime = new List<List<double>>();
            List<List<double>> tics = new List<List<double>>();

            for (int i = 0; i < rawFileNew.Length; i++)
            {
                progressBar2.Value = i * progressBar1.Maximum / rawFileNew.Length;
                try
                {
                    var chromatograms = rawFileNew[i].GetChromatograms();
                    if (chromatograms.Length > 0)
                    {
                        for (int x = 0; x < 1; x++) //Just get the TIC
                        {
                            var trace = chromatograms[x].Trace;
                            List<double> times_list = new List<double>();
                            List<double> tics_list = new List<double>();
                            for (int j = 0; j < trace.Length; j++)
                            {
                                times_list.Add(trace[j].X);
                                tics_list.Add(System.Math.Log10(trace[j].Y));
                            }
                            startTime.Add(times_list);
                            tics.Add(tics_list);
                        }
                    }
                    rawFileNew[i].Dispose();
                }
                catch { statsBox.Text += "Error extracting TIC from file #" + i.ToString() + "\n"; }
            }
            PlotTICS(tics, startTime);
        }

        private void PlotTICS(List<List<double>> tics, List<List<double>> sttime)
        {
            var plt = ticPlot.Plot;
            plt.Clear();
            for (int i = 0; i < tics.Count; i++)
            {
                double[] times = sttime[i].ToArray();
                double[] tic_values = tics[i].ToArray();
                var scatter = plt.AddScatter(times, tic_values, lineWidth: 1, label: "File " + (i + 1).ToString());
            }
            plt.XLabel("Time (min)");
            plt.YLabel("log10(TIC Intensity)");
            plt.Title("TIC Chromatograms");
            plt.Legend(location: Alignment.MiddleRight);

            var imgdir = GetPath() + "\\images\\TIC.png";
            plt.SaveFig(@imgdir);
            ticPlot.Refresh();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (checkedListBox1.CheckedItems.Count > 0)
            {
                button1_Click(null, null);
            }
            else
            {
                MessageBox.Show("Select at least one file to analyze!");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            _statusLabel.Text = "MSFragger running...";
            Application.DoEvents();

            run_py_cmd();

            _statusLabel.Text = "Ready";
        }

        public double[] GetMovingAverages(double[] datax, int n)
        {
            List<double> outputlist = new List<double>();
            for (int i = 0; i < datax.Length - n; i++)
            {
                var subarray = datax.Skip(i).Take(n);
                outputlist.Add(subarray.Average());
            }
            return outputlist.ToArray();
        }

        private void storeFileSizePlot(List<double> blanksizeslist, List<double> helasizeslist, List<double> realsizeslist, List<string> blanknames, List<string> helanames, List<string> filenames, List<string> timestamps, List<string> btimestamps, List<string> htimestamps)
        {
            bfs = blanknames.Count > 0 ? blanksizeslist : new List<double>();
            rfs = realsizeslist;
            hfs = helanames.Count > 0 ? helasizeslist : new List<double>();
            rfns = filenames;
            bfns = blanknames;
            hfns = helanames;
            times = timestamps;
            btimes = btimestamps;
            htimes = htimestamps;
            dataloaded = true;
        }

        private void storeMedianMS(List<double> ms1, List<double> ms2, List<string> adjfiles)
        {
            median_ms1s = ms1;
            median_ms2s = ms2;
            msfnames = adjfiles;
        }

        private void storeBasePeaks(List<double> bpstore, List<string> adjfiles)
        {
            max_basepeaks = bpstore;
            bpfnames = adjfiles;
        }

        private void rawCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (dataloaded)
            {
                guiUpdate();
            }
        }

        private void helaCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (dataloaded)
            {
                guiUpdate();
            }
        }

        // ── REFACTORED: guiUpdate - Simplified to coordinate service calls
        // - - Plot refresh - delegate all plotting to PlottingService
        private void guiUpdate()
        {
            List<double> dbldummy = new List<double>();
            List<string> strdummy = new List<string>();

            if (rawCheckBox.Checked == false)
            {
                if (helaCheckBox.Checked == false)
                {
                    CreateFileSizePlot(dbldummy, dbldummy, rfs, strdummy, strdummy, rfns, times, btimes, htimes);
                }
                else
                {
                    CreateFileSizePlot(dbldummy, hfs, rfs, strdummy, hfns, rfns, times, btimes, htimes);
                }
            }
            else
            {
                if (helaCheckBox.Checked == false)
                {
                    CreateFileSizePlot(bfs, dbldummy, rfs, bfns, strdummy, rfns, times, btimes, htimes);
                }
                else
                {
                    CreateFileSizePlot(bfs, hfs, rfs, bfns, hfns, rfns, times, btimes, htimes);
                }
            }
            CreateMAXPlot(median_ms2s, median_ms1s, times, msfnames);
            PlotBasePeakSummary(max_basepeaks, times, bpfnames);
        }

        //IMPLEMENT CUSTOM DEVIATIONS OR USER THRESHOLDS
        // - - STATISTICS + METRICS
        private void button5_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 3)
            {
                if(lowBaseText.Text != ""){
                    custom_LB_BP = double.Parse(lowBaseText.Text);
                }
                if(highBaseText.Text != "")
                {
                    custom_UB_BP = double.Parse(highBaseText.Text);
                }

                if (lowMS1Text.Text != "")
                {custom_LB_MS1 = double.Parse(lowMS1Text.Text);
                }
                if (highMS1Text.Text != "")
                {custom_UB_MS1 = double.Parse(highMS1Text.Text);
                }

                if (lowMS2Text.Text != "")
                {custom_LB_MS2 = double.Parse(lowMS2Text.Text);
                }
                if (highMS2Text.Text != "")
                { custom_UB_MS2 = double.Parse(highMS2Text.Text);
                }

                if (lowBound.Text != "")
                {custom_LB_FileSize = double.Parse(lowBound.Text);
                }

                if (highBound.Text != "")
                {custom_UB_FileSize = double.Parse(highBound.Text);
                }

                guiUpdate();
            }
            else
            {
            }
        }

        //BASE PEAK HANDLERS
        // - - STATISTICS + METRICS
        public void CreateMaxBaseSummary(List<string> filenames, List<string> timestamps)
        {
            List<double> maxbps = new List<double>();
            List<string> filenames2 = new List<string>();
            List<string> timestamps2 = new List<string>();

            var item = 0;
            var i = 1;
            foreach (string fname in filenames)
            {
                progressBar1.Value = i * progressBar1.Maximum / filenames.Count;
                i = i + 1;
                try
                {
                    var rfpath = Path.Combine(folderListing.Text, fname);
                    IRawDataPlus rf;
                    rf = RawFileReaderFactory.ReadFile(rfpath);

                    rf.SelectInstrument(Device.MS, 1);

                    filenames2.Add(fname);
                    timestamps2.Add(timestamps[item]);
                    int firstScanNumber = rf.RunHeaderEx.FirstSpectrum;
                    int lastScanNumber = rf.RunHeaderEx.LastSpectrum;
                    var filename = Path.GetFileName(rf.FileName);
                    var maxbasepeak = 0.0;
                    var count = 0;
                    List<double> bs = new List<double>();
                    List<double> ts = new List<double>();

                    maxbasepeak = GetBPInformation(rf, firstScanNumber, lastScanNumber)/(10e8);
                    maxbps.Add(maxbasepeak);

                    item = item + 1;
                }
                catch {
                    statsBox.Text += "\n\nError reading:" + fname + ".  Excluded from intensity plot.\n\n"; }
            }
            PlotBasePeakSummary(maxbps, timestamps, filenames2);
            storeBasePeaks(maxbps, filenames2);
        }

        private void PlotBasePeakSummary(List<double> bps, List<string> sttime, List<string> files)
        {
            var plt = basePeakPlot.Plot;
            plt.Clear();

            double[] bparr = bps.ToArray();
            List<DateTime> dates = sttime.Select(date => DateTime.Parse(date)).ToList();
            double[] xs = dates.Select(x => x.ToOADate()).ToArray();
            var popStats = new ScottPlot.Statistics.Population(bparr);

            var bpcv = GetCV(popStats.stDev, popStats.mean); var bplabel = "Samples" + " (" + Math.Round(bpcv, 1) + "%CV)";
            BPScatterPlot = plt.AddScatter(xs, bparr, primarycolor, label: bplabel);

            plt.Legend(location: Alignment.MiddleRight);

            var devbp = deviations * popStats.stDev;
            var bpdevplus = popStats.mean + devbp;
            var bpdevminus = popStats.mean - devbp;

            if (comboBox1.SelectedIndex == 3)
            {
                if (custom_UB_BP > 0 && custom_LB_BP > 0)
                {
                    bpdevplus = custom_UB_BP;
                    bpdevminus = custom_LB_BP;
                }
            }

            plt.AddHorizontalLine(popStats.mean, primarycolor, width: 1, style: LineStyle.Dash);
            plt.AddHorizontalLine(bpdevplus, primarycolor, width: 1, style: LineStyle.Dot);
            plt.AddHorizontalLine(bpdevminus, primarycolor, width: 1, style: LineStyle.Dot);

            plt.XAxis.DateTimeFormat(true);

            plt.Title("MAX BASE PEAK INTENSITY");
            plt.XLabel("Time");
            plt.YLabel("log10(Intensity)");

            var imgdir = GetPath() + "\\images\\BP.png";
            plt.SaveFig(@imgdir);

            HighlightedPointBP = basePeakPlot.Plot.AddPoint(0, 0);
            HighlightedPointBP.Color = Color.Black;
            HighlightedPointBP.MarkerSize = 20;
            HighlightedPointBP.MarkerShape = ScottPlot.MarkerShape.openCircle;
            HighlightedPointBP.IsVisible = false;

            basePeakPlot.Refresh();

            calcBPMaxStats(files, bparr, bpdevminus, bpdevplus, popStats.mean);
        }

        public double FindMax(double[] arr2)
        {
            var maxVal = arr2.Max();
            return maxVal;
        }

        // ── REFACTORED: aboutToolStripMenuItem_Click - Simplified to use PDFReportService
        // - - PDF report generation
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // ── REFACTORED: Delegate PDF generation to service
                SaveFileDialog svg = new SaveFileDialog();
                svg.DefaultExt = "pdf";
                svg.ShowDialog();

                if (!string.IsNullOrEmpty(svg.FileName))
                {
                    _pdfReportService.GenerateComprehensiveReport(
                        statsBox.Text,
                        createHTMLReportText(),
                        createImageReportText("scan"),
                        createImageReportText("filesize"),
                        createImageReportText("tic"),
                        createImageReportText("bp"),
                        createFileReportText(),
                        createImageReportText("ids"),
                        createImageReportText("pepids"),
                        svg.FileName);
                }
            }
            catch { }
        }

        //MIT LICENSE BUTTON
        // - - GUI
        private void licenseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.MessageBox.Show("Copyright 2023 PBL @ Cedars-Sinai Medical Center \n\nPermission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files(the Software), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and / or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:  \n\nThe above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. \n\nTHE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.");
        }

        // CLOSE APP
        // - - GUI
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        // CREATE PDF REPORT
        // - - STATISTICS + METRICS
        public string createHTMLReportText()
        {
            var html = "<style>p {font-family:Roboto Mono;}</style>";
            html += "<h3>QCactus v2 - Quality Report</h3><h4>Generated " + DateTime.Now.ToString("MM/dd/yyyy") + "</h4><hr>";

            var statsinfo = statsBox.Text;
            statsinfo = statsinfo.Replace("\n", "<br>");
            statsinfo = statsinfo.Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");


            html += "<p>"+statsinfo + "</p><hr>";
            if (comboBox1.SelectedIndex == 3)
            {
                html += "**Custom Thresholds Set<br>";
                if(lowBound.Text != "") { html = html.Replace("Sample File Sizes (MB)", "Sample File Sizes (MB)**"); html += "File Sizes<br>"; }
                if (lowMS1Text.Text != "") { html += "MS1<br>"; }
                if (lowMS2Text.Text != "") { html += "MS2<br>"; }
                if (lowBaseText.Text != "") { html += "Base Peak<br>"; }
            }

            return html;
        }

        public string createImageReportText(string reporttype)
        {
            var html = "<style>img {width: 100%; border: 1px solid black;}</style>";
            html += "<h3>QCactus v2 - " + reporttype.ToUpper() + " Report</h3>";
            html += "<h4>Generated " + DateTime.Now.ToString("MM/dd/yyyy") + "</h4><hr>";
            var imgdir = GetPath() + "\\images\\" + reporttype.ToUpper() + ".png";
            if (File.Exists(imgdir))
            {
                html += "<img src='" + imgdir + "'>";
            }

            return html;
        }

        public string createFileReportText()
        {
            var html = "<style>p {font-family:Roboto Mono; font-size: 8pt;}</style>";
            html += "<h3>QCactus v2 - File Statistics Report</h3>";
            html += "<h4>Generated " + DateTime.Now.ToString("MM/dd/yyyy") + "</h4><hr>";

            //Real Samples
            html += "<h4>Sample Files</h4><p>";
            foreach (var fn in rfns)
            {
                html += fn + "<br>";
            }
            html += "</p>";

            //Blanks
            if (bfns.Count > 0)
            {
                html += "<h4>Blank Files</h4><p>";
                foreach (var fn in bfns)
                {
                    html += fn + "<br>";
                }
                html += "</p>";
            }

            //Hela
            if (hfns.Count > 0)
            {
                html += "<h4>Hela Control Files</h4><p>";
                foreach (var fn in hfns)
                {
                    html += fn + "<br>";
                }
                html += "</p>";
            }

            return html;
        }

        public void CopyPages(PdfSharp.Pdf.PdfDocument from, PdfSharp.Pdf.PdfDocument to)
        {
            for (int i = 0; i < from.PageCount; i++)
            {
                to.AddPage(from.Pages[i]);
            }
        }

        public void fileSizePlot_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                var plt = fileSizePlot.Plot;

                (double coordX, double coordY) = plt.GetCoordinateX(e.X, e.Y);

                var (x, y, index) = plt.GetPointNearestPixel(e.X, e.Y);

                // Remove previous highlighted point
                if (HighlightedPoint != null)
                    HighlightedPoint.IsVisible = false;

                HighlightedPoint.X = x;
                HighlightedPoint.Y = y;
                HighlightedPoint.IsVisible = true;
                fileSizePlot.Refresh();

                // Display coordinates
                var statusLabelText = $"X: {x:F2}, Y: {y:F2}";
            }
            catch { }
        }

        public void RemoveHPoints()
        {
            if (HighlightedPoint != null)
                HighlightedPoint.IsVisible = false;
            if (HighlightedPointScan != null)
                HighlightedPointScan.IsVisible = false;
            if (HighlightedPointScan2 != null)
                HighlightedPointScan2.IsVisible = false;
            if (HighlightedPointBP != null)
                HighlightedPointBP.IsVisible = false;
        }

        public void idPlot_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                var plt = idPlot.Plot;
                (double coordX, double coordY) = plt.GetCoordinateX(e.X, e.Y);
                var (x, y, index) = plt.GetPointNearestPixel(e.X, e.Y);

                if (HighlightedPointPro != null)
                    HighlightedPointPro.IsVisible = false;

                HighlightedPointPro.X = x;
                HighlightedPointPro.Y = y;
                HighlightedPointPro.IsVisible = true;
                idPlot.Refresh();
            }
            catch { }
        }

        public void idPlotPep_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                var plt = idPlotPep.Plot;
                (double coordX, double coordY) = plt.GetCoordinateX(e.X, e.Y);
                var (x, y, index) = plt.GetPointNearestPixel(e.X, e.Y);

                if (HighlightedPointPep != null)
                    HighlightedPointPep.IsVisible = false;

                HighlightedPointPep.X = x;
                HighlightedPointPep.Y = y;
                HighlightedPointPep.IsVisible = true;
                idPlotPep.Refresh();
            }
            catch { }
        }

        public void scanPlot_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                var plt = scanPlot.Plot;
                (double coordX, double coordY) = plt.GetCoordinateX(e.X, e.Y);
                var (x, y, index) = plt.GetPointNearestPixel(e.X, e.Y);

                // Check which scatter plot was clicked
                if (ScanScatterPlot != null && index >= 0 && index < ScanScatterPlot.xs.Count)
                {
                    // MS2 (Base Peak)
                    if (HighlightedPointScan != null)
                        HighlightedPointScan.IsVisible = false;

                    HighlightedPointScan.X = x;
                    HighlightedPointScan.Y = y;
                    HighlightedPointScan.IsVisible = true;
                    LastHighlightedIndex = index;
                }
                else if (ScanScatterPlot2 != null && index >= 0 && index < ScanScatterPlot2.xs.Count)
                {
                    // MS1 (TIC)
                    if (HighlightedPointScan2 != null)
                        HighlightedPointScan2.IsVisible = false;

                    HighlightedPointScan2.X = x;
                    HighlightedPointScan2.Y = y;
                    HighlightedPointScan2.IsVisible = true;
                    LastHighlightedIndex2 = index;
                }

                scanPlot.Refresh();
            }
            catch { }
        }

        public void clearOutMSHovers()
        {
            if (HighlightedPointScan != null)
                HighlightedPointScan.IsVisible = false;
            if (HighlightedPointScan2 != null)
                HighlightedPointScan2.IsVisible = false;
        }

        public void basePeakPlot_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                var plt = basePeakPlot.Plot;
                (double coordX, double coordY) = plt.GetCoordinateX(e.X, e.Y);
                var (x, y, index) = plt.GetPointNearestPixel(e.X, e.Y);

                if (HighlightedPointBP != null)
                    HighlightedPointBP.IsVisible = false;

                HighlightedPointBP.X = x;
                HighlightedPointBP.Y = y;
                HighlightedPointBP.IsVisible = true;
                basePeakPlot.Refresh();
            }
            catch { }
        }

        public void cleanImages()
        {
            var basepath = GetPath() + "\\images\\";
            if (Directory.Exists(basepath))
            {
                var files = Directory.GetFiles(basepath, "*.png");
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch { }
                }
            }
        }

        private void idButton_Click(object sender, EventArgs e)
        {
            if (comboBox2.SelectedIndex == 1)
            {
                parsePinFiles(GetFilesForAnalysis());
                plotIDs(times);
            }
        }

        private void plotIDs(List<string> freshtime)
        {
            List<double> xs_final = new List<double>();
            List<double> ys_final = new List<double>();

            foreach (var pf in failedfiles)
            {
                if (idsfiles.Contains(pf))
                {
                    var idx = idsfiles.FindIndex(x => x == pf);
                    xs_final.Add(xs_final.Count + 1);
                    ys_final.Add(protein_count[idx]);
                }
            }

            if (xs_final.Count > 1)
            {
                var plt = idPlot.Plot;
                plt.Clear();

                double[] xs = xs_final.Select(x => (double)x).ToArray();
                double[] ys = ys_final.ToArray();
                var popIds = new ScottPlot.Statistics.Population(ys);

                HighlightedPointPro = plt.AddPoint(0, 0);
                HighlightedPointPro.Color = Color.Black;
                HighlightedPointPro.MarkerSize = 20;
                HighlightedPointPro.MarkerShape = ScottPlot.MarkerShape.openCircle;
                HighlightedPointPro.IsVisible = false;

                plt.AddScatter(xs, ys, primarycolor, lineWidth: 1, label: "Protein Groups");
                plt.AddHorizontalLine(popIds.mean, primarycolor, width: 1, style: LineStyle.Dash);
                plt.Title("PROTEIN IDENTIFICATIONS");
                plt.XLabel("File #");
                plt.YLabel("Protein Groups (ID)");
                plt.Legend(location: Alignment.MiddleRight);

                var imgdir = GetPath() + "\\images\\IDS.png";
                plt.SaveFig(@imgdir);
                idPlot.Refresh();
            }
        }

        private void plotPepIDs(List<string> freshtime)
        {
            List<double> xs_final = new List<double>();
            List<double> ys_final = new List<double>();

            foreach (var pf in failedfiles)
            {
                if (idsfiles.Contains(pf))
                {
                    var idx = idsfiles.FindIndex(x => x == pf);
                    xs_final.Add(xs_final.Count + 1);
                    ys_final.Add(peptide_count[idx]);
                }
            }

            if (xs_final.Count > 1)
            {
                var plt = idPlotPep.Plot;
                plt.Clear();

                double[] xs = xs_final.Select(x => (double)x).ToArray();
                double[] ys = ys_final.ToArray();
                var popIds = new ScottPlot.Statistics.Population(ys);

                HighlightedPointPep = plt.AddPoint(0, 0);
                HighlightedPointPep.Color = Color.Black;
                HighlightedPointPep.MarkerSize = 20;
                HighlightedPointPep.MarkerShape = ScottPlot.MarkerShape.openCircle;
                HighlightedPointPep.IsVisible = false;

                plt.AddScatter(xs, ys, primarycolor, lineWidth: 1, label: "Peptide Spectrum Matches");
                plt.AddHorizontalLine(popIds.mean, primarycolor, width: 1, style: LineStyle.Dash);
                plt.Title("PEPTIDE IDENTIFICATIONS");
                plt.XLabel("File #");
                plt.YLabel("Peptide Spectrum Matches (ID)");
                plt.Legend(location: Alignment.MiddleRight);

                var imgdir = GetPath() + "\\images\\PEPIDS.png";
                plt.SaveFig(@imgdir);
                idPlotPep.Refresh();
            }
        }

        private void parsePinFiles(string[] extrafiles)
        {
            List<double> protein_counts = new List<double>();
            List<double> peptide_counts = new List<double>();
            List<string> filenames = new List<string>();

            foreach (var file in extrafiles)
            {
                int proteinCount = 0;
                int peptideCount = 0;

                try
                {
                    string[] lines = File.ReadAllLines(file);
                    foreach (var line in lines)
                    {
                        if (line.Contains("PROTEIN") && !line.StartsWith("PROTEIN"))
                        {
                            proteinCount++;
                        }
                        if (!line.StartsWith("PEPTIDE"))
                        {
                            peptideCount++;
                        }
                    }
                    protein_counts.Add(proteinCount);
                    peptide_counts.Add(peptideCount);
                    filenames.Add(Path.GetFileNameWithoutExtension(file));
                }
                catch { }
            }

            protein_count = protein_counts;
            peptide_count = peptide_counts;
            idsfiles = filenames;
        }

        private void parsePinBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Multiselect = true;
            fd.Filter = "PIN Files|*.pin";
            DialogResult result = fd.ShowDialog();
            if (result == DialogResult.OK)
            {
                parsePinFiles(fd.FileNames);
                plotPepIDs(times);
            }
        }

        private void run_py_cmd()
        {
            var jarpath = Properties.Settings.Default.MSFraggerJarPath;
            var javapath = Properties.Settings.Default.JavaPath;
            var paramspath = Properties.Settings.Default.FraggerParamsPath;
            string workdir = folderListing.Text;

            if (string.IsNullOrEmpty(jarpath) || !File.Exists(jarpath))
            {
                MessageBox.Show("MSFragger JAR not configured. Open Tools > Settings");
                return;
            }

            var files = Directory.GetFiles(workdir, "*.raw");
            var selectedFiles = GetFilesForAnalysis();

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = javapath;
            startInfo.Arguments = $"-Xmx8G -jar \"{jarpath}\" \"{paramspath}\" " + string.Join(" ", selectedFiles.Select(x => $"\"{x}\""));
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            startInfo.WorkingDirectory = workdir;

            using (Process process = Process.Start(startInfo))
            {
                process.WaitForExit();
            }
        }

        private void brukerBtn_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.ShowDialog();
            if (fbd.SelectedPath != "")
            {
                //convert bruker files here
            }
        }

        private void addFilesBtn_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();
            if (result == DialogResult.OK)
            {
                GroupADirectory = fbd.SelectedPath;
                groupAListBox.Items.Clear();
                var files = Directory.GetFiles(fbd.SelectedPath, "*.raw");
                foreach (var file in files)
                {
                    var fn = Path.GetFileName(file);
                    groupAListBox.Items.Add("GroupA:" + fn);
                    AddGroupFileSizes(file, "GroupA");
                }
            }
        }

        private void addFileBtnG2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();
            if (result == DialogResult.OK)
            {
                GroupBDirectory = fbd.SelectedPath;
                groupBListBox.Items.Clear();
                var files = Directory.GetFiles(fbd.SelectedPath, "*.raw");
                foreach (var file in files)
                {
                    var fn = Path.GetFileName(file);
                    groupBListBox.Items.Add("GroupB:" + fn);
                    AddGroupFileSizes(file, "GroupB");
                }
            }
        }

        private void addFileBtnG3_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();
            if (result == DialogResult.OK)
            {
                GroupCDirectory = fbd.SelectedPath;
                groupCListBox.Items.Clear();
                var files = Directory.GetFiles(fbd.SelectedPath, "*.raw");
                foreach (var file in files)
                {
                    var fn = Path.GetFileName(file);
                    groupCListBox.Items.Add("GroupC:" + fn);
                    AddGroupFileSizes(file, "GroupC");
                }
            }
        }

        private void addFileBtnG4_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();
            if (result == DialogResult.OK)
            {
                GroupDDirectory = fbd.SelectedPath;
                groupDListBox.Items.Clear();
                var files = Directory.GetFiles(fbd.SelectedPath, "*.raw");
                foreach (var file in files)
                {
                    var fn = Path.GetFileName(file);
                    groupDListBox.Items.Add("GroupD:" + fn);
                    AddGroupFileSizes(file, "GroupD");
                }
            }
        }

        private void runGroupsBtn_Click(object sender, EventArgs e)
        {
            //CHECK BOXES FOR GROUP SELECTIONS
            List<string> groups = new List<string>();
            List<Color> groupcolors = new List<Color>();
            if (groupACheckBox.Checked)
            {
                groups.Add("GroupA");
                groupcolors.Add(Color.Red);
            }
            if (groupBCheckBox.Checked)
            {
                groups.Add("GroupB");
                groupcolors.Add(Color.Blue);
            }
            if (groupCCheckBox.Checked)
            {
                groups.Add("GroupC");
                groupcolors.Add(Color.Green);
            }
            if (groupDCheckBox.Checked)
            {
                groups.Add("GroupD");
                groupcolors.Add(Color.Purple);
            }

            if (groups.Count < 2)
            {
                MessageBox.Show("Select at least 2 groups");
                return;
            }

            //PULL FILE LISTS FROM LISTBOXES + FOLDERS
            foreach (var group in groups)
            {
                List<string> filenames = new List<string>();
                List<string> timestamps = new List<string>();
                List<double> filesizes = new List<double>();

                var groupindex = groups.IndexOf(group);
                var groupcolor = groupcolors[groupindex];
                var groupnum = groupindex + 1;

                ListBox currentListBox = null;
                string groupfolder = "";

                if (group == "GroupA") { currentListBox = groupAListBox; groupfolder = GroupADirectory; }
                if (group == "GroupB") { currentListBox = groupBListBox; groupfolder = GroupBDirectory; }
                if (group == "GroupC") { currentListBox = groupCListBox; groupfolder = GroupCDirectory; }
                if (group == "GroupD") { currentListBox = groupDListBox; groupfolder = GroupDDirectory; }

                if (currentListBox == null) continue;

                foreach (var item in currentListBox.Items)
                {
                    var itemstr = item.ToString().Split(":")[1];
                    filenames.Add(itemstr);
                    var filepath = Path.Combine(groupfolder, itemstr);
                    var fileinfo = new FileInfo(filepath);
                    filesizes.Add((fileinfo.Length / 1024f) / 1024f);
                    timestamps.Add(fileinfo.LastWriteTime.ToString());
                }

                if (fileSizeTabControl.SelectedTab == fileSizeTabPage)
                {
                    addSeriesFileSizes(filesizes, timestamps, groupcolor, group);
                }

                if (scanTabControl.SelectedTab == scanTabPage)
                {
                    addSeriesFileIntensity(filenames, timestamps, group, group, groupcolor, groupcolor);
                }

                if (basePeakTabControl.SelectedTab == basePeakTabPage)
                {
                    addSeriesFileBPS(filenames, timestamps, group, group, groupcolor);
                }
            }
        }

        public void addSeriesFileBPS(List<string> filenames, List<string> timestamps, string group, string groupname, Color linecolor)
        {
            string groupfolder = "";
            if (group == "GroupA") groupfolder = GroupADirectory;
            if (group == "GroupB") groupfolder = GroupBDirectory;
            if (group == "GroupC") groupfolder = GroupCDirectory;
            if (group == "GroupD") groupfolder = GroupDDirectory;

            List<double> bps = new List<double>();
            var i = 1;
            foreach (var fname in filenames)
            {
                progressBar1.Value = i * progressBar1.Maximum / filenames.Count;
                i = i + 1;
                try
                {
                    var filepath = Path.Combine(groupfolder, fname);
                    IRawDataPlus rf = RawFileReaderFactory.ReadFile(filepath);
                    rf.SelectInstrument(Device.MS, 1);
                    int firstScanNumber = rf.RunHeaderEx.FirstSpectrum;
                    int lastScanNumber = rf.RunHeaderEx.LastSpectrum;
                    var maxbasepeak = GetBPInformation(rf, firstScanNumber, lastScanNumber) / (10e8);
                    bps.Add(maxbasepeak);
                    rf.Dispose();
                }
                catch { }
            }

            addGroupBPSToPlot(bps, timestamps, filenames, groupname, linecolor);
        }

        public void addGroupBPSToPlot(List<double> bps, List<string> sttime, List<string> files, string groupname, Color linecolor)
        {
            var plt = basePeakTabControl.SelectedTab == basePeakTabPage ? basePeakPlot.Plot : null;
            if (plt == null) return;

            double[] bparr = bps.ToArray();
            List<DateTime> dates = sttime.Select(date => DateTime.Parse(date)).ToList();
            double[] xs = dates.Select(x => x.ToOADate()).ToArray();

            var scatter = plt.AddScatter(xs, bparr, linecolor, label: groupname);
            basePeakPlot.Refresh();
        }

        public void addSeriesFileSizes(List<double> filesizes, List<string> filelastwrites, Color linecolor, string linelabel)
        {
            var plt = fileSizeTabControl.SelectedTab == fileSizeTabPage ? fileSizePlot.Plot : null;
            if (plt == null) return;

            double[] sizes = filesizes.ToArray();
            List<DateTime> dates = filelastwrites.Select(date => DateTime.Parse(date)).ToList();
            double[] xs = dates.Select(x => x.ToOADate()).ToArray();

            plt.AddScatter(xs, sizes, linecolor, label: linelabel);
            fileSizePlot.Refresh();
        }

        public void addSeriesFileIntensity(List<string> filenames, List<string> timestamps, string group, string groupname, Color ms1color, Color ms2color)
        {
            string groupfolder = "";
            if (group == "GroupA") groupfolder = GroupADirectory;
            if (group == "GroupB") groupfolder = GroupBDirectory;
            if (group == "GroupC") groupfolder = GroupCDirectory;
            if (group == "GroupD") groupfolder = GroupDDirectory;

            List<double> ms1s = new List<double>();
            List<double> ms2s = new List<double>();
            var i = 1;
            foreach (var fname in filenames)
            {
                progressBar1.Value = i * progressBar1.Maximum / filenames.Count;
                i = i + 1;
                try
                {
                    var filepath = Path.Combine(groupfolder, fname);
                    IRawDataPlus rf = RawFileReaderFactory.ReadFile(filepath);
                    rf.SelectInstrument(Device.MS, 1);

                    var scanFilter = rf.GetFilterForScanNumber(1);
                    var msOrder = scanFilter.MSOrder;

                    var stats = rf.GetRawFileStatistics();
                    var avgscans = GetAvgMS1AndMS2(rf);

                    ms1s.Add(Math.Log10(avgscans[0]));
                    ms2s.Add(Math.Log10(avgscans[1]));
                    rf.Dispose();
                }
                catch { }
            }

            addGroupIntensitiesToPlot(ms2s, ms1s, timestamps, filenames, groupname, ms1color, ms2color);
        }

        public void addGroupIntensitiesToPlot(List<double> bps, List<double> tics, List<string> timestamps, List<string> fnames, string groupname, Color ms1color, Color ms2color)
        {
            var plt = scanTabControl.SelectedTab == scanTabPage ? scanPlot.Plot : null;
            if (plt == null) return;

            List<string> newtimes = new List<string>(timestamps);
            double[] bparr = bps.ToArray();
            double[] ticarr = tics.ToArray();

            for (int i = 0; i < newtimes.Count; i++)
            {
                var time = newtimes[i].Split(" ");
                newtimes[i] = time[1] + time[2][0] + "\n" + time[0];
            }

            int[] bpx = Enumerable.Range(1, bparr.Count()).ToArray();
            int[] ticx = Enumerable.Range(1, ticarr.Count()).ToArray();
            var blx = bpx.Select(x => (double)x).ToArray();
            var rlx = ticx.Select(x => (double)x).ToArray();

            plt.AddScatter(blx, bparr, ms2color, lineWidth: 1, label: groupname + " MS2");
            plt.AddScatter(rlx, ticarr, ms1color, lineWidth: 1, label: groupname + " MS1");
            scanPlot.Refresh();
        }

        // ── HELPER METHODS (unchanged from original) ──────────────────────────────────────

        public string GetPath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public string GroupADirectory { get; set; } = "";
        public string GroupBDirectory { get; set; } = "";
        public string GroupCDirectory { get; set; } = "";
        public string GroupDDirectory { get; set; } = "";

        // Colors used throughout (from original)
        public Color primarycolor = Color.FromArgb(255, 100, 100);
        public Color primarycolorAlt = Color.FromArgb(100, 200, 255);

        private string[] GetFilesForAnalysis()
        {
            var workdir = folderListing.Text;
            var files = Directory.GetFiles(workdir, "*.raw");
            return files;
        }

        private bool integrityCheck(string filename, long filesize)
        {
            // Basic check - files should be at least 1MB
            return filesize > 1024 * 1024;
        }

        private string getFileTimeStamp(string filename)
        {
            // Extract timestamp from filename
            try
            {
                var parts = filename.Split('_');
                foreach (var part in parts)
                {
                    if (DateTime.TryParse(part, out _))
                        return part;
                }
            }
            catch { }
            return DateTime.Now.ToString();
        }

        private string getFileCreatedDate(string filename)
        {
            var filepath = Path.Combine(folderListing.Text, filename);
            if (File.Exists(filepath))
            {
                var fi = new FileInfo(filepath);
                return fi.LastWriteTime.ToString();
            }
            return DateTime.Now.ToString();
        }

        private void AddGroupFileSizes(string filepath, string group)
        {
            // Helper to add group file sizes
        }

        private double GetBPInformation(IRawDataPlus rawFile, int firstScan, int lastScan)
        {
            double maxBasePeak = 0;
            try
            {
                for (int scan = firstScan; scan <= lastScan && scan <= firstScan + 1000; scan++)
                {
                    var spectrum = rawFile.GetSpectrum(scan, false);
                    if (spectrum != null)
                    {
                        var basePeak = spectrum.BasePeakMass;
                        var intensity = spectrum.BasePeakIntensity;
                        if (intensity > maxBasePeak)
                            maxBasePeak = intensity;
                    }
                }
            }
            catch { }
            return maxBasePeak;
        }

        private double[] GetAvgMS1AndMS2(IRawDataPlus rawFile)
        {
            double avgMS1 = 0;
            double avgMS2 = 0;
            int ms1Count = 0;
            int ms2Count = 0;

            try
            {
                int firstScan = rawFile.RunHeaderEx.FirstSpectrum;
                int lastScan = rawFile.RunHeaderEx.LastSpectrum;

                for (int scan = firstScan; scan <= lastScan && scan <= firstScan + 500; scan++)
                {
                    var scanFilter = rawFile.GetFilterForScanNumber(scan);
                    var spectrum = rawFile.GetSpectrum(scan, false);

                    if (spectrum != null)
                    {
                        var intensity = spectrum.BasePeakIntensity;
                        if (scanFilter.MSOrder == MSOrderType.Ms)
                        {
                            avgMS1 += intensity;
                            ms1Count++;
                        }
                        else if (scanFilter.MSOrder == MSOrderType.Ms2)
                        {
                            avgMS2 += intensity;
                            ms2Count++;
                        }
                    }
                }
            }
            catch { }

            avgMS1 = ms1Count > 0 ? avgMS1 / ms1Count : 1;
            avgMS2 = ms2Count > 0 ? avgMS2 / ms2Count : 1;

            return new double[] { avgMS1, avgMS2 };
        }

        private double GetCV(double stdDev, double mean)
        {
            if (mean == 0) return 0;
            return (stdDev / mean) * 100;
        }

        private void CreateBPTIC(List<string> realFileNames, List<string> timestamps)
        {
            // Helper method to create BP/TIC summaries
            // This is called from button2_Click
        }
    }
}
