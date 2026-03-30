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

// Path to examples directory
string examplesDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "examples"));

// Verify template directory exists
if (!Directory.Exists(templateDirectory))
{
    Console.WriteLine($"Template directory not found: {templateDirectory}");
    return;
}

Console.WriteLine($"Using templates from: {templateDirectory}");

// Create the processor and template provider
var processor = new FhirProcessor(new ProcessorSettings(), logger);
var templateProvider = new TemplateProvider(templateDirectory, DataType.Fhir);

// Load FHIR Bundle from file or use default
string fhirBundle;
string inputFile = Path.Combine(examplesDirectory, "Bundle-medication-bundle-example.json");

if (args.Length > 0 && File.Exists(args[0]))
{
    inputFile = args[0];
}

if (File.Exists(inputFile))
{
    Console.WriteLine($"Loading input from: {inputFile}");
    fhirBundle = File.ReadAllText(inputFile);
}
else
{
    Console.WriteLine("No input file found. Using sample data.");
    fhirBundle = """
    {
      "resourceType": "Bundle",
      "type": "collection",
      "entry": [
        {
          "resource": {
            "resourceType": "Patient",
            "id": "patient-example",
            "name": [{ "given": ["John"], "family": "Smith" }],
            "gender": "male",
            "birthDate": "1960-05-15"
          }
        }
      ]
    }
    """;
}

try
{
    // Convert FHIR to PIQI format
    string result = processor.Convert(fhirBundle, "BundleJSON", templateProvider);

    Console.WriteLine("\n--- Output PIQI Format ---");
    Console.WriteLine(result);
}
catch (Exception ex)
{
    Console.WriteLine($"Error during conversion: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}