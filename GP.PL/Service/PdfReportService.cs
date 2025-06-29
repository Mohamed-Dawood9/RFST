using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Kernel.Colors;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using PuppeteerSharp;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using GP.PL.VIewModel;
using iText.Layout.Borders;

namespace GP.PL.Services
{
    public class PdfReportService
    {
        private readonly ILogger<PdfReportService> _logger;

        public PdfReportService(ILogger<PdfReportService> logger)
        {
            _logger = logger;
        }

        public async Task<byte[]> GenerateReport(ReportViewModel reportVM, string wwwrootPath)
        {
            try
            {
                if (reportVM == null)
                    throw new ArgumentNullException(nameof(reportVM), "ReportViewModel cannot be null.");
                if (string.IsNullOrEmpty(wwwrootPath) || !Directory.Exists(wwwrootPath))
                    throw new ArgumentException($"Invalid wwwroot path: {wwwrootPath}", nameof(wwwrootPath));

                using var stream = new MemoryStream();
                var writer = new PdfWriter(stream);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4);
                /*document.SetMargins(40, 40, 0, 40);*/ // Reduced bottom margin to 10 points

                var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                var lightGray = new DeviceRgb(240, 240, 240);

                // Header
                var headerTable = new Table(UnitValue.CreatePercentArray(1)).UseAllAvailableWidth();
                headerTable.SetBackgroundColor(lightGray);
                headerTable.AddCell(new Cell()
                    .Add(new Paragraph("Spine Analysis Report")
                        .SetFont(boldFont)
                        .SetFontSize(16)
                        .SetFontColor(ColorConstants.BLACK)
                        .SetTextAlignment(TextAlignment.CENTER))
                    .Add(new Paragraph($"Patient: {reportVM.PatientName ?? "Unknown"} (Age: {reportVM.Age})\nDate: {reportVM.AppointmentDate:yyyy-MM-dd}")
                        .SetFont(regularFont)
                        .SetFontSize(11)
                        .SetFontColor(ColorConstants.BLACK)
                        .SetTextAlignment(TextAlignment.CENTER))
                    .SetPadding(10));
                document.Add(headerTable);
                document.Add(new Paragraph("\n").SetFontSize(6)); // Minimal spacing

                // Page 1: Cobb Table (Left), Large 3D Image (Right), Three Larger Images Below
                var mainTable = new Table(new float[] { 40, 60 }).UseAllAvailableWidth().SetBorder(Border.NO_BORDER);

                // First row: Cobb Table (Left), Large 3D Image (Right)
                var cobbCell = new Cell().SetPadding(10).SetBorder(Border.NO_BORDER);
                var cobbHeader = new Paragraph("Cobb Angle Report")
                    .SetFont(boldFont)
                    .SetFontSize(12)
                    .SetFontColor(ColorConstants.RED);
                cobbCell.Add(cobbHeader);
                var cobbTable = new Table(new float[] { 50, 50 }).UseAllAvailableWidth().SetBorder(Border.NO_BORDER);
                cobbTable.AddHeaderCell(new Cell().Add(new Paragraph("Metric").SetFont(boldFont)).SetBackgroundColor(lightGray).SetBorder(Border.NO_BORDER));
                cobbTable.AddHeaderCell(new Cell().Add(new Paragraph("Value").SetFont(boldFont)).SetBackgroundColor(lightGray).SetBorder(Border.NO_BORDER));
                cobbTable.AddCell(new Cell().Add(new Paragraph("Cobb Angle").SetFont(regularFont)).SetBackgroundColor(ColorConstants.WHITE).SetBorder(Border.NO_BORDER));
                cobbTable.AddCell(new Cell().Add(new Paragraph($"{reportVM.CobbAngle:F2}°").SetFont(regularFont)).SetBackgroundColor(ColorConstants.WHITE).SetBorder(Border.NO_BORDER));
                cobbTable.AddCell(new Cell().Add(new Paragraph("Diagnosis").SetFont(regularFont)).SetBackgroundColor(lightGray).SetBorder(Border.NO_BORDER));
                cobbTable.AddCell(new Cell().Add(new Paragraph(reportVM.Diagnosis ?? GetDiagnosis(reportVM.CobbAngle.Value)).SetFont(regularFont)).SetBackgroundColor(lightGray).SetBorder(Border.NO_BORDER));
                cobbTable.AddCell(new Cell().Add(new Paragraph("Last Cobb Angle").SetFont(regularFont)).SetBackgroundColor(ColorConstants.WHITE).SetBorder(Border.NO_BORDER));
                cobbTable.AddCell(new Cell().Add(new Paragraph(reportVM.PreviousCobbAngle.HasValue ? $"{reportVM.PreviousCobbAngle:F2}°" : "N/A").SetFont(regularFont)).SetBackgroundColor(ColorConstants.WHITE).SetBorder(Border.NO_BORDER));
                if (reportVM.CobbProgressPercentage.HasValue)
                {
                    cobbTable.AddCell(new Cell().Add(new Paragraph("Progress").SetFont(regularFont)).SetBackgroundColor(lightGray).SetBorder(Border.NO_BORDER));
                    cobbTable.AddCell(new Cell().Add(new Paragraph($"{Math.Abs(reportVM.CobbProgressPercentage.Value):F1}% {reportVM.CobbProgressStatus}").SetFont(regularFont)).SetBackgroundColor(lightGray).SetBorder(Border.NO_BORDER));
                }
                cobbCell.Add(cobbTable);
                mainTable.AddCell(cobbCell);

                var largeImageCell = new Cell().SetPadding(10).SetBorder(Border.NO_BORDER);
                await AddImageCell(largeImageCell, "Original Photo", await ConvertHtmlToImagePath(reportVM.OriginalPhotoPath, wwwrootPath, "images"), boldFont, regularFont, 250, 200);
                mainTable.AddCell(largeImageCell);

                // Second row: Three larger 3D images spanning both columns
                var threeImagesCell = new Cell(1, 2).SetPadding(10).SetBorder(Border.NO_BORDER);
                var threeImagesTable = new Table(new float[] { 33.3f, 33.3f, 33.3f }).UseAllAvailableWidth().SetBorder(Border.NO_BORDER);

                var cellModel = new Cell().SetBorder(Border.NO_BORDER).SetMarginTop(10);
                await AddImageCell(cellModel, "3D Model", await ConvertHtmlToImage(reportVM.ProcessedPhotoPath1, wwwrootPath), boldFont, regularFont, 170, 280); // Increased height
                threeImagesTable.AddCell(cellModel);

                var cellHeatmap = new Cell().SetBorder(Border.NO_BORDER).SetMarginTop(10);
                await AddImageCell(cellHeatmap, "Heatmap", await ConvertHtmlToImage(reportVM.ProcessedPhotoPath2, wwwrootPath), boldFont, regularFont, 170, 280); // Increased height
                threeImagesTable.AddCell(cellHeatmap);

                var cellFitted = new Cell().SetBorder(Border.NO_BORDER).SetMarginTop(10);
                await AddImageCell(cellFitted, "Fitted Spine", await ConvertHtmlToImage(reportVM.ProcessedPhotoPath3, wwwrootPath), boldFont, regularFont, 170, 280); // Increased height
                threeImagesTable.AddCell(cellFitted);

                threeImagesCell.Add(threeImagesTable);
                mainTable.AddCell(threeImagesCell);
                document.Add(mainTable);

               

                // Footer (Page 1)
                document.Add(new LineSeparator(new SolidLine()));
                var footer = new Paragraph($"Generated by Scoliosis Clinic\nPage {pdf.GetNumberOfPages()}")
                    .SetFont(regularFont)
                    .SetFontSize(8)
                    .SetFontColor(ColorConstants.GRAY)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(10); // Reduced margin
                document.Add(footer);

                // Page 2: Measurement Table (Left), 2D Images (Right, Stacked)
                document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                var page2Table = new Table(new float[] { 40, 60 }).UseAllAvailableWidth().SetBorder(Border.NO_BORDER);

                // Left: Measurement Table
                var measurementCell = new Cell().SetPadding(10).SetBorder(Border.NO_BORDER);
                var measurementHeader = new Paragraph("Posture Analysis Report")
                    .SetFont(boldFont)
                    .SetFontSize(12)
                    .SetFontColor(ColorConstants.RED);
                measurementCell.Add(measurementHeader);
                var measurementTable = new Table(new float[] { 25, 20, 20, 35 }).UseAllAvailableWidth().SetBorder(Border.NO_BORDER);
                measurementTable.AddHeaderCell(new Cell().Add(new Paragraph("Metric").SetFont(boldFont)).SetBackgroundColor(lightGray).SetBorder(Border.NO_BORDER));
                measurementTable.AddHeaderCell(new Cell().Add(new Paragraph("Current (%)").SetFont(boldFont)).SetBackgroundColor(lightGray).SetBorder(Border.NO_BORDER));
                measurementTable.AddHeaderCell(new Cell().Add(new Paragraph("Previous (%)").SetFont(boldFont)).SetBackgroundColor(lightGray).SetBorder(Border.NO_BORDER));
                measurementTable.AddHeaderCell(new Cell().Add(new Paragraph("Diagnosis").SetFont(boldFont)).SetBackgroundColor(lightGray).SetBorder(Border.NO_BORDER));
                AddPostureRow(measurementTable, "HDI-S (Shoulders)", reportVM.HDI_S, reportVM.PreviousHDI_S, regularFont);
                AddPostureRow(measurementTable, "HDI-A (Underarms)", reportVM.HDI_A, reportVM.PreviousHDI_A, regularFont);
                AddPostureRow(measurementTable, "HDI-T (Trunk Shift)", reportVM.HDI_T, reportVM.PreviousHDI_T, regularFont);
                AddPostureRow(measurementTable, "FAI-C7 (C7 Centering)", reportVM.FAI_C7, reportVM.PreviousFAI_C7, regularFont);
                AddPostureRow(measurementTable, "FAI-A (Underarm Asymmetry)", reportVM.FAI_A, reportVM.PreviousFAI_A, regularFont);
                AddPostureRow(measurementTable, "FAI-T (Trunk Asymmetry)", reportVM.FAI_T, reportVM.PreviousFAI_T, regularFont);
                measurementCell.Add(measurementTable);
                page2Table.AddCell(measurementCell);

                // Right: 2D Images (Stacked)
                var images2DCell = new Cell().SetPadding(10).SetBorder(Border.NO_BORDER);
                var section2D = new Paragraph("2D Back Analysis Visualizations")
                    .SetFont(boldFont)
                    .SetFontSize(12)
                    .SetFontColor(ColorConstants.RED);
                images2DCell.Add(section2D);
                await AddImageCell(images2DCell, "Original Back Image", await ConvertHtmlToImagePath(reportVM.AnalysisOriginalPhotoPath, wwwrootPath, "images"), boldFont, regularFont, 300, 225);
                await AddImageCell(images2DCell, "Annotated Back Image", await ConvertHtmlToImagePath(reportVM.AnalysisProcessedPhotoPath, wwwrootPath), boldFont, regularFont, 300, 225);
                page2Table.AddCell(images2DCell);
                document.Add(page2Table);

                // Notes (Page 2)
                var notesHeader2 = new Paragraph("Notes")
                    .SetFont(boldFont)
                    .SetFontSize(12)
                    .SetFontColor(ColorConstants.RED);
                document.Add(notesHeader2);
                if (reportVM.Notes?.Any() == true)
                {
                    var noteList = new List()
                        .SetFont(regularFont)
                        .SetFontSize(10)
                        .SetListSymbol("\u2022 ").
                        SetMargin(3); // Reduced margin
                    foreach (var note in reportVM.Notes)
                    {
                        noteList.Add(new ListItem(note.Content).SetMarginBottom(5) as ListItem);
                    }
                    document.Add(noteList);
                }
                else
                {
                    document.Add(new Paragraph("No notes available.")
                        .SetFont(regularFont)
                        .SetFontSize(10)
                        .SetFontColor(ColorConstants.GRAY)
                        );
                }

                // Footer (Page 2)
                document.Add(new LineSeparator(new SolidLine()));
                var footer2 = new Paragraph($"Generated by Scoliosis Clinic\nPage {pdf.GetNumberOfPages()}")
                    .SetFont(regularFont)
                    .SetFontSize(8)
                    .SetFontColor(ColorConstants.GRAY)
                    .SetTextAlignment(TextAlignment.CENTER)
                   .SetMarginTop(10); // Reduced margin
                document.Add(footer2);

                document.Close();
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GenerateReport Error: {Message}", ex.Message);
                throw new Exception($"Failed to generate PDF report: {ex.Message}", ex);
            }
        }

        private async Task<string> ConvertHtmlToImagePath(string imagePath, string wwwrootPath, string subfolder = null)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath)) return null;
                var fullPath = string.IsNullOrEmpty(subfolder)
                    ? Path.Combine(wwwrootPath, imagePath.TrimStart('/'))
                    : Path.Combine(wwwrootPath, subfolder, imagePath.TrimStart('/'));
                if (File.Exists(fullPath)) return fullPath;
                return await ConvertHtmlToImage(imagePath, wwwrootPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ConvertHtmlToImagePath Error: {Message}", ex.Message);
                return null;
            }
        }

        private async Task<string> ConvertHtmlToImage(string htmlPath, string wwwrootPath)
        {
            try
            {
                if (string.IsNullOrEmpty(htmlPath)) return null;
                var fullHtmlPath = Path.Combine(wwwrootPath, htmlPath.TrimStart('/'));
                if (!File.Exists(fullHtmlPath)) return null;

                var outputPath = Path.Combine(wwwrootPath, "temp", $"{Guid.NewGuid()}.png");
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                await new BrowserFetcher().DownloadAsync();
                using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
                using var page = await browser.NewPageAsync();
                await page.GoToAsync($"file://{fullHtmlPath}");
                await page.ScreenshotAsync(outputPath, new ScreenshotOptions { FullPage = true });
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ConvertHtmlToImage Error: {Message}", ex.Message);
                return null;
            }
        }

        private async Task AddImageCell(Cell cell, string title, string imagePath, PdfFont boldFont, PdfFont regularFont, float width, float height)
        {
            try
            {
                var titlePara = new Paragraph(title)
                    .SetFont(boldFont)
                    .SetFontSize(10)
                    .SetFontColor(ColorConstants.RED)
                    .SetTextAlignment(TextAlignment.CENTER);
                cell.Add(titlePara);
                if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                {
                    var imageData = ImageDataFactory.Create(imagePath);
                    var img = new Image(imageData)
                        .ScaleToFit(width, height)
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                        .SetBorder(Border.NO_BORDER);
                    cell.Add(img);
                }
                else
                {
                    cell.Add(new Paragraph("Image not available.")
                        .SetFont(regularFont)
                        .SetFontSize(8)
                        .SetFontColor(ColorConstants.GRAY)
                        .SetTextAlignment(TextAlignment.CENTER));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddImageCell Error: {Message}", ex.Message);
                cell.Add(new Paragraph($"Failed to add image '{title}': {ex.Message}")
                    .SetFont(regularFont)
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.CENTER));
            }
        }

        private void AddPostureRow(Table table, string metric, float current, float? previous, PdfFont regularFont)
        {
            table.AddCell(new Cell().Add(new Paragraph(metric).SetFont(regularFont)).SetBackgroundColor(ColorConstants.WHITE).SetBorder(Border.NO_BORDER));
            table.AddCell(new Cell().Add(new Paragraph($"{current:F2}").SetFont(regularFont)).SetBackgroundColor(ColorConstants.WHITE).SetBorder(Border.NO_BORDER));
            table.AddCell(new Cell().Add(new Paragraph(previous.HasValue ? $"{previous:F2}" : "N/A").SetFont(regularFont)).SetBackgroundColor(ColorConstants.WHITE).SetBorder(Border.NO_BORDER));
            table.AddCell(new Cell().Add(new Paragraph(GetMetricDescription(metric, current)).SetFont(regularFont)).SetBackgroundColor(ColorConstants.WHITE).SetBorder(Border.NO_BORDER));
        }

        private string GetDiagnosis(decimal cobbAngle)
        {
            if (cobbAngle >= 80) return "Very-severe scoliosis";
            if (cobbAngle >= 40) return "Severe scoliosis";
            if (cobbAngle >= 25) return "Moderate scoliosis";
            if (cobbAngle >= 10) return "Mild scoliosis";
            return "No significant scoliosis";
        }

        private string GetMetricDescription(string metricName, float value)
        {
            switch (metricName)
            {
                case "HDI-S (Shoulders)":
                case "HDI-A (Underarms)":
                case "HDI-T (Trunk Shift)":
                    if (value <= 3) return "Normal";
                    if (value <= 5) return "Mild Asymmetry";
                    return "Severe Asymmetry";
                case "FAI-C7 (C7 Centering)":
                    if (value <= 3) return "Normal";
                    if (value <= 6) return "Mild Asymmetry";
                    return "Severe Asymmetry";
                case "FAI-A (Underarm Asymmetry)":
                case "FAI-T (Trunk Asymmetry)":
                    if (value <= 2) return "Normal";
                    if (value <= 4) return "Mild Asymmetry";
                    return "Severe Asymmetry";
                default:
                    return "Unknown";
            }
        }
    }
}