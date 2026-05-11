using System.IO;
using System.Text;

namespace LibraryManagementSystem.Services;

public static class CsvExportService
{
    public static void Export(string path, IEnumerable<string[]> rows)
    {
        var builder = new StringBuilder();
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", row.Select(Escape)));
        }

        File.WriteAllText(path, builder.ToString());
    }

    public static void ExportExcel(string path, IEnumerable<string[]> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<?xml version=\"1.0\"?>");
        builder.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
        builder.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
        builder.AppendLine("<Worksheet ss:Name=\"Library Report\"><Table>");
        foreach (var row in rows)
        {
            builder.AppendLine("<Row>");
            foreach (var cell in row)
            {
                builder.Append("<Cell><Data ss:Type=\"String\">");
                builder.Append(System.Security.SecurityElement.Escape(cell));
                builder.AppendLine("</Data></Cell>");
            }
            builder.AppendLine("</Row>");
        }
        builder.AppendLine("</Table></Worksheet></Workbook>");
        File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
    }

    public static void ExportPdf(string path, IEnumerable<string[]> rows)
    {
        var lines = rows.Select(row => string.Join(" | ", row)).Take(42).ToList();
        var content = new StringBuilder();
        content.AppendLine("BT");
        content.AppendLine("/F1 10 Tf");
        content.AppendLine("36 790 Td");
        foreach (var line in lines)
        {
            content.Append('(').Append(EscapePdf(line)).AppendLine(") Tj");
            content.AppendLine("0 -16 Td");
        }
        content.AppendLine("ET");

        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content.ToString())} >>\nstream\n{content}endstream"
        };

        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.WriteLine("%PDF-1.4");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            writer.Flush();
            offsets.Add(stream.Position);
            writer.WriteLine($"{i + 1} 0 obj");
            writer.WriteLine(objects[i]);
            writer.WriteLine("endobj");
        }

        writer.Flush();
        var xrefPosition = stream.Position;
        writer.WriteLine("xref");
        writer.WriteLine($"0 {objects.Count + 1}");
        writer.WriteLine("0000000000 65535 f ");
        foreach (var offset in offsets.Skip(1))
        {
            writer.WriteLine($"{offset:0000000000} 00000 n ");
        }
        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xrefPosition);
        writer.WriteLine("%%EOF");
    }

    private static string Escape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
        {
            return '"' + value.Replace("\"", "\"\"") + '"';
        }

        return value;
    }

    private static string EscapePdf(string value) =>
        value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
