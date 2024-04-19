using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/compile", async (HttpRequest req) =>
{
    using var reader = new StreamReader(req.Body);
    var latexSource = await reader.ReadToEndAsync();

    var tempDirPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    Directory.CreateDirectory(tempDirPath);
    var latexFilePath = Path.Combine(tempDirPath, "document.tex");
    var pdfFilePath = Path.Combine(tempDirPath, "document.pdf");

    await File.WriteAllTextAsync(latexFilePath, latexSource);

    // Process setup for pdflatex
    var pdflatexProcess = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "pdflatex",
            Arguments = $"-interaction=nonstopmode -output-directory {tempDirPath} {latexFilePath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = tempDirPath
        }
    };

    // Run pdflatex first time
    pdflatexProcess.Start();
    await pdflatexProcess.WaitForExitAsync();

    // Process setup for biber (bibliography management)
    var biberProcess = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "biber",
            Arguments = Path.GetFileNameWithoutExtension(latexFilePath), // typically just 'document'
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = tempDirPath
        }
    };

    // Run Biber
    biberProcess.Start();
    await biberProcess.WaitForExitAsync();

    // Run pdflatex second time
    pdflatexProcess.Start();
    await pdflatexProcess.WaitForExitAsync();

    // Run pdflatex third time (if needed)
    pdflatexProcess.Start();
    await pdflatexProcess.WaitForExitAsync();

    if (File.Exists(pdfFilePath))
    {
        var fileBytes = await File.ReadAllBytesAsync(pdfFilePath);
        return Results.File(fileBytes, "application/pdf", "compiled.pdf");
    }
    else
    {
        Directory.Delete(tempDirPath, true);
        return Results.Problem("Failed to compile LaTeX document.");
    }
})
.WithName("CompileLaTeX");

app.Run();
