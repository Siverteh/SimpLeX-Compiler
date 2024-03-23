using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

// Initializes a new WebApplication builder, passing in the command-line arguments.
var builder = WebApplication.CreateBuilder(args);

// Adds API exploration and Swagger generation services to the application's service container. This allows the app to document and test its API.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Builds the WebApplication instance from the builder.
var app = builder.Build();

// Enables Swagger and Swagger UI only in development environments. This provides an interactive API documentation.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirects HTTP requests to HTTPS to improve security.
app.UseHttpsRedirection();

// Defines a POST endpoint at "/compile" that asynchronously processes incoming HTTP requests to compile LaTeX documents.
app.MapPost("/compile", async (HttpRequest req) =>
{
    // Reads the incoming request body (containing LaTeX code) into a string.
    using var reader = new StreamReader(req.Body);
    var latexSource = await reader.ReadToEndAsync();

    // Creates a temporary directory for storing the LaTeX source and output files.
    var tempDirPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    Directory.CreateDirectory(tempDirPath);
    var latexFilePath = Path.Combine(tempDirPath, "document.tex");
    var pdfFilePath = Path.Combine(tempDirPath, "document.pdf");

    // Writes the LaTeX code to a .tex file in the temporary directory.
    await File.WriteAllTextAsync(latexFilePath, latexSource);

    // Sets up and starts a new process to run the `pdflatex` command, specifying the LaTeX file to compile and the output directory.
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

    // Checks if the PDF file was created successfully. If yes, reads the file and returns it as a response. Otherwise, returns an error.
    if (File.Exists(pdfFilePath))
    {
        var fileBytes = await File.ReadAllBytesAsync(pdfFilePath);
        return Results.File(fileBytes, "application/pdf", "compiled.pdf");
    }
    else
    {
        // Deletes the temporary directory if the compilation failed.
        Directory.Delete(tempDirPath, true);
        return Results.Problem("Failed to compile LaTeX document.");
    }
})
.WithName("CompileLaTeX"); // Names the endpoint for easier reference and documentation.

// Runs the application, listening for incoming requests.
app.Run();
