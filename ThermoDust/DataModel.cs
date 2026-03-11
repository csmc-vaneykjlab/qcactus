using System;
using System.Collections.Generic;
using ScottPlot;
using ScottPlot.Plottable;

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
        public ScatterPlot BasepeakScatter { get; set; } = new(null, null);
        public MarkerPlot HighlightedBasePeak { get; set; } = new();

        // Scan Count Charts
        public ScatterPlot ScanCountScatter { get; set; } = new(null, null);
        public ScatterPlot ScanCountScatter2 { get; set; } = new(null, null);
        public MarkerPlot HighlightedScanCount { get; set; } = new();
        public MarkerPlot HighlightedScanCount2 { get; set; } = new();

        // Intensity Charts
        public ScatterPlot IntensityScatter { get; set; } = new(null, null);
        public ScatterPlot IntensityScatter2 { get; set; } = new(null, null);
        public ScatterPlot IntensityScatter3 { get; set; } = new(null, null);
        public MarkerPlot HighlightedIntensity { get; set; } = new();

        // File Size Charts - Group A
        public ScatterPlot FileSizeScatterGroupA { get; set; } = new(null, null);
        public MarkerPlot HighlightedFileSizeGroupA { get; set; } = new();

        // File Size Charts - Group B
        public ScatterPlot FileSizeScatterGroupB { get; set; } = new(null, null);
        public MarkerPlot HighlightedFileSizeGroupB { get; set; } = new();

        // File Size Charts - Group C
        public ScatterPlot FileSizeScatterGroupC { get; set; } = new(null, null);
        public MarkerPlot HighlightedFileSizeGroupC { get; set; } = new();

        // File Size Charts - Group D
        public ScatterPlot FileSizeScatterGroupD { get; set; } = new(null, null);
        public MarkerPlot HighlightedFileSizeGroupD { get; set; } = new();

        // Intensity Charts - Group A
        public ScatterPlot IntensityScatterGroupA { get; set; } = new(null, null);
        public ScatterPlot IntensityScatterGroupA2 { get; set; } = new(null, null);
        public MarkerPlot HighlightedIntensityGroupA { get; set; } = new();
        public MarkerPlot HighlightedIntensityGroupA2 { get; set; } = new();

        // Intensity Charts - Group B
        public ScatterPlot IntensityScatterGroupB { get; set; } = new(null, null);
        public ScatterPlot IntensityScatterGroupB2 { get; set; } = new(null, null);
        public MarkerPlot HighlightedIntensityGroupB { get; set; } = new();
        public MarkerPlot HighlightedIntensityGroupB2 { get; set; } = new();

        // Intensity Charts - Group C
        public ScatterPlot IntensityScatterGroupC { get; set; } = new(null, null);
        public ScatterPlot IntensityScatterGroupC2 { get; set; } = new(null, null);
        public MarkerPlot HighlightedIntensityGroupC { get; set; } = new();
        public MarkerPlot HighlightedIntensityGroupC2 { get; set; } = new();

        // Intensity Charts - Group D
        public ScatterPlot IntensityScatterGroupD { get; set; } = new(null, null);
        public ScatterPlot IntensityScatterGroupD2 { get; set; } = new(null, null);
        public MarkerPlot HighlightedIntensityGroupD { get; set; } = new();
        public MarkerPlot HighlightedIntensityGroupD2 { get; set; } = new();

        // Base Peak Charts - Group A
        public ScatterPlot BasePeakScatterGroupA { get; set; } = new(null, null);
        public MarkerPlot HighlightedBasePeakGroupA { get; set; } = new();

        // Base Peak Charts - Group B
        public ScatterPlot BasePeakScatterGroupB { get; set; } = new(null, null);
        public MarkerPlot HighlightedBasePeakGroupB { get; set; } = new();

        // Base Peak Charts - Group C
        public ScatterPlot BasePeakScatterGroupC { get; set; } = new(null, null);
        public MarkerPlot HighlightedBasePeakGroupC { get; set; } = new();

        // Base Peak Charts - Group D
        public ScatterPlot BasePeakScatterGroupD { get; set; } = new(null, null);
        public MarkerPlot HighlightedBasePeakGroupD { get; set; } = new();

        // Protein & Peptide Charts
        public MarkerPlot HighlightedProtein { get; set; } = new();
        public MarkerPlot HighlightedPeptide { get; set; } = new();

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
