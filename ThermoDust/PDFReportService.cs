using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using TheArtOfDev.HtmlRenderer.PdfSharp;

namespace ThermoDust
{
    /// <summary>
    /// Handles PDF report generation for quality control analysis results.
    /// Creates multi-page PDF documents with statistics, charts, and analysis summaries.
    /// </summary>
    public class PDFReportService
    {
        private readonly string _outputDirectory;

        public PDFReportService(string outputDirectory)
        {
            _outputDirectory = outputDirectory;
        }

        // ── HTML Report Generation ──────────────────────────────────

        /// <summary>
        /// Creates HTML for statistical summary with customizable thresholds
        /// </summary>
        public string GenerateStatisticalSummaryHtml(
            string statisticsText,
            bool customThresholdsApplied = false,
            string customThresholdInfo = "")
        {
            var html = new StringBuilder();
            html.Append("<style>p {font-family:Roboto Mono; font-size: 11px;}</style>");
            html.Append("<h3>QCactus v2 - Quality Report</h3>");
            html.AppendLine($"<h4>Generated {DateTime.Now:MM/dd/yyyy HH:mm:ss}</h4>");
            html.Append("<hr>");

            // Convert statistics text to HTML format
            string formattedStats = statisticsText
                .Replace("\n", "<br>")
                .Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");

            html.Append($"<p>{formattedStats}</p>");
            html.Append("<hr>");

            if (customThresholdsApplied && !string.IsNullOrEmpty(customThresholdInfo))
            {
                html.Append("<p><strong>**Custom Thresholds Applied**</strong><br>");
                html.Append(customThresholdInfo);
                html.Append("</p>");
            }

            return html.ToString();
        }

        /// <summary>
        /// Creates HTML for image inclusion in report
        /// </summary>
        public string GenerateImagePageHtml(string imageName, string imageTitle)
        {
            if (string.IsNullOrEmpty(imageName))
                return "";

            var html = new StringBuilder();
            html.Append("<style>div {text-align: center;}</style>");
            html.AppendLine($"<h5>QCactus v2 - {imageTitle}</h5>");
            html.AppendLine($"<h6>Generated {DateTime.Now:MM/dd/yyyy}</h6>");
            html.Append("<hr>");
            html.AppendLine($"<div><img src='images/{imageName}' style='width: 500px;' /></div>");

            return html.ToString();
        }

        /// <summary>
        /// Creates HTML for file inventory/summary
        /// </summary>
        public string GenerateFileSummaryHtml(
            List<string> realFiles,
            List<string> blankFiles,
            List<string> helaFiles,
            List<string> failedFiles)
        {
            var html = new StringBuilder();
            html.Append("<style>div {font-size: 9px; font-family: monospace;}</style>");
            html.Append("<h3>File Summary</h3>");
            html.Append("<div>");

            // Sample Files
            html.Append("<h4>Sample Files</h4>");
            html.AppendLine($"<p>Total: {realFiles.Count}</p>");
            foreach (var file in realFiles)
            {
                html.AppendLine($"<p>{file}</p>");
            }

            // Blank Files
            html.Append("<h4>Blank Files</h4>");
            html.AppendLine($"<p>Total: {blankFiles.Count}</p>");
            foreach (var file in blankFiles)
            {
                html.AppendLine($"<p>{file}</p>");
            }

            // HeLa Files
            html.Append("<h4>HeLa Files</h4>");
            html.AppendLine($"<p>Total: {helaFiles.Count}</p>");
            foreach (var file in helaFiles)
            {
                html.AppendLine($"<p>{file}</p>");
            }

            // Failed Files
            if (failedFiles.Count > 0)
            {
                html.Append("<h4>Failed/Corrupt Files</h4>");
                html.AppendLine($"<p>Total: {failedFiles.Count}</p>");
                foreach (var file in failedFiles)
                {
                    html.AppendLine($"<p>{file}</p>");
                }
            }

            html.Append("</div>");
            return html.ToString();
        }

        // ── PDF Generation ──────────────────────────────────────────

        /// <summary>
        /// Creates a PDF document from HTML content
        /// </summary>
        public PdfDocument ConvertHtmlToPdf(string htmlContent)
        {
            var config = new HtmlRenderingConfig();
            config.PageSize = PdfSharpCore.PageSize.Letter;

            return HtmlRender.RenderToDocument(htmlContent, null, config);
        }

        /// <summary>
        /// Creates a PDF from HTML and saves to file
        /// </summary>
        public bool GeneratePdfReport(string htmlContent, string outputFileName)
        {
            try
            {
                var document = ConvertHtmlToPdf(htmlContent);
                string outputPath = Path.Combine(_outputDirectory, outputFileName);

                Directory.CreateDirectory(_outputDirectory);
                document.Save(outputPath);
                document.Close();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating PDF: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Merges multiple PDF documents into one
        /// </summary>
        public bool MergePdfDocuments(List<PdfDocument> documents, string outputFileName)
        {
            try
            {
                var mergedDocument = new PdfDocument();

                foreach (var doc in documents)
                {
                    CopyPages(doc, mergedDocument);
                }

                string outputPath = Path.Combine(_outputDirectory, outputFileName);
                Directory.CreateDirectory(_outputDirectory);
                mergedDocument.Save(outputPath);
                mergedDocument.Close();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error merging PDFs: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Merges multiple PDF files by path
        /// </summary>
        public bool MergePdfFiles(List<string> pdfFilePaths, string outputFileName)
        {
            try
            {
                var mergedDocument = new PdfDocument();

                foreach (var filePath in pdfFilePaths)
                {
                    if (!File.Exists(filePath))
                        continue;

                    var doc = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
                    CopyPages(doc, mergedDocument);
                    doc.Close();
                }

                string outputPath = Path.Combine(_outputDirectory, outputFileName);
                Directory.CreateDirectory(_outputDirectory);
                mergedDocument.Save(outputPath);
                mergedDocument.Close();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error merging PDF files: {ex.Message}");
                return false;
            }
        }

        // ── PDF Page Management ──────────────────────────────────────

        /// <summary>
        /// Copies all pages from source PDF to destination PDF
        /// </summary>
        private void CopyPages(PdfDocument source, PdfDocument destination)
        {
            for (int i = 0; i < source.PageCount; i++)
            {
                destination.AddPage(source.Pages[i]);
            }
        }

        /// <summary>
        /// Exports a list of PDFs to a single combined document
        /// </summary>
        public string CreateComprehensiveReport(
            string statisticsHtml,
            List<(string imageName, string title)> imagePages,
            string fileSummaryHtml,
            string reportFileName)
        {
            try
            {
                var fullHtml = new StringBuilder();

                // Add statistics page
                fullHtml.AppendLine(statisticsHtml);
                fullHtml.AppendLine("<div style='page-break-after: always;'></div>");

                // Add image pages
                foreach (var (imageName, title) in imagePages)
                {
                    fullHtml.AppendLine(GenerateImagePageHtml(imageName, title));
                    fullHtml.AppendLine("<div style='page-break-after: always;'></div>");
                }

                // Add file summary
                fullHtml.AppendLine(fileSummaryHtml);

                // Generate PDF
                if (GeneratePdfReport(fullHtml.ToString(), reportFileName))
                {
                    return Path.Combine(_outputDirectory, reportFileName);
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating comprehensive report: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the directory where reports are saved
        /// </summary>
        public string GetReportDirectory()
        {
            return _outputDirectory;
        }

        /// <summary>
        /// Ensures image directory exists for embedded images in PDFs
        /// </summary>
        public void EnsureImageDirectory()
        {
            string imageDir = Path.Combine(_outputDirectory, "images");
            Directory.CreateDirectory(imageDir);
        }
    }
}
