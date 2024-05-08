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
        public Form1()
        {
            InitializeComponent();
            
        }

        //file sizes
        // - - globals for refreshing charts / updating statistics
        // - - holding the file sizes
        public List<double> bfs = new List<double>();
        public List<double> rfs = new List<double>();
        public List<double> hfs = new List<double>();

        //file names
        // - - globals for file names
        // - - 
        public List<string> rfns = new List<string>();
        public List<string> bfns = new List<string>();
        public List<string> hfns = new List<string>();
        public List<string> failedfiles = new List<string>();
        public List<string> idsfiles = new List<string>();

        //time stamps
        // - - globals for time stamps
        // - - 
        public List<string> times = new List<string>();
        public List<string> btimes = new List<string>();
        public List<string> htimes = new List<string>();
        public List<string> fragcombotimes = new List<string>();

        public List<string> bpfnames = new List<string>();
        public List<string> msfnames = new List<string>();

        //quantitative values
        // - - globals for quant values
        // - - 
        public List<double> median_ms1s = new List<double>();
        public List<double> median_ms2s = new List<double>();
        public List<double> max_basepeaks = new List<double>();

        //setting for standard deviations for thresholds
        // - - global
        public double deviations = 2;

        //setting if data has been loaded into app
        // - - global
        public bool dataloaded = false;

        // - - global
        public List<double> protein_count = new List<double>();
        public List<double> peptide_count = new List<double>();

        //custom thresholds for charts, set by user
        // - - global
        public double custom_UB_FileSize = 0;
        public double custom_LB_FileSize = 0;
        public double custom_UB_MS1 = 0;
        public double custom_LB_MS1 = 0;
        public double custom_UB_MS2 = 0;
        public double custom_LB_MS2 = 0;
        public double custom_UB_BP = 0;
        public double custom_LB_BP = 0;

        //a bunch of objects to help workaround some hovering on points
        // - - the mouse hover works but more difficult to work with and should be rewritten
        // - - TODO: consolidate scottplot objects if possible
        // - - NOTE: Trickier to get closest point highlighted when more than one series
        public ScottPlot.Plottable.ScatterPlot BPScatterPlot = new ScottPlot.Plottable.ScatterPlot(null, null);
        public ScottPlot.Plottable.ScatterPlot ScanScatterPlot = new ScottPlot.Plottable.ScatterPlot(null, null);
        public ScottPlot.Plottable.ScatterPlot ScanScatterPlot2 = new ScottPlot.Plottable.ScatterPlot(null, null);

        public ScottPlot.Plottable.ScatterPlot MyScatterPlot = new ScottPlot.Plottable.ScatterPlot(null,null);
        public ScottPlot.Plottable.ScatterPlot MyScatterPlot2 = new ScottPlot.Plottable.ScatterPlot(null, null);
        public ScottPlot.Plottable.ScatterPlot MyScatterPlot3 = new ScottPlot.Plottable.ScatterPlot(null, null);


        public ScottPlot.Plottable.MarkerPlot HighlightedPointScan = new ScottPlot.Plottable.MarkerPlot();
        public ScottPlot.Plottable.MarkerPlot HighlightedPointScan2 = new ScottPlot.Plottable.MarkerPlot();
        public ScottPlot.Plottable.MarkerPlot HighlightedPointBP = new ScottPlot.Plottable.MarkerPlot();
        public ScottPlot.Plottable.MarkerPlot HighlightedPoint = new ScottPlot.Plottable.MarkerPlot();

        public ScottPlot.Plottable.MarkerPlot HighlightedPointPro = new ScottPlot.Plottable.MarkerPlot();
        public ScottPlot.Plottable.MarkerPlot HighlightedPointPep = new ScottPlot.Plottable.MarkerPlot();

        public int LastHighlightedIndex = -1;
        public int LastHighlightedIndex2 = -1;

        //helpers and placeholders for the odd hover bugs in scottplot - I could reduce this but get it working first
        public ScottPlot.Plottable.ScatterPlot scatFPlotA = new ScottPlot.Plottable.ScatterPlot(null, null);
        public ScottPlot.Plottable.ScatterPlot scatFPlotB = new ScottPlot.Plottable.ScatterPlot(null, null);
        public ScottPlot.Plottable.ScatterPlot scatFPlotC = new ScottPlot.Plottable.ScatterPlot(null, null);
        public ScottPlot.Plottable.ScatterPlot scatFPlotD = new ScottPlot.Plottable.ScatterPlot(null, null);
        public ScottPlot.Plottable.MarkerPlot scatFhpA = new ScottPlot.Plottable.MarkerPlot();
        public ScottPlot.Plottable.MarkerPlot scatFhpB = new ScottPlot.Plottable.MarkerPlot();
        public ScottPlot.Plottable.MarkerPlot scatFhpC = new ScottPlot.Plottable.MarkerPlot();
        public ScottPlot.Plottable.MarkerPlot scatFhpD = new ScottPlot.Plottable.MarkerPlot();

        public ScottPlot.Plottable.ScatterPlot scatIPlotA = new ScottPlot.Plottable.ScatterPlot(null, null);
        public ScottPlot.Plottable.ScatterPlot scatIPlotB = new ScottPlot.Plottable.ScatterPlot(null, null);
        public ScottPlot.Plottable.ScatterPlot scatIPlotC = new ScottPlot.Plottable.ScatterPlot(null, null);
        public ScottPlot.Plottable.ScatterPlot scatIPlotD = new ScottPlot.Plottable.ScatterPlot(null, null);
        public ScottPlot.Plottable.ScatterPlot scatIPlotA2 = new ScottPlot.Plottable.ScatterPlot(null, null);
        public ScottPlot.Plottable.ScatterPlot scatIPlotB2 = new ScottPlot.Plottable.ScatterPlot(null, null);
        public ScottPlot.Plottable.ScatterPlot scatIPlotC2 = new ScottPlot.Plottable.ScatterPlot(null, null);
        public ScottPlot.Plottable.ScatterPlot scatIPlotD2 = new ScottPlot.Plottable.ScatterPlot(null, null);
        public ScottPlot.Plottable.MarkerPlot scatIhpA = new ScottPlot.Plottable.MarkerPlot();
        public ScottPlot.Plottable.MarkerPlot scatIhpB = new ScottPlot.Plottable.MarkerPlot();
        public ScottPlot.Plottable.MarkerPlot scatIhpC = new ScottPlot.Plottable.MarkerPlot();
        public ScottPlot.Plottable.MarkerPlot scatIhpD = new ScottPlot.Plottable.MarkerPlot();
        public ScottPlot.Plottable.MarkerPlot scatIhpA2 = new ScottPlot.Plottable.MarkerPlot();
        public ScottPlot.Plottable.MarkerPlot scatIhpB2 = new ScottPlot.Plottable.MarkerPlot();
        public ScottPlot.Plottable.MarkerPlot scatIhpC2 = new ScottPlot.Plottable.MarkerPlot();
        public ScottPlot.Plottable.MarkerPlot scatIhpD2 = new ScottPlot.Plottable.MarkerPlot();

        public ScottPlot.Plottable.ScatterPlot scatBPlotA = new ScottPlot.Plottable.ScatterPlot(null, null);
        public ScottPlot.Plottable.ScatterPlot scatBPlotB = new ScottPlot.Plottable.ScatterPlot(null, null);
        public ScottPlot.Plottable.ScatterPlot scatBPlotC = new ScottPlot.Plottable.ScatterPlot(null, null);
        public ScottPlot.Plottable.ScatterPlot scatBPlotD = new ScottPlot.Plottable.ScatterPlot(null, null);
        public ScottPlot.Plottable.MarkerPlot scatBhpA = new ScottPlot.Plottable.MarkerPlot();
        public ScottPlot.Plottable.MarkerPlot scatBhpB = new ScottPlot.Plottable.MarkerPlot();
        public ScottPlot.Plottable.MarkerPlot scatBhpC = new ScottPlot.Plottable.MarkerPlot();
        public ScottPlot.Plottable.MarkerPlot scatBhpD = new ScottPlot.Plottable.MarkerPlot();

        //Load up the form with some helpful presents
        // - - 
        private void Form1_Load(object sender, EventArgs e)
        {
            pictureBox4.ImageLocation = GetPath() + "\\images\\cactus86.png";
            comboBox1.SelectedIndex = 1;
            cleanImages();
        }

        //Standard deviation set + default handling
        // - - if the combo box 1 changes, set deviations and call gui update
        private void setDeviations()
        {   
            deviations = comboBox1.SelectedIndex+1;
            if (deviations == 4)
            {
                deviations = 2;
            }
            
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            
                setDeviations();
            
            
            if (dataloaded == true)
            {
                guiUpdate();
            }

        }


        //TIC BUTTON HANDLING
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



            ExtractTICS(rawfiles.ToArray());
            

        }


        //Import all the project files via the selectio button click hanlder
        // - - probably should separate some of this out - not really best coding practice
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
                        
                    }
                    else
                    {
                        if (integrityCheck(files[i].Name, files[i].Length) == true)
                        {   
                            var ftx = files[i];
                            var ftxPath = ftx.Name;
                            var ftxSize = (ftx.Length / 1024f) / 1024f;
                            //fileList.Items.Add(ftxPath);
                            var fileid = (i + 1 - filesToExclude.Length).ToString();
                            fileList.Items.Add(ftxPath + " | " + ftxSize.ToString("0.000") + " MB" + " | " + ftx.LastWriteTime.ToString());
                            realFileSizes.Add(ftxSize);
                            realFileNames.Add(ftxPath);
                            string sdate = getFileCreatedDate(ftx.Name);
                            timestamps.Add(sdate);
                            //timestamps.Add(ftx.LastWriteTime.ToString());
                            
                        }
                    }
                }

                //CHECK IF SAMPLES ARE INCLUDED, SKIP AND NOTIFY USER IF ONLY BLANKS/HELA


                storeFileSizePlot(blankFileSizes, helaFileSizes, realFileSizes, blankFileNames, helaFileNames, realFileNames, timestamps, btimestamps,htimestamps);

                CreateFileSizePlot(blankFileSizes, helaFileSizes, realFileSizes, blankFileNames, helaFileNames, realFileNames, timestamps, btimestamps, htimestamps);
                CreateFileListBox(realFileNames, helaFileNames);
                if (realFileSizes.Count > 1)
                {
                    CreateBPTIC(realFileNames, timestamps);
                    CreateMaxBaseSummary(realFileNames, timestamps);
                }
                
                dataloaded = true;
            }
            // Cancel button was pressed.
            else if (result == DialogResult.Cancel)
            {
                return;
            }
        }

        //CREATE MEDIAN INTENSITY PLOTS
        // - - PLOTTING
        public void CreateBPTIC(List<string> filenames, List<string> timestamps)
        {
            List<double> maxbps = new List<double>();
            List<double> maxtics = new List<double>();
            List<string> filenames2 = new List<string>();
            
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
                    int firstScanNumber = rf.RunHeaderEx.FirstSpectrum;
                int lastScanNumber = rf.RunHeaderEx.LastSpectrum;
                var filename = Path.GetFileName(rf.FileName);
                var maxbasepeak = 0.0;
                var maxtic = 0.0;
                var count = 0;
                List<double> bs = new List<double>();
                List<double> ts = new List<double>();

                    //TESTING NEW FUNCTION HERE
                    //GetBPInformation(rf, firstScanNumber, lastScanNumber);
                    

                    foreach (var scanNumber in Enumerable
                                   .Range(1, lastScanNumber - firstScanNumber))
                    {
                        var scanStatistics = rf.GetScanStatsForScanNumber(scanNumber);
                        var scanFilter = rf.GetFilterForScanNumber(scanNumber);
                        if (scanFilter.MSOrder == MSOrderType.Ms)
                        {
                            count = count + 1;


                            double newTic = Math.Log10(scanStatistics.TIC); //closest
                            

                            ts.Add(newTic);


                        }
                        if (scanFilter.MSOrder == MSOrderType.Ms2)
                        {
                            double newTic = Math.Log10(scanStatistics.TIC);
                            bs.Add(newTic);
                        }
                           

                        }
                    maxbasepeak = GetMedian(bs.ToArray()); //ACTUALLY MS2
                    
                    maxtic = GetMedian(ts.ToArray()); //ACTUALLY MS1

                    maxbps.Add(maxbasepeak);
                    maxtics.Add(maxtic);
                    item = item + 1;
                }
                catch { timestamps.RemoveAt(item); statsBox.Text += "\nError reading:" + fname +".  Excluded from intensity plot.\n"; }

            }
            CreateMAXPlot(maxbps, maxtics, timestamps,filenames);
            storeMedianMS(maxtics,maxbps,filenames);

        }

        public double GetBPInformation(IRawDataPlus rwfile, int startScan, int endScan)
        {
            ChromatogramTraceSettings settings = new ChromatogramTraceSettings(TraceType.BasePeak);
            var d = rwfile.GetChromatogramData(new IChromatogramSettings[] { settings }, startScan, endScan);
            var trace = ChromatogramSignal.FromChromatogramData(d);
            double bpmax = 0.0;
            if (trace[0].Length > 0)
            {
                
                for(int i = 0; i < trace[0].Length; i++)
                {   
                    if (trace[0].Intensities[i] > bpmax)
                    {
                        bpmax = trace[0].Intensities[i];
                    }
                }
            }
            //label20.Text = bpmax.ToString();
            return bpmax;
        }

        public double GetIntensityInformation(IRawDataPlus rwfile, int startScan, int endScan)
        {
            double avgIntensity = 0.0;

            return avgIntensity;
        }

        //MEDIAN CALC
        // - - FOR PLOTTING
        public static double GetMedian(double[] sourceNumbers)
        {
                   
            if (sourceNumbers == null || sourceNumbers.Length == 0)
                throw new System.Exception("Median of empty array not defined.");

            
            double[] sortedPNumbers = (double[])sourceNumbers.Clone();
            Array.Sort(sortedPNumbers);

           
            int size = sortedPNumbers.Length;
            int mid = size / 2;
            double median = (size % 2 != 0) ? (double)sortedPNumbers[mid] : ((double)sortedPNumbers[mid] + (double)sortedPNumbers[mid - 1]) / 2;
            return median;
        }

        //CREATE INTENSITY PLOTS
        // - - PLOTTING
        private void CreateMAXPlot(List<double> bps, List<double> tics, List<string> timestamps, List<string> fnames)
        {
            List<string> newtimes = new List<string>(timestamps);

            
            var plt = scanPlot.Plot;
            plt.Clear();
            double[] bparr = bps.ToArray();
            double[] ticarr = tics.ToArray();
            for (int i = 0; i < newtimes.Count; i++)
            {
                var time = newtimes[i].Split(" ");
                
                newtimes[i]=time[1] + time[2][0] + "\n" + time[0];
            }
            int[] bpx = Enumerable.Range(1, bparr.Count()).ToArray();
            int[] ticx = Enumerable.Range(1, ticarr.Count()).ToArray();
            var blx = bpx.Select(x => (double)x).ToArray();
            var rlx = ticx.Select(x => (double)x).ToArray();
           
            var popBlank = new ScottPlot.Statistics.Population(bparr);
            var popReal = new ScottPlot.Statistics.Population(ticarr);
            
            plt.Title("Median Intensity");
            plt.XLabel("Time");
            plt.YLabel("log10(Intensity)");
            plt.Legend(location: Alignment.MiddleRight);
            var lightred = System.Drawing.ColorTranslator.FromHtml("#ffcccb");
            var lightblue = System.Drawing.ColorTranslator.FromHtml("#ADD8E6");

            var ms1cv = GetCV(popReal.stDev, popReal.mean); var ms1label = "MS1" + " (" + Math.Round(ms1cv, 1) + "%CV)";
            var ms2cv = GetCV(popBlank.stDev, popBlank.mean); var ms2label = "MS2" + " (" + Math.Round(ms2cv, 1) + "%CV)";

            ScanScatterPlot = plt.AddScatter(blx, bparr, primarycolor, lineWidth: 1, label: ms2label);
            ScanScatterPlot2 = plt.AddScatter(rlx, ticarr, primarycolorAlt, lineWidth: 1, label: ms1label);

            var devs1 = deviations * popBlank.stDev;
            var rdevplus = popBlank.mean + devs1;
            var rdevminus = popBlank.mean - devs1;

            if (comboBox1.SelectedIndex == 3)
            {
                if (custom_UB_MS2 > 0 && custom_LB_MS2 > 0)
                {
                    rdevplus = custom_UB_MS2;
                    rdevminus = custom_LB_MS2;

                }
            }

            plt.AddHorizontalLine(popBlank.mean, primarycolor, width: 1, style: LineStyle.Dash);
            plt.AddHorizontalLine(rdevplus, primarycolor, width: 1, style: LineStyle.Dot);
            plt.AddHorizontalLine(rdevminus, primarycolor, width: 1, style: LineStyle.Dot);

            var devs2 = deviations * popReal.stDev;
            var rdevplus2 = popReal.mean + devs2;
            var rdevminus2 = popReal.mean - devs2;
            if (comboBox1.SelectedIndex == 3)
            {
                if (custom_UB_MS1 > 0 && custom_LB_MS1 > 0)
                {
                    rdevplus2 = custom_UB_MS1;
                    rdevminus2 = custom_LB_MS1;

                }
            }

            plt.AddHorizontalLine(rdevplus2, primarycolorAlt, width: 1, style: LineStyle.Dot);
            plt.AddHorizontalLine(rdevminus2, primarycolorAlt, width: 1, style: LineStyle.Dot);


            plt.AddHorizontalLine(popReal.mean, primarycolorAlt, width: 1, style: LineStyle.Dash);

            double[] xPositions = blx;
            string[] xLabels = newtimes.ToArray();
            for (int i = 0; i < xLabels.Count(); i++)
            {
                if (i == 0 || i == xLabels.Count() - 1)
                {
                    
                }
                else
                {
                    xLabels[i] = "";
                }
            }
            plt.XAxis.ManualTickPositions(xPositions, xLabels);
            plt.XAxis.TickDensity(0.1);

            var imgdir = GetPath() + "\\images\\MEANS.png";
            plt.SaveFig(@imgdir);

            HighlightedPointScan = scanPlot.Plot.AddPoint(0, 0);
            HighlightedPointScan.Color = Color.Black;
            HighlightedPointScan.MarkerSize = 20;
            HighlightedPointScan.MarkerShape = ScottPlot.MarkerShape.openCircle;
            HighlightedPointScan.IsVisible = false;

            HighlightedPointScan2 = scanPlot.Plot.AddPoint(0, 0);
            HighlightedPointScan2.Color = Color.Black;
            HighlightedPointScan2.MarkerSize = 20;
            HighlightedPointScan2.MarkerShape = ScottPlot.MarkerShape.openCircle;
            HighlightedPointScan2.IsVisible = false;


            scanPlot.Refresh();
            calcIntensityStats(fnames, ticarr, rdevminus2, rdevplus2, popReal.mean,"MS1");
            calcIntensityStats(fnames, bparr, rdevminus, rdevplus, popBlank.mean,"MS2");
            
            
        }

        //CREATE FILE SIZE PLOTS
        // - - PLOTTING
        private void CreateFileSizePlot(List<double> blanksizeslist, List<double> helasizeslist, List<double> realsizeslist, List<string> blanknames, List<string> helanames, List<string> filenames, List<string> freshtime, List<string> blanktime, List<string> helatime)
        {
           
            var plt = fileSizePlot.Plot;
            plt.Clear();
            List<string> newtimes = new List<string>(freshtime);
            double[] realfiles = realsizeslist.ToArray();
            int[] realx = Enumerable.Range(1, realfiles.Count()).ToArray();
            var rlx = realx.Select(x => (double)x).ToArray();

            if (realsizeslist.Count > 1)
            {
                var popReal = new ScottPlot.Statistics.Population(realfiles);


                var rdev = deviations * popReal.stDev;
                var rdevplus = popReal.mean + rdev;
                var rdevminus = popReal.mean - rdev;

                if (comboBox1.SelectedIndex == 3)
                {
                    if (custom_UB_FileSize > 0 && custom_LB_FileSize > 0)
                    {
                        rdevplus = custom_UB_FileSize;
                        rdevminus = custom_LB_FileSize;
                    }
                }




                List<DateTime> dates = freshtime.Select(date => DateTime.Parse(date)).ToList();
                double[] xs = dates.Select(x => x.ToOADate()).ToArray();
                //var rcv = GetCV(popReal.stDev, popReal.mean); var rlabel = "Samples" + " (" + Math.Round(rcv,1) + "%CV)";
                MyScatterPlot = plt.AddScatter(xs, realfiles, primarycolor, label: "Samples");

                plt.AddHorizontalLine(popReal.mean, primarycolor, width: 1, style: LineStyle.Dash);
                plt.AddHorizontalLine(rdevminus, primarycolor, width: 1, style: LineStyle.Dot);
                plt.AddHorizontalLine(rdevplus, primarycolor, width: 1, style: LineStyle.Dot);

                calcFileStats(filenames, realfiles, rdevminus, rdevplus, popReal.mean);
            }

            if (blanksizeslist.Count > 1)
                       {
                           double[] blankfiles = blanksizeslist.ToArray();
                            int[] blankx = Enumerable.Range(1, blankfiles.Count()).ToArray();
                
                List<DateTime> bdates = blanktime.Select(date => DateTime.Parse(date)).ToList();
                double[] blx = bdates.Select(x => x.ToOADate()).ToArray();
                
                var popBlank = new ScottPlot.Statistics.Population(blankfiles);

                            plt.AddScatter(blx, blankfiles, Color.Blue, label: "Blank");
                            plt.AddHorizontalLine(popBlank.mean, Color.Blue, width: 1, style: LineStyle.Dash);
                        }

                        if (helasizeslist.Count > 1)
                        {
                            double[] helafiles = helasizeslist.ToArray();
                            int[] helax = Enumerable.Range(1, helafiles.Count()).ToArray();
                
                List<DateTime> hdates = helatime.Select(date => DateTime.Parse(date)).ToList();
                double[] hlx = hdates.Select(x => x.ToOADate()).ToArray();
                var popHela = new ScottPlot.Statistics.Population(helafiles);
                            plt.AddScatter(hlx, helafiles, Color.Green, label: "Hela");
                            plt.AddHorizontalLine(popHela.mean, Color.Green, width: 1, style: LineStyle.Dash);
                        }

                        
            plt.XAxis.TickDensity(0.75);
            
            plt.XAxis.DateTimeFormat(true);
            
            plt.Title("FILE SIZES");
            plt.XLabel("Time");
            plt.YLabel("Size (MB)");
            plt.Legend(location: Alignment.MiddleRight);
            

            
            var imgdir = GetPath() + "\\images\\ALLFILESIZES.png";
            plt.SaveFig(@imgdir);

            
            HighlightedPoint = fileSizePlot.Plot.AddPoint(0, 0);
            HighlightedPoint.Color = Color.Black;
            HighlightedPoint.MarkerSize = 20;
            HighlightedPoint.MarkerShape = ScottPlot.MarkerShape.openCircle;
            HighlightedPoint.IsVisible = false;
            fileSizePlot.Refresh();

            
        }

        //CALCULATE File Statistics (ID Free Metrics)
        // - - STATISTICS + METRICS
        private void calcFileStats(List<string> fnames, double[] fsizes, double LB, double UB, double mean)
        {
            statsBox.Text = "Sample File Sizes (MB) \n";
          
            if (lowBound.Text != "")
            {
                statsBox.Text += "UB\t\tMean\t\tLB\n";
            }
            else
            {
                statsBox.Text += "-" + deviations + "SD\t\tMean\t\t+" + deviations + "SD\n";
            }

            statsBox.Text += LB.ToString("F") + "\t\t" + mean.ToString("F") + "\t\t" + UB.ToString("F") + "\n";
            statsBox.Text += " \n";
            statsBox.Text += "QC Warning: \n";
            
            for (int i = 0; i < fnames.Count; i++)
            {
                if (fsizes[i] < LB || fsizes[i] > UB)
                {
                    statsBox.Text += fnames[i] + "\n";
                }
            }
        }


        //CALCULATE Base Peak Statistics (ID Free Metrics)
        // - - STATISTICS + METRICS
        private void calcBPMaxStats(List<string> fnames, double[] fsizes, double LB, double UB, double mean)
        {
            statsBox.Text += "\n";
            statsBox.Text += "Base Peak (Max) \n";

            if (lowBaseText.Text != "")
            {
                statsBox.Text += "UB\t\tMean\t\tLB\n";
            }
            else { statsBox.Text += "-" + deviations + "SD\t\tMean\t\t+" + deviations + "SD\n";
            }
            
            statsBox.Text += LB.ToString("F") + "\t\t" + mean.ToString("F") + "\t\t" + UB.ToString("F") + "\n";
            statsBox.Text += " \n";
            statsBox.Text += "QC Warning: \n";
            try
            {
                for (int i = 0; i < fnames.Count; i++)
                {
                    if (fsizes[i] < LB || fsizes[i] > UB)
                    {

                        statsBox.Text += fnames[i] + "\n";
                    }
                }
            }
            catch
            {
                statsBox.Text += fnames.Count.ToString();
                statsBox.Text += fsizes.Length.ToString();
            }
        }

        //CALCULATE INTENSITY Statistics (ID Free Metrics)
        // - - STATISTICS + METRICS
        private void calcIntensityStats(List<string> fnames, double[] fsizes, double LB, double UB, double mean, string title)
        {
            statsBox.Text += "\n";
            statsBox.Text += title + " log10(Intensities) " + "\n";

            if (title == "MS1")
            {
                if (lowMS1Text.Text != "")
                {
                    statsBox.Text += "UB\t\tMean\t\tLB\n";
                }
                else
                {
                    statsBox.Text += "-" + deviations + "SD\t\tMean\t\t+" + deviations + "SD\n";
                }
                
            }

            if (title == "MS2")
            {
                if (lowMS2Text.Text != "")
                {
                    statsBox.Text += "UB\t\tMean\t\tLB\n";
                }
                else
                {
                    statsBox.Text += "-" + deviations + "SD\t\tMean\t\t+" + deviations + "SD\n";
                }
                
            }


            statsBox.Text += LB.ToString("F") +"\t\t"+ mean.ToString("F") + "\t\t" + UB.ToString("F") + "\n";


            statsBox.Text += " \n";
            statsBox.Text += "QC Warning: \n";
            try
            {
                for (int i = 0; i < fnames.Count; i++)
                {
                    if (fsizes[i] < LB || fsizes[i] > UB)
                    {
                        statsBox.Text += fnames[i] + "\n";
                    }
                }
            }
            catch { }
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
                List<double> starts = new List<double>();
                List<double> tic = new List<double>();
                IRawDataPlus rawFilex = rawFileNew[i];
              
                rawFilex.SelectInstrument(Device.MS, 1);

                int firstScanNumber = rawFilex.RunHeaderEx.FirstSpectrum;
                int lastScanNumber = rawFilex.RunHeaderEx.LastSpectrum;
                var filename = Path.GetFileName(rawFilex.FileName);
                foreach (var scanNumber in Enumerable
                            .Range(1, lastScanNumber - firstScanNumber))
                {
                    var scanFilter = rawFilex.GetFilterForScanNumber(scanNumber);
                    if (scanFilter.MSOrder == MSOrderType.Ms)
                    {
                        var scanStatistics = rawFilex.GetScanStatsForScanNumber(scanNumber);
                        starts.Add(scanStatistics.StartTime);
                        double newTic = scanStatistics.TIC / (Math.Pow(10, 9));
                        tic.Add(newTic);
                    }

                }
               
                tics.Add(tic);
                startTime.Add(starts);

            }

            PlotTICS(tics, startTime);
        }
        private void PlotTICS(List<List<double>> tics, List<List<double>> sttime)
        {

            var plt = TICPlot.Plot;
            plt.Clear();
            plt.Title("TIC");
            plt.XLabel("Retention Time");
            plt.YLabel("TIC (10^9)");


            for (int i = 0; i < tics.Count; i++)
            {
                double[] ytics = tics[i].ToArray();
                double[] xrts = sttime[i].ToArray();


                plt.AddScatterLines(xrts, ytics);
            }


            plt.XAxis.Ticks(true);

            var imgdir = GetPath() + "\\images\\ALLTICS.png";
            plt.SaveFig(@imgdir);

            TICPlot.Refresh();


        }

        //BUTTON HANDLERS FOR SELECT ALL OR DESELECT ALL CHECK BOXES
        // - - GUI + SET Checked
        private void button3_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, true);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, false);
            }
        }

        //Get a system path / directory
        // - - 
        public string GetPath()
        {
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var directory = System.IO.Path.GetDirectoryName(path);
            return directory;
        }

        //CALCULATE Moving Averages - NOT USED....YET....
        // - - STATISTICS + METRICS
        public double[] GetMovingAverages(double[] datax, int n)
        {
            var movingAverages = new double[datax.Length];
            var runningTotal = 0.0d;

            for (int i = 0; i < datax.Length; ++i)
            {
                runningTotal += datax[i];
                if (i - n >= 0)
                {
                    var lost = datax[i - n];
                    runningTotal -= lost;
                    movingAverages[i] = runningTotal / n;
                }
            }
            return movingAverages;
        }


        //STORING (POORLY) A BUNCH OF STUFF I SHOULD'VE MOVED TO CLASSES OR BETTER STRUCTS
        // - - STATISTICS + METRICS + FILES
        private void storeFileSizePlot(List<double> blanksizeslist, List<double> helasizeslist, List<double> realsizeslist, List<string> blanknames, List<string> helanames, List<string> filenames, List<string> timestamps, List<string> btimestamps, List<string> htimestamps)
        {
            bfs = blanksizeslist;
            rfs = realsizeslist;
            hfs = helasizeslist;

            rfns = filenames;
            bfns = blanknames;
            hfns = helanames;

            times = timestamps;
            btimes = btimestamps;
            htimes = htimestamps;
    }
        private void storeMedianMS(List<double> ms1, List<double> ms2,List<string> adjfiles)
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
            List<double> dbldummy = new List<double>();
            List<string> strdummy = new List<string>();

            

            if (rawCheckBox.Checked == false)
            {
                if(helaCheckBox.Checked == false)
                {
                    CreateFileSizePlot(dbldummy, dbldummy, rfs, strdummy, strdummy, rfns, times,btimes,htimes);

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

        private void helaCheckBox_CheckedChanged(object sender, EventArgs e)
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


        //REFRESH GUI
        // - -
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
            PlotBasePeakSummary(max_basepeaks, times,bpfnames);
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
            PlotBasePeakSummary(maxbps, timestamps,filenames2);
            storeBasePeaks(maxbps,filenames2);
            


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
            var rdev = deviations * popStats.stDev;
            var rdevplus = popStats.mean + rdev;
            var rdevminus = popStats.mean - rdev;
            if (comboBox1.SelectedIndex == 3)
            {   if (custom_UB_BP > 0 && custom_LB_BP > 0)
                {
                    rdevplus = custom_UB_BP;
                    rdevminus = custom_LB_BP;
                }
            }

            plt.AddHorizontalLine(popStats.mean, primarycolor, width: 1, style: LineStyle.Dash);
            plt.AddHorizontalLine(rdevminus, primarycolor, width: 1, style: LineStyle.Dot);
            plt.AddHorizontalLine(rdevplus, primarycolor, width: 1, style: LineStyle.Dot);



            HighlightedPointBP = basePeakPlot.Plot.AddPoint(0, 0);
            HighlightedPointBP.Color = Color.Black;
            HighlightedPointBP.MarkerSize = 20;
            HighlightedPointBP.MarkerShape = ScottPlot.MarkerShape.openCircle;
            HighlightedPointBP.IsVisible = false;

            plt.Title("Base Peak (Max) vs Time");
            plt.XLabel("Time");
            plt.YLabel("BP (E9)");
            plt.XAxis.Ticks(true);
            plt.XAxis.TickDensity(0.75);
            plt.XAxis.DateTimeFormat(true);
            var imgdir = GetPath() + "\\images\\ALLBASEPEAKS.png";
            plt.SaveFig(@imgdir);

            basePeakPlot.Refresh();
            calcBPMaxStats(files, bparr, rdevminus, rdevplus, popStats.mean);

        }

        public double FindMax(double[] arr2)
        {
          
            int maxIndex = Enumerable.Range(0, arr2.Length).Aggregate((max, i) => arr2[max] > arr2[i] ? max : i);
            double maxVal = arr2[maxIndex];

            return maxVal;
        }




        // Tool Strip Menu Buttons
        // - - GUI + LOGISTICS
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                var newreport = createHTMLReportText();
                var doc = TheArtOfDev.HtmlRenderer.PdfSharp.PdfGenerator.GeneratePdf(newreport, PageSize.Letter);

                doc.Save("stats.pdf");

                var imagereport = createImageReportText("scan");
                var imagedoc = TheArtOfDev.HtmlRenderer.PdfSharp.PdfGenerator.GeneratePdf(imagereport, PageSize.Letter);
                imagedoc.Save("scan.pdf");

                var sizereport = createImageReportText("filesize");
                var sizedoc = TheArtOfDev.HtmlRenderer.PdfSharp.PdfGenerator.GeneratePdf(sizereport, PageSize.Letter);
                sizedoc.Save("filesize.pdf");

                var ticreport = createImageReportText("tic");
                var ticdoc = TheArtOfDev.HtmlRenderer.PdfSharp.PdfGenerator.GeneratePdf(ticreport, PageSize.Letter);
                ticdoc.Save("tic.pdf");

                var bpreport = createImageReportText("bp");
                var bpdoc = TheArtOfDev.HtmlRenderer.PdfSharp.PdfGenerator.GeneratePdf(bpreport, PageSize.Letter);
                bpdoc.Save("bp.pdf");

                var filereport = createFileReportText();
                var filedoc = TheArtOfDev.HtmlRenderer.PdfSharp.PdfGenerator.GeneratePdf(filereport, PageSize.Letter);
                filedoc.Save("filestats.pdf");

                var idreport = createImageReportText("ids");
                var idedoc = TheArtOfDev.HtmlRenderer.PdfSharp.PdfGenerator.GeneratePdf(idreport, PageSize.Letter);
                idedoc.Save("ids.pdf");

                var pepreport = createImageReportText("pepids");
                var pepdoc = TheArtOfDev.HtmlRenderer.PdfSharp.PdfGenerator.GeneratePdf(pepreport, PageSize.Letter);
                pepdoc.Save("pepids.pdf");


                SaveFileDialog svg = new SaveFileDialog();
                svg.DefaultExt = "pdf";
                svg.ShowDialog();

                using (PdfSharp.Pdf.PdfDocument one = PdfReader.Open("stats.pdf", PdfDocumentOpenMode.Import))
                using (PdfSharp.Pdf.PdfDocument two = PdfReader.Open("filesize.pdf", PdfDocumentOpenMode.Import))
                using (PdfSharp.Pdf.PdfDocument twoA = PdfReader.Open("scan.pdf", PdfDocumentOpenMode.Import))
                using (PdfSharp.Pdf.PdfDocument twoB = PdfReader.Open("bp.pdf", PdfDocumentOpenMode.Import))
                using (PdfSharp.Pdf.PdfDocument twoC = PdfReader.Open("tic.pdf", PdfDocumentOpenMode.Import))
                using (PdfSharp.Pdf.PdfDocument twoD = PdfReader.Open("ids.pdf", PdfDocumentOpenMode.Import))
                using (PdfSharp.Pdf.PdfDocument twoF = PdfReader.Open("pepids.pdf", PdfDocumentOpenMode.Import))
                using (PdfSharp.Pdf.PdfDocument three = PdfReader.Open("filestats.pdf", PdfDocumentOpenMode.Import))
                using (PdfSharp.Pdf.PdfDocument outPdf = new PdfSharp.Pdf.PdfDocument())
                {
                    CopyPages(one, outPdf);
                    CopyPages(two, outPdf); CopyPages(twoA, outPdf); CopyPages(twoB, outPdf); CopyPages(twoC, outPdf); CopyPages(twoD, outPdf); CopyPages(twoF, outPdf);
                    CopyPages(three, outPdf);

                    outPdf.Save(svg.FileName);


                }

            }
            catch { }
            

        }

        //MIT LICENSE BUTTON
        // - - GUI
        private void licenseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.MessageBox.Show("Copyright 2023 PBL @ Cedars-Sinai Medical Center \n\nPermission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files(the Software), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and / or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:  \n\nThe above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. \n\nTHE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.");
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
                if (lowMS1Text.Text != "") { html = html.Replace("MS1 log10(Intensities)", "MS1 log10(Intensities)**"); html += "MS1 Intensities<br>"; }
                if (lowMS2Text.Text != "") { html = html.Replace("MS2 log10(Intensities)", "MS2 log10(Intensities)**"); html += "MS2 Intensities<br>"; }
                if (lowBaseText.Text != "") { html = html.Replace("Base Peak (Max)", "Base Peak (Max)**"); html += "Max Base Peak<br>"; }

            }
            return html;
        }
        public string createImageReportText(string imagetype)
        {
            var html = "";
            html += "<h5>QCactus v2 - Quality Report</h5><h6>Generated " + DateTime.Now.ToString("MM/dd/yyyy") + "</h6><hr>";
            if (imagetype == "filesize")
            {html += "<img src = 'images/ALLFILESIZES.PNG' style='width: 500px;  ' />";}

            if (imagetype == "scan")
            { html += "<img src = 'images/MEANS.PNG' style='width: 500px;  '/>"; }
            if (imagetype == "bp")
            { html += "<img src = 'images/ALLBASEPEAKS.PNG' style='width: 500px;  '/>"; }
            if (imagetype == "tic")
            { html += "<img src = 'images/ALLTICS.PNG' style='width: 500px;  '/>"; }

            if (imagetype == "ids")
            { html += "<img src = 'images/ALLIDS.PNG' style='width: 500px;  '/>"; }

            if (imagetype == "pepids")
            { html += "<img src = 'images/ALLPEPIDS.PNG' style='width: 500px;  '/>"; }


            return html;
        }

        public string createFileReportText()
        {
            var html = "<style>div {font-size: 8px;}</style>";
            html += "<h3>File Summary</h3>";
            html += "<div>";
            html += "<hr>Sample Files<br>";
            var myFs = fileList.Items.Cast<String>().ToList();
            html += "Total: " + myFs.Count + "<br>";
            for (var i = 0; i < myFs.Count; i++)
            {
                html += myFs[i] + "<br>";
            }
            html += "<hr>Blank Files<br>";

            var myBs = blankList.Items.Cast<String>().ToList();
            html += "Total: " + myBs.Count + "<br>";
            for (var i = 0; i < myBs.Count; i++)
            {
                html += myBs[i] + "<br>";
            }
            html += "<hr>HeLa Files<br>";
            var myHs = helaList.Items.Cast<String>().ToList();
            html += "Total: " + myHs.Count + "<br>";
            for (var i = 0; i < myHs.Count; i++)
            {
                html += myHs[i] + "<br>";
            }

            html += "<hr>FAILED! Files below were corrupt or incomplete:<br>";
            var myFails = failedfiles;
            html += "Total: " + myFails.Count + "<br>";
            for (var i = 0; i < myFails.Count; i++)
            {
                html += myFails[i] + "<br>";
            }
            html += "</div>";
            return html;
        }

        public void CopyPages(PdfSharp.Pdf.PdfDocument from, PdfSharp.Pdf.PdfDocument to)
        {
            for (int i = 0; i < from.PageCount; i++)
            {
                to.AddPage(from.Pages[i]);
            }
        }




        // Graph interaction and hover 
        // Calculate and 'add' the labels as the user interacts so a couple functions to help
        // A complete pain in the butt

        public void fileSizePlot_MouseMove(object sender, MouseEventArgs e)
        {
            //new something
            

            try { 
            (double mouseCoordX, double mouseCoordY) = fileSizePlot.GetMouseCoordinates();
            double xyRatio = fileSizePlot.Plot.XAxis.Dims.PxPerUnit / fileSizePlot.Plot.YAxis.Dims.PxPerUnit;

            if (mouseCoordX > 0 && mouseCoordY > 0 && dataloaded==true)
            {
                (double pointX, double pointY, int pointIndex) = MyScatterPlot.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio); double pxdif = Math.Abs(mouseCoordX - pointX); double pydif = Math.Abs(mouseCoordY - pointY); double pavg = (pxdif + pydif) / 2;
                    List<double> diffs = new List<double>() { pavg };


                    double ax = 0; double ay = 0; int aIndex = 1;
                    double bx = 0; double by = 0; int bIndex = 1;
                    double cx = 0; double cy = 0; int cIndex = 1;
                    double dx = 0; double dy = 0; int dIndex = 1;
                    if (GroupAActive==1)
                    {
                        (ax, ay, aIndex) = scatFPlotA.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio); double axdif = Math.Abs(mouseCoordX - ax); double aydif = Math.Abs(mouseCoordY - ay); double aavg = (axdif + aydif) / 2;
                        diffs.Add(aavg);
                    
                    }
                    if (GroupBActive == 1)
                    {
                        (bx,  by, bIndex) = scatFPlotB.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio); double bxdif = Math.Abs(mouseCoordX - bx); double bydif = Math.Abs(mouseCoordY - by); double bavg = (bxdif + bydif) / 2;
                        diffs.Add(bavg);
                    }
                    if (GroupCActive == 1)
                    {
                         (cx, cy,  cIndex) = scatFPlotC.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio); double cxdif = Math.Abs(mouseCoordX - cx); double cydif = Math.Abs(mouseCoordY - cy); double cavg = (cxdif + cydif) / 2;
                        diffs.Add(cavg);
                    }
                    if (GroupDActive == 1)
                    {
                        ( dx,  dy, dIndex) = scatFPlotD.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio); double dxdif = Math.Abs(mouseCoordX - dx); double dydif = Math.Abs(mouseCoordY - dy); double davg = (dxdif + dydif) / 2;
                        diffs.Add(davg);
                    }


                        int minValIndex = diffs.IndexOf(diffs.Min());

                    RemoveHPoints();
                    if (minValIndex == 0)
                    {
                        HighlightedPoint.X = pointX;HighlightedPoint.Y = pointY;HighlightedPoint.IsVisible = true;
                        sizeY.Text = pointY.ToString("F");
                        sizeFile.Text = rfns[pointIndex];
                        
                            LastHighlightedIndex = pointIndex;
                            fileSizePlot.Render();
                        //}
                    }
                    if (minValIndex == 1)
                    {
                        
                        scatFhpA.X = ax; scatFhpA.Y = ay; scatFhpA.IsVisible = true;
                        sizeY.Text = ay.ToString("F");
                        sizeFile.Text = groupFilesA.Items[aIndex].ToString();
                        
                            LastHighlightedIndex = aIndex;
                            fileSizePlot.Render();
                        //}
                    }
                    if (minValIndex == 2)
                    {

                        scatFhpB.X = bx; scatFhpB.Y = by; scatFhpB.IsVisible = true;
                        sizeY.Text = by.ToString("F");
                        sizeFile.Text = groupFilesB.Items[bIndex].ToString();
                        
                        LastHighlightedIndex = bIndex;
                        fileSizePlot.Render();
                        //}
                    }
                    if (minValIndex == 3)
                    {

                        scatFhpC.X = cx; scatFhpC.Y = cy; scatFhpC.IsVisible = true;
                        sizeY.Text = cy.ToString("F");
                        sizeFile.Text = groupFilesC.Items[cIndex].ToString();
                        
                        LastHighlightedIndex =cIndex;
                        fileSizePlot.Render();
                        //}
                    }
                    if (minValIndex == 4)
                    {

                        scatFhpD.X = dx; scatFhpD.Y = dy; scatFhpD.IsVisible = true;
                        sizeY.Text = dy.ToString("F");
                        sizeFile.Text = groupFilesD.Items[dIndex].ToString();
                        
                        LastHighlightedIndex = dIndex;
                        fileSizePlot.Render();
                        
                    }

                }
            }
            catch { }
        }

        public void RemoveHPoints()
        {
            HighlightedPoint.IsVisible = false;
            scatFhpA.IsVisible = false;
            scatFhpB.IsVisible = false;
            scatFhpC.IsVisible = false;
            scatFhpD.IsVisible = false;
        }

        public void idPlot_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                (double mouseCoordX, double mouseCoordY) = idPlot.GetMouseCoordinates();
                double xyRatio = idPlot.Plot.XAxis.Dims.PxPerUnit / idPlot.Plot.YAxis.Dims.PxPerUnit;

                if (mouseCoordX > 0 && mouseCoordY > 0 && dataloaded == true)
                {
                    (double pointX, double pointY, int pointIndex) = MyScatterPlot2.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio);
                    // place the highlight over the point of interest
                    HighlightedPointPro.X = pointX;
                    HighlightedPointPro.Y = pointY;
                    HighlightedPointPro.IsVisible = true;

                    // render if the highlighted point chnaged
                    if (LastHighlightedIndex2 != pointIndex)
                    {
                        LastHighlightedIndex2 = pointIndex;
                        idPlot.Render();
                    }

                    // update the GUI to describe the highlighted point
                    idY.Text = pointY.ToString("F");
                    string subfilename = idsfiles[pointIndex];
                    idFile.Text = "... " + idsfiles[pointIndex].Substring((subfilename.Length-15));
                }
            }
            catch { }
        }

        public void idPlotPep_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                (double mouseCoordX, double mouseCoordY) = idPlotPep.GetMouseCoordinates(); 
                double xyRatio = idPlotPep.Plot.XAxis.Dims.PxPerUnit / idPlotPep.Plot.YAxis.Dims.PxPerUnit;

                if (mouseCoordX > 0 && mouseCoordY > 0 && dataloaded == true)
                {
                    (double pointX, double pointY, int pointIndex) = MyScatterPlot3.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio);
                    // place the highlight over the point of interest
                    HighlightedPointPep.X = pointX;
                    HighlightedPointPep.Y = pointY;
                    HighlightedPointPep.IsVisible = true;

                    // render if the highlighted point chnaged
                    if (LastHighlightedIndex != pointIndex)
                    {
                        LastHighlightedIndex = pointIndex;
                        idPlotPep.Render();
                    }

                    // update the GUI to describe the highlighted point
                    idPepY.Text = pointY.ToString("F");
                    string subfilename = idsfiles[pointIndex];
                    idPepFile.Text = "... " + idsfiles[pointIndex].Substring((subfilename.Length - 15)); 
                }
            }
            catch { }
        }

        public void scanPlot_MouseMove(object sender, MouseEventArgs e)
        {
            List<double> diffs = new List<double>();
            //scanPlot.Render();
            try { 
            (double mouseCoordX, double mouseCoordY) = scanPlot.GetMouseCoordinates();
            

                

                if (mouseCoordX > 0 && mouseCoordY > 0 && dataloaded == true)
            {       
                    double xyRatio = scanPlot.Plot.XAxis.Dims.PxPerUnit / scanPlot.Plot.YAxis.Dims.PxPerUnit;
                    double pointX = 0; double pointY = 0; int pointIndex = 1;
                    double pointX2 = 0; double pointY2 = 0; int pointIndex2 = 1;

                    (pointX, pointY, pointIndex) = ScanScatterPlot.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio); 
                    double pxdif = Math.Abs(mouseCoordX - pointX); 
                    double pydif = Math.Abs(mouseCoordY - pointY); 
                    double pavg = (pxdif + pydif) / 2;
                    diffs.Add(pavg);

                    (pointX2, pointY2, pointIndex2) = ScanScatterPlot2.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio); 
                    double pxdif2 = Math.Abs(mouseCoordX - pointX2); 
                    double pydif2 = Math.Abs(mouseCoordY - pointY2); 
                    double pavg2 = (pxdif2 + pydif2) / 2;
                    diffs.Add(pavg2);
                    // place the highlight over the point of interest
                    
                    double ax = 0; double ay = 0; int aIndex = 1;
                    double bx = 0; double by = 0; int bIndex = 1;
                    double cx = 0; double cy = 0; int cIndex = 1;
                    double dx = 0; double dy = 0; int dIndex = 1;
                    double ax2 = 0; double ay2 = 0; int aIndex2 = 1;
                    double bx2 = 0; double by2 = 0; int bIndex2 = 1;
                    double cx2 = 0; double cy2 = 0; int cIndex2 = 1;
                    double dx2 = 0; double dy2 = 0; int dIndex2 = 1;

                    if (GroupAActive == 1)
                    {
                        (ax, ay, aIndex) = scatIPlotA.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio); double axdif = Math.Abs(mouseCoordX - ax); double aydif = Math.Abs(mouseCoordY - ay); double aavg = (axdif + aydif) / 2;
                        (ax2, ay2, aIndex2) = scatIPlotA2.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio); double axdif2 = Math.Abs(mouseCoordX - ax2); double aydif2 = Math.Abs(mouseCoordY - ay2); double aavg2 = (axdif2 + aydif2) / 2;
                        diffs.Add(aavg); diffs.Add(aavg2);

                    }
                    if (GroupBActive == 1)
                    {
                        (bx, by, bIndex) = scatIPlotB.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio); double bxdif = Math.Abs(mouseCoordX - bx); double bydif = Math.Abs(mouseCoordY - by); double bavg = (bxdif + bydif) / 2;
                        diffs.Add(bavg);
                        (bx2, by2, bIndex2) = scatIPlotB2.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio); double bxdif2 = Math.Abs(mouseCoordX - bx2); double bydif2 = Math.Abs(mouseCoordY - by2); double bavg2 = (bxdif2 + bydif2) / 2;
                        diffs.Add(bavg2);
                    }
                    if (GroupCActive == 1)
                    {
                        (cx, cy, cIndex) = scatIPlotC.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio); double cxdif = Math.Abs(mouseCoordX - cx); double cydif = Math.Abs(mouseCoordY - cy); double cavg = (cxdif + cydif) / 2;
                        diffs.Add(cavg);
                        (cx2, cy2, cIndex2) = scatIPlotC2.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio); double cxdif2 = Math.Abs(mouseCoordX - cx2); double cydif2 = Math.Abs(mouseCoordY - cy2); double cavg2 = (cxdif2 + cydif2) / 2;
                        diffs.Add(cavg2);
                    }
                    if (GroupDActive == 1)
                    {
                        (dx, dy, dIndex) = scatIPlotD.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio); double dxdif = Math.Abs(mouseCoordX - dx); double dydif = Math.Abs(mouseCoordY - dy); double davg = (dxdif + dydif) / 2;
                        diffs.Add(davg);
                        (dx2, dy2, dIndex2) = scatIPlotD2.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio); double dxdif2 = Math.Abs(mouseCoordX - dx2); double dydif2 = Math.Abs(mouseCoordY - dy2); double davg2 = (dxdif2 + dydif2) / 2;
                        diffs.Add(davg2);
                    }


                    int minValIndex = diffs.IndexOf(diffs.Min());

                    HighlightedPointScan.IsVisible = false;
                    HighlightedPointScan2.IsVisible = false;
                    scatIhpA.IsVisible = false;
                    scatIhpA2.IsVisible = false;
                    scatIhpB.IsVisible = false;
                    scatIhpB2.IsVisible = false;
                    scatIhpC.IsVisible = false;
                    scatIhpC2.IsVisible = false;
                    scatIhpD.IsVisible = false;
                    scatIhpD2.IsVisible = false;
                    var adistance = GetDistance(mouseCoordX, mouseCoordY, pointX, pointY);
                    var bdistance = GetDistance(mouseCoordX, mouseCoordY, pointX2, pointY2);
                    if (minValIndex < 2)
                    {
                        if (adistance < bdistance)
                        {


                            HighlightedPointScan.X = pointX;
                            HighlightedPointScan.Y = pointY;
                            HighlightedPointScan.IsVisible = true;

                            scanPlot.Render(); scanY2.Text = "----"; scanY.Text = pointY.ToString("F"); scanFile.Text = rfns[pointIndex];
                        }
                        else
                        {
                            HighlightedPointScan2.X = pointX2;
                            HighlightedPointScan2.Y = pointY2;
                            HighlightedPointScan2.IsVisible = true;

                            scanPlot.Render(); scanY.Text = "----"; scanY2.Text = pointY2.ToString("F"); scanFile.Text = rfns[pointIndex2];

                        }
                    }


                    if (minValIndex == 2)
                    {

                        scatIhpA.X = ax; scatIhpA.Y = ay; scatIhpA.IsVisible = true;
                        scanFile.Text = groupFilesA.Items[aIndex].ToString();
                        scanY.Text = ay.ToString("F");
                        scanY2.Text = "----";

                        scanPlot.Render();
                        //}
                    }
                    if (minValIndex == 3)
                    {

                        scatIhpA2.X = ax2; scatIhpA2.Y = ay2; scatIhpA2.IsVisible = true;
                        scanFile.Text = groupFilesA.Items[aIndex2].ToString();
                        scanY.Text = "----";
                        scanY2.Text = ay2.ToString("F");

                        scanPlot.Render();

                        //}
                    }
                    if (minValIndex == 4)
                    {

                        scatIhpB.X = bx; scatIhpB.Y = by; scatIhpB.IsVisible = true;
                        sizeY.Text = by.ToString("F");
                        scanFile.Text = groupFilesB.Items[bIndex].ToString(); 
                        scanY.Text = by.ToString("F");
                        scanY2.Text = "----"; 

                        scanPlot.Render();
                        //}
                    }
                    if (minValIndex == 5)
                    {
                        scatIhpB2.X = bx2; scatIhpB2.Y = by2; scatIhpB2.IsVisible = true;
                        sizeY.Text = by2.ToString("F");
                        scanFile.Text = groupFilesB.Items[bIndex2].ToString();
                        scanY.Text = "----"; 
                        scanY2.Text = by2.ToString("F");

                        scanPlot.Render();

                        //}
                    }
                    if (minValIndex == 6)
                    {

                        scatIhpC.X = cx; scatIhpC.Y = cy; scatIhpC.IsVisible = true;
                        sizeY.Text = cy.ToString("F");
                        scanFile.Text = groupFilesC.Items[cIndex].ToString();
                        scanY.Text = cy.ToString("F");
                        scanY2.Text = "----";

                        scanPlot.Render();
                        //}
                    }
                    if (minValIndex == 7)
                    {
                        scatIhpC2.X = cx2; scatIhpC2.Y = cy2; scatIhpC2.IsVisible = true;
                        sizeY.Text = cy2.ToString("F");
                        scanFile.Text = groupFilesC.Items[cIndex2].ToString();
                        scanY.Text = "----";
                        scanY2.Text = cy2.ToString("F");

                        scanPlot.Render();

                        //}
                    }
                    if (minValIndex == 8)
                    {

                        scatIhpD.X = dx; scatIhpD.Y = dy; scatIhpD.IsVisible = true;
                        sizeY.Text = dy.ToString("F");
                        scanFile.Text = groupFilesD.Items[dIndex].ToString();
                        scanY.Text = dy.ToString("F");
                        scanY2.Text = "----";

                        scanPlot.Render();
                        //}
                    }
                    if (minValIndex == 9)
                    {
                        scatIhpD2.X = dx2; scatIhpD2.Y = dy2; scatIhpD2.IsVisible = true;
                        sizeY.Text = dy2.ToString("F");
                        scanFile.Text = groupFilesD.Items[dIndex2].ToString();
                        scanY.Text = "----";
                        scanY2.Text = dy2.ToString("F");

                        scanPlot.Render();

                        //}
                    }





                }

            }
            catch { }





        }

        public void clearOutMSHovers()
        {
            HighlightedPointScan.IsVisible = false;
            HighlightedPointScan2.IsVisible = false;
            scatIhpA.IsVisible = false;
            scatIhpA2.IsVisible = false;
            scatIhpB.IsVisible = false;
            scatIhpB2.IsVisible = false;
            scatIhpC.IsVisible = false;
            scatIhpC2.IsVisible = false;
            scatIhpD.IsVisible = false;
            scatIhpD2.IsVisible = false;
        }

        private static double GetDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow((x2 - x1), 2) + Math.Pow((y2 - y1), 2));
        }

        private static double GetCV(double stdev, double mean)
        {
            return ((stdev / mean) * 100);
        }
        public void basePeakPlot_MouseMove(object sender, MouseEventArgs e)
        {
            try { 
            (double mouseCoordX, double mouseCoordY) = basePeakPlot.GetMouseCoordinates();
            double xyRatio = basePeakPlot.Plot.XAxis.Dims.PxPerUnit / basePeakPlot.Plot.YAxis.Dims.PxPerUnit;

            if (mouseCoordX > 0 && mouseCoordY > 0 && dataloaded == true)
            {
                (double pointX, double pointY, int pointIndex) = BPScatterPlot.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio);
                    double pxdif = Math.Abs(mouseCoordX - pointX); double pydif = Math.Abs(mouseCoordY - pointY); double pavg = (pxdif + pydif) / 2;
                    List<double> diffs = new List<double>() { pavg };

                    double ax = 0; double ay = 0; int aIndex = 1;
                    double bx = 0; double by = 0; int bIndex = 1;
                    double cx = 0; double cy = 0; int cIndex = 1;
                    double dx = 0; double dy = 0; int dIndex = 1;
                    if (GroupAActive == 1)
                    {
                        (ax, ay, aIndex) = scatBPlotA.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio); double axdif = Math.Abs(mouseCoordX - ax); double aydif = Math.Abs(mouseCoordY - ay); double aavg = (axdif + aydif) / 2;
                        diffs.Add(aavg);

                    }
                    if (GroupBActive == 1)
                    {
                        (bx, by, bIndex) = scatBPlotB.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio); double bxdif = Math.Abs(mouseCoordX - bx); double bydif = Math.Abs(mouseCoordY - by); double bavg = (bxdif + bydif) / 2;
                        diffs.Add(bavg);
                    }
                    if (GroupCActive == 1)
                    {
                        (cx, cy, cIndex) = scatBPlotC.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio); double cxdif = Math.Abs(mouseCoordX - cx); double cydif = Math.Abs(mouseCoordY - cy); double cavg = (cxdif + cydif) / 2;
                        diffs.Add(cavg);
                    }
                    if (GroupDActive == 1)
                    {
                        (dx, dy, dIndex) = scatBPlotD.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio); double dxdif = Math.Abs(mouseCoordX - dx); double dydif = Math.Abs(mouseCoordY - dy); double davg = (dxdif + dydif) / 2;
                        diffs.Add(davg);
                    }


                    int minValIndex = diffs.IndexOf(diffs.Min());

                    HighlightedPointBP.IsVisible = false;
                    scatBhpA.IsVisible = false;
                    scatBhpB.IsVisible = false;
                    scatBhpC.IsVisible = false;
                    scatBhpD.IsVisible = false;

                    if (minValIndex == 0)
                    {
                        HighlightedPointBP.X = pointX;
                        HighlightedPointBP.Y = pointY;
                        HighlightedPointBP.IsVisible = true;
                        basePeakPlot.Render();
                        bpY.Text = pointY.ToString("F");
                        bpFile.Text = rfns[pointIndex];

                    }
                    if (minValIndex == 1)
                    {

                        scatBhpA.X = ax; scatBhpA.Y = ay; scatBhpA.IsVisible = true;
                        bpY.Text = ay.ToString("F");
                        bpFile.Text = groupFilesA.Items[aIndex].ToString();

                        basePeakPlot.Render();
                        //}
                    }
                    if (minValIndex == 2)
                    {

                        scatBhpB.X = bx; scatBhpB.Y = by; scatBhpB.IsVisible = true;
                        bpY.Text = by.ToString("F");
                        bpFile.Text = groupFilesB.Items[bIndex].ToString();

                        basePeakPlot.Render();
                        //}
                    }
                    if (minValIndex == 3)
                    {

                        scatBhpC.X = cx; scatBhpC.Y = cy; scatBhpC.IsVisible = true;
                        bpY.Text = cy.ToString("F");
                        bpFile.Text = groupFilesC.Items[cIndex].ToString();

                        basePeakPlot.Render();
                        //}
                    }
                    if (minValIndex == 4)
                    {

                        scatBhpD.X = dx; scatBhpD.Y = dy; scatBhpD.IsVisible = true;
                        bpY.Text = dy.ToString("F");
                        bpFile.Text = groupFilesD.Items[dIndex].ToString();

                        basePeakPlot.Render();

                    }

                }

            }
            catch { }
        }

        // Integrity Check
        // --------------------------
        // Quick check of file integrity
        
        public bool integrityCheck(string fname, long filemb)
        {   

                    var integrity = true;
            var ftxSize = (filemb / 1024f) / 1024f;
            var rfpath = Path.Combine(folderListing.Text, fname);
                    IRawDataPlus rf;
                    rf = RawFileReaderFactory.ReadFile(rfpath);
                    try
                    {
                        rf.SelectInstrument(Device.MS, 1);

                    }
                    catch
                    {

                        integrity = false;
                    }
            if (ftxSize < 50)
            {
                integrity = false;
            }

            if (integrity == false)
            {
                failedfiles.Add(fname);
                failedFileList.Items.Add(fname);
            }
                
            return integrity;
        }

        // HELPERS FOR FILE INFO
        // --------------------------
        // Some functions to pull information on raw files

        public string getFileCreatedDate(string fname)
        {
            var rfpath = Path.Combine(folderListing.Text, fname);
            IRawDataPlus rf;
            rf = RawFileReaderFactory.ReadFile(rfpath);
            string creationdate = rf.CreationDate.ToString();
            return creationdate;
        }

        public string getFileCreatedDateFullPath(string fullpath)
        {
            var rfpath = fullpath;
            IRawDataPlus rf;
            rf = RawFileReaderFactory.ReadFile(rfpath);
            string creationdate = rf.CreationDate.ToString();
            return creationdate;
        }

        public DateTime getFileTimeStamp(string fname)
        {
            var rfpath = Path.Combine(folderListing.Text, fname);
            IRawDataPlus rf;
            rf = RawFileReaderFactory.ReadFile(rfpath);
            DateTime creationdate = rf.CreationDate;
            return creationdate;
        }


        //FRESH SLATE FOR TIC IMAGE IN GUI
        public void cleanImages()
        {
            var plt = new ScottPlot.Plot();

            plt.Title("TIC");
            plt.XLabel("Retention Time");
            plt.YLabel("TIC (10^9)");

            var imgdir = GetPath() + "\\images\\ALLTICS.png";
            plt.SaveFig(@imgdir);
        }



        // IDENTIFICATION AND SEARCH VIA MSFRAGGER
        //DEV, you'll need to create a folder for all the nice extras needed like jdk and msfragger.jar and a file for msfragger params
        // Example below of how I called msfragger from C#
        
        public string devfragparams = "C:\\Users\\DwightZ\\Documents\\QCactus_FILES\\fragger.params";
        public string devfragcall = "-Xmx6G -jar C:\\Users\\DwightZ\\Documents\\QCactus_FILES\\MSFragger-3.8.jar";
        public string devjavalocation = @"C:\Users\DwightZ\Documents\QCactus_FILES\jdk-20.0.2\bin\java.exe";



        private void idButton_Click(object sender, EventArgs e)
        {
            // Utilize MSFragger to provide identifications / search
            // Make sure you have a good fasta library or use the one included

            //call msfragger from terminal example
            //sudo java -Xmx32g - jar MSFragger - 3.8.jar fragger.params 2023_Sample_205.raw
            List<string> exfiles = new List<string>();
            List<string> fragtimes = new List<string>();
            string javalocation = "";
            string msfraggerfiles = "";
            string msfraggerparams = devfragparams;
            string msfraggercall = devfragcall;
            javalocation = devjavalocation;

            //marco setting
            msfraggerparams = marcofragparams;
            msfraggercall = marcofragcall;
            javalocation = marcojavalocation;

            string finalcall = "";
            peptide_count.Clear();
            protein_count.Clear();
            idsfiles.Clear();

            foreach (Object item in checkedListBox2.CheckedItems)
            {

                int itemnum = checkedListBox2.Items.IndexOf(item); 
                var rawpath = Path.Combine(folderListing.Text, item.ToString());
                msfraggerfiles += rawpath.ToString() + " ";
                exfiles.Add(rawpath.ToString());
                //idTextBox.Text += rawpath.ToString() + "\n";
                //idTextBox.Text += times[itemnum].ToString();
                fragtimes.Add(fragcombotimes[itemnum].ToString());
                idsfiles.Add(item.ToString());
                
            }
            if (msfraggerfiles == "")
            {
                idTextBox.Text = "No files selected.";
            }
            else
            {
                string call = msfraggercall + " " + msfraggerparams + " " + msfraggerfiles;
                finalcall = call;
                
            }
            

            Process myProcess = new Process();
            myProcess.StartInfo.UseShellExecute = false;
            myProcess.StartInfo.RedirectStandardOutput = true;
            myProcess.StartInfo.CreateNoWindow = false;

            myProcess.StartInfo.FileName = javalocation;

            myProcess.StartInfo.Arguments = finalcall;

            label21.Text = "Start: " + DateTime.Now.ToString("h: mm tt");
            idTextBox.Text += "Start MSFragger...\n";
            myProcess.Start();
            string output = myProcess.StandardOutput.ReadToEnd();


            myProcess.WaitForExit();
            idTextBox.Text += "\nEnd MSFragger.";
            idTextBox.Text += output;
            label21.Text += " | End: " + DateTime.Now.ToString("h: mm tt");

            parsePinFiles(exfiles.ToArray());
            plotIDs(fragtimes);
            plotPepIDs(fragtimes);


            foreach (string file in exfiles)
            {
                string pin_path = file.Replace(".raw", ".pin");
                string xml_path = file.Replace(".raw", "_rank1.pepXML");
                string mzml_path = file.Replace(".raw", "_uncalibrated.mzML");

                File.Delete(xml_path);
                File.Delete(mzml_path);

                string newpath = System.IO.Path.Combine("C:\\Temp\\", System.IO.Path.GetFileNameWithoutExtension(pin_path) + "_" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + System.IO.Path.GetExtension(pin_path));
                File.Move(pin_path, newpath);


            }



        }

        private void plotIDs(List<string> freshtime)
        {
            var plt = idPlot.Plot;
            plt.Clear();
            
            double[] proteins = protein_count.ToArray();

            int[] realx = Enumerable.Range(1, proteins.Count()).ToArray();
            var rlx = realx.Select(x => (double)x).ToArray();
            var popReal = new ScottPlot.Statistics.Population(proteins);


            var rdev = deviations * popReal.stDev;
            var rdevplus = popReal.mean + rdev;
            var rdevminus = popReal.mean - rdev;

            List<DateTime> dates = freshtime.Select(date => DateTime.Parse(date)).ToList();
            double[] xs = dates.Select(x => x.ToOADate()).ToArray();




                MyScatterPlot2 = plt.AddScatter(xs, proteins, primarycolor, label: "Proteins");
                //MyScatterPlot = plt.AddScatter(rlx, peptides, Color.Green, label: "Peptides");

                plt.AddHorizontalLine(popReal.mean, Color.Gray, width: 1, style: LineStyle.Dash);
                plt.AddHorizontalLine(rdevminus, Color.Gray, width: 1, style: LineStyle.Dot);
                plt.AddHorizontalLine(rdevplus, Color.Gray, width: 1, style: LineStyle.Dot);

                HighlightedPointPro = idPlot.Plot.AddPoint(0, 0);
                HighlightedPointPro.Color = Color.Black;
                HighlightedPointPro.MarkerSize = 20;
                HighlightedPointPro.MarkerShape = ScottPlot.MarkerShape.openCircle;
                HighlightedPointPro.IsVisible = false;

                plt.XAxis.Ticks(true);
                plt.XAxis.TickDensity(0.75);
                plt.XAxis.DateTimeFormat(true);


                plt.Title("Protein Identifications");
                //plt.XLabel("File");
                plt.YLabel("Count");


                var imgdir = GetPath() + "\\images\\ALLIDS.png";
                plt.SaveFig(@imgdir);

                idPlot.Refresh();


        }

        private void plotPepIDs(List<string> freshtime)
        {
            var plt = idPlotPep.Plot;
            plt.Clear();

            //double[] proteins = protein_count.ToArray();
            double[] peptides = peptide_count.ToArray();
            int[] realx = Enumerable.Range(1, peptides.Count()).ToArray();
            var rlx = realx.Select(x => (double)x).ToArray();
            var popReal = new ScottPlot.Statistics.Population(peptides);


            var rdev = deviations * popReal.stDev;
            var rdevplus = popReal.mean + rdev;
            var rdevminus = popReal.mean - rdev;


            List<DateTime> dates = freshtime.Select(date => DateTime.Parse(date)).ToList();
            double[] xs = dates.Select(x => x.ToOADate()).ToArray();
            MyScatterPlot3 = plt.AddScatter(xs, peptides, Color.Blue, label: "Peptides");

            plt.AddHorizontalLine(popReal.mean, Color.Gray, width: 1, style: LineStyle.Dash);
            plt.AddHorizontalLine(rdevminus, Color.Gray, width: 1, style: LineStyle.Dot);
            plt.AddHorizontalLine(rdevplus, Color.Gray, width: 1, style: LineStyle.Dot);

            HighlightedPointPep = idPlotPep.Plot.AddPoint(0, 0);
            HighlightedPointPep.Color = Color.Black;
            HighlightedPointPep.MarkerSize = 20;
            HighlightedPointPep.MarkerShape = ScottPlot.MarkerShape.openCircle;
            HighlightedPointPep.IsVisible = false;

            plt.XAxis.Ticks(true);
            plt.XAxis.TickDensity(0.75);
            plt.XAxis.DateTimeFormat(true);


            plt.Title("Peptide Identifications");
            plt.XLabel("File");
            plt.YLabel("Count");

            var imgdir = GetPath() + "\\images\\ALLPEPIDS.png";
            plt.SaveFig(@imgdir);

            idPlotPep.Refresh();
        }

        private void parsePinFiles(string[] extrafiles)
        {
            
            foreach (string fullfile in extrafiles)
            {
                string filepath = fullfile.Replace(".raw", ".pin");
                idTextBox.Text += filepath + "\n";
                string[] lines;
                var list = new List<string>();
                var fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read);
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        list.Add(line);
                    }
                }
                lines = list.ToArray();


                List<string> peps = new List<string>();
                List<string> pros = new List<string>();

                foreach (string line in lines)
                {

                    string[] curLine = line.Split("\t");
                    if (curLine[6].ToString() != "0")
                    {


                        peps.Add(curLine[35].ToString());
                        pros.Add(curLine[36].ToString());
                    }
                }

                var noDupPep = peps.Distinct().ToList();
                var noDupPro = pros.Distinct().ToList();

                pinSummary.Text += "File: " + filepath.ToString() + "\n";
                pinSummary.Text += "Total Lines: " + lines.Length.ToString() + "\n";
                pinSummary.Text += "-> Proteins: " + noDupPro.Count.ToString() + "\n";
                pinSummary.Text += "-> Peptides: " + noDupPep.Count.ToString() + "\n";

                protein_count.Add(noDupPro.Count);
                peptide_count.Add(noDupPep.Count);
            }

        }
        //PARSE MSFRAGGER RESULTS .pin
        private void parsePinBtn_Click(object sender, EventArgs e)
        {
           //testing button for file parsing etc
           //emptied for GitHub

        }



        // WORK IN PROGRESS SECTION
        // --------------------------
        // It would be nice to read in timstof data and so far one of the quickest ways was pass parameters to a python script
        // the pyton script would then have the necessary classes to dig into and query data from bruker files / projects
        // works as a prototype but still thinking on ion mobility and what else to include

        
        private void run_py_cmd()
        {
            //empty the output box
            statsBox.Text = "";

            //list the python script to run and start a process
            string fileName = @"C:\Users\DwightZ\Desktop\timsdata\examples\py\timsdata_example.py";
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo(@"C:\Python310\python.exe", fileName)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            p.Start();

            string output = p.StandardOutput.ReadToEnd();

            p.WaitForExit();
            statsBox.Text += "Start Python Script...\n";
            statsBox.Text += output;


            statsBox.Text += "...End Python Script.";

        }

        private void brukerBtn_Click(object sender, EventArgs e)
        {
            //button calls the rum py cmd
            run_py_cmd();


        }

        private string getFilePath(string queryfile)
        {
            FileInfo qf = new FileInfo(queryfile);
            string path = qf.FullName;
            return path;    
        }


        
        // GROUPS
        // --------------------------
        // I added a bunch of groups option for comparison in qcactus but this section is just hardcoded to get the point across
        // this should be rewritten and consolidated into a group class and reorganized because this was a last minute addition and 
        // painted myself into a corner with some earlier decisions

        public string GroupADirectory = "";
        public string GroupBDirectory = "";
        public string GroupCDirectory = "";
        public string GroupDDirectory = "";
        private void addFilesBtn_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialogA.ShowDialog(); 
            openFileDialogA.DefaultExt = "raw";
            List<FileInfo> unorderedList = new List<FileInfo>();
            if (result == DialogResult.OK)
            {

                foreach (String file in openFileDialogA.FileNames)
                {

                    FileInfo fname = new FileInfo(file);
                    unorderedList.Add(fname);
                    GroupADirectory = fname.DirectoryName.ToString();
                }

                List<FileInfo> orderedList = unorderedList.OrderBy(x => getFileCreatedDateFullPath(x.FullName)).ToList();

                foreach (FileInfo filei in orderedList)
                {
                    groupFilesA.Items.Add(filei.Name);
                }

                for (int c = 0; c < groupFilesA.Items.Count; c++)
                {
                    groupFilesA.SetItemChecked(c, true);
                }

            }

        }

        

        private void addFileBtnG2_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialogB.ShowDialog();
            openFileDialogB.DefaultExt = "raw";
            List<FileInfo> unorderedList = new List<FileInfo>();
            if (result == DialogResult.OK)
            {

                foreach (String file in openFileDialogB.FileNames)
                {
                    FileInfo fname = new FileInfo(file);
                    unorderedList.Add(fname);
                    GroupBDirectory = fname.DirectoryName.ToString();
                }

                List<FileInfo> orderedList = unorderedList.OrderBy(x => getFileCreatedDateFullPath(x.FullName)).ToList();

                foreach (FileInfo filei in orderedList)
                {
                    groupFilesB.Items.Add(filei.Name);
                }

                for (int c = 0; c < groupFilesB.Items.Count; c++)
                {
                    groupFilesB.SetItemChecked(c, true);
                }

            }
        }

        private void addFileBtnG3_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialogC.ShowDialog();
            openFileDialogC.DefaultExt = "raw";
            List<FileInfo> unorderedList = new List<FileInfo>();
            if (result == DialogResult.OK)
            {

                foreach (String file in openFileDialogC.FileNames)
                {
                    FileInfo fname = new FileInfo(file);
                    unorderedList.Add(fname);
                    GroupCDirectory = fname.DirectoryName.ToString();
                }

                List<FileInfo> orderedList = unorderedList.OrderBy(x => getFileCreatedDateFullPath(x.FullName)).ToList();

                foreach (FileInfo filei in orderedList)
                {
                    groupFilesC.Items.Add(filei.Name);
                }

                for (int c = 0; c < groupFilesC.Items.Count; c++)
                {
                    groupFilesC.SetItemChecked(c, true);
                }

            }
        }

        private void addFileBtnG4_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialogD.ShowDialog();
            openFileDialogD.DefaultExt = "raw";
            List<FileInfo> unorderedList = new List<FileInfo>();
            if (result == DialogResult.OK)
            {

                foreach (String file in openFileDialogD.FileNames)
                {
                    FileInfo fname = new FileInfo(file);
                    unorderedList.Add(fname);
                    GroupDDirectory = fname.DirectoryName.ToString();
                }

                List<FileInfo> orderedList = unorderedList.OrderBy(x => getFileCreatedDateFullPath(x.FullName)).ToList();

                foreach (FileInfo filei in orderedList)
                {
                    groupFilesD.Items.Add(filei.Name);
                }

                for (int c = 0; c < groupFilesD.Items.Count; c++)
                {
                    groupFilesD.SetItemChecked(c, true);
                }

            }
        }

        public System.Drawing.Color primarycolor = System.Drawing.ColorTranslator.FromHtml("#DB5461");
        public System.Drawing.Color primarycolorLight = System.Drawing.ColorTranslator.FromHtml("#6A6D82");
        public System.Drawing.Color primarycolorAlt = System.Drawing.ColorTranslator.FromHtml("#6E44FF");
        public System.Drawing.Color primarycolorAltLight = System.Drawing.ColorTranslator.FromHtml("#9B93BA");

        public List<string> ATimes = new List<string>();
        public List<string> BTimes = new List<string>();
        public List<string> CTimes = new List<string>();
        public List<string> DTimes = new List<string>();
        private void runGroupsBtn_Click(object sender, EventArgs e)
        {
            // store size series info
            List<double> AFileSizes = new List<double>();
            List<string> AFileNames = new List<string>();

            List<double> BFileSizes = new List<double>();
            List<string> BFileNames = new List<string>();

            List<double> CFileSizes = new List<double>();
            List<string> CFileNames = new List<string>();

            List<double> DFileSizes = new List<double>();
            List<string> DFileNames = new List<string>();

            //loop through the imported file names from the list box
            foreach (String ftx in groupFilesA.Items)
            {   string fintx = GroupADirectory + "\\" + ftx;
                FileInfo file_stuff = new FileInfo(fintx);
                double size = file_stuff.Length;
                size = size / 1025f / 1024f;


                AFileSizes.Add(size);
                AFileNames.Add(file_stuff.Name);
                string sdate = getFileCreatedDateFullPath(file_stuff.FullName);
                ATimes.Add(sdate);
                checkedListBox1.Items.Add(("GroupA:" + file_stuff.Name));
            }

            foreach (String ftx in groupFilesB.Items)
            {
                string fintx = GroupBDirectory + "\\" + ftx;
                FileInfo file_stuff = new FileInfo(fintx);
                double size = file_stuff.Length;
                size = size / 1025f / 1024f;


                BFileSizes.Add(size);
                BFileNames.Add(file_stuff.Name);
                string sdate = getFileCreatedDateFullPath(file_stuff.FullName);
                BTimes.Add(sdate);
                checkedListBox1.Items.Add(("GroupB:" + file_stuff.Name));
            }

            foreach (String ftx in groupFilesC.Items)
            {
                string fintx = GroupCDirectory + "\\" + ftx;
                FileInfo file_stuff = new FileInfo(fintx);
                double size = file_stuff.Length;
                size = size / 1025f / 1024f;


                CFileSizes.Add(size);
                CFileNames.Add(file_stuff.Name);
                string sdate = getFileCreatedDateFullPath(file_stuff.FullName);
                CTimes.Add(sdate);
                checkedListBox1.Items.Add(("GroupC:" + file_stuff.Name));
            }

            foreach (String ftx in groupFilesD.Items)
            {
                string fintx = GroupDDirectory + "\\" + ftx;
                FileInfo file_stuff = new FileInfo(fintx);
                double size = file_stuff.Length;
                size = size / 1025f / 1024f;


                DFileSizes.Add(size);
                DFileNames.Add(file_stuff.Name);
                string sdate = getFileCreatedDateFullPath(file_stuff.FullName);
                DTimes.Add(sdate);
                checkedListBox1.Items.Add(("GroupD:" + file_stuff.Name));
            }

            System.Drawing.Color cola = System.Drawing.ColorTranslator.FromHtml("#5EBB89");
            System.Drawing.Color colb = System.Drawing.ColorTranslator.FromHtml("#3FA7D6");
            System.Drawing.Color colc = System.Drawing.ColorTranslator.FromHtml("#FAC05E");
            System.Drawing.Color cold = System.Drawing.ColorTranslator.FromHtml("#00E8FC");

            if (groupFilesA.Items.Count > 0)
            {
                // GROUP A
                // #1 ADD NEW FILES TO FILE SIZE PLOT
                addSeriesFileSizes(AFileSizes, ATimes, cola, "Group A");
                // #2 ADD NEW FILES TO INTENSITY PLOT
                addSeriesFileIntensity(AFileNames, ATimes, GroupADirectory, "Group A", cola, cola);
                // #3 ADD NEW FILES TO BP PLOT
                addSeriesFileBPS(AFileNames, ATimes, GroupADirectory, "Group A", cola);
            }
            if (groupFilesB.Items.Count > 0)
            {
                // GROUP B
                // #1 ADD NEW FILES TO FILE SIZE PLOT
                addSeriesFileSizes(BFileSizes, BTimes,colb, "Group B");
                // #2 ADD NEW FILES TO INTENSITY PLOT
                addSeriesFileIntensity(BFileNames, BTimes, GroupBDirectory, "Group B",colb, colb);
                // #3 ADD NEW FILES TO BP PLOT
                addSeriesFileBPS(BFileNames, BTimes, GroupBDirectory, "Group B", colb);
            }
            if (groupFilesC.Items.Count > 0)
            {
                // GROUP C
                // #1 ADD NEW FILES TO FILE SIZE PLOT
                addSeriesFileSizes(CFileSizes, CTimes, colc, "Group C");
                // #2 ADD NEW FILES TO INTENSITY PLOT
                addSeriesFileIntensity(CFileNames, CTimes, GroupCDirectory, "Group C", colc, colc);
                // #3 ADD NEW FILES TO BP PLOT
                addSeriesFileBPS(CFileNames, CTimes, GroupCDirectory, "Group C",colc);
            }
            if (groupFilesD.Items.Count > 0)
            {
                // GROUP D
                // #1 ADD NEW FILES TO FILE SIZE PLOT
                addSeriesFileSizes(DFileSizes, DTimes, cold, "Group D");
                // #2 ADD NEW FILES TO INTENSITY PLOT
                addSeriesFileIntensity(DFileNames, DTimes, GroupDDirectory, "Group D", cold, cold);
                // #3 ADD NEW FILES TO BP PLOT
                addSeriesFileBPS(DFileNames, DTimes, GroupDDirectory, "Group D", cold);
            }


        }

        public void addSeriesFileBPS(List<string> filenames, List<string> timestamps, string group, string groupname, Color linecolor)
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
                    var rfpath = Path.Combine(group, fname);
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

                    maxbasepeak = GetBPInformation(rf, firstScanNumber, lastScanNumber) / (10e8);
                    maxbps.Add(maxbasepeak);

                    item = item + 1;
                }
                catch
                {
                    statsBox.Text += "\n\nError reading:" + fname + ".  Excluded from intensity plot.\n\n";
                }

            }
            addGroupBPSToPlot(maxbps, timestamps, filenames2,groupname, linecolor);


        }

        public void addGroupBPSToPlot(List<double> bps, List<string> sttime, List<string> files, string groupname, Color linecolor)
        {
            var plt = basePeakPlot.Plot;
            List<DateTime> adjustedFileWrites = adjustTimeGap(sttime);

            double[] bparr = bps.ToArray();
            // List<DateTime> dates = sttime.Select(date => DateTime.Parse(date)).ToList();

            List<DateTime> dates = adjustedFileWrites;
            double[] xs = dates.Select(x => x.ToOADate()).ToArray();

            var popbs = new ScottPlot.Statistics.Population(bparr); var bscv = GetCV(popbs.stDev, popbs.mean);
            var bslabel = " " + " (" + Math.Round(bscv, 1) + "%CV)";

            switch (groupname)
            {
                case "Group A":
                    scatBPlotA = plt.AddScatter(xs, bparr, linecolor, label: (groupname + bslabel));
                    scatBhpA = plt.AddPoint(0, 0); scatBhpA.Color = Color.Black; scatBhpA.MarkerSize = 20; scatBhpA.MarkerShape = ScottPlot.MarkerShape.openCircle; scatBhpA.IsVisible = false;
                    GroupAActive = 1;
                    break;
                case "Group B":
                    scatBPlotB = plt.AddScatter(xs, bparr, linecolor, label: (groupname + bslabel));
                    scatBhpB = plt.AddPoint(0, 0); scatBhpB.Color = Color.Black; scatBhpB.MarkerSize = 20; scatBhpB.MarkerShape = ScottPlot.MarkerShape.openCircle; scatBhpB.IsVisible = false;
                    GroupBActive = 1;
                    break;
                case "Group C":
                    scatBPlotC = plt.AddScatter(xs, bparr, linecolor, label: (groupname + bslabel));
                    scatBhpC = plt.AddPoint(0, 0); scatBhpC.Color = Color.Black; scatBhpC.MarkerSize = 20; scatBhpC.MarkerShape = ScottPlot.MarkerShape.openCircle; scatBhpC.IsVisible = false;
                    GroupCActive = 1;
                    break;
                case "Group D":
                    scatBPlotD = plt.AddScatter(xs, bparr, linecolor, label: (groupname + bslabel));
                    scatBhpD = plt.AddPoint(0, 0); scatBhpD.Color = Color.Black; scatBhpD.MarkerSize = 20; scatBhpD.MarkerShape = ScottPlot.MarkerShape.openCircle; scatBhpD.IsVisible = false;
                    GroupDActive = 1;
                    break;
            }


            //BPScatterPlot = plt.AddScatter(xs, bparr, linecolor, label: (groupname + bslabel));
            plt.Legend(location: Alignment.MiddleRight);
            plt.AxisAuto();
            var imgdir = GetPath() + "\\images\\ALLBASEPEAKS.png";
            plt.SaveFig(@imgdir);

            basePeakPlot.Refresh();
        }



        public int GroupAActive = 0;
        public int GroupBActive = 0;
        public int GroupCActive = 0;
        public int GroupDActive = 0;

        public void addSeriesFileSizes(List<double> filesizes, List<string> filelastwrites, Color linecolor, string linelabel)
        {
            List<DateTime> adjustedFileWrites = adjustTimeGap(filelastwrites);

            var plt = fileSizePlot.Plot;
            //plt.Clear();
            List<string> newtimes = new List<string>(filelastwrites);
            double[] realfiles1 = filesizes.ToArray();
            int[] realx = Enumerable.Range(1, realfiles1.Count()).ToArray();
            var rlx = realx.Select(x => (double)x).ToArray();

 

            List<DateTime> dates = filelastwrites.Select(date => DateTime.Parse(date)).ToList();


            double[] xs1 = adjustedFileWrites.Select(x => x.ToOADate()).ToArray();
            switch (linelabel)
            {
                case "Group A":
                    scatFPlotA = plt.AddScatter(xs1, realfiles1, linecolor, label: linelabel);
                    scatFhpA = plt.AddPoint(0, 0); scatFhpA.Color = Color.Black; scatFhpA.MarkerSize = 20; scatFhpA.MarkerShape = ScottPlot.MarkerShape.openCircle; scatFhpA.IsVisible = false;
                    GroupAActive = 1;
                    break;
                case "Group B":
                    scatFPlotB = plt.AddScatter(xs1, realfiles1, linecolor, label: linelabel);
                    scatFhpB = plt.AddPoint(0, 0); scatFhpB.Color = Color.Black; scatFhpB.MarkerSize = 20; scatFhpB.MarkerShape = ScottPlot.MarkerShape.openCircle; scatFhpB.IsVisible = false;
                    GroupBActive = 1;
                    break;
                case "Group C":
                    scatFPlotC = plt.AddScatter(xs1, realfiles1, linecolor, label: linelabel);
                    scatFhpC = plt.AddPoint(0, 0); scatFhpC.Color = Color.Black; scatFhpC.MarkerSize = 20; scatFhpC.MarkerShape = ScottPlot.MarkerShape.openCircle; scatFhpC.IsVisible = false;
                    GroupCActive = 1;
                    break;
                case "Group D":
                    scatFPlotD = plt.AddScatter(xs1, realfiles1, linecolor, label: linelabel);
                    scatFhpC = plt.AddPoint(0, 0); scatFhpD.Color = Color.Black; scatFhpD.MarkerSize = 20; scatFhpD.MarkerShape = ScottPlot.MarkerShape.openCircle; scatFhpD.IsVisible = false;
                    GroupDActive = 1;
                    break;
            }

            



            //MyScatterPlot += plt.AddScatter(xs1, realfiles1, linecolor, label: linelabel);
            //plt.AddScatter(xs1, realfiles1, linecolor, label: linelabel);
            plt.AxisAuto();


            var imgdir = GetPath() + "\\images\\ALLFILESIZES.png";
            plt.SaveFig(@imgdir);

            fileSizePlot.Refresh();
            
        }



        public void addSeriesFileIntensity(List<string> filenames, List<string> timestamps, string group, string groupname, Color ms1color, Color ms2color)
        {
            List<double> maxbps = new List<double>();
            List<double> maxtics = new List<double>();
            List<string> filenames2 = new List<string>();

            var item = 0;
            var i = 1;
            foreach (string fname in filenames)
            {

                progressBar1.Value = i * progressBar1.Maximum / filenames.Count;
                i = i + 1;

                try
                {
                    var rfpath = Path.Combine(group, fname);
                    IRawDataPlus rf;
                    rf = RawFileReaderFactory.ReadFile(rfpath);

                    rf.SelectInstrument(Device.MS, 1);


                    filenames2.Add(fname);
                    int firstScanNumber = rf.RunHeaderEx.FirstSpectrum;
                    int lastScanNumber = rf.RunHeaderEx.LastSpectrum;
                    var filename = Path.GetFileName(rf.FileName);
                    var maxbasepeak = 0.0;
                    var maxtic = 0.0;
                    var count = 0;
                    List<double> bs = new List<double>();
                    List<double> ts = new List<double>();

                    foreach (var scanNumber in Enumerable
                                   .Range(1, lastScanNumber - firstScanNumber))
                    {
                        var scanStatistics = rf.GetScanStatsForScanNumber(scanNumber);
                        var scanFilter = rf.GetFilterForScanNumber(scanNumber);
                        if (scanFilter.MSOrder == MSOrderType.Ms)
                        {
                            count = count + 1;


                            double newTic = Math.Log10(scanStatistics.TIC); 

                            ts.Add(newTic);

                        }
                        if (scanFilter.MSOrder == MSOrderType.Ms2)
                        {
                            double newTic = Math.Log10(scanStatistics.TIC);
                            bs.Add(newTic);
                        }


                    }
                    maxbasepeak = GetMedian(bs.ToArray()); //ACTUALLY MS2

                    maxtic = GetMedian(ts.ToArray()); //ACTUALLY MS1

                    maxbps.Add(maxbasepeak);
                    maxtics.Add(maxtic);
                    item = item + 1;
                }
                catch { timestamps.RemoveAt(item); statsBox.Text += "\nError reading:" + fname + ".  Excluded from intensity plot.\n"; }

            }
            addGroupIntensitiesToPlot(maxbps, maxtics, timestamps, filenames,groupname,ms1color,ms2color);
            //storeMedianMS(maxtics, maxbps, filenames);

        }

        public void addGroupIntensitiesToPlot(List<double> bps, List<double> tics, List<string> timestamps, List<string> fnames, string groupname, Color ms1color, Color ms2color)
        {
            List<string> newtimes = new List<string>(timestamps);


            var plt = scanPlot.Plot;

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

            var popms2 = new ScottPlot.Statistics.Population(bparr); var ms2cv = GetCV(popms2.stDev, popms2.mean);
            var popms1 = new ScottPlot.Statistics.Population(ticarr); var ms1cv = GetCV(popms1.stDev, popms1.mean);
            var ms1label = "-MS1" + " (" + Math.Round(ms1cv, 1) + "%CV)";
            var ms2label = "-MS2" + " (" + Math.Round(ms2cv, 1) + "%CV)";

            switch (groupname)
            {
                case "Group A":
                    scatIPlotA = plt.AddScatter(blx, bparr, ms2color, lineWidth: 1, label: (groupname + ms2label));
                    scatIPlotA2 = plt.AddScatter(rlx, ticarr, ms1color, lineWidth: 1, label: (groupname + ms1label));
                    scatIhpA = plt.AddPoint(0, 0); scatIhpA.Color = Color.Black; scatIhpA.MarkerSize = 20; scatIhpA.MarkerShape = ScottPlot.MarkerShape.openCircle; scatIhpA.IsVisible = false;
                    scatIhpA2 = plt.AddPoint(0, 0); scatIhpA2.Color = Color.Black; scatIhpA2.MarkerSize = 20; scatIhpA2.MarkerShape = ScottPlot.MarkerShape.openCircle; scatIhpA2.IsVisible = false;
                    GroupAActive = 1;
                    break;
                case "Group B":
                    scatIPlotB = plt.AddScatter(blx, bparr, ms2color, lineWidth: 1, label: (groupname + ms2label));
                    scatIPlotB2 = plt.AddScatter(rlx, ticarr, ms1color, lineWidth: 1, label: (groupname + ms1label));
                    scatIhpB = plt.AddPoint(0, 0); scatIhpB.Color = Color.Black; scatIhpB.MarkerSize = 20; scatIhpB.MarkerShape = ScottPlot.MarkerShape.openCircle; scatIhpB.IsVisible = false;
                    scatIhpB2 = plt.AddPoint(0, 0); scatIhpB2.Color = Color.Black; scatIhpB2.MarkerSize = 20; scatIhpB2.MarkerShape = ScottPlot.MarkerShape.openCircle; scatIhpB2.IsVisible = false;
                    GroupBActive = 1;
                    break;
                case "Group C":
                    scatIPlotC = plt.AddScatter(blx, bparr, ms2color, lineWidth: 1, label: (groupname + ms2label));
                    scatIPlotC2 = plt.AddScatter(rlx, ticarr, ms1color, lineWidth: 1, label: (groupname + ms1label));
                    scatIhpC = plt.AddPoint(0, 0); scatIhpC.Color = Color.Black; scatIhpC.MarkerSize = 20; scatIhpC.MarkerShape = ScottPlot.MarkerShape.openCircle; scatIhpC.IsVisible = false;
                    scatIhpC2 = plt.AddPoint(0, 0); scatIhpC2.Color = Color.Black; scatIhpC2.MarkerSize = 20; scatIhpC2.MarkerShape = ScottPlot.MarkerShape.openCircle; scatIhpC2.IsVisible = false;
                    GroupCActive = 1;
                    break;
                case "Group D":
                    scatIPlotD = plt.AddScatter(blx, bparr, ms2color, lineWidth: 1, label: (groupname + ms2label));
                    scatIPlotD2 = plt.AddScatter(rlx, ticarr, ms1color, lineWidth: 1, label: (groupname + ms1label));
                    scatIhpD = plt.AddPoint(0, 0); scatIhpD.Color = Color.Black; scatIhpD.MarkerSize = 20; scatIhpD.MarkerShape = ScottPlot.MarkerShape.openCircle; scatIhpD.IsVisible = false;
                    scatIhpD2 = plt.AddPoint(0, 0); scatIhpD2.Color = Color.Black; scatIhpD2.MarkerSize = 20; scatIhpD2.MarkerShape = ScottPlot.MarkerShape.openCircle; scatIhpD2.IsVisible = false;
                    GroupDActive = 1;
                    break;
            }
            //ScanScatterPlot = plt.AddScatter(blx, bparr, ms2color, lineWidth: 1, label: (groupname + ms2label));
            //ScanScatterPlot2 = plt.AddScatter(rlx, ticarr, ms1color, lineWidth: 1, label: (groupname + ms1label));

            plt.AxisAuto();
            var imgdir = GetPath() + "\\images\\MEANS.png";
            plt.SaveFig(@imgdir);


            scanPlot.Refresh();
        }

        public List<DateTime> adjustTimeGap(List<string> tstamps)
        {
            List<DateTime> olddates = tstamps.Select(date => DateTime.Parse(date)).ToList();
            List<DateTime> newdates = times.Select(date => DateTime.Parse(date)).ToList();
            List<DateTime> adjusted = new List<DateTime>();
            TimeSpan timewarp = (newdates[0] - olddates[0]);
            for (int i = 0; i < olddates.Count; i++)
            {   
                //richTextBox1.Text += olddates[i].ToString() + "\n";
                DateTime nx = olddates[i].Add(timewarp);
                //richTextBox1.Text += nx.ToString() + "\n";
                adjusted.Add( nx );
            }

            //return olddates;
            return adjusted;
        }
    }


    

}

