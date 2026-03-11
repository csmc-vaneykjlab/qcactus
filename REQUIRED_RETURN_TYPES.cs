// ========================================================================
// REQUIRED RETURN TYPES AND DATA STRUCTURES
// For PlottingService and AnalysisEngine refactoring
// ========================================================================

using System;
using System.Collections.Generic;

namespace ThermoDust
{
    /// <summary>
    /// Result data returned from PlotIntensityComparison method.
    /// Contains plot references and statistical calculations for both MS1 and MS2 data.
    /// </summary>
    public class PlotIntensityResult
    {
        public ScottPlot.Plottable.ScatterPlot MS2Scatter { get; set; }
        public ScottPlot.Plottable.ScatterPlot MS1Scatter { get; set; }

        // MS1 Statistics (Median TIC)
        public double[] MS1Values { get; set; }
        public double MS1Mean { get; set; }
        public double MS1UpperBound { get; set; }
        public double MS1LowerBound { get; set; }
        public double MS1StdDev { get; set; }
        public double MS1CV { get; set; }  // Coefficient of Variation

        // MS2 Statistics (Base Peak)
        public double[] MS2Values { get; set; }
        public double MS2Mean { get; set; }
        public double MS2UpperBound { get; set; }
        public double MS2LowerBound { get; set; }
        public double MS2StdDev { get; set; }
        public double MS2CV { get; set; }

        // File information for warnings
        public List<string> MS1OutlierFiles { get; set; }
        public List<string> MS2OutlierFiles { get; set; }

        public PlotIntensityResult()
        {
            MS1OutlierFiles = new List<string>();
            MS2OutlierFiles = new List<string>();
        }
    }

    /// <summary>
    /// Result data returned from PlotFileSizes method.
    /// Contains plot references and statistical calculations for file size analysis.
    /// </summary>
    public class FileSizePlotResult
    {
        public ScottPlot.Plottable.ScatterPlot RealFilesScatter { get; set; }
        public ScottPlot.Plottable.ScatterPlot BlankFilesScatter { get; set; }
        public ScottPlot.Plottable.ScatterPlot HelaFilesScatter { get; set; }

        // Real Files Statistics
        public double[] RealFileSizes { get; set; }
        public double RealMean { get; set; }
        public double RealUpperBound { get; set; }
        public double RealLowerBound { get; set; }
        public double RealStdDev { get; set; }
        public double RealCV { get; set; }

        // Blank Files Statistics
        public double[] BlankFileSizes { get; set; }
        public double BlankMean { get; set; }
        public double BlankUpperBound { get; set; }
        public double BlankLowerBound { get; set; }
        public double BlankStdDev { get; set; }

        // Hela Files Statistics
        public double[] HelaFileSizes { get; set; }
        public double HelaMean { get; set; }
        public double HelaUpperBound { get; set; }
        public double HelaLowerBound { get; set; }
        public double HelaStdDev { get; set; }

        // Outlier tracking
        public List<string> RealFileOutliers { get; set; }
        public List<string> BlankFileOutliers { get; set; }
        public List<string> HelaFileOutliers { get; set; }

        public FileSizePlotResult()
        {
            RealFileOutliers = new List<string>();
            BlankFileOutliers = new List<string>();
            HelaFileOutliers = new List<string>();
        }
    }

    /// <summary>
    /// Result from CategorizeRawFiles method.
    /// Separates files into categories with metadata and validation results.
    /// </summary>
    public class CategorizedFilesResult
    {
        // Real Sample Files (actual experimental samples)
        public List<string> RealFileNames { get; set; }
        public List<double> RealFileSizes { get; set; }
        public List<string> RealTimestamps { get; set; }
        public List<string> RealFiles { get; set; }  // Display format: "filename | size MB | timestamp"

        // Blank Control Files
        public List<string> BlankFileNames { get; set; }
        public List<double> BlankFileSizes { get; set; }
        public List<string> BlankTimestamps { get; set; }
        public List<string> BlankFiles { get; set; }  // Display format

        // HeLa Control Files
        public List<string> HelaFileNames { get; set; }
        public List<double> HelaFileSizes { get; set; }
        public List<string> HelaTimestamps { get; set; }
        public List<string> HelaFiles { get; set; }  // Display format

        // Failed/Invalid Files
        public List<string> FailedFiles { get; set; }
        public List<string> FailedFilesDisplay { get; set; }  // Display format with reasons

        // Validation summary
        public int TotalFilesProcessed { get; set; }
        public int ValidFilesCount { get; set; }
        public int FailedFilesCount { get; set; }
        public double MinimumFileSize { get; set; }

        public CategorizedFilesResult()
        {
            RealFileNames = new List<string>();
            RealFileSizes = new List<double>();
            RealTimestamps = new List<string>();
            RealFiles = new List<string>();

            BlankFileNames = new List<string>();
            BlankFileSizes = new List<double>();
            BlankTimestamps = new List<string>();
            BlankFiles = new List<string>();

            HelaFileNames = new List<string>();
            HelaFileSizes = new List<double>();
            HelaTimestamps = new List<string>();
            HelaFiles = new List<string>();

            FailedFiles = new List<string>();
            FailedFilesDisplay = new List<string>();

            MinimumFileSize = 50.0;  // Default 50 MB minimum
        }
    }

    /// <summary>
    /// Data extracted from TIC analysis.
    /// Stores the TIC curves and retention times for all processed files.
    /// </summary>
    public class TicExtractionData
    {
        // Tics[fileIndex][scanIndex] = TIC value
        public List<List<double>> Tics { get; set; }

        // StartTimes[fileIndex][scanIndex] = Retention time in minutes
        public List<List<double>> StartTimes { get; set; }

        // File information for reference
        public List<string> FileNames { get; set; }

        // Metadata
        public int NumberOfFiles { get; set; }
        public List<int> ScansPerFile { get; set; }

        public TicExtractionData()
        {
            Tics = new List<List<double>>();
            StartTimes = new List<List<double>>();
            FileNames = new List<string>();
            ScansPerFile = new List<int>();
        }
    }

    /// <summary>
    /// Result from BasePeak plot creation.
    /// Contains statistics for base peak analysis.
    /// </summary>
    public class BasePeakPlotResult
    {
        public ScottPlot.Plottable.ScatterPlot ScatterPlot { get; set; }

        public double[] BasePeakValues { get; set; }
        public double Mean { get; set; }
        public double UpperBound { get; set; }
        public double LowerBound { get; set; }
        public double StdDev { get; set; }
        public double CV { get; set; }  // Coefficient of Variation

        public List<string> OutlierFiles { get; set; }

        public BasePeakPlotResult()
        {
            OutlierFiles = new List<string>();
        }
    }

    /// <summary>
    /// Result from comprehensive report generation.
    /// Contains paths to all generated PDF report files.
    /// </summary>
    public class ComprehensiveReportResult
    {
        // Individual report PDF paths
        public string StatisticsReportPath { get; set; }
        public string FileSizeReportPath { get; set; }
        public string IntensityReportPath { get; set; }
        public string BasePeakReportPath { get; set; }
        public string TicReportPath { get; set; }
        public string FileListReportPath { get; set; }
        public string IdentificationsReportPath { get; set; }
        public string PeptidesReportPath { get; set; }

        // All paths as list (for iteration)
        public List<string> AllReportPaths { get; set; }

        // Summary information
        public string GeneratedDate { get; set; }
        public string ReportTitle { get; set; }
        public bool IncludesCustomThresholds { get; set; }

        public ComprehensiveReportResult()
        {
            AllReportPaths = new List<string>();
            GeneratedDate = DateTime.Now.ToString("MM/dd/yyyy");
            ReportTitle = "QCactus v2 - Quality Report";
        }

        /// <summary>
        /// Adds a report path to the internal list and returns self for chaining.
        /// </summary>
        public void AddReportPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                AllReportPaths.Add(path);
            }
        }
    }

    /// <summary>
    /// File metadata extracted during categorization.
    /// Used internally by AnalysisEngine.
    /// </summary>
    public class FileMetadata
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public double FileSizeMB { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedDateFormatted { get; set; }
        public FileCategory Category { get; set; }
        public bool IsValid { get; set; }
        public string ValidationError { get; set; }

        public FileMetadata()
        {
            IsValid = true;
            Category = FileCategory.Unknown;
        }
    }

    /// <summary>
    /// Enum for file categorization results.
    /// </summary>
    public enum FileCategory
    {
        Unknown = 0,
        Real = 1,           // Standard experimental samples
        Blank = 2,          // Blank controls
        Hela = 3,           // HeLa cell line controls
        Failed = 4          // Corrupted or invalid files
    }

    /// <summary>
    /// Statistics for a single data population.
    /// Used to encapsulate statistical calculations.
    /// </summary>
    public class PopulationStatistics
    {
        public double[] Values { get; set; }
        public double Mean { get; set; }
        public double StdDev { get; set; }
        public double Median { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double CV { get; set; }  // Coefficient of Variation (%)

        public PopulationStatistics(double[] values)
        {
            Values = values;
            CalculateStatistics();
        }

        private void CalculateStatistics()
        {
            if (Values == null || Values.Length == 0)
                return;

            // Mean
            Mean = 0;
            foreach (double val in Values)
                Mean += val;
            Mean /= Values.Length;

            // Std Dev
            double sumSquares = 0;
            foreach (double val in Values)
                sumSquares += Math.Pow(val - Mean, 2);
            StdDev = Math.Sqrt(sumSquares / Values.Length);

            // CV
            CV = (StdDev / Mean) * 100;

            // Min/Max
            Min = double.MaxValue;
            Max = double.MinValue;
            foreach (double val in Values)
            {
                if (val < Min) Min = val;
                if (val > Max) Max = val;
            }

            // Median
            double[] sorted = (double[])Values.Clone();
            Array.Sort(sorted);
            if (sorted.Length % 2 == 0)
                Median = (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2;
            else
                Median = sorted[sorted.Length / 2];
        }
    }
}
