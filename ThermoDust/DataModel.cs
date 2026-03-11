using System;
using System.Collections.Generic;
using ScottPlot;

namespace ThermoDust
{
    /// <summary>
    /// Centralized data model for ThermoDust analysis.
    /// Manages all data collections, metrics, and plot objects.
    /// </summary>
    public class DataModel
    {
        // ── File Size Data ──────────────────────────────────────────
        public List<double> BlankFileSizes { get; set; } = new();
        public List<double> RealFileSizes { get; set; } = new();
        public List<double> HelaFileSizes { get; set; } = new();

        // ── File Names ──────────────────────────────────────────────
        public List<string> RealFileNames { get; set; } = new();
        public List<string> BlankFileNames { get; set; } = new();
        public List<string> HelaFileNames { get; set; } = new();
        public List<string> FailedFiles { get; set; } = new();
        public List<string> IdFiles { get; set; } = new();

        // ── Timestamps ──────────────────────────────────────────────
        public List<string> RealTimes { get; set; } = new();
        public List<string> BlankTimes { get; set; } = new();
        public List<string> HelaTimes { get; set; } = new();
        public List<string> FragCombinedTimes { get; set; } = new();

        public List<string> BasePeakFileNames { get; set; } = new();
        public List<string> MSFraggerFileNames { get; set; } = new();

        // ── Quantitative Metrics ────────────────────────────────────
        public List<double> MedianMS1Values { get; set; } = new();
        public List<double> MedianMS2Values { get; set; } = new();
        public List<double> MaxBasePeaks { get; set; } = new();

        public List<double> ProteinCounts { get; set; } = new();
        public List<double> PeptideCounts { get; set; } = new();

        // ── Quality Control Settings ────────────────────────────────
        /// <summary>Number of standard deviations for control limits</summary>
        public double StandardDeviations { get; set; } = 2;

        /// <summary>Whether data has been loaded into the application</summary>
        public bool IsDataLoaded { get; set; } = false;

        // ── Custom Thresholds (User-Defined) ────────────────────────
        public double CustomUpperBoundFileSize { get; set; }
        public double CustomLowerBoundFileSize { get; set; }
        public double CustomUpperBoundMS1 { get; set; }
        public double CustomLowerBoundMS1 { get; set; }
        public double CustomUpperBoundMS2 { get; set; }
        public double CustomLowerBoundMS2 { get; set; }
        public double CustomUpperBoundBasePeak { get; set; }
        public double CustomLowerBoundBasePeak { get; set; }

        // ── ScottPlot Objects for Interactive Charts ────────────────

        // Base Peak Charts
        public Plottable.ScatterPlot BasepeakScatter { get; set; } = new(null, null);
        public Plottable.MarkerPlot HighlightedBasePeak { get; set; } = new();

        // Scan Count Charts
        public Plottable.ScatterPlot ScanCountScatter { get; set; } = new(null, null);
        public Plottable.ScatterPlot ScanCountScatter2 { get; set; } = new(null, null);
        public Plottable.MarkerPlot HighlightedScanCount { get; set; } = new();
        public Plottable.MarkerPlot HighlightedScanCount2 { get; set; } = new();

        // Intensity Charts
        public Plottable.ScatterPlot IntensityScatter { get; set; } = new(null, null);
        public Plottable.ScatterPlot IntensityScatter2 { get; set; } = new(null, null);
        public Plottable.ScatterPlot IntensityScatter3 { get; set; } = new(null, null);
        public Plottable.MarkerPlot HighlightedIntensity { get; set; } = new();

        // File Size Charts - Group A
        public Plottable.ScatterPlot FileSizeScatterGroupA { get; set; } = new(null, null);
        public Plottable.MarkerPlot HighlightedFileSizeGroupA { get; set; } = new();

        // File Size Charts - Group B
        public Plottable.ScatterPlot FileSizeScatterGroupB { get; set; } = new(null, null);
        public Plottable.MarkerPlot HighlightedFileSizeGroupB { get; set; } = new();

        // File Size Charts - Group C
        public Plottable.ScatterPlot FileSizeScatterGroupC { get; set; } = new(null, null);
        public Plottable.MarkerPlot HighlightedFileSizeGroupC { get; set; } = new();

        // File Size Charts - Group D
        public Plottable.ScatterPlot FileSizeScatterGroupD { get; set; } = new(null, null);
        public Plottable.MarkerPlot HighlightedFileSizeGroupD { get; set; } = new();

        // Intensity Charts - Group A
        public Plottable.ScatterPlot IntensityScatterGroupA { get; set; } = new(null, null);
        public Plottable.ScatterPlot IntensityScatterGroupA2 { get; set; } = new(null, null);
        public Plottable.MarkerPlot HighlightedIntensityGroupA { get; set; } = new();
        public Plottable.MarkerPlot HighlightedIntensityGroupA2 { get; set; } = new();

        // Intensity Charts - Group B
        public Plottable.ScatterPlot IntensityScatterGroupB { get; set; } = new(null, null);
        public Plottable.ScatterPlot IntensityScatterGroupB2 { get; set; } = new(null, null);
        public Plottable.MarkerPlot HighlightedIntensityGroupB { get; set; } = new();
        public Plottable.MarkerPlot HighlightedIntensityGroupB2 { get; set; } = new();

        // Intensity Charts - Group C
        public Plottable.ScatterPlot IntensityScatterGroupC { get; set; } = new(null, null);
        public Plottable.ScatterPlot IntensityScatterGroupC2 { get; set; } = new(null, null);
        public Plottable.MarkerPlot HighlightedIntensityGroupC { get; set; } = new();
        public Plottable.MarkerPlot HighlightedIntensityGroupC2 { get; set; } = new();

        // Intensity Charts - Group D
        public Plottable.ScatterPlot IntensityScatterGroupD { get; set; } = new(null, null);
        public Plottable.ScatterPlot IntensityScatterGroupD2 { get; set; } = new(null, null);
        public Plottable.MarkerPlot HighlightedIntensityGroupD { get; set; } = new();
        public Plottable.MarkerPlot HighlightedIntensityGroupD2 { get; set; } = new();

        // Base Peak Charts - Group A
        public Plottable.ScatterPlot BasePeakScatterGroupA { get; set; } = new(null, null);
        public Plottable.MarkerPlot HighlightedBasePeakGroupA { get; set; } = new();

        // Base Peak Charts - Group B
        public Plottable.ScatterPlot BasePeakScatterGroupB { get; set; } = new(null, null);
        public Plottable.MarkerPlot HighlightedBasePeakGroupB { get; set; } = new();

        // Base Peak Charts - Group C
        public Plottable.ScatterPlot BasePeakScatterGroupC { get; set; } = new(null, null);
        public Plottable.MarkerPlot HighlightedBasePeakGroupC { get; set; } = new();

        // Base Peak Charts - Group D
        public Plottable.ScatterPlot BasePeakScatterGroupD { get; set; } = new(null, null);
        public Plottable.MarkerPlot HighlightedBasePeakGroupD { get; set; } = new();

        // Protein & Peptide Charts
        public Plottable.MarkerPlot HighlightedProtein { get; set; } = new();
        public Plottable.MarkerPlot HighlightedPeptide { get; set; } = new();

        // ── Chart State Tracking ─────────────────────────────────────
        public int LastHighlightedIndex { get; set; } = -1;
        public int LastHighlightedIndex2 { get; set; } = -1;

        /// <summary>
        /// Resets all data collections to initial state
        /// </summary>
        public void Reset()
        {
            BlankFileSizes.Clear();
            RealFileSizes.Clear();
            HelaFileSizes.Clear();

            RealFileNames.Clear();
            BlankFileNames.Clear();
            HelaFileNames.Clear();
            FailedFiles.Clear();
            IdFiles.Clear();

            RealTimes.Clear();
            BlankTimes.Clear();
            HelaTimes.Clear();
            FragCombinedTimes.Clear();

            BasePeakFileNames.Clear();
            MSFraggerFileNames.Clear();

            MedianMS1Values.Clear();
            MedianMS2Values.Clear();
            MaxBasePeaks.Clear();

            ProteinCounts.Clear();
            PeptideCounts.Clear();

            IsDataLoaded = false;
            LastHighlightedIndex = -1;
            LastHighlightedIndex2 = -1;
        }
    }
}
