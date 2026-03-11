using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;
using Microsoft.Data.Sqlite;

namespace ThermoDust
{
    /// <summary>
    /// Handles all mass spectrometry data analysis and processing.
    /// Extracts metrics from raw files, calculates statistics, and manages data parsing.
    /// </summary>
    public class AnalysisEngine
    {
        private readonly DataModel _data;

        public AnalysisEngine(DataModel dataModel)
        {
            _data = dataModel;
        }

        // ── TIC/Scan Extraction ─────────────────────────────────────

        /// <summary>
        /// Extracts Total Ion Chromatogram (TIC) data from raw mass spectrometry files
        /// </summary>
        public (List<List<double>> tics, List<List<double>> startTimes) ExtractTicsFromRawFiles(IRawDataPlus[] rawFiles)
        {
            var startTimes = new List<List<double>>();
            var tics = new List<List<double>>();

            foreach (var rawFile in rawFiles)
            {
                var startTimeList = new List<double>();
                var ticList = new List<double>();

                rawFile.SelectInstrument(Device.MS, 1);

                int firstScanNumber = rawFile.RunHeaderEx.FirstSpectrum;
                int lastScanNumber = rawFile.RunHeaderEx.LastSpectrum;

                foreach (var scanNumber in Enumerable.Range(1, lastScanNumber - firstScanNumber))
                {
                    var scanFilter = rawFile.GetFilterForScanNumber(scanNumber);
                    if (scanFilter.MSOrder == MSOrderType.Ms)
                    {
                        var scanStatistics = rawFile.GetScanStatsForScanNumber(scanNumber);
                        startTimeList.Add(scanStatistics.StartTime);
                        double normalizedTic = scanStatistics.TIC / Math.Pow(10, 9);
                        ticList.Add(normalizedTic);
                    }
                }

                tics.Add(ticList);
                startTimes.Add(startTimeList);
            }

            return (tics, startTimes);
        }

        // ── File Size Analysis ──────────────────────────────────────

        /// <summary>
        /// Gets file sizes in megabytes for a list of file paths
        /// </summary>
        public double[] GetFileSizesInMB(List<string> filePaths)
        {
            return filePaths
                .Select(path => new FileInfo(path).Length / (1024.0 * 1024.0))
                .ToArray();
        }

        /// <summary>
        /// Validates files and categorizes them by type (blank, real, hela)
        /// </summary>
        public void CategorizeRawFiles(
            string sourcePath,
            out List<double> blankSizes, out List<double> realSizes, out List<double> helaSizes,
            out List<string> blankNames, out List<string> realNames, out List<string> helaNames,
            out List<string> failedFiles)
        {
            blankSizes = new List<double>();
            realSizes = new List<double>();
            helaSizes = new List<double>();
            blankNames = new List<string>();
            realNames = new List<string>();
            helaNames = new List<string>();
            failedFiles = new List<string>();

            if (!Directory.Exists(sourcePath))
                return;

            var rawFiles = Directory.GetFiles(sourcePath, "*.raw");

            foreach (var filePath in rawFiles)
            {
                try
                {
                    double fileSize = new FileInfo(filePath).Length / (1024.0 * 1024.0);
                    string fileName = Path.GetFileName(filePath);

                    if (fileName.Contains("blank", StringComparison.OrdinalIgnoreCase))
                    {
                        blankSizes.Add(fileSize);
                        blankNames.Add(fileName);
                    }
                    else if (fileName.Contains("hela", StringComparison.OrdinalIgnoreCase))
                    {
                        helaSizes.Add(fileSize);
                        helaNames.Add(fileName);
                    }
                    else
                    {
                        realSizes.Add(fileSize);
                        realNames.Add(fileName);
                    }
                }
                catch
                {
                    failedFiles.Add(Path.GetFileName(filePath));
                }
            }
        }

        // ── Statistical Calculations ────────────────────────────────

        /// <summary>
        /// Calculates mean, standard deviation, and control limits for a dataset
        /// </summary>
        public (double mean, double stdev, double lowerBound, double upperBound)
            CalculateStatistics(double[] values, double standardDeviations, double? customLower = null, double? customUpper = null)
        {
            if (values.Length == 0)
                return (0, 0, 0, 0);

            double mean = values.Average();
            double stdev = Math.Sqrt(values.Average(x => Math.Pow(x - mean, 2)));

            double lowerBound = customLower ?? (mean - standardDeviations * stdev);
            double upperBound = customUpper ?? (mean + standardDeviations * stdev);

            return (mean, stdev, lowerBound, upperBound);
        }

        /// <summary>
        /// Gets file names that fall outside control limits
        /// </summary>
        public List<string> GetOutlierFiles(List<string> fileNames, double[] values, double lowerBound, double upperBound)
        {
            var outliers = new List<string>();
            for (int i = 0; i < fileNames.Count; i++)
            {
                if (values[i] < lowerBound || values[i] > upperBound)
                {
                    outliers.Add(fileNames[i]);
                }
            }
            return outliers;
        }

        // ── Statistics Formatting ───────────────────────────────────

        /// <summary>
        /// Formats statistical results as a readable string
        /// </summary>
        public string FormatStatisticalResults(
            string title,
            double mean,
            double stdev,
            double lowerBound,
            double upperBound,
            List<string> outlierFiles,
            double deviations,
            bool useCustomThresholds)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(title);
            sb.AppendLine();

            if (useCustomThresholds)
            {
                sb.AppendLine("UB\t\tMean\t\tLB");
            }
            else
            {
                sb.AppendLine($"-{deviations}SD\t\tMean\t\t+{deviations}SD");
            }

            sb.AppendLine($"{lowerBound:F}\t\t{mean:F}\t\t{upperBound:F}");
            sb.AppendLine();
            sb.AppendLine("QC Warnings:");

            if (outlierFiles.Count == 0)
            {
                sb.AppendLine("  [None]");
            }
            else
            {
                foreach (var file in outlierFiles)
                {
                    sb.AppendLine($"  {file}");
                }
            }

            return sb.ToString();
        }

        // ── Database Operations ─────────────────────────────────────

        /// <summary>
        /// Stores calculated metrics in SQLite database
        /// </summary>
        public void StoreMetricsToDatabase(string dbPath, Dictionary<string, object> metrics)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={dbPath}"))
                {
                    connection.Open();
                    // Implementation would depend on database schema
                    // This is a placeholder for database integration
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database error: {ex.Message}");
            }
        }

        // ── MSFragger Integration ───────────────────────────────────

        /// <summary>
        /// Parses pin files from MSFragger search results
        /// </summary>
        public void ParsePinFiles(string[] filePaths)
        {
            _data.ProteinCounts.Clear();
            _data.PeptideCounts.Clear();

            foreach (var filePath in filePaths)
            {
                try
                {
                    var proteinCounts = new HashSet<string>();
                    var peptideCounts = new HashSet<string>();

                    using (var reader = new StreamReader(filePath))
                    {
                        string line;
                        bool isHeader = true;

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (isHeader)
                            {
                                isHeader = false;
                                continue;
                            }

                            var fields = line.Split('\t');
                            if (fields.Length >= 2)
                            {
                                // Assuming peptide is column 1, protein is column 2
                                if (!string.IsNullOrEmpty(fields[1]))
                                    peptideCounts.Add(fields[1]);
                                if (fields.Length > 2 && !string.IsNullOrEmpty(fields[2]))
                                    proteinCounts.Add(fields[2]);
                            }
                        }
                    }

                    _data.PeptideCounts.Add(peptideCounts.Count);
                    _data.ProteinCounts.Add(proteinCounts.Count);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing pin file {filePath}: {ex.Message}");
                }
            }
        }

        // ── Utility Methods ─────────────────────────────────────────

        /// <summary>
        /// Calculates Coefficient of Variation (CV%)
        /// </summary>
        public static double CalculateCV(double stdev, double mean)
        {
            return mean == 0 ? 0 : (stdev / mean) * 100;
        }

        /// <summary>
        /// Calculates Euclidean distance between two points
        /// </summary>
        public static double CalculateDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        /// <summary>
        /// Gets the closest data point to mouse coordinates
        /// </summary>
        public static (int index, double distance) FindClosestPoint(double mouseX, double mouseY, double[] xData, double[] yData)
        {
            if (xData.Length == 0)
                return (-1, double.MaxValue);

            int closestIndex = 0;
            double minDistance = double.MaxValue;

            for (int i = 0; i < xData.Length; i++)
            {
                double distance = CalculateDistance(mouseX, mouseY, xData[i], yData[i]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = i;
                }
            }

            return (closestIndex, minDistance);
        }
    }
}
