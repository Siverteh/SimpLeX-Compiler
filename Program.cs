using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.MapPost("/compile", async (HttpRequest req) =>
    {
        using var reader = new StreamReader(req.Body);
        var latexSource = await reader.ReadToEndAsync();

        var tempDirPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirPath);
        var latexFilePath = Path.Combine(tempDirPath, "document.tex");
        var pdfFilePath = Path.Combine(tempDirPath, "document.pdf");

        await File.WriteAllTextAsync(latexFilePath, latexSource);

        var process = new Process
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

        process.Start();
        await process.WaitForExitAsync();

        if (File.Exists(pdfFilePath))
        {
            var fileBytes = await File.ReadAllBytesAsync(pdfFilePath);
            return Results.File(fileBytes, "application/pdf", "compiled.pdf");
        }
        else
        {
            // Cleanup if failed
            Directory.Delete(tempDirPath, true);
            return Results.Problem("Failed to compile LaTeX document.");
        }
    })
    .WithName("CompileLaTeX");

app.Run();