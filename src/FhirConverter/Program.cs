using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Liquid.Converter;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Microsoft.Health.Fhir.Liquid.Converter.Processors;

// Configure logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Warning);
});

var logger = loggerFactory.CreateLogger<FhirProcessor>();

// Path to templates directory
string templateDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "FHIRToPIQI"));

// Fixed pipeline directories
string inputDirectory = @"C:\repos\emi-pipeline\fhir-examples";
string outputDirectory = @"C:\repos\emi-pipeline\piqi-output";

// Parse command line arguments
// Usage: dotnet run <inputFileName> <outputFileName>
if (args.Length < 2)
{
    Console.WriteLine("Usage: dotnet run <inputFileName> <outputFileName>");
    Console.WriteLine($"  Input directory:  {inputDirectory}");
    Console.WriteLine($"  Output directory: {outputDirectory}");
    return;
}

string inputFileName = args[0];
string outputFileName = args[1];

string inputFile = Path.Combine(inputDirectory, inputFileName);
string outputFile = Path.Combine(outputDirectory, outputFileName);

// Verify template directory exists
if (!Directory.Exists(templateDirectory))
{
    Console.WriteLine($"Template directory not found: {templateDirectory}");
    return;
}

// Verify input file exists
if (!File.Exists(inputFile))
{
    Console.WriteLine($"Input file not found: {inputFile}");
    return;
}

// Ensure output directory exists
Directory.CreateDirectory(outputDirectory);

Console.WriteLine($"Using templates from: {templateDirectory}");
Console.WriteLine($"Input:  {inputFile}");
Console.WriteLine($"Output: {outputFile}");

// Create the processor and template provider
var processor = new FhirProcessor(new ProcessorSettings(), logger);
var templateProvider = new TemplateProvider(templateDirectory, DataType.Fhir);

try
{
    string fhirBundle = File.ReadAllText(inputFile);

    // Convert FHIR to PIQI format
    string result = processor.Convert(fhirBundle, "BundleJSON", templateProvider);

    // Write to output file
    File.WriteAllText(outputFile, result);
    Console.WriteLine($"\nSuccess: {outputFileName}");
}
catch (Exception ex)
{
    Console.WriteLine($"\nError: {ex.Message}");
}
