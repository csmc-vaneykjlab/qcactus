using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using ScottPlot;
using ScottPlot.Statistics;

namespace ThermoDust
{
    /// <summary>
    /// Handles all plotting and visualization for mass spectrometry analysis results.
    /// Creates and manages ScottPlot charts, calculates plot data, and exports visualizations.
    /// </summary>
    public class PlottingService
    {
        private readonly DataModel _data;
        private readonly string _outputDirectory;

        // Color scheme
        private static readonly Color AccentColor = Color.FromArgb(82, 196, 217);
        private static readonly Color AccentAltColor = Color.FromArgb(100, 200, 150);
        private static readonly Color LightRed = ColorTranslator.FromHtml("#ffcccb");
        private static readonly Color LightBlue = ColorTranslator.FromHtml("#ADD8E6");

        public PlottingService(DataModel dataModel, string outputDirectory = "")
        {
            _data = dataModel;
            _outputDirectory = outputDirectory;
        }

        // ── TIC Plotting ────────────────────────────────────────────

        /// <summary>
        /// Creates a TIC (Total Ion Chromatogram) plot dataset
        /// </summary>
        public (double[][] xData, double[][] yData) PrepareTicData(
            List<List<double>> tics,
            List<List<double>> startTimes)
        {
            var xData = new double[tics.Count][];
            var yData = new double[tics.Count][];

            for (int i = 0; i < tics.Count; i++)
            {
                xData[i] = startTimes[i].ToArray();
                yData[i] = tics[i].ToArray();
            }

            return (xData, yData);
        }

        /// <summary>
        /// Adds TIC data to a plot
        /// </summary>
        public void PlotTics(Plot plot, List<List<double>> tics, List<List<double>> startTimes)
        {
            plot.Clear();
            plot.Title("TIC");
            plot.XLabel("Retention Time");
            plot.YLabel("TIC (10^9)");

            for (int i = 0; i < tics.Count; i++)
            {
                double[] ytics = tics[i].ToArray();
                double[] xrts = startTimes[i].ToArray();
                plot.AddScatterLines(xrts, ytics);
            }

            plot.XAxis.Ticks(true);
        }

        // ── MS1/MS2 Intensity Plotting ──────────────────────────────

        /// <summary>
        /// Creates intensity comparison plot (MS1 vs MS2 median intensity)
        /// </summary>
        public ChartStatistics PlotIntensityComparison(
            Plot plot,
            List<double> ms2Values,
            List<double> ms1Values,
            List<string> timestamps,
            double deviations,
            double customUpperMS2 = 0,
            double customLowerMS2 = 0,
            double customUpperMS1 = 0,
            double customLowerMS1 = 0)
        {
            plot.Clear();

            double[] ms2Array = ms2Values.ToArray();
            double[] ms1Array = ms1Values.ToArray();

            // Create x-axis positions
            int[] xPositions = Enumerable.Range(1, ms2Array.Length).ToArray();
            var xDouble = xPositions.Select(x => (double)x).ToArray();

            // Calculate statistics
            var ms2Pop = new Population(ms2Array);
            var ms1Pop = new Population(ms1Array);

            // Calculate control limits
            double ms2LowerBound = customLowerMS2 > 0 ? customLowerMS2 : (ms2Pop.mean - deviations * ms2Pop.stDev);
            double ms2UpperBound = customUpperMS2 > 0 ? customUpperMS2 : (ms2Pop.mean + deviations * ms2Pop.stDev);

            double ms1LowerBound = customLowerMS1 > 0 ? customLowerMS1 : (ms1Pop.mean - deviations * ms1Pop.stDev);
            double ms1UpperBound = customUpperMS1 > 0 ? customUpperMS1 : (ms1Pop.mean + deviations * ms1Pop.stDev);

            // Plot data
            plot.Title("Median Intensity");
            plot.XLabel("Time");
            plot.YLabel("log10(Intensity)");
            plot.Legend(location: Alignment.MiddleRight);

            var ms1Cv = CalculateCV(ms1Pop.stDev, ms1Pop.mean);
            var ms2Cv = CalculateCV(ms2Pop.stDev, ms2Pop.mean);

            var ms2Label = $"MS2 ({ms2Cv:F1}%CV)";
            var ms1Label = $"MS1 ({ms1Cv:F1}%CV)";

            _data.ScanScatterPlot = plot.AddScatter(xDouble, ms2Array, AccentColor, lineWidth: 1, label: ms2Label);
            _data.ScanScatterPlot2 = plot.AddScatter(xDouble, ms1Array, AccentAltColor, lineWidth: 1, label: ms1Label);

            // Add control lines
            plot.AddHorizontalLine(ms2Pop.mean, AccentColor, width: 1, style: LineStyle.Dash);
            plot.AddHorizontalLine(ms2UpperBound, AccentColor, width: 1, style: LineStyle.Dot);
            plot.AddHorizontalLine(ms2LowerBound, AccentColor, width: 1, style: LineStyle.Dot);

            plot.AddHorizontalLine(ms1Pop.mean, AccentAltColor, width: 1, style: LineStyle.Dash);
            plot.AddHorizontalLine(ms1UpperBound, AccentAltColor, width: 1, style: LineStyle.Dot);
            plot.AddHorizontalLine(ms1LowerBound, AccentAltColor, width: 1, style: LineStyle.Dot);

            // Format x-axis with timestamps
            FormatTimestampAxis(plot, timestamps, xDouble);

            // Add highlighter markers
            _data.HighlightedPointScan = plot.AddPoint(0, 0);
            _data.HighlightedPointScan.Color = Color.Black;
            _data.HighlightedPointScan.MarkerSize = 20;
            _data.HighlightedPointScan.MarkerShape = MarkerShape.openCircle;
            _data.HighlightedPointScan.IsVisible = false;

            _data.HighlightedPointScan2 = plot.AddPoint(0, 0);
            _data.HighlightedPointScan2.Color = Color.Black;
            _data.HighlightedPointScan2.MarkerSize = 20;
            _data.HighlightedPointScan2.MarkerShape = MarkerShape.openCircle;
            _data.HighlightedPointScan2.IsVisible = false;

            return new ChartStatistics
            {
                Mean = ms1Pop.mean,
                StdDev = ms1Pop.stDev,
                LowerBound = ms1LowerBound,
                UpperBound = ms1UpperBound,
                CV = ms1Cv
            };
        }

        // ── File Size Plotting ──────────────────────────────────────

        /// <summary>
        /// Creates file size comparison plot for multiple sample groups
        /// </summary>
        public void PlotFileSizes(
            Plot plot,
            List<double> blankSizes,
            List<double> helaSizes,
            List<double> realSizes,
            List<string> blankNames,
            List<string> helaNames,
            List<string> realNames,
            double deviations,
            double customUpperBound = 0,
            double customLowerBound = 0)
        {
            plot.Clear();
            plot.Title("File Size Analysis");
            plot.XLabel("Sample");
            plot.YLabel("File Size (MB)");

            // Combine data with group labels
            var allData = new List<(double size, string name, Color color)>();

            foreach (var size in blankSizes)
                allData.Add((size, "Blank", LightRed));

            foreach (var size in helaSizes)
                allData.Add((size, "HeLa", LightBlue));

            foreach (var size in realSizes)
                allData.Add((size, "Sample", AccentColor));

            // Plot by group with different colors
            if (realSizes.Count > 0)
            {
                double[] realArray = realSizes.ToArray();
                var realPop = new Population(realArray);
                int[] xPos = Enumerable.Range(1, realArray.Length).ToArray();
                var xDouble = xPos.Select(x => (double)x).ToArray();

                var lowerBound = customLowerBound > 0 ? customLowerBound : (realPop.mean - deviations * realPop.stDev);
                var upperBound = customUpperBound > 0 ? customUpperBound : (realPop.mean + deviations * realPop.stDev);

                plot.AddScatter(xDouble, realArray, AccentColor, lineWidth: 1, label: "Real Samples");
                plot.AddHorizontalLine(realPop.mean, AccentColor, width: 1, style: LineStyle.Dash);
                plot.AddHorizontalLine(upperBound, AccentColor, width: 1, style: LineStyle.Dot);
                plot.AddHorizontalLine(lowerBound, AccentColor, width: 1, style: LineStyle.Dot);
            }

            if (blankSizes.Count > 0)
            {
                double[] blankArray = blankSizes.ToArray();
                var blankPop = new Population(blankArray);
                int[] xPos = Enumerable.Range(realSizes.Count + 1, blankArray.Length).ToArray();
                var xDouble = xPos.Select(x => (double)x).ToArray();

                plot.AddScatter(xDouble, blankArray, LightRed, lineWidth: 1, label: "Blank");
            }

            plot.Legend(location: Alignment.UpperRight);
        }

        // ── Base Peak Plotting ──────────────────────────────────────

        /// <summary>
        /// Creates base peak (maximum intensity) plot with statistics
        /// </summary>
        public ChartStatistics PlotBasePeaks(
            Plot plot,
            List<double> basePeaks,
            List<string> timestamps,
            List<string> fileNames,
            double deviations,
            double customUpperBound = 0,
            double customLowerBound = 0)
        {
            plot.Clear();

            double[] bpArray = basePeaks.ToArray();
            int[] xPositions = Enumerable.Range(1, bpArray.Length).ToArray();
            var xDouble = xPositions.Select(x => (double)x).ToArray();

            var bpPop = new Population(bpArray);
            var lowerBound = customLowerBound > 0 ? customLowerBound : (bpPop.mean - deviations * bpPop.stDev);
            var upperBound = customUpperBound > 0 ? customUpperBound : (bpPop.mean + deviations * bpPop.stDev);

            plot.Title("Base Peak Summary");
            plot.XLabel("Sample");
            plot.YLabel("Max Intensity");

            var cv = CalculateCV(bpPop.stDev, bpPop.mean);
            var label = $"Base Peak ({cv:F1}%CV)";

            _data.BasepeakScatter = plot.AddScatter(xDouble, bpArray, AccentColor, lineWidth: 1, label: label);

            plot.AddHorizontalLine(bpPop.mean, AccentColor, width: 1, style: LineStyle.Dash);
            plot.AddHorizontalLine(upperBound, AccentColor, width: 1, style: LineStyle.Dot);
            plot.AddHorizontalLine(lowerBound, AccentColor, width: 1, style: LineStyle.Dot);

            // Format x-axis
            if (timestamps.Count > 0)
                FormatTimestampAxis(plot, timestamps, xDouble);

            _data.HighlightedBasePeak = plot.AddPoint(0, 0);
            _data.HighlightedBasePeak.Color = Color.Black;
            _data.HighlightedBasePeak.MarkerSize = 20;
            _data.HighlightedBasePeak.MarkerShape = MarkerShape.openCircle;
            _data.HighlightedBasePeak.IsVisible = false;

            plot.Legend(location: Alignment.UpperRight);

            return new ChartStatistics
            {
                Mean = bpPop.mean,
                StdDev = bpPop.stDev,
                LowerBound = lowerBound,
                UpperBound = upperBound,
                CV = cv
            };
        }

        // ── Utility Methods ─────────────────────────────────────────

        /// <summary>
        /// Formats x-axis to show first and last timestamps only
        /// </summary>
        private void FormatTimestampAxis(Plot plot, List<string> timestamps, double[] xPositions)
        {
            var labels = new string[timestamps.Count];
            for (int i = 0; i < labels.Length; i++)
            {
                if (i == 0 || i == labels.Length - 1)
                {
                    labels[i] = timestamps[i];
                }
                else
                {
                    labels[i] = "";
                }
            }

            plot.XAxis.ManualTickPositions(xPositions, labels);
            plot.XAxis.TickDensity(0.1);
        }

        /// <summary>
        /// Saves current plot to file
        /// </summary>
        public void SavePlotImage(Plot plot, string fileName)
        {
            if (string.IsNullOrEmpty(_outputDirectory))
                return;

            string imagePath = Path.Combine(_outputDirectory, "images", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(imagePath));

            try
            {
                plot.SaveFig(imagePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving plot: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculates Coefficient of Variation percentage
        /// </summary>
        public static double CalculateCV(double stdev, double mean)
        {
            return mean == 0 ? 0 : (stdev / mean) * 100;
        }

        /// <summary>
        /// Calculates Euclidean distance between two points (for hover detection)
        /// </summary>
        public static double CalculateDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }
    }

    /// <summary>
    /// Holds statistical information about a chart
    /// </summary>
    public class ChartStatistics
    {
        public double Mean { get; set; }
        public double StdDev { get; set; }
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
        public double CV { get; set; }
    }
}
