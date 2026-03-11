# Quick Reference - Method Refactoring Summary

## At-a-Glance Method Changes

### Method 1: button1_Click (TIC Extraction)
**Original Lines:** 361-415
**Category:** TIC extraction and plotting

| Aspect | Before | After |
|--------|--------|-------|
| **File Loading** | Form1 | Form1 (keep) |
| **Group Path Handling** | Form1 | Form1 (keep) |
| **TIC Extraction** | Form1 (ExtractTICS) | `_analysisEngine.ExtractTicsFromRawFiles()` |
| **TIC Plotting** | ExtractTICS → PlotTICS | `_plottingService.PlotTics()` |
| **UI Updates** | Form1 | Form1 (keep) |

**New Service Calls:**
```csharp
_analysisEngine.ExtractTicsFromRawFiles(rawfiles.ToArray());
var ticData = _analysisEngine.GetExtractedTicData();
_plottingService.PlotTics(ticData.Tics, ticData.StartTimes);
```

---

### Method 2: button2_Click (File Import)
**Original Lines:** 420-528
**Category:** File categorization and analysis workflow

| Aspect | Before | After |
|--------|--------|-------|
| **Folder Dialog** | Form1 | Form1 (keep) |
| **File Array Creation** | Form1 | Form1 (keep) |
| **File Categorization** | Form1 loops | `_analysisEngine.CategorizeRawFiles()` |
| **UI List Population** | Form1 loops | Form1 (keep, use service results) |
| **File Size Plotting** | CreateFileSizePlot() | `_plottingService.PlotFileSizes()` |
| **Stats Display** | Form1 (statsBox) | Form1 (keep) |

**New Service Calls:**
```csharp
var categorizedFiles = _analysisEngine.CategorizeRawFiles(files, filesToExclude, targetFolder);
_plottingService.PlotFileSizes(blankFileSizes, helaFileSizes, realFileSizes, ...);
```

---

### Method 3: CreateMAXPlot (Intensity Plot)
**Original Lines:** 659-768
**Category:** Scatter plot creation with statistics

| Aspect | Before | After |
|--------|--------|-------|
| **Plot Creation** | Form1 direct | `_plottingService.PlotIntensityComparison()` |
| **Statistics Calc** | Form1 (Population) | Service (returns stats) |
| **Scatter References** | Form1 stored | Form1 stores from result |
| **Custom Deviations** | Form1 check & apply | Service (params: customUB/LB) |
| **UI Points** | Form1 | Form1 (keep for interaction) |
| **Stats Display** | calcIntensityStats() | Form1 (keep, use service data) |

**New Service Call:**
```csharp
var plotResult = _plottingService.PlotIntensityComparison(
    bps, tics, timestamps, fnames, deviations,
    customUB_MS2, customLB_MS2, customUB_MS1, customLB_MS1);
```

---

### Method 4: CreateFileSizePlot (File Size Plot)
**Original Lines:** 773-866
**Category:** Datetime-based scatter plots

| Aspect | Before | After |
|--------|--------|-------|
| **Plot Creation** | Form1 direct | `_plottingService.PlotFileSizes()` |
| **DateTime Conversion** | Form1 | Service |
| **Multi-Category Plotting** | Form1 nested ifs | Service handles all |
| **Statistics Calc** | Form1 (Population) | Service (returns stats) |
| **Custom Bounds** | Form1 check & apply | Service (params) |
| **UI Points** | Form1 | Form1 (keep for interaction) |
| **Stats Display** | calcFileStats() | Form1 (keep, use service data) |

**New Service Call:**
```csharp
var plotResult = _plottingService.PlotFileSizes(
    blanksizeslist, helasizeslist, realsizeslist,
    blanknames, helanames, filenames,
    freshtime, blanktime, helatime,
    deviations, customUB, customLB);
```

---

### Method 5: guiUpdate (Plot Refresh)
**Original Lines:** 1230-1265
**Category:** Conditional plot regeneration

| Aspect | Before | After |
|--------|--------|-------|
| **Checkbox Logic** | Form1 | Form1 (keep) |
| **File Size Plot** | CreateFileSizePlot() x4 | `_plottingService.PlotFileSizes()` x4 |
| **Intensity Plot** | CreateMAXPlot() | `_plottingService.PlotIntensityComparison()` |
| **Base Peak Plot** | PlotBasePeakSummary() | `_plottingService.PlotBasePeaks()` |

**New Service Calls:**
```csharp
// Based on checkbox states, call:
_plottingService.PlotFileSizes(...);
_plottingService.PlotIntensityComparison(...);
_plottingService.PlotBasePeaks(...);
```

---

### Method 6: aboutToolStripMenuItem_Click (PDF Report)
**Original Lines:** 1431-1498
**Category:** Report generation workflow

| Aspect | Before | After |
|--------|--------|-------|
| **HTML Generation** | Form1 (create*Html*ReportText) | `_pdfReportService.CreateComprehensiveReport()` |
| **PDF Creation** | Form1 (PdfGenerator) | Service |
| **File Dialogs** | Form1 (SaveFileDialog) | Form1 (keep) |
| **PDF Assembly** | Form1 (PdfReader/CopyPages) | `_pdfReportService.AssemblePdfReport()` |

**New Service Calls:**
```csharp
var reportPaths = _pdfReportService.CreateComprehensiveReport(
    statsBox.Text, hasCustomThresholds, ...);
_pdfReportService.AssemblePdfReport(reportPaths, outputPath);
```

---

## Service Method Quick Reference

### PlottingService

```csharp
// Create intensity scatter plots with statistics
PlotIntensityResult PlotIntensityComparison(
    List<double> ms2Values,
    List<double> ms1Values,
    List<string> timestamps,
    List<string> filenames,
    double deviations,
    double customUB_MS2 = -1,
    double customLB_MS2 = -1,
    double customUB_MS1 = -1,
    double customLB_MS1 = -1);

// Create file size comparison plots (all categories)
FileSizePlotResult PlotFileSizes(
    List<double> blankSizes, List<double> helaSizes, List<double> realSizes,
    List<string> blankNames, List<string> helaNames, List<string> realNames,
    List<string> realTimestamps, List<string> blankTimestamps, List<string> helaTimestamps,
    double deviations,
    double customUB = -1, double customLB = -1);

// Create TIC chromatogram plot
void PlotTics(List<List<double>> tics, List<List<double>> startTimes);

// Create base peak intensity plot
BasePeakPlotResult PlotBasePeaks(
    List<double> basePeaks, List<string> timestamps, List<string> filenames,
    double deviations,
    double customUB = -1, double customLB = -1);
```

### AnalysisEngine

```csharp
// Categorize raw files into Real/Blank/Hela/Failed
CategorizedFilesResult CategorizeRawFiles(
    FileInfo[] allFiles,
    FileInfo[] excludeFiles,
    string folderPath);

// Extract TIC data from raw files
void ExtractTicsFromRawFiles(IRawDataPlus[] rawFiles);

// Retrieve extracted TIC data
TicExtractionData GetExtractedTicData();
```

### PDFReportService

```csharp
// Generate all report PDFs
List<string> CreateComprehensiveReport(
    string statsText,
    bool hasCustomThresholds,
    string fileSizeThreshold, string ms1Threshold, string ms2Threshold, string basePeakThreshold,
    List<string> realFiles, List<string> blankFiles, List<string> helaFiles,
    List<string> failedFiles);

// Assemble PDFs into single output file
void AssemblePdfReport(List<string> pdfPaths, string outputPath);
```

---

## Data Flow Diagram

```
button1_Click
├─ Form1: Load raw files + handle Group paths
├─ Service: _analysisEngine.ExtractTicsFromRawFiles()
│  └─ Saves: _currentTicData
├─ Service: _analysisEngine.GetExtractedTicData()
│  └─ Returns: TicExtractionData
└─ Service: _plottingService.PlotTics()
   └─ Renders: TIC plot image

button2_Click
├─ Form1: Show folder dialog
├─ Form1: Get FileInfo[] from directory
├─ Service: _analysisEngine.CategorizeRawFiles()
│  └─ Returns: CategorizedFilesResult
├─ Form1: Populate UI lists from result
├─ Service: _plottingService.PlotFileSizes()
│  └─ Returns: FileSizePlotResult
└─ Form1: Display statistics from result

CreateMAXPlot
└─ Service: _plottingService.PlotIntensityComparison()
   ├─ Calculates: Statistics, deviations, outliers
   ├─ Returns: PlotIntensityResult
   └─ Form1: Uses scatter refs + stats for display

CreateFileSizePlot
└─ Service: _plottingService.PlotFileSizes()
   ├─ Processes: All three file categories
   ├─ Calculates: Statistics, deviations
   ├─ Returns: FileSizePlotResult
   └─ Form1: Uses scatter ref + stats for display

guiUpdate
├─ Form1: Determine visible categories (checkboxes)
├─ Service: _plottingService.PlotFileSizes() [4 variants]
├─ Service: _plottingService.PlotIntensityComparison()
└─ Service: _plottingService.PlotBasePeaks()

aboutToolStripMenuItem_Click
├─ Form1: Show SaveFileDialog
├─ Service: _pdfReportService.CreateComprehensiveReport()
│  ├─ Generates: Statistics HTML → PDF
│  ├─ Generates: Image reports (6x) → PDF
│  ├─ Generates: File list HTML → PDF
│  └─ Returns: List<string> PDF paths
└─ Service: _pdfReportService.AssemblePdfReport()
   └─ Assembles: All PDFs → final output
```

---

## Key Behavioral Invariants

These must remain **exactly the same** after refactoring:

1. **File Categorization Rules**
   - Files containing "blank" → Blank category
   - Files containing "hela" → Hela category
   - All others → Real category
   - Minimum file size: 50 MB

2. **Statistics Calculations**
   - Mean, StdDev calculated on Population class
   - CV% = (StdDev / Mean) × 100
   - Bounds = Mean ± (deviations × StdDev)
   - Custom bounds override when comboBox index == 3

3. **Outlier Detection**
   - Files below lower bound → QC Warning
   - Files above upper bound → QC Warning
   - Listed in statsBox text

4. **Plot Generation**
   - Colors must match: MS2=#DB5461, MS1=#6E44FF
   - X-axis labels: first, last, rest blank
   - Title, XLabel, YLabel must match exactly
   - Image saved to: `{OutputDirectory}\images\{FILENAME}.png`

5. **PDF Report Structure**
   - Page 1: Statistics (from statsBox)
   - Pages 2-8: Image reports (MEANS, FILESIZES, BP, TIC, IDS, PEPIDS)
   - Last page: File summary (lists all files by category)
   - Custom threshold notation must be present

---

## Common Mistakes to Avoid

### ❌ Don't
- Pass entire Form1 reference to services
- Modify UI controls directly in service code
- Use static methods for services
- Ignore custom threshold comboBox selection
- Forget to handle empty file lists
- Mix display formatting with statistics calculation

### ✅ Do
- Keep UI (dialogs, lists, textboxes) in Form1
- Have services return data, not modify UI
- Use dependency injection for service references
- Check `comboBox1.SelectedIndex == 3` before applying custom bounds
- Handle empty collections gracefully
- Separate data calculation from presentation formatting

---

## Testing Tips

### For Service Methods
```csharp
// Create test data
var testFiles = new List<FileInfo> { /* test files */ };
var testBps = new List<double> { 1.0, 2.5, 3.2 };

// Call service
var result = _plottingService.PlotIntensityComparison(
    testBps, testMs1, testTimes, testFiles, 2.0);

// Verify statistics
Assert.AreEqual(2.0, result.MS1Mean);
Assert.AreEqual(3, result.MS1OutlierFiles.Count);
```

### For Form1 Methods
```csharp
// Verify UI state before
Assert.AreEqual(0, form.fileList.Items.Count);

// Call method
form.SimulateButton2Click(testFolder);

// Verify UI state after
Assert.IsTrue(form.dataloaded);
Assert.IsTrue(form.fileList.Items.Count > 0);
```

---

## Rollback Keywords

If you need to find original code:
- Search: `CreateMAXPlot` → Original at line 659
- Search: `CreateFileSizePlot` → Original at line 773
- Search: `ExtractTICS` → Original at line 1008
- Search: `PlotTICS` → Original at line 1045
- Search: `createHTMLReportText` → Original at line 1516
- Search: `createImageReportText` → Original at line 1538

---

## Files Modified/Created

### New Files Created
1. `/tmp/qcactus/REFACTORED_METHODS.md` - Complete refactored method code
2. `/tmp/qcactus/REQUIRED_RETURN_TYPES.cs` - Data structure classes
3. `/tmp/qcactus/MIGRATION_IMPLEMENTATION_GUIDE.md` - Step-by-step guide
4. `/tmp/qcactus/QUICK_REFERENCE.md` - This file

### Files to Modify
1. `/tmp/qcactus/ThermoDust/Form1.cs` - Replace 6 methods (lines 361, 420, 659, 773, 1230, 1431)
2. `/tmp/qcactus/ThermoDust/Services/PlottingService.cs` - Add 4 methods
3. `/tmp/qcactus/ThermoDust/Services/AnalysisEngine.cs` - Add 3 methods
4. `/tmp/qcactus/ThermoDust/Services/PDFReportService.cs` - Add 2 methods

### No Changes Needed
- Original service classes (keep as-is)
- Original helper methods (integrityCheck, getFileCreatedDate, etc.)
- UI controls and event handlers (except the 6 methods)
- DataModel (already set up)
