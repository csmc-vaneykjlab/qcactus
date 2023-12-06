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
        public ScottPlot.Plottable.MarkerPlot HighlightedPointScan = new ScottPlot.Plottable.MarkerPlot();
        public ScottPlot.Plottable.MarkerPlot HighlightedPointScan2 = new ScottPlot.Plottable.MarkerPlot();
        public ScottPlot.Plottable.MarkerPlot HighlightedPointBP = new ScottPlot.Plottable.MarkerPlot();
        public ScottPlot.Plottable.MarkerPlot HighlightedPoint = new ScottPlot.Plottable.MarkerPlot();

        public ScottPlot.Plottable.MarkerPlot HighlightedPointPro = new ScottPlot.Plottable.MarkerPlot();
        public ScottPlot.Plottable.MarkerPlot HighlightedPointPep = new ScottPlot.Plottable.MarkerPlot();

        public int LastHighlightedIndex = -1;
        public int LastHighlightedIndex2 = -1;

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

                /////////////// FIX HERE FIX HERE !!!
                //List<FileInfo> orderedFList = files.OrderBy(x => x.LastWriteTime).ToList();
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
                            //double newBP = Math.Log10(scanStatistics.BasePeakIntensity);
                            
                            //bs.Add(newBP);
                            //ts.Add(newTic);

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

            ScanScatterPlot = plt.AddScatter(blx, bparr, Color.Red, lineWidth: 1, label: ms2label);
            ScanScatterPlot2 = plt.AddScatter(rlx, ticarr, Color.Blue, lineWidth: 1, label: ms1label);

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

            plt.AddHorizontalLine(popBlank.mean, Color.Red, width: 1, style: LineStyle.Dash);
            plt.AddHorizontalLine(rdevplus, Color.Red, width: 1, style: LineStyle.Dot);
            plt.AddHorizontalLine(rdevminus, Color.Red, width: 1, style: LineStyle.Dot);

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

            plt.AddHorizontalLine(rdevplus2, Color.Blue, width: 1, style: LineStyle.Dot);
            plt.AddHorizontalLine(rdevminus2, Color.Blue, width: 1, style: LineStyle.Dot);


            plt.AddHorizontalLine(popReal.mean, Color.Blue, width: 1, style: LineStyle.Dash);

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
                MyScatterPlot = plt.AddScatter(xs, realfiles, Color.Red, label: "Samples");

                plt.AddHorizontalLine(popReal.mean, Color.Red, width: 1, style: LineStyle.Dash);
                plt.AddHorizontalLine(rdevminus, Color.Red, width: 1, style: LineStyle.Dot);
                plt.AddHorizontalLine(rdevplus, Color.Red, width: 1, style: LineStyle.Dot);

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
                
                    //foreach (var scanNumber in Enumerable
                    //               .Range(1, lastScanNumber - firstScanNumber))
                    //{
                    //    var scanStatistics = rf.GetScanStatsForScanNumber(scanNumber);
                    //    var scanFilter = rf.GetFilterForScanNumber(scanNumber);
                    //    if (scanFilter.MSOrder == MSOrderType.Ms)
                    //    {
                    //        count = count + 1;
                    //        double newBP = Math.Log10(scanStatistics.BasePeakIntensity);  
                    //        bs.Add(newBP);
                    //    }

                    //}
                    //maxbasepeak = FindMax(bs.ToArray());
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
            BPScatterPlot = plt.AddScatter(xs, bparr, Color.Red, label: bplabel);
            var fancy = plt.AddAnnotation(bplabel, Alignment.UpperLeft);
            fancy.Font.Size = 18;
            fancy.BackgroundColor = Color.White;

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

            plt.AddHorizontalLine(popStats.mean, Color.Red, width: 1, style: LineStyle.Dash);
            plt.AddHorizontalLine(rdevminus, Color.Red, width: 1, style: LineStyle.Dot);
            plt.AddHorizontalLine(rdevplus, Color.Red, width: 1, style: LineStyle.Dot);



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




        //Tool Strip Menu Buttons
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
            System.Windows.Forms.MessageBox.Show("Copyright 2023 PBL @ Cedars-Sinai Medical Center \n\nPermission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files(the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and / or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:  \n\nThe above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. \n\nTHE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.");
        }

        //CLOSE APP
        // - - GUI
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        //CREATE PDF REPORT
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




        //MOUSE OVER THE PLOTS + Interaction
        // Calculate and 'add' the labels as the user interacts so a couple functions to help

        public void fileSizePlot_MouseMove(object sender, MouseEventArgs e)
        {
            try { 
            (double mouseCoordX, double mouseCoordY) = fileSizePlot.GetMouseCoordinates();
            double xyRatio = fileSizePlot.Plot.XAxis.Dims.PxPerUnit / fileSizePlot.Plot.YAxis.Dims.PxPerUnit;

            if (mouseCoordX > 0 && mouseCoordY > 0 && dataloaded==true)
            {
                (double pointX, double pointY, int pointIndex) = MyScatterPlot.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio);
                // place the highlight over the point of interest
                HighlightedPoint.X = pointX;
                HighlightedPoint.Y = pointY;
                HighlightedPoint.IsVisible = true;

                // render if the highlighted point chnaged
                if (LastHighlightedIndex != pointIndex)
                {
                    LastHighlightedIndex = pointIndex;
                    fileSizePlot.Render();
                }

                // update the GUI to describe the highlighted point
                sizeY.Text = pointY.ToString("F");
                sizeFile.Text = rfns[pointIndex];
            }
            }
            catch { }
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
                    (double pointX, double pointY, int pointIndex) = MyScatterPlot.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio);
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
            try { 
            (double mouseCoordX, double mouseCoordY) = scanPlot.GetMouseCoordinates();
            double xyRatio = scanPlot.Plot.XAxis.Dims.PxPerUnit / scanPlot.Plot.YAxis.Dims.PxPerUnit;

            if (mouseCoordX > 0 && mouseCoordY > 0 && dataloaded == true)
            {
                (double pointX, double pointY, int pointIndex) = ScanScatterPlot.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio);
                (double pointX2, double pointY2, int pointIndex2) = ScanScatterPlot2.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio);
                    // place the highlight over the point of interest
                    
               var adistance = GetDistance(mouseCoordX, mouseCoordY, pointX, pointY);
               var bdistance = GetDistance(mouseCoordX, mouseCoordY, pointX2, pointY2);

                    if (adistance < bdistance)
                    {


                        HighlightedPointScan.X = pointX;
                        HighlightedPointScan.Y = pointY;
                        HighlightedPointScan.IsVisible = true;
                        HighlightedPointScan2.IsVisible = false;
                        if (LastHighlightedIndex != pointIndex)
                {
                    LastHighlightedIndex = pointIndex;
                    
                }scanPlot.Render(); scanY2.Text = "----"; scanY.Text = pointY.ToString("F");scanFile.Text = rfns[pointIndex];
                    }
                    else
                    {
                        HighlightedPointScan2.X = pointX2;
                        HighlightedPointScan2.Y = pointY2;
                        HighlightedPointScan2.IsVisible = true;
                        HighlightedPointScan.IsVisible = false;
                if (LastHighlightedIndex != pointIndex2)
                {
                    LastHighlightedIndex2 = pointIndex2;
                    
                }scanPlot.Render();scanY.Text = "----"; scanY2.Text = pointY2.ToString("F"); scanFile.Text = rfns[pointIndex2];

                    }

                
                
                
            }

            }
            catch { }
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
                // place the highlight over the point of interest
                HighlightedPointBP.X = pointX;
                HighlightedPointBP.Y = pointY;
                HighlightedPointBP.IsVisible = true;

                // render if the highlighted point chnaged
                if (LastHighlightedIndex != pointIndex)
                {
                    LastHighlightedIndex = pointIndex;
                    basePeakPlot.Render();
                }

                
                bpY.Text = pointY.ToString("F");
                bpFile.Text = rfns[pointIndex];
            }

            }
            catch { }
        }

        //INTEGRITY CHECK FOR FILES VIA SIZE AND READABILITY
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

        public string getFileCreatedDate(string fname)
        {
            var rfpath = Path.Combine(folderListing.Text, fname);
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
        //DEV
        public string devfragparams = "C:\\Users\\DwightZ\\Documents\\QCactus_Requirements\\fragger.params";
        public string devfragcall = "-Xmx6G -jar C:\\Users\\DwightZ\\Documents\\QCactus_Requirements\\MSFragger-3.8.jar";
        public string devjavalocation = @"C:\Users\DwightZ\Documents\QCactus_Requirements\jdk-20.0.2\bin\java.exe";
        //PRODUCTION ON MARCO
        public string marcofragparams = "C:\\Users\\Exploris_marco\\Documents\\QCactus_Requirements\\fragger.params";
        public string marcofragcall = "-Xmx8G -jar C:\\Users\\Exploris_marco\\Documents\\QCactus_Requirements\\MSFragger-3.8.jar";
        public string marcojavalocation = @"C:\Users\Exploris_marco\Documents\QCactus_Requirements\jdk-20.0.2\bin\java.exe";
        //public string marcosystemjavalocation = @"C:\Program Files (x86)\Java\jre-1.8\bin\java.exe";
        //PRODUCTION ON Henson
        //C:\Users\Exploris_Henson\Documents\QCactus_Requirements
        public string hensonfragparams = "C:\\Users\\Exploris_Henson\\Documents\\QCactus_Requirements\\fragger.params";
        public string hensonfragcall = "-Xmx8G -jar C:\\Users\\Exploris_Henson\\Documents\\QCactus_Requirements\\MSFragger-3.8.jar";
        public string hensonjavalocation = @"C:\Users\Exploris_Henson\Documents\QCactus_Requirements\jdk-20.0.2\bin\java.exe";

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
            msfraggerparams = hensonfragparams;
            msfraggercall = hensonfragcall;
            javalocation = hensonjavalocation;

            //DEV ENV
            //string msfraggerparams = "C:\\Users\\DwightZ\\Documents\\QCactus_Requirements\\fragger.params";
            //string msfraggercall = "-Xmx6G -jar C:\\Users\\DwightZ\\Documents\\QCactus_Requirements\\MSFragger-3.8.jar";

            //PRODU
            //string msfraggerparams = "C:\\Users\\Exploris_marco\\Documents\\QCactus_Requirements\\fragger.params";
            //string msfraggercall = "-Xmx12G -jar C:\\Users\\Exploris_marco\\Documents\\QCactus_Requirements\\MSFragger-3.8.jar";

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
            

            //testing note:
            // INVALID HEAP SIZE java -Xmx6G -jar C:\\Users\\Exploris_marco\\Documents\\QCactus_Requirements\\MSFragger-3.8.jar C:\\Users\\Exploris_marco\\Documents\\QCactus_Requirements\\fragger.params C:\\Users\\Exploris_marco\\Documents\\QCactus_Requirements\\2023_Sample_432.raw
            //INVALID HEAP SIZE java -Xmx8G -jar C:\\Users\\Exploris_marco\\Documents\\QCactus_Requirements\\MSFragger-3.8.jar C:\\Users\\Exploris_marco\\Documents\\QCactus_Requirements\\fragger.params C:\\Users\\Exploris_marco\\Documents\\QCactus_Requirements\\2023_Sample_432.raw
            //C:\\Users\\Exploris_marco\\Documents\\QCactus_Requirements\\jdk-20.0.2\\bin\\java.exe -Xmx8G -jar C:\\Users\\Exploris_marco\\Documents\\QCactus_Requirements\\MSFragger-3.8.jar C:\\Users\\Exploris_marco\\Documents\\QCactus_Requirements\\fragger.params C:\\Users\\Exploris_marco\\Documents\\QCactus_Requirements\\2023_Sample_432.raw



            Process myProcess = new Process();
            myProcess.StartInfo.UseShellExecute = false;
            myProcess.StartInfo.RedirectStandardOutput = true;
            myProcess.StartInfo.CreateNoWindow = false;
            //myProcess.StartInfo.FileName = the location of java.exe

            //dev
            //myProcess.StartInfo.FileName = @"C:\Users\DwightZ\Documents\QCactus_Requirements\jdk-20.0.2\bin\java.exe";
            //prod
            //myProcess.StartInfo.FileName = @"C:\Users\Exploris_marco\Documents\QCactus_Requirements\jdk-20.0.2\bin\java.exe";
            myProcess.StartInfo.FileName = javalocation;

            myProcess.StartInfo.Arguments = finalcall;

            label21.Text = "Start: " + DateTime.Now.ToString("h: mm tt");
            //myProcess.StartInfo.Arguments = "-Xmx6G -jar C:\\Users\\DwightZ\\Desktop\\QCactus_Pub\\testingvisualstudio\\MSFragger-3.8.jar C:\\Users\\DwightZ\\Desktop\\QCactus_Pub\\testingvisualstudio\\fragger.params C:\\Users\\DwightZ\\Desktop\\QCactus_Pub\\testingvisualstudio\\2023_Sample_432.raw";
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




                MyScatterPlot2 = plt.AddScatter(xs, proteins, Color.Red, label: "Proteins");
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
            MyScatterPlot = plt.AddScatter(xs, peptides, Color.Blue, label: "Peptides");

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
                //var fileStream = new FileStream(@"C:\\Users\\DwightZ\\Desktop\\QCactus_Pub\\testingvisualstudio\\2023_Sample_432.pin", FileMode.Open, FileAccess.Read);
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

        // running a 
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


    }


    

}

