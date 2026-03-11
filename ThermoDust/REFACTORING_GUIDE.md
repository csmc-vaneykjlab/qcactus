# ThermoDust Refactoring Guide

## Overview

Form1.cs (3,133 lines) has been refactored into four separate service classes to improve maintainability, testability, and code organization.

## New Architecture

### Classes Created

#### 1. **DataModel.cs** (~180 lines)
**Purpose:** Centralized data storage and state management

**Contains:**
- File size collections (blanks, reals, helas)
- File name collections
- Timestamps
- Quantitative metrics (MS1, MS2, base peaks, protein/peptide counts)
- QC thresholds (standard deviations, custom bounds)
- ScottPlot visualization objects
- Data reset functionality

**Usage:**
```csharp
var dataModel = new DataModel();
dataModel.RealFileSizes.Add(5.2);
dataModel.RealFileNames.Add("sample_001.raw");
```

---

#### 2. **AnalysisEngine.cs** (~280 lines)
**Purpose:** All data processing and statistical calculations

**Contains:**
- TIC extraction from raw files
- File size analysis and categorization
- Statistical calculations (mean, stdev, control limits)
- Outlier detection
- Statistical result formatting
- PIN file parsing for MSFragger results
- Utility methods (CV calculation, distance calculation)

**Usage:**
```csharp
var engine = new AnalysisEngine(dataModel);

// Extract TICs
var (tics, startTimes) = engine.ExtractTicsFromRawFiles(rawFilesArray);

// Calculate statistics
var (mean, stdev, lb, ub) = engine.CalculateStatistics(
    fileSizes,
    standardDeviations: 2.0
);

// Format results
string report = engine.FormatStatisticalResults(
    title: "File Size Analysis",
    mean: mean,
    stdev: stdev,
    lowerBound: lb,
    upperBound: ub,
    outlierFiles: outliers,
    deviations: 2,
    useCustomThresholds: false
);
```

---

#### 3. **PlottingService.cs** (~340 lines)
**Purpose:** All visualization and chart creation

**Contains:**
- TIC plotting
- MS1/MS2 intensity comparison plots
- File size comparison plots
- Base peak (max intensity) plots
- Plot data preparation
- Marker/highlight management
- Image export functionality

**Usage:**
```csharp
var plotter = new PlottingService(dataModel, outputDir);

// Plot intensity data
var stats = plotter.PlotIntensityComparison(
    plot: myPlot,
    ms2Values: basepeaks,
    ms1Values: intensities,
    timestamps: timeList,
    deviations: 2.0
);

// Save to file
plotter.SavePlotImage(myPlot, "intensity.png");
```

---

#### 4. **PDFReportService.cs** (~280 lines)
**Purpose:** PDF report generation and document management

**Contains:**
- HTML report generation (statistics, images, file summaries)
- PDF document creation from HTML
- PDF merging functionality
- Comprehensive multi-page report creation
- Page management utilities

**Usage:**
```csharp
var pdfService = new PDFReportService(outputDirectory);

// Generate statistics page
string html = pdfService.GenerateStatisticalSummaryHtml(
    statisticsText: statsText,
    customThresholdsApplied: true,
    customThresholdInfo: "File sizes: 100-500 MB"
);

// Create comprehensive report
string pdfPath = pdfService.CreateComprehensiveReport(
    statisticsHtml: statsHtml,
    imagePages: new List<(string, string)> {
        ("MEANS.PNG", "MS1/MS2 Intensity"),
        ("ALLBASEPEAKS.PNG", "Base Peak Analysis")
    },
    fileSummaryHtml: summaryHtml,
    reportFileName: "QC_Report_2024.pdf"
);
```

---

## Integration Steps for Form1.cs

### Step 1: Add Service Initialization in Constructor

```csharp
public partial class Form1 : Form
{
    private DataModel _dataModel;
    private AnalysisEngine _analysisEngine;
    private PlottingService _plottingService;
    private PDFReportService _pdfReportService;

    public Form1()
    {
        InitializeComponent();

        // Initialize services
        _dataModel = new DataModel();
        _analysisEngine = new AnalysisEngine(_dataModel);
        _plottingService = new PlottingService(_dataModel, GetOutputDirectory());
        _pdfReportService = new PDFReportService(GetOutputDirectory());
    }
}
```

### Step 2: Replace Data Access

**Before:**
```csharp
blankFileSizes.Add(5.2);
realFileNames.Add("sample.raw");
deviations = 2.5;
```

**After:**
```csharp
_dataModel.BlankFileSizes.Add(5.2);
_dataModel.RealFileNames.Add("sample.raw");
_dataModel.StandardDeviations = 2.5;
```

### Step 3: Replace Analysis Logic

**Before:**
```csharp
private void button2_Click(object sender, EventArgs e)
{
    // 200+ lines of file processing logic
    for (int i = 0; i < rawFileNew.Length; i++)
    {
        // Complex file analysis...
    }
    // Statistics calculations mixed with UI updates
}
```

**After:**
```csharp
private void button2_Click(object sender, EventArgs e)
{
    _analysisEngine.CategorizeRawFiles(
        sourcePath,
        out var blankSizes,
        out var realSizes,
        out var helaSizes,
        out var blankNames,
        out var realNames,
        out var helaNames,
        out var failedFiles
    );

    // Update UI with results
    UpdateFileListBox(realNames, helaNames);
    statsBox.Text = "Files categorized successfully.";
}
```

### Step 4: Replace Plotting Code

**Before:**
```csharp
private void CreateMAXPlot(List<double> bps, List<double> tics, ...)
{
    var plt = scanPlot.Plot;
    plt.Clear();
    // 100+ lines of plot configuration
}
```

**After:**
```csharp
private void CreateIntensityPlots()
{
    var stats = _plottingService.PlotIntensityComparison(
        plot: scanPlot.Plot,
        ms2Values: _dataModel.MaxBasePeaks,
        ms1Values: _dataModel.MedianMS1Values,
        timestamps: _dataModel.RealTimes,
        deviations: _dataModel.StandardDeviations
    );

    scanPlot.Refresh();
    statsBox.Text = FormatStatsDisplay(stats);
}
```

### Step 5: Replace PDF Generation

**Before:**
```csharp
private void button5_Click(object sender, EventArgs e)
{
    // 100+ lines of HTML/PDF generation
    var html = "<style>p {font-family:Roboto Mono;}</style>";
    html += "<h3>QCactus v2 - Quality Report</h3>";
    // ...many more lines...
}
```

**After:**
```csharp
private void button5_Click(object sender, EventArgs e)
{
    _pdfReportService.EnsureImageDirectory();

    string statsHtml = _pdfReportService.GenerateStatisticalSummaryHtml(
        statisticsText: statsBox.Text
    );

    string pdfPath = _pdfReportService.CreateComprehensiveReport(
        statisticsHtml: statsHtml,
        imagePages: new List<(string, string)> {
            ("MEANS.PNG", "Intensity Profile"),
            ("ALLBASEPEAKS.PNG", "Base Peak Summary"),
            ("ALLFILESIZES.PNG", "File Size Analysis")
        },
        fileSummaryHtml: GenerateFileSummaryHtml(),
        reportFileName: "QCactus_Report.pdf"
    );

    if (!string.IsNullOrEmpty(pdfPath))
        System.Diagnostics.Process.Start(pdfPath);
}
```

---

## Benefits of This Refactoring

### Code Organization ✓
- **Before:** 3,133 lines in one class
- **After:**
  - DataModel: 180 lines
  - AnalysisEngine: 280 lines
  - PlottingService: 340 lines
  - PDFReportService: 280 lines
  - Form1: ~400 lines (UI only)
  - **Total extracted: 1,080+ lines** into focused, reusable classes

### Testability ✓
- Each service can be unit tested independently
- No dependencies on Windows Forms controls
- Easy to mock data for testing

### Reusability ✓
- Services can be used in web applications, console apps, or other UIs
- No UI framework coupling
- Clear interfaces for each service

### Maintainability ✓
- Each class has a single responsibility
- Easier to locate and fix bugs
- Simpler to add new features
- Clear data flow between components

---

## Migration Checklist

- [ ] Copy new service classes to ThermoDust project
- [ ] Update Form1.cs constructor to initialize services
- [ ] Replace direct field access with `_dataModel` property access
- [ ] Refactor button click handlers to use services
- [ ] Replace plotting logic with `PlottingService` calls
- [ ] Replace PDF generation with `PDFReportService` calls
- [ ] Remove old methods from Form1.cs (over 50 methods can be removed)
- [ ] Test each feature (file loading, analysis, plotting, PDF export)
- [ ] Update any external method calls to use services
- [ ] Consider extracting configuration paths to a config service

---

## Future Improvements

1. **ConfigurationService** - Extract path/settings management
2. **Async/Await** - Add async operations for large file processing
3. **Logger Service** - Centralized error/event logging
4. **Unit Tests** - Create test projects for each service
5. **Dependency Injection** - Use DI container for service initialization
6. **Settings Persistence** - Expand SettingsForm integration

---

## File Structure After Refactoring

```
ThermoDust/
├── Form1.cs                    (UI event handlers only)
├── Form1.Designer.cs           (unchanged)
├── SettingsForm.cs             (unchanged)
├── SettingsForm.Designer.cs    (unchanged)
├── DataModel.cs                (NEW - data storage)
├── AnalysisEngine.cs           (NEW - data processing)
├── PlottingService.cs          (NEW - visualization)
├── PDFReportService.cs         (NEW - reporting)
├── Program.cs                  (unchanged)
├── ThermoDust.csproj           (unchanged)
└── Properties/                 (unchanged)
```

---

## Notes

- All new classes are in the `ThermoDust` namespace
- No breaking changes to existing UI components
- Gradual migration is possible (refactor one feature at a time)
- The refactoring maintains backward compatibility with existing workflows
