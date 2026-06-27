using System.Text;
using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UserLayer.Controllers;

[Authorize]
public class CertificatesController : Controller
{
    private readonly ICertificateService _certificateService;

    public CertificatesController(ICertificateService certificateService)
    {
        _certificateService = certificateService;
    }

    public async Task<IActionResult> Index()
    {
        var studentId = Guid.Parse(User.FindFirst("UserId")!.Value);
        ViewData["Title"] = "My Certificates";

        var result = await _certificateService.GetStudentCertificatesAsync(studentId);
        return View(result.Data ?? Enumerable.Empty<DataLayer.Entities.Certificate>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(Guid enrollmentId)
    {
        var result = await _certificateService.GenerateCertificateAsync(enrollmentId);
        if (result.IsSuccess && result.Data != null)
        {
            TempData["Success"] = "Certificate generated successfully.";
            return RedirectToAction(nameof(Details), new { id = result.Data.Id });
        }

        TempData["Error"] = result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var studentId = Guid.Parse(User.FindFirst("UserId")!.Value);
        var result = await _certificateService.GetCertificateByIdAsync(id);
        if (!result.IsSuccess || result.Data == null) return NotFound();
        if (result.Data.StudentId != studentId) return Forbid();

        ViewData["Title"] = "Certificate";
        return View(result.Data);
    }

    public async Task<IActionResult> Download(Guid id)
    {
        var studentId = Guid.Parse(User.FindFirst("UserId")!.Value);
        var result = await _certificateService.GetCertificateByIdAsync(id);
        if (!result.IsSuccess || result.Data == null) return NotFound();
        if (result.Data.StudentId != studentId) return Forbid();

        var verificationUrl = BuildAbsoluteUrl(Url.Action(nameof(VerifyById), "Certificates", new { id = result.Data.Id }) ?? $"/Certificates/Verify/{result.Data.Id}");
        var pdf = BuildCertificatePdf(result.Data, verificationUrl);
        return File(pdf, "application/pdf", $"certificate-{result.Data.CertificateNumber}.pdf");
    }

    [AllowAnonymous]
    [HttpGet("/Certificates/Verify/{id:guid}")]
    public async Task<IActionResult> VerifyById(Guid id)
    {
        ViewData["Title"] = "Verify Certificate";
        var result = await _certificateService.GetCertificateByIdAsync(id);

        if (!result.IsSuccess || result.Data == null || string.IsNullOrWhiteSpace(result.Data.VerificationUrl))
        {
            ViewBag.NotFound = true;
            return View("Verify");
        }

        return View("Verify", result.Data);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Verify(string number)
    {
        ViewData["Title"] = "Verify Certificate";
        var result = await _certificateService.GetCertificateByNumberAsync(number);

        if (!result.IsSuccess || result.Data == null || string.IsNullOrWhiteSpace(result.Data.VerificationUrl))
        {
            ViewBag.NotFound = true;
            return View();
        }

        return View(result.Data);
    }

    private string BuildAbsoluteUrl(string path)
    {
        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return path;
        return $"{Request.Scheme}://{Request.Host}{path}";
    }

    private static byte[] BuildCertificatePdf(DataLayer.Entities.Certificate certificate, string verificationUrl)
    {
        var student = PdfText(CertificateDisplay.StudentName(certificate));
        var course = PdfText(CertificateDisplay.CourseName(certificate));
        var instructor = PdfText(CertificateDisplay.InstructorName(certificate));
        var number = PdfText(certificate.CertificateNumber);
        var shortNumber = number[..Math.Min(number.Length, 24)];
        var issued = PdfText(certificate.IssuedAt.ToString("MMMM dd, yyyy"));
        var qrSeed = certificate.Id.ToString("N");

        var commands = new StringBuilder();

        // A4 landscape points: 842 x 595. This template is print-first, not a web-page export.
        commands.AppendLine("0.985 0.975 0.945 rg 0 0 842 595 re f");
        commands.AppendLine("1 0.998 0.982 rg 36 34 770 527 re f");
        commands.AppendLine("0.82 0.62 0.18 RG 3 w 54 52 734 491 re S");
        commands.AppendLine("0.10 0.14 0.22 RG 1.1 w 70 68 702 459 re S");
        commands.AppendLine("0.82 0.62 0.18 RG 1 w 86 84 670 427 re S");

        commands.AppendLine("0.992 0.985 0.955 rg 337 220 168 168 re f");
        commands.AppendLine("1 0.998 0.982 rg 351 234 140 140 re f");
        commands.AppendLine("BT /F2 28 Tf 370 300 Td 0.965 0.925 0.820 rg (LH) Tj ET");
        commands.AppendLine("0.82 0.62 0.18 rg 111 494 78 2 re f");
        commands.AppendLine("0.82 0.62 0.18 rg 653 494 78 2 re f");
        commands.AppendLine("0.82 0.62 0.18 rg 111 99 78 2 re f");
        commands.AppendLine("0.82 0.62 0.18 rg 653 99 78 2 re f");

        AppendCenteredText(commands, "LearnHub", 489, 25, "F2", "0.08 0.12 0.20");
        AppendCenteredText(commands, "PROFESSIONAL LEARNING CERTIFICATION", 469, 8, "F1", "0.46 0.38 0.22");
        commands.AppendLine("0.82 0.62 0.18 rg 381 454 80 3 re f");

        AppendCenteredText(commands, "CERTIFICATE OF COMPLETION", 415, 32, "F2", "0.08 0.12 0.20");
        AppendCenteredText(commands, "Presented To", 377, 12, "F1", "0.37 0.42 0.52");
        AppendCenteredText(commands, student, 324, 46, "F2", "0.04 0.06 0.11");
        commands.AppendLine("0.82 0.62 0.18 rg 250 304 342 2 re f");

        AppendCenteredText(commands, "For successfully completing", 273, 12, "F1", "0.37 0.42 0.52");
        AppendCenteredText(commands, course, 235, 27, "F2", "0.65 0.43 0.08");
        AppendCenteredText(commands, "This certificate recognizes demonstrated commitment, achievement, and successful completion", 200, 10, "F1", "0.30 0.35 0.45");
        AppendCenteredText(commands, "of the required learning experience through LearnHub.", 184, 10, "F1", "0.30 0.35 0.45");

        commands.AppendLine("0.82 0.62 0.18 RG 1.2 w 126 128 274 128 m 274 128 l S");
        commands.AppendLine($"BT /F3 17 Tf 148 137 Td 0.08 0.12 0.20 rg ({instructor}) Tj ET");
        commands.AppendLine("BT /F1 8 Tf 156 113 Td 0.37 0.42 0.52 rg (Instructor Signature) Tj ET");

        AppendCenteredText(commands, issued, 137, 15, "F2", "0.08 0.12 0.20");
        AppendCenteredText(commands, "Completion Date", 113, 8, "F1", "0.37 0.42 0.52");

        commands.AppendLine("0.82 0.62 0.18 rg 609 101 88 88 re f");
        commands.AppendLine("1 0.998 0.982 rg 619 111 68 68 re f");
        commands.AppendLine("0.65 0.43 0.08 RG 1.4 w 619 111 68 68 re S");
        commands.AppendLine("BT /F2 16 Tf 634 141 Td 0.65 0.43 0.08 rg (LH) Tj ET");
        commands.AppendLine("BT /F1 6 Tf 629 128 Td 0.46 0.38 0.22 rg (OFFICIAL SEAL) Tj ET");

        AppendQrPattern(commands, 718, 103, 44, qrSeed);
        commands.AppendLine($"BT /F1 6 Tf 694 89 Td 0.37 0.42 0.52 rg (Certificate ID: {shortNumber}) Tj ET");

        return BuildSimplePdf(commands.ToString());
    }

    private static void AppendQrPattern(StringBuilder commands, int x, int y, int size, string seed)
    {
        commands.AppendLine("1 1 1 rg " + x + " " + y + " " + size + " " + size + " re f");
        commands.AppendLine("0.08 0.12 0.20 rg");
        var cells = 17;
        var cell = size / cells;
        for (var row = 0; row < cells; row++)
        {
            for (var col = 0; col < cells; col++)
            {
                var finder = (row < 5 && col < 5) || (row < 5 && col > 11) || (row > 11 && col < 5);
                var index = (row * 31 + col * 17) % seed.Length;
                var filled = finder || ((seed[index] + row + col) % 3 == 0);
                if (filled) commands.AppendLine($"{x + col * cell} {y + row * cell} {cell} {cell} re f");
            }
        }
    }

    private static void AppendCenteredText(StringBuilder commands, string text, int y, int fontSize, string font, string color)
    {
        var x = CenteredX(text, fontSize, 842);
        commands.AppendLine($"BT /{font} {fontSize} Tf {x} {y} Td {color} rg ({text}) Tj ET");
    }

    private static int CenteredX(string text, int fontSize, int pageWidth)
    {
        var width = Math.Min(pageWidth - 160, text.Length * fontSize / 2);
        return Math.Max(80, (pageWidth - width) / 2);
    }
    private static string PdfText(string value)
        => (value ?? string.Empty).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    private static byte[] BuildSimplePdf(string pageContent)
    {
        var objects = new List<string>
        {
            "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n",
            "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj\n",
            "3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 4 0 R /F2 6 0 R >> >> /Contents 5 0 R >> endobj\n",
            "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj\n"
        };
        objects.Add($"5 0 obj << /Length {Encoding.ASCII.GetByteCount(pageContent)} >> stream\n{pageContent}\nendstream endobj\n");
        objects.Add("6 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Times-Bold >> endobj\n");

        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Encoding.ASCII, leaveOpen: true);
        writer.Write("%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        foreach (var obj in objects)
        {
            writer.Flush();
            offsets.Add(ms.Position);
            writer.Write(obj);
        }
        writer.Flush();
        var xref = ms.Position;
        writer.Write($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) writer.Write($"{offset:0000000000} 00000 n \n");
        writer.Write($"trailer << /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        writer.Flush();
        return ms.ToArray();
    }
}

internal static class CertificateDisplay
{
    public static string StudentName(DataLayer.Entities.Certificate certificate)
        => !string.IsNullOrWhiteSpace(certificate.Student?.FullName)
            ? certificate.Student.FullName
            : certificate.Enrollment?.Student?.FullName ?? "Student name unavailable";

    public static string CourseName(DataLayer.Entities.Certificate certificate)
        => !string.IsNullOrWhiteSpace(certificate.Course?.Title)
            ? certificate.Course.Title
            : certificate.Enrollment?.Course?.Title ?? "Course name unavailable";

    public static string InstructorName(DataLayer.Entities.Certificate certificate)
        => !string.IsNullOrWhiteSpace(certificate.Course?.Instructor?.FullName)
            ? certificate.Course.Instructor.FullName
            : "LearnHub Instructor";
}




