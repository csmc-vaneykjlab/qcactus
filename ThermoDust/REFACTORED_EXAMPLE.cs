using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoFisher.CommonCore.RawFileReader;

namespace ThermoDust.Examples
{
    /// <summary>
    /// This file demonstrates how to refactor existing Form1.cs methods
    /// to use the new service classes.
    ///
    /// DO NOT include this file in the project - it's for reference only.
    /// Copy the patterns shown here into your actual Form1.cs
    /// </summary>
    public partial class Form1_Refactored
    {
        // New service fields (add to Form1 constructor)
        private DataModel _dataModel;
        private AnalysisEngine _analysisEngine;
        private PlottingService _plottingService;
        private PDFReportService _pdfReportService;

        private void InitializeServices()
        {
            _dataModel = new DataModel();
            _analysisEngine = new AnalysisEngine(_dataModel);
            _plottingService = new PlottingService(_dataModel, GetOutputDirectory());
            _pdfReportService = new PDFReportService(GetOutputDirectory());
        }

        // ──────────────────────────────────────────────────────────────
        // EXAMPLE 1: File Import & Analysis (button2_Click)
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// BEFORE: Original implementation with 200+ lines of mixed logic
        ///
        /// Issues:
        /// - File processing logic mixed with UI updates
        /// - Statistics calculations embedded in file loading
        /// - Hard to test or reuse
        /// - Data stored in multiple class-level variables
        /// </summary>
        private void button2_Click_BEFORE(object sender, EventArgs e)
        {
            statsBox.Text = "Creating inventory of valid files...";

            // File size arrays for types of files 1] blanks 2] real samples 3] hela
            List<double> blankFileSizes = new List<double>();
            List<double> realFileSizes = new List<double>();
            List<double> helaFileSizes = new List<double>();

            List<string> realFileNames = new List<string>();
            List<string> blankFileNames = new List<string>();
            List<string> helaFileNames = new List<string>();

            // File processing logic (simplified from original)
            if (!Directory.Exists(folderListing.Text))
            {
                statsBox.Text = "No directory selected!";
                return;
            }

            var files = Directory.GetFiles(folderListing.Text, "*.raw");
            int processed = 0;

            foreach (var file in files)
            {
                try
                {
                    progressBar1.Value = (processed++ * 100) / files.Length;
                    var fileInfo = new FileInfo(file);
                    double fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);
                    string fileName = Path.GetFileName(file);

                    if (fileName.Contains("blank", StringComparison.OrdinalIgnoreCase))
                    {
                        blankFileSizes.Add(fileSizeMB);
                        blankFileNames.Add(fileName);
                    }
                    else if (fileName.Contains("hela", StringComparison.OrdinalIgnoreCase))
                    {
                        helaFileSizes.Add(fileSizeMB);
                        helaFileNames.Add(fileName);
                    }
                    else
                    {
                        realFileSizes.Add(fileSizeMB);
                        realFileNames.Add(fileName);
                    }
                }
                catch { }
            }

            // Now store in form-level variables
            // ... (20+ lines of variable assignments)
            bfs = blankFileSizes;
            rfs = realFileSizes;
            hfs = helaFileSizes;
            bfns = blankFileNames;
            rfns = realFileNames;
            hfns = helaFileNames;

            dataloaded = true;
            statsBox.AppendText($"\nLoaded {realFileNames.Count} real, {blankFileNames.Count} blank, {helaFileNames.Count} hela files");
        }

        /// <summary>
        /// AFTER: Refactored implementation using services
        ///
        /// Benefits:
        /// - Clear separation of concerns
        /// - Easily testable (mock AnalysisEngine)
        /// - Reusable in other applications
        /// - All data centralized in DataModel
        /// - ~20 lines of code instead of 200+
        /// </summary>
        private void button2_Click_AFTER(object sender, EventArgs e)
        {
            if (!Directory.Exists(folderListing.Text))
            {
                statsBox.Text = "Error: No directory selected!";
                return;
            }

            statsBox.Text = "Creating inventory of valid files...";

            // Use AnalysisEngine to handle all file processing
            _analysisEngine.CategorizeRawFiles(
                folderListing.Text,
                out var blankSizes,
                out var realSizes,
                out var helaSizes,
                out var blankNames,
                out var realNames,
                out var helaNames,
                out var failedFiles
            );

            // Store in DataModel (centralized)
            _dataModel.BlankFileSizes.AddRange(blankSizes);
            _dataModel.RealFileSizes.AddRange(realSizes);
            _dataModel.HelaFileSizes.AddRange(helaSizes);
            _dataModel.BlankFileNames.AddRange(blankNames);
            _dataModel.RealFileNames.AddRange(realNames);
            _dataModel.HelaFileNames.AddRange(helaNames);
            _dataModel.FailedFiles.AddRange(failedFiles);
            _dataModel.IsDataLoaded = true;

            // Update UI
            UpdateFileListBoxes(realNames, helaNames);
            statsBox.AppendText(
                $"\nLoaded {realNames.Count} real, {blankNames.Count} blank, " +
                $"{helaNames.Count} hela files"
            );

            progressBar1.Value = 100;
        }

        // ──────────────────────────────────────────────────────────────
        // EXAMPLE 2: Create Intensity Plot (CreateMAXPlot)
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// BEFORE: 100+ lines of direct plot manipulation
        /// </summary>
        private void CreateMAXPlot_BEFORE(
            List<double> bps,
            List<double> tics,
            List<string> timestamps,
            List<string> fnames)
        {
            // This is the original - 100+ lines of plot creation
            // See Form1.cs lines 655-765
        }

        /// <summary>
        /// AFTER: Simple 20-line method delegating to PlottingService
        /// </summary>
        private void CreateMaxPlot_AFTER(
            List<double> basePeaks,
            List<double> intensities,
            List<string> timestamps,
            List<string> fileNames)
        {
            // Delegate plot creation to PlottingService
            var stats = _plottingService.PlotIntensityComparison(
                plot: scanPlot.Plot,
                ms2Values: basePeaks,
                ms1Values: intensities,
                timestamps: timestamps,
                deviations: _dataModel.StandardDeviations,
                customUpperMS2: _dataModel.CustomUpperBoundMS2,
                customLowerMS2: _dataModel.CustomLowerBoundMS2,
                customUpperMS1: _dataModel.CustomUpperBoundMS1,
                customLowerMS1: _dataModel.CustomLowerBoundMS1
            );

            // Refresh plot and update UI
            scanPlot.Refresh();
            _plottingService.SavePlotImage(scanPlot.Plot, "MEANS.png");

            // Display statistics
            DisplayStatistics(fileNames, basePeaks, intensities, stats);
        }

        // ──────────────────────────────────────────────────────────────
        // EXAMPLE 3: Generate PDF Report (button5_Click)
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// BEFORE: 200+ lines of HTML/PDF generation code
        /// - HTML construction with string concatenation
        /// - PDF creation logic mixed with HTML formatting
        /// - Chart embedding logic scattered throughout
        /// </summary>
        private void button5_Click_BEFORE(object sender, EventArgs e)
        {
            // Original implementation - 200+ lines
            // See Form1.cs lines 1265-1359
        }

        /// <summary>
        /// AFTER: 30-line method using PDFReportService
        ///
        /// Much cleaner and easier to maintain!
        /// </summary>
        private void button5_Click_AFTER(object sender, EventArgs e)
        {
            try
            {
                _pdfReportService.EnsureImageDirectory();

                // Generate each section
                string statsHtml = _pdfReportService.GenerateStatisticalSummaryHtml(
                    statisticsText: statsBox.Text,
                    customThresholdsApplied: IsCustomThresholdsEnabled(),
                    customThresholdInfo: GetCustomThresholdInfo()
                );

                string fileSummaryHtml = _pdfReportService.GenerateFileSummaryHtml(
                    realFiles: _dataModel.RealFileNames,
                    blankFiles: _dataModel.BlankFileNames,
                    helaFiles: _dataModel.HelaFileNames,
                    failedFiles: _dataModel.FailedFiles
                );

                // Assemble complete report
                string pdfPath = _pdfReportService.CreateComprehensiveReport(
                    statisticsHtml: statsHtml,
                    imagePages: GetImagePages(),
                    fileSummaryHtml: fileSummaryHtml,
                    reportFileName: "QCactus_Report.pdf"
                );

                if (!string.IsNullOrEmpty(pdfPath))
                {
                    System.Diagnostics.Process.Start(pdfPath);
                    statusLabel.Text = $"Report saved: {pdfPath}";
                }
                else
                {
                    statusLabel.Text = "Error creating report";
                }
            }
            catch (Exception ex)
            {
                statsBox.Text = $"Error generating report: {ex.Message}";
            }
        }

        // ──────────────────────────────────────────────────────────────
        // EXAMPLE 4: Calculate and Display Statistics
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// BEFORE: Calculations mixed with UI display
        /// </summary>
        private void calcFileStats_BEFORE(
            List<string> fnames,
            double[] fsizes,
            double LB,
            double UB,
            double mean)
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

        /// <summary>
        /// AFTER: Separation of calculation and display
        /// </summary>
        private void CalculateAndDisplayFileStats_AFTER(
            List<string> fileNames,
            double[] fileSizes)
        {
            // Calculate statistics using service
            var (mean, stdev, lowerBound, upperBound) = _analysisEngine.CalculateStatistics(
                fileSizes,
                _dataModel.StandardDeviations,
                _dataModel.CustomLowerBoundFileSize > 0 ? _dataModel.CustomLowerBoundFileSize : null,
                _dataModel.CustomUpperBoundFileSize > 0 ? _dataModel.CustomUpperBoundFileSize : null
            );

            // Get outliers
            var outliers = _analysisEngine.GetOutlierFiles(
                fileNames,
                fileSizes,
                lowerBound,
                upperBound
            );

            // Format as string
            string report = _analysisEngine.FormatStatisticalResults(
                title: "Sample File Sizes (MB)",
                mean: mean,
                stdev: stdev,
                lowerBound: lowerBound,
                upperBound: upperBound,
                outlierFiles: outliers,
                deviations: _dataModel.StandardDeviations,
                useCustomThresholds: _dataModel.CustomLowerBoundFileSize > 0
            );

            // Display (UI responsibility)
            statsBox.Text = report;
        }

        // ──────────────────────────────────────────────────────────────
        // EXAMPLE 5: Parse MSFragger Results
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// BEFORE: File parsing logic in button handler
        /// </summary>
        private void parsePinFiles_BEFORE(string[] extrafiles)
        {
            // Original scattered logic
            // See Form1.cs lines 2431-2480
        }

        /// <summary>
        /// AFTER: Simple delegation to AnalysisEngine
        /// </summary>
        private void ParsePinFilesAfter(string[] pinFilePaths)
        {
            _analysisEngine.ParsePinFiles(pinFilePaths);

            // Display results from DataModel
            statsBox.AppendText(
                $"\nProtein IDs: {_dataModel.ProteinCounts.Sum()}\n" +
                $"Peptide IDs: {_dataModel.PeptideCounts.Sum()}"
            );
        }

        // ──────────────────────────────────────────────────────────────
        // Helper methods (would already exist or be added)
        // ──────────────────────────────────────────────────────────────

        private string GetOutputDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QCactus"
            );
        }

        private void UpdateFileListBoxes(List<string> realFiles, List<string> helaFiles)
        {
            fileList.Items.Clear();
            foreach (var file in realFiles)
                fileList.Items.Add(file);

            helaList.Items.Clear();
            foreach (var file in helaFiles)
                helaList.Items.Add(file);
        }

        private bool IsCustomThresholdsEnabled()
        {
            return _dataModel.CustomUpperBoundFileSize > 0 ||
                   _dataModel.CustomUpperBoundMS1 > 0 ||
                   _dataModel.CustomUpperBoundMS2 > 0 ||
                   _dataModel.CustomUpperBoundBasePeak > 0;
        }

        private string GetCustomThresholdInfo()
        {
            var info = new System.Text.StringBuilder();
            if (_dataModel.CustomLowerBoundFileSize > 0)
                info.AppendLine($"File Size: {_dataModel.CustomLowerBoundFileSize}-{_dataModel.CustomUpperBoundFileSize} MB");
            if (_dataModel.CustomLowerBoundMS1 > 0)
                info.AppendLine($"MS1: {_dataModel.CustomLowerBoundMS1}-{_dataModel.CustomUpperBoundMS1}");
            if (_dataModel.CustomLowerBoundMS2 > 0)
                info.AppendLine($"MS2: {_dataModel.CustomLowerBoundMS2}-{_dataModel.CustomUpperBoundMS2}");
            if (_dataModel.CustomLowerBoundBasePeak > 0)
                info.AppendLine($"Base Peak: {_dataModel.CustomLowerBoundBasePeak}-{_dataModel.CustomUpperBoundBasePeak}");
            return info.ToString();
        }

        private List<(string imageName, string title)> GetImagePages()
        {
            return new List<(string, string)>
            {
                ("MEANS.PNG", "MS1/MS2 Intensity Profile"),
                ("ALLBASEPEAKS.PNG", "Base Peak Summary"),
                ("ALLFILESIZES.PNG", "File Size Analysis"),
                ("ALLTICS.PNG", "Total Ion Chromatogram"),
                ("ALLIDS.PNG", "Protein IDs"),
                ("ALLPEPIDS.PNG", "Peptide IDs")
            };
        }

        private void DisplayStatistics(
            List<string> fileNames,
            List<double> basePeaks,
            List<double> intensities,
            PlottingService.ChartStatistics stats)
        {
            var outliers = _analysisEngine.GetOutlierFiles(
                fileNames,
                intensities.ToArray(),
                stats.LowerBound,
                stats.UpperBound
            );

            string report = _analysisEngine.FormatStatisticalResults(
                title: "MS1 Intensity Analysis",
                mean: stats.Mean,
                stdev: stats.StdDev,
                lowerBound: stats.LowerBound,
                upperBound: stats.UpperBound,
                outlierFiles: outliers,
                deviations: _dataModel.StandardDeviations,
                useCustomThresholds: _dataModel.CustomLowerBoundMS1 > 0
            );

            statsBox.AppendText("\n" + report);
        }
    }
}
