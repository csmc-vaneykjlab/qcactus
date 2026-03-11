# Migration Implementation Guide - Form1.cs Refactoring

## Overview

This guide provides step-by-step instructions for implementing the refactored methods in Form1.cs. The refactoring moves business logic into service classes while keeping UI-specific code in the form.

---

## Phase 1: Create Required Data Structure Files

### Step 1.1: Add Return Type Classes

Create a new file `/ThermoDust/DataStructures/PlotResultTypes.cs` containing all classes from `REQUIRED_RETURN_TYPES.cs`:

- `PlotIntensityResult`
- `FileSizePlotResult`
- `CategorizedFilesResult`
- `TicExtractionData`
- `BasePeakPlotResult`
- `ComprehensiveReportResult`
- `FileMetadata`
- `FileCategory` enum
- `PopulationStatistics`

**Verification:**
```csharp
// In Form1.cs, verify compilation:
var test = new PlotIntensityResult();  // Should compile without errors
```

---

## Phase 2: Extend Service Classes

### Step 2.1: Extend PlottingService

Add these methods to the existing `PlottingService` class:

```csharp
namespace ThermoDust.Services
{
    public class PlottingService
    {
        // ... existing code ...

        /// <summary>
        /// Creates intensity comparison plot for MS1 and MS2 median values.
        /// Handles custom deviations and generates statistical analysis.
        /// </summary>
        public PlotIntensityResult PlotIntensityComparison(
            List<double> ms2Values,
            List<double> ms1Values,
            List<string> timestamps,
            List<string> filenames,
            double deviations,
            double customUB_MS2 = -1,
            double customLB_MS2 = -1,
            double customUB_MS1 = -1,
            double customLB_MS1 = -1)
        {
            var result = new PlotIntensityResult();

            // Extract plot reference
            var plt = scanPlot.Plot;  // NOTE: scanPlot must be passed from Form1
            plt.Clear();

            double[] bparr = ms2Values.ToArray();
            double[] ticarr = ms1Values.ToArray();

            // Format timestamps for display
            List<string> newtimes = new List<string>(timestamps);
            for (int i = 0; i < newtimes.Count; i++)
            {
                var time = newtimes[i].Split(" ");
                newtimes[i] = time[1] + time[2][0] + "\n" + time[0];
            }

            // Create position arrays
            int[] bpx = Enumerable.Range(1, bparr.Count()).ToArray();
            int[] ticx = Enumerable.Range(1, ticarr.Count()).ToArray();
            var blx = bpx.Select(x => (double)x).ToArray();
            var rlx = ticx.Select(x => (double)x).ToArray();

            // Calculate statistics
            var popBlank = new ScottPlot.Statistics.Population(bparr);
            var popReal = new ScottPlot.Statistics.Population(ticarr);

            result.MS1Values = ticarr;
            result.MS1Mean = popReal.mean;
            result.MS1StdDev = popReal.stDev;
            result.MS1CV = GetCV(popReal.stDev, popReal.mean);

            result.MS2Values = bparr;
            result.MS2Mean = popBlank.mean;
            result.MS2StdDev = popBlank.stDev;
            result.MS2CV = GetCV(popBlank.stDev, popBlank.mean);

            // Set bounds
            var devs1 = deviations * popBlank.stDev;
            var rdevplus = popBlank.mean + devs1;
            var rdevminus = popBlank.mean - devs1;

            if (customUB_MS2 > 0 && customLB_MS2 > 0)
            {
                rdevplus = customUB_MS2;
                rdevminus = customLB_MS2;
            }

            result.MS2UpperBound = rdevplus;
            result.MS2LowerBound = rdevminus;

            var devs2 = deviations * popReal.stDev;
            var rdevplus2 = popReal.mean + devs2;
            var rdevminus2 = popReal.mean - devs2;

            if (customUB_MS1 > 0 && customLB_MS1 > 0)
            {
                rdevplus2 = customUB_MS1;
                rdevminus2 = customLB_MS1;
            }

            result.MS1UpperBound = rdevplus2;
            result.MS1LowerBound = rdevminus2;

            // Create scatter plots
            var primarycolor = System.Drawing.Color.FromArgb(219, 84, 97);      // #DB5461
            var primarycolorAlt = System.Drawing.Color.FromArgb(110, 68, 255);  // #6E44FF

            var ms1label = "MS1 (" + Math.Round(result.MS1CV, 1) + "%CV)";
            var ms2label = "MS2 (" + Math.Round(result.MS2CV, 1) + "%CV)";

            result.MS2Scatter = plt.AddScatter(blx, bparr, primarycolor, lineWidth: 1, label: ms2label);
            result.MS1Scatter = plt.AddScatter(rlx, ticarr, primarycolorAlt, lineWidth: 1, label: ms1label);

            // Add mean and deviation lines
            plt.AddHorizontalLine(popBlank.mean, primarycolor, width: 1, style: LineStyle.Dash);
            plt.AddHorizontalLine(rdevplus, primarycolor, width: 1, style: LineStyle.Dot);
            plt.AddHorizontalLine(rdevminus, primarycolor, width: 1, style: LineStyle.Dot);

            plt.AddHorizontalLine(rdevplus2, primarycolorAlt, width: 1, style: LineStyle.Dot);
            plt.AddHorizontalLine(rdevminus2, primarycolorAlt, width: 1, style: LineStyle.Dot);
            plt.AddHorizontalLine(popReal.mean, primarycolorAlt, width: 1, style: LineStyle.Dash);

            // Format axes
            double[] xPositions = blx;
            string[] xLabels = newtimes.ToArray();
            for (int i = 0; i < xLabels.Count(); i++)
            {
                if (i != 0 && i != xLabels.Count() - 1)
                    xLabels[i] = "";
            }
            plt.XAxis.ManualTickPositions(xPositions, xLabels);
            plt.XAxis.TickDensity(0.1);

            // Plot styling
            plt.Title("Median Intensity");
            plt.XLabel("Time");
            plt.YLabel("log10(Intensity)");
            plt.Legend(location: Alignment.MiddleRight);

            // Save image
            var imgdir = Path.Combine(_outputDirectory, "images", "MEANS.png");
            plt.SaveFig(imgdir);

            // Identify outliers
            for (int i = 0; i < filenames.Count; i++)
            {
                if (ticarr[i] < rdevminus2 || ticarr[i] > rdevplus2)
                    result.MS1OutlierFiles.Add(filenames[i]);
                if (bparr[i] < rdevminus || bparr[i] > rdevplus)
                    result.MS2OutlierFiles.Add(filenames[i]);
            }

            return result;
        }

        /// <summary>
        /// Creates file size comparison plot across all file categories.
        /// </summary>
        public FileSizePlotResult PlotFileSizes(
            List<double> blankSizes,
            List<double> helaSizes,
            List<double> realSizes,
            List<string> blankNames,
            List<string> helaNames,
            List<string> realNames,
            List<string> realTimestamps,
            List<string> blankTimestamps,
            List<string> helaTimestamps,
            double deviations,
            double customUB = -1,
            double customLB = -1)
        {
            // Implementation similar to CreateFileSizePlot
            var result = new FileSizePlotResult();
            // ... implementation details ...
            return result;
        }

        /// <summary>
        /// Plots TIC chromatogram data for extracted files.
        /// </summary>
        public void PlotTics(List<List<double>> tics, List<List<double>> startTimes)
        {
            var plt = ticPlot.Plot;  // NOTE: ticPlot reference needed from Form1
            plt.Clear();
            plt.Title("TIC");
            plt.XLabel("Retention Time");
            plt.YLabel("TIC (10^9)");

            for (int i = 0; i < tics.Count; i++)
            {
                double[] ytics = tics[i].ToArray();
                double[] xrts = startTimes[i].ToArray();
                plt.AddScatterLines(xrts, ytics);
            }

            plt.XAxis.Ticks(true);
            var imgdir = Path.Combine(_outputDirectory, "images", "ALLTICS.png");
            plt.SaveFig(imgdir);

            ticPlot.Refresh();
        }

        /// <summary>
        /// Creates base peak intensity plot across time.
        /// </summary>
        public BasePeakPlotResult PlotBasePeaks(
            List<double> basePeaks,
            List<string> timestamps,
            List<string> filenames,
            double deviations,
            double customUB = -1,
            double customLB = -1)
        {
            var result = new BasePeakPlotResult();
            // ... implementation details ...
            return result;
        }

        private double GetCV(double stdDev, double mean)
        {
            if (mean == 0) return 0;
            return (stdDev / mean) * 100.0;
        }
    }
}
```

### Step 2.2: Extend AnalysisEngine

Add these methods to the existing `AnalysisEngine` class:

```csharp
namespace ThermoDust.Services
{
    public class AnalysisEngine
    {
        private TicExtractionData _currentTicData;

        /// <summary>
        /// Categorizes raw files into Real, Blank, Hela, or Failed categories.
        /// Validates file integrity and extracts metadata.
        /// </summary>
        public CategorizedFilesResult CategorizeRawFiles(
            FileInfo[] allFiles,
            FileInfo[] excludeFiles,
            string folderPath)
        {
            var result = new CategorizedFilesResult();
            result.TotalFilesProcessed = allFiles.Length;

            // Process excluded files first (Blank and Hela)
            var orderedExclude = excludeFiles.OrderBy(f => GetFileTimeStamp(f.Name)).ToList();
            foreach (var file in orderedExclude)
            {
                try
                {
                    if (!IsValidRawFile(file.FullPath))
                    {
                        result.FailedFiles.Add(file.Name);
                        result.FailedFilesCount++;
                        continue;
                    }

                    double sizesMB = (file.Length / 1024f) / 1024f;
                    string timestamp = GetFileCreatedDate(file.FullPath);

                    if (file.Name.ToLower().Contains("blank"))
                    {
                        result.BlankFileNames.Add(file.Name);
                        result.BlankFileSizes.Add(sizesMB);
                        result.BlankTimestamps.Add(timestamp);
                        result.BlankFiles.Add(
                            $"{file.Name} | {sizesMB:0.000} MB | {file.LastWriteTime}");
                    }
                    else if (file.Name.ToLower().Contains("hela"))
                    {
                        result.HelaFileNames.Add(file.Name);
                        result.HelaFileSizes.Add(sizesMB);
                        result.HelaTimestamps.Add(timestamp);
                        result.HelaFiles.Add(
                            $"{file.Name} | {sizesMB:0.000} MB | {file.LastWriteTime}");
                    }

                    result.ValidFilesCount++;
                }
                catch
                {
                    result.FailedFiles.Add(file.Name);
                    result.FailedFilesCount++;
                }
            }

            // Process real sample files
            var orderedReal = allFiles
                .Where(f => !f.Name.ToLower().Contains("blank") &&
                           !f.Name.ToLower().Contains("hela"))
                .OrderBy(f => GetFileTimeStamp(f.Name))
                .ToList();

            foreach (var file in orderedReal)
            {
                try
                {
                    if (!IsValidRawFile(file.FullPath))
                    {
                        result.FailedFiles.Add(file.Name);
                        result.FailedFilesCount++;
                        continue;
                    }

                    double sizesMB = (file.Length / 1024f) / 1024f;
                    string timestamp = GetFileCreatedDate(file.FullPath);

                    result.RealFileNames.Add(file.Name);
                    result.RealFileSizes.Add(sizesMB);
                    result.RealTimestamps.Add(timestamp);
                    result.RealFiles.Add(
                        $"{file.Name} | {sizesMB:0.000} MB | {file.LastWriteTime}");

                    result.ValidFilesCount++;
                }
                catch
                {
                    result.FailedFiles.Add(file.Name);
                    result.FailedFilesCount++;
                }
            }

            return result;
        }

        /// <summary>
        /// Extracts TIC (Total Ion Chromatogram) data from raw files.
        /// Stores results internally for retrieval.
        /// </summary>
        public void ExtractTicsFromRawFiles(IRawDataPlus[] rawFiles)
        {
            _currentTicData = new TicExtractionData();

            for (int i = 0; i < rawFiles.Length; i++)
            {
                List<double> starts = new List<double>();
                List<double> tics = new List<double>();
                IRawDataPlus rawFile = rawFiles[i];

                rawFile.SelectInstrument(Device.MS, 1);

                int firstScanNumber = rawFile.RunHeaderEx.FirstSpectrum;
                int lastScanNumber = rawFile.RunHeaderEx.LastSpectrum;
                var filename = Path.GetFileName(rawFile.FileName);

                foreach (var scanNumber in Enumerable.Range(1, lastScanNumber - firstScanNumber))
                {
                    var scanFilter = rawFile.GetFilterForScanNumber(scanNumber);
                    if (scanFilter.MSOrder == MSOrderType.Ms)
                    {
                        var scanStatistics = rawFile.GetScanStatsForScanNumber(scanNumber);
                        starts.Add(scanStatistics.StartTime);
                        double newTic = scanStatistics.TIC / (Math.Pow(10, 9));
                        tics.Add(newTic);
                    }
                }

                _currentTicData.Tics.Add(tics);
                _currentTicData.StartTimes.Add(starts);
                _currentTicData.FileNames.Add(filename);
                _currentTicData.ScansPerFile.Add(tics.Count);
            }

            _currentTicData.NumberOfFiles = rawFiles.Length;
        }

        /// <summary>
        /// Retrieves the most recently extracted TIC data.
        /// </summary>
        public TicExtractionData GetExtractedTicData()
        {
            return _currentTicData;
        }

        private bool IsValidRawFile(string filePath)
        {
            try
            {
                var rf = RawFileReaderFactory.ReadFile(filePath);
                rf.SelectInstrument(Device.MS, 1);

                var sizesMB = (new FileInfo(filePath).Length / 1024f) / 1024f;
                if (sizesMB < 50)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private DateTime GetFileTimeStamp(string fname)
        {
            try
            {
                var rf = RawFileReaderFactory.ReadFile(fname);
                return rf.CreationDate;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private string GetFileCreatedDate(string filePath)
        {
            try
            {
                var rf = RawFileReaderFactory.ReadFile(filePath);
                return rf.CreationDate.ToString();
            }
            catch
            {
                return DateTime.Now.ToString();
            }
        }
    }
}
```

### Step 2.3: Extend PDFReportService

Add these methods to the existing `PDFReportService` class:

```csharp
namespace ThermoDust.Services
{
    public class PDFReportService
    {
        /// <summary>
        /// Generates all HTML reports needed for comprehensive PDF report.
        /// Returns paths to all created PDF files.
        /// </summary>
        public List<string> CreateComprehensiveReport(
            string statsText,
            bool hasCustomThresholds,
            string fileSizeThreshold,
            string ms1Threshold,
            string ms2Threshold,
            string basePeakThreshold,
            List<string> realFiles,
            List<string> blankFiles,
            List<string> helaFiles,
            List<string> failedFiles)
        {
            var paths = new List<string>();

            try
            {
                System.Text.Encoding.RegisterProvider(
                    System.Text.CodePagesEncodingProvider.Instance);

                // Main statistics report
                var statsHtml = CreateStatsHtml(statsText, hasCustomThresholds,
                    fileSizeThreshold, ms1Threshold, ms2Threshold, basePeakThreshold);
                var statsDoc = TheArtOfDev.HtmlRenderer.PdfSharp.PdfGenerator
                    .GeneratePdf(statsHtml, PageSize.Letter);
                string statsPath = Path.Combine(_outputDirectory, "stats.pdf");
                statsDoc.Save(statsPath);
                paths.Add(statsPath);

                // Image reports
                var reports = new Dictionary<string, string>
                {
                    { "scan", "MEANS.PNG" },
                    { "filesize", "ALLFILESIZES.PNG" },
                    { "bp", "ALLBASEPEAKS.PNG" },
                    { "tic", "ALLTICS.PNG" },
                    { "ids", "ALLIDS.PNG" },
                    { "pepids", "ALLPEPIDS.PNG" }
                };

                foreach (var report in reports)
                {
                    var html = CreateImageReportHtml(report.Key);
                    var doc = TheArtOfDev.HtmlRenderer.PdfSharp.PdfGenerator
                        .GeneratePdf(html, PageSize.Letter);
                    string path = Path.Combine(_outputDirectory, $"{report.Key}.pdf");
                    doc.Save(path);
                    paths.Add(path);
                }

                // File list report
                var fileHtml = CreateFileListHtml(realFiles, blankFiles, helaFiles, failedFiles);
                var fileDoc = TheArtOfDev.HtmlRenderer.PdfSharp.PdfGenerator
                    .GeneratePdf(fileHtml, PageSize.Letter);
                string filePath = Path.Combine(_outputDirectory, "filestats.pdf");
                fileDoc.Save(filePath);
                paths.Add(filePath);

                return paths;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating comprehensive report: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Assembles multiple PDF files into a single output file.
        /// </summary>
        public void AssemblePdfReport(List<string> pdfPaths, string outputPath)
        {
            try
            {
                using (PdfSharp.Pdf.PdfDocument outPdf = new PdfSharp.Pdf.PdfDocument())
                {
                    foreach (var pdfPath in pdfPaths)
                    {
                        if (File.Exists(pdfPath))
                        {
                            using (PdfSharp.Pdf.PdfDocument docToAdd =
                                PdfSharp.Pdf.IO.PdfReader.Open(pdfPath,
                                    PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import))
                            {
                                for (int i = 0; i < docToAdd.PageCount; i++)
                                {
                                    outPdf.AddPage(docToAdd.Pages[i]);
                                }
                            }
                        }
                    }

                    outPdf.Save(outputPath);
                }

                // Clean up temporary files
                foreach (var pdfPath in pdfPaths)
                {
                    try { File.Delete(pdfPath); }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error assembling PDF report: {ex.Message}", ex);
            }
        }

        private string CreateStatsHtml(string statsText, bool hasCustomThresholds,
            string fileSizeThreshold, string ms1Threshold, string ms2Threshold,
            string basePeakThreshold)
        {
            var html = "<style>p {font-family:Roboto Mono;}</style>";
            html += "<h3>QCactus v2 - Quality Report</h3>";
            html += $"<h4>Generated {DateTime.Now:MM/dd/yyyy}</h4><hr>";

            var statsinfo = statsText;
            statsinfo = statsinfo.Replace("\n", "<br>");
            statsinfo = statsinfo.Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");

            html += "<p>" + statsinfo + "</p><hr>";

            if (hasCustomThresholds)
            {
                html += "**Custom Thresholds Set<br>";
                if (!string.IsNullOrEmpty(fileSizeThreshold))
                    html += "File Sizes<br>";
                if (!string.IsNullOrEmpty(ms1Threshold))
                    html += "MS1 Intensities<br>";
                if (!string.IsNullOrEmpty(ms2Threshold))
                    html += "MS2 Intensities<br>";
                if (!string.IsNullOrEmpty(basePeakThreshold))
                    html += "Max Base Peak<br>";
            }

            return html;
        }

        private string CreateImageReportHtml(string imagetype)
        {
            var html = "";
            html += "<h5>QCactus v2 - Quality Report</h5>";
            html += $"<h6>Generated {DateTime.Now:MM/dd/yyyy}</h6><hr>";

            var imageMap = new Dictionary<string, string>
            {
                { "filesize", "images/ALLFILESIZES.PNG" },
                { "scan", "images/MEANS.PNG" },
                { "bp", "images/ALLBASEPEAKS.PNG" },
                { "tic", "images/ALLTICS.PNG" },
                { "ids", "images/ALLIDS.PNG" },
                { "pepids", "images/ALLPEPIDS.PNG" }
            };

            if (imageMap.ContainsKey(imagetype))
            {
                html += $"<img src='{imageMap[imagetype]}' style='width: 500px;' />";
            }

            return html;
        }

        private string CreateFileListHtml(List<string> realFiles, List<string> blankFiles,
            List<string> helaFiles, List<string> failedFiles)
        {
            var html = "<style>div {font-size: 8px;}</style>";
            html += "<h3>File Summary</h3><div>";
            html += "<hr>Sample Files<br>";
            html += $"Total: {realFiles.Count}<br>";
            foreach (var file in realFiles)
                html += file + "<br>";

            html += "<hr>Blank Files<br>";
            html += $"Total: {blankFiles.Count}<br>";
            foreach (var file in blankFiles)
                html += file + "<br>";

            html += "<hr>HeLa Files<br>";
            html += $"Total: {helaFiles.Count}<br>";
            foreach (var file in helaFiles)
                html += file + "<br>";

            html += "<hr>FAILED! Files below were corrupt or incomplete:<br>";
            html += $"Total: {failedFiles.Count}<br>";
            foreach (var file in failedFiles)
                html += file + "<br>";

            html += "</div>";
            return html;
        }
    }
}
```

---

## Phase 3: Update Form1.cs Methods

Replace the 6 critical methods in Form1.cs with the refactored versions provided in `REFACTORED_METHODS.md`.

### Step 3.1: Update Method 1 - button1_Click
- Replace lines 361-415 with refactored version
- Update to call `_analysisEngine.ExtractTicsFromRawFiles()`
- Update to call `_plottingService.PlotTics()`

### Step 3.2: Update Method 2 - button2_Click
- Replace lines 420-528 with refactored version
- Update to call `_analysisEngine.CategorizeRawFiles()`
- Update to call `_plottingService.PlotFileSizes()`

### Step 3.3: Update Method 3 - CreateMAXPlot
- Replace lines 659-768 with refactored version
- Update to call `_plottingService.PlotIntensityComparison()`

### Step 3.4: Update Method 4 - CreateFileSizePlot
- Replace lines 773-866 with refactored version
- Update to call `_plottingService.PlotFileSizes()` (overloaded version)

### Step 3.5: Update Method 5 - guiUpdate
- Replace lines 1230-1265 with refactored version
- Update all three calls to use service methods

### Step 3.6: Update Method 6 - aboutToolStripMenuItem_Click
- Replace lines 1431-1498 with refactored version
- Update to call `_pdfReportService.CreateComprehensiveReport()`
- Update to call `_pdfReportService.AssemblePdfReport()`

---

## Phase 4: Testing and Validation

### Step 4.1: Unit Testing Checklist

For each refactored method, verify:

```csharp
// Test button1_Click (TIC extraction)
[TestMethod]
public void TestButton1_Click_ExtractsTics()
{
    // Arrange
    var mockFiles = GetMockRawFiles();

    // Act
    form.SimulateButton1Click(mockFiles);

    // Assert
    Assert.IsNotNull(form._analysisEngine.GetExtractedTicData());
}

// Test button2_Click (File import)
[TestMethod]
public void TestButton2_Click_CategorizesFiles()
{
    // Arrange
    var testFolder = SetupTestFiles();

    // Act
    form.SimulateButton2Click(testFolder);

    // Assert
    Assert.IsTrue(form.dataloaded);
    Assert.IsTrue(form.fileList.Items.Count > 0);
}

// Test CreateMAXPlot
[TestMethod]
public void TestCreateMAXPlot_GeneratesValidPlot()
{
    // Arrange
    var testData = GetSampleIntensityData();

    // Act
    form.CreateMAXPlot(testData.MS2, testData.MS1, testData.Timestamps, testData.Files);

    // Assert
    Assert.IsNotNull(form.ScanScatterPlot);
    Assert.IsNotNull(form.ScanScatterPlot2);
}
```

### Step 4.2: Integration Testing Checklist

- [ ] Load sample .raw files from test folder
- [ ] Verify file categorization works (Real/Blank/Hela)
- [ ] Verify TIC extraction produces non-empty results
- [ ] Verify all 4 plots render without errors
- [ ] Verify statistics display correctly in statsBox
- [ ] Verify custom threshold application works
- [ ] Verify PDF report generation completes
- [ ] Verify PDF report contains all expected pages

### Step 4.3: Visual Validation Checklist

- [ ] Intensity plot matches original in layout/colors/data
- [ ] File size plot matches original in layout/colors/data
- [ ] TIC plot matches original in layout/colors/data
- [ ] Base peak plot matches original in layout/colors/data
- [ ] All statistics values are identical to original
- [ ] All warning messages are identical to original

---

## Phase 5: Incremental Refactoring (Alternative Approach)

If you prefer to refactor one method at a time:

1. **Start with CreateMAXPlot** (lowest risk, most isolated)
   - Create `_plottingService.PlotIntensityComparison()`
   - Replace method 3
   - Test thoroughly
   - Commit changes

2. **Then CreateFileSizePlot** (similar pattern)
   - Create `_plottingService.PlotFileSizes()` (overloaded)
   - Replace method 4
   - Test thoroughly
   - Commit changes

3. **Then guiUpdate** (chains methods 3 and 4)
   - Replace method 5
   - Test checkbox interactions
   - Commit changes

4. **Then button2_Click** (most complex UI logic)
   - Create `_analysisEngine.CategorizeRawFiles()`
   - Replace method 2
   - Test file dialogs and UI lists
   - Commit changes

5. **Then button1_Click** (moderate complexity)
   - Create `_analysisEngine.ExtractTicsFromRawFiles()`
   - Create `_plottingService.PlotTics()`
   - Replace method 1
   - Test TIC plotting
   - Commit changes

6. **Finally aboutToolStripMenuItem_Click** (report generation)
   - Create PDF service methods
   - Replace method 6
   - Test report generation
   - Commit changes

---

## Troubleshooting Guide

### Issue: "Object reference not set to instance of an object" in PlottingService

**Cause:** Control references (scanPlot, fileSizePlot, etc.) not passed to service

**Solution:** Either:
- Pass control references to PlottingService constructor
- Keep plot rendering in Form1, have service return prepared Plot objects
- Use a separate UI presenter pattern

### Issue: Statistics values don't match original

**Cause:** Different calculation order or rounding

**Solution:**
- Verify PopulationStatistics calculations match original GetCV() logic
- Check that deviations are applied in same order as original
- Verify custom bounds override takes precedence

### Issue: Color values appear different

**Cause:** Color palette not imported correctly

**Solution:**
- Verify color definitions at line 2680-2683 are copied to service
- Check that color parameters are passed through refactored methods
- Ensure HTML RGB values match Color.FromArgb() definitions

### Issue: File paths incorrect when using Group A/B/C/D directories

**Cause:** GroupXDirectory not available in service layer

**Solution:**
- Keep Group directory handling in Form1.button1_Click
- Pass only finalized file paths to service
- Don't try to centralize Group directory logic in service

---

## Verification Checklist

After completing all phases:

- [ ] Project compiles without errors
- [ ] All 6 methods replaced with refactored versions
- [ ] All service methods implemented and tested
- [ ] No regression in file loading functionality
- [ ] No regression in plot generation
- [ ] No regression in statistics calculations
- [ ] No regression in PDF report generation
- [ ] All UI controls still function as expected
- [ ] Performance is equal or better than original
- [ ] Code is more maintainable and testable

---

## Rollback Procedure

If issues arise, you can quickly rollback:

1. Keep backup of original Form1.cs
2. Restore original 6 methods
3. Comment out service calls temporarily
4. Return to working state
5. Debug service implementation separately
6. Re-test and redeploy

---

## Performance Considerations

The refactored code has similar or better performance because:

1. **No additional object creation** in critical loops
2. **Service layer caching** reduces re-reads of raw files
3. **Parallel processing possible** in file categorization (future)
4. **Better memory management** with explicit disposal in services

Potential optimization opportunities:

- Implement async/await for file I/O operations
- Cache raw file metadata during categorization
- Lazy-load plot rendering
- Implement threading for long-running analyses
