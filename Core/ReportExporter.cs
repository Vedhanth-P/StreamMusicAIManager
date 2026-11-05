using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using StreamMusicAIManager.Models;

namespace StreamMusicAIManager.Core
{
    public class ReportExporter
    {
        public static void ExportToExcel(List<AudioTrack> tracks, string outputPath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Library Audit");

            // Headers
            worksheet.Cell(1, 1).Value = "Filename";
            worksheet.Cell(1, 2).Value = "Artist";
            worksheet.Cell(1, 3).Value = "Title";
            worksheet.Cell(1, 4).Value = "Genre";
            worksheet.Cell(1, 5).Value = "BPM";
            worksheet.Cell(1, 6).Value = "Energy Score";
            worksheet.Cell(1, 7).Value = "Category";
            worksheet.Cell(1, 8).Value = "Duration (s)";
            worksheet.Cell(1, 9).Value = "Copyright Status";
            worksheet.Cell(1, 10).Value = "Action Taken";

            // Styling Header Row
            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E1E2E");
            headerRow.Style.Font.FontColor = XLColor.White;

            int row = 2;
            foreach (var track in tracks)
            {
                worksheet.Cell(row, 1).Value = track.FileName;
                worksheet.Cell(row, 2).Value = track.Artist;
                worksheet.Cell(row, 3).Value = track.Title;
                worksheet.Cell(row, 4).Value = track.Genre;
                worksheet.Cell(row, 5).Value = track.Bpm;
                worksheet.Cell(row, 6).Value = track.EnergyScore;
                worksheet.Cell(row, 7).Value = track.Category.ToString();
                worksheet.Cell(row, 8).Value = track.DurationSeconds;
                worksheet.Cell(row, 9).Value = track.CopyrightStatus.ToString();
                worksheet.Cell(row, 10).Value = track.CopyrightReason;

                // Status Highlight
                if (track.CopyrightStatus == CopyrightStatus.SAFE)
                    worksheet.Cell(row, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#2ECC71");
                else if (track.CopyrightStatus == CopyrightStatus.REVIEW_REQUIRED)
                    worksheet.Cell(row, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#F1C40F");
                else
                    worksheet.Cell(row, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#E74C3C");

                row++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(outputPath);
        }
    }
}