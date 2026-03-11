# Refactored Critical Methods for Form1.cs

This document contains refactored versions of 6 critical methods that have been updated to use the new service architecture (AnalysisEngine, PlottingService, PDFReportService).

---

## Method 1: button1_Click (Line 361) - TIC Extraction

**Location:** Line 361
**Category:** TIC extraction workflow
**Original:** Directly calls ExtractTICS with raw file loading logic

### Refactored Version

```csharp
//TIC BUTTON HANDLING
// - - Get the files selected and delegate TIC extraction to service
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

            // Handle multi-group file paths
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

    // DELEGATE TIC EXTRACTION TO SERVICE
    _analysisEngine.ExtractTicsFromRawFiles(rawfiles.ToArray());

    // DELEGATE PLOTTING TO SERVICE
    var ticData = _analysisEngine.GetExtractedTicData();
    if (ticData != null)
    {
        _plottingService.PlotTics(ticData.Tics, ticData.StartTimes);
    }
}
```

**Key Changes:**
- File loading and group handling remains in Form1 (UI-specific)
- Raw file array creation remains in Form1
- `ExtractTICS()` call replaced with `_analysisEngine.ExtractTicsFromRawFiles()`
- Plotting delegated to `_plottingService.PlotTics()`
- New method `_analysisEngine.GetExtractedTicData()` retrieves processed TIC data

---

## Method 2: button2_Click (Line 420) - Main File Import Workflow

**Location:** Line 420
**Category:** Main file categorization and analysis workflow
**Original:** Complex method handling folder selection, file categorization, and plot creation

### Refactored Version

```csharp
//Import all the project files via the selection button click handler
// - - Delegates file categorization and statistics formatting to services
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

    // KEEP: Folder dialog (UI-specific)
    DialogResult result = folderBrowserDialog1.ShowDialog();

    if (result == DialogResult.OK)
    {
        var targetFolder = folderBrowserDialog1.SelectedPath;
        folderListing.Text = targetFolder;
        DirectoryInfo di = new DirectoryInfo(targetFolder);

        FileInfo[] filesToExclude = di.GetFiles("*blank*");
        FileInfo[] qcfilesToExclude = di.GetFiles("*hela*");
        filesToExclude = filesToExclude.Concat(qcfilesToExclude).ToArray();

        FileInfo[] files = di.GetFiles("*.raw");

        // KEEP: UI list clearing
        fileList.Items.Clear();
        blankList.Items.Clear();

        // DELEGATE: File categorization to service
        var categorizedFiles = _analysisEngine.CategorizeRawFiles(
            files,
            filesToExclude,
            targetFolder
        );

        // Populate UI with categorized results
        blankFileSizes = categorizedFiles.BlankFileSizes;
        blankFileNames = categorizedFiles.BlankFileNames;
        btimestamps = categorizedFiles.BlankTimestamps;

        realFileSizes = categorizedFiles.RealFileSizes;
        realFileNames = categorizedFiles.RealFileNames;
        timestamps = categorizedFiles.RealTimestamps;

        helaFileSizes = categorizedFiles.HelaFileSizes;
        helaFileNames = categorizedFiles.HelaFileNames;
        htimestamps = categorizedFiles.HelaTimestamps;

        // KEEP: Update UI lists
        foreach (var file in categorizedFiles.BlankFiles)
        {
            blankList.Items.Add(file);
        }
        foreach (var file in categorizedFiles.RealFiles)
        {
            fileList.Items.Add(file);
        }
        foreach (var file in categorizedFiles.HelaFiles)
        {
            helaList.Items.Add(file);
        }

        // Store data in DataModel via service
        storeFileSizePlot(blankFileSizes, helaFileSizes, realFileSizes,
                         blankFileNames, helaFileNames, realFileNames,
                         timestamps, btimestamps, htimestamps);

        // DELEGATE: Plotting to service
        _plottingService.PlotFileSizes(blankFileSizes, helaFileSizes, realFileSizes,
                                       blankFileNames, helaFileNames, realFileNames,
                                       timestamps, btimestamps, htimestamps);

        CreateFileListBox(realFileNames, helaFileNames);

        if (realFileSizes.Count > 1)
        {
            // DELEGATE: Analysis and plotting for intensity metrics
            CreateBPTIC(realFileNames, timestamps);
            CreateMaxBaseSummary(realFileNames, timestamps);
        }

        dataloaded = true;
    }
    else if (result == DialogResult.Cancel)
    {
        return;
    }
}
```

**Key Changes:**
- Folder dialog and UI list management remain in Form1
- File iteration and categorization delegated to `_analysisEngine.CategorizeRawFiles()`
- Integrity checks moved to service layer
- File size plotting delegated to `_plottingService.PlotFileSizes()`
- Statistics display remains in Form1 (statsBox updates)

---

## Method 3: CreateMAXPlot (Line 659) - Intensity Comparison Plot

**Location:** Line 659
**Category:** Plot creation for MS1/MS2 intensity comparison
**Original:** Creates scatter plots with custom deviation lines

### Refactored Version

```csharp
//CREATE MEDIAN INTENSITY PLOTS
// - - PLOTTING - Delegates plot creation to service, keeps stats display
private void CreateMAXPlot(List<double> bps, List<double> tics, List<string> timestamps, List<string> fnames)
{
    // DELEGATE: Plot creation to service
    var plotResult = _plottingService.PlotIntensityComparison(
        bps,
        tics,
        timestamps,
        fnames,
        deviations,
        comboBox1.SelectedIndex == 3 ? custom_UB_MS2 : -1,
        comboBox1.SelectedIndex == 3 ? custom_LB_MS2 : -1,
        comboBox1.SelectedIndex == 3 ? custom_UB_MS1 : -1,
        comboBox1.SelectedIndex == 3 ? custom_LB_MS1 : -1
    );

    // KEEP: Store scatter plot references for interaction
    ScanScatterPlot = plotResult.MS2Scatter;
    ScanScatterPlot2 = plotResult.MS1Scatter;

    // KEEP: Highlighted point management for UI interaction
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

    // KEEP: Statistics display in UI
    calcIntensityStats(fnames, plotResult.MS1Values, plotResult.MS1LowerBound,
                       plotResult.MS1UpperBound, plotResult.MS1Mean, "MS1");
    calcIntensityStats(fnames, plotResult.MS2Values, plotResult.MS2LowerBound,
                       plotResult.MS2UpperBound, plotResult.MS2Mean, "MS2");
}
```

**Key Changes:**
- Entire plot creation delegated to `_plottingService.PlotIntensityComparison()`
- Custom deviation handling moved to service (accepts custom bounds as parameters)
- Scatter plot references stored for UI interaction (hover effects)
- Highlighted point management kept in Form1 (interactive UI element)
- Statistics display kept in Form1 using data from service

---

## Method 4: CreateFileSizePlot (Line 773) - File Size Comparison Plot

**Location:** Line 773
**Category:** Plot creation for file size analysis
**Original:** Creates scatter plots with datetime x-axis for multiple file categories

### Refactored Version

```csharp
private void CreateFileSizePlot(List<double> blanksizeslist, List<double> helasizeslist,
                                List<double> realsizeslist, List<string> blanknames,
                                List<string> helanames, List<string> filenames,
                                List<string> freshtime, List<string> blanktime, List<string> helatime)
{
    // DELEGATE: Plot creation to service
    var plotResult = _plottingService.PlotFileSizes(
        blanksizeslist,
        helasizeslist,
        realsizeslist,
        blanknames,
        helanames,
        filenames,
        freshtime,
        blanktime,
        helatime,
        deviations,
        comboBox1.SelectedIndex == 3 ? custom_UB_FileSize : -1,
        comboBox1.SelectedIndex == 3 ? custom_LB_FileSize : -1
    );

    // KEEP: Highlighted point management for UI interaction
    HighlightedPoint = fileSizePlot.Plot.AddPoint(0, 0);
    HighlightedPoint.Color = Color.Black;
    HighlightedPoint.MarkerSize = 20;
    HighlightedPoint.MarkerShape = ScottPlot.MarkerShape.openCircle;
    HighlightedPoint.IsVisible = false;

    fileSizePlot.Refresh();

    // KEEP: Store scatter plot reference for UI interaction
    MyScatterPlot = plotResult.RealFilesScatter;

    // KEEP: Statistics display in UI
    if (realsizeslist.Count > 1)
    {
        calcFileStats(filenames, plotResult.RealFileSizes, plotResult.RealLowerBound,
                      plotResult.RealUpperBound, plotResult.RealMean);
    }
}
```

**Key Changes:**
- Entire plot creation delegated to `_plottingService.PlotFileSizes()`
- Custom deviation bounds passed to service
- Scatter plot reference stored for UI interaction
- Highlighted point management kept in Form1
- Statistics display kept in Form1

---

## Method 5: guiUpdate (Line 1230) - Refresh All Plots Based on Checkbox State

**Location:** Line 1230
**Category:** Plot refresh workflow triggered by checkbox changes
**Original:** Conditionally calls plot methods based on checkbox states

### Refactored Version

```csharp
private void guiUpdate()
{
    List<double> dbldummy = new List<double>();
    List<string> strdummy = new List<string>();

    // DELEGATE: Plot creation based on checkbox filtering logic
    // (Checkbox filtering logic stays in Form1, plot creation delegated to service)

    if (rawCheckBox.Checked == false)
    {
        if (helaCheckBox.Checked == false)
        {
            _plottingService.PlotFileSizes(dbldummy, dbldummy, rfs,
                                          strdummy, strdummy, rfns,
                                          times, btimes, htimes,
                                          deviations,
                                          comboBox1.SelectedIndex == 3 ? custom_UB_FileSize : -1,
                                          comboBox1.SelectedIndex == 3 ? custom_LB_FileSize : -1);
        }
        else
        {
            _plottingService.PlotFileSizes(dbldummy, hfs, rfs,
                                          strdummy, hfns, rfns,
                                          times, btimes, htimes,
                                          deviations,
                                          comboBox1.SelectedIndex == 3 ? custom_UB_FileSize : -1,
                                          comboBox1.SelectedIndex == 3 ? custom_LB_FileSize : -1);
        }
    }
    else
    {
        if (helaCheckBox.Checked == false)
        {
            _plottingService.PlotFileSizes(bfs, dbldummy, rfs,
                                          bfns, strdummy, rfns,
                                          times, btimes, htimes,
                                          deviations,
                                          comboBox1.SelectedIndex == 3 ? custom_UB_FileSize : -1,
                                          comboBox1.SelectedIndex == 3 ? custom_LB_FileSize : -1);
        }
        else
        {
            _plottingService.PlotFileSizes(bfs, hfs, rfs,
                                          bfns, hfns, rfns,
                                          times, btimes, htimes,
                                          deviations,
                                          comboBox1.SelectedIndex == 3 ? custom_UB_FileSize : -1,
                                          comboBox1.SelectedIndex == 3 ? custom_LB_FileSize : -1);
        }
    }

    // DELEGATE: Intensity plot creation
    var intensityResult = _plottingService.PlotIntensityComparison(
        median_ms2s,
        median_ms1s,
        times,
        msfnames,
        deviations,
        comboBox1.SelectedIndex == 3 ? custom_UB_MS2 : -1,
        comboBox1.SelectedIndex == 3 ? custom_LB_MS2 : -1,
        comboBox1.SelectedIndex == 3 ? custom_UB_MS1 : -1,
        comboBox1.SelectedIndex == 3 ? custom_LB_MS1 : -1
    );

    // KEEP: Update scatter plot references
    ScanScatterPlot = intensityResult.MS2Scatter;
    ScanScatterPlot2 = intensityResult.MS1Scatter;

    // DELEGATE: Base peak plot creation
    _plottingService.PlotBasePeaks(max_basepeaks, times, bpfnames,
                                  deviations,
                                  comboBox1.SelectedIndex == 3 ? custom_UB_BP : -1,
                                  comboBox1.SelectedIndex == 3 ? custom_LB_BP : -1);
}
```

**Key Changes:**
- Checkbox filtering logic remains in Form1 (UI decision logic)
- All three plot creation calls delegated to `_plottingService` methods
- Custom deviation values passed based on comboBox selection
- Scatter plot references updated for interaction

---

## Method 6: aboutToolStripMenuItem_Click (Line 1431) - PDF Report Generation

**Location:** Line 1431
**Category:** PDF report generation workflow
**Original:** Creates multiple HTML reports and assembles them into a single PDF

### Refactored Version

```csharp
// GENERATE PDF REPORT
// - - Delegates HTML generation and PDF assembly to service
private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
{
    try
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        // DELEGATE: Comprehensive report generation to service
        var reportPaths = _pdfReportService.CreateComprehensiveReport(
            statsBox.Text,
            comboBox1.SelectedIndex == 3,
            lowBound.Text,
            lowMS1Text.Text,
            lowMS2Text.Text,
            lowBaseText.Text,
            fileList.Items.Cast<String>().ToList(),
            blankList.Items.Cast<String>().ToList(),
            helaList.Items.Cast<String>().ToList(),
            failedfiles
        );

        // KEEP: File dialog for save location (UI-specific)
        SaveFileDialog svg = new SaveFileDialog();
        svg.DefaultExt = "pdf";
        if (svg.ShowDialog() == DialogResult.OK)
        {
            // DELEGATE: PDF assembly to service
            _pdfReportService.AssemblePdfReport(reportPaths, svg.FileName);
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show("Error generating report: " + ex.Message);
    }
}
```

**Key Changes:**
- HTML report generation delegated to `_pdfReportService.CreateComprehensiveReport()`
- Statistics text passed directly from statsBox
- File list data extracted from UI controls and passed to service
- PDF assembly delegated to `_pdfReportService.AssemblePdfReport()`
- Save dialog remains in Form1 (UI-specific)
- Error handling improved with try-catch

---

## Supporting Changes Required in Service Layer

### PlottingService Methods to Add

```csharp
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
    // Implementation returns PlotIntensityResult with all statistical data
}

public FileSizeplotResult PlotFileSizes(
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
    // Implementation returns FileSizeplotResult with statistics
}

public void PlotTics(List<List<double>> tics, List<List<double>> startTimes)
{
    // Implementation plots TIC chromatograms
}

public void PlotBasePeaks(
    List<double> basePeaks,
    List<string> timestamps,
    List<string> filenames,
    double deviations,
    double customUB = -1,
    double customLB = -1)
{
    // Implementation creates base peak plot
}
```

### AnalysisEngine Methods to Add

```csharp
public CategorizedFilesResult CategorizeRawFiles(
    FileInfo[] allFiles,
    FileInfo[] excludeFiles,
    string folderPath)
{
    // Returns categorized files with sizes and timestamps
}

public void ExtractTicsFromRawFiles(IRawDataPlus[] rawFiles)
{
    // Extracts TIC data and stores in DataModel
}

public TicExtractionData GetExtractedTicData()
{
    // Returns the extracted TIC data
}
```

### PDFReportService Methods to Add

```csharp
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
    // Returns list of PDF file paths created
}

public void AssemblePdfReport(List<string> pdfPaths, string outputPath)
{
    // Assembles multiple PDFs into single output file
}
```

---

## Summary of Refactoring Benefits

1. **Separation of Concerns**: Business logic moved to services, UI logic stays in Form1
2. **Testability**: Services can be tested independently without UI
3. **Reusability**: Services can be used by other UI components
4. **Maintainability**: Changes to plotting/analysis logic don't affect UI code
5. **Scalability**: Easy to add new plotting methods or analysis features
6. **Data Consistency**: All data flows through DataModel via services

## Migration Checklist

- [ ] Create return types: `PlotIntensityResult`, `FileSizeplotResult`, `CategorizedFilesResult`, `TicExtractionData`
- [ ] Implement all `PlottingService` methods
- [ ] Implement all `AnalysisEngine` methods
- [ ] Implement all `PDFReportService` methods
- [ ] Replace old methods in Form1.cs with refactored versions
- [ ] Test each refactored method thoroughly
- [ ] Verify all statistics calculations match original behavior
- [ ] Verify all plots render identically to original
