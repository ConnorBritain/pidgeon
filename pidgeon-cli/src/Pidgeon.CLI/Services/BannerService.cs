using System.Reflection;
using System.Text;

namespace Pidgeon.CLI.Services;

/// <summary>
/// Provides ASCII art banner display for Pidgeon CLI startup.
/// Incorporates Pidgeon logo design with HL7 encoding character motifs.
/// </summary>
public class BannerService
{
    private const string BannerArt = """

        ▓▓▓▓▓▓▓▓     ██████╗ ██╗██████╗  ██████╗ ███████╗ ██████╗ ███╗   ██╗     ▓▓▓▓▓▓▓▓
         ▒▒▒▒▒▒▒▒    ██╔══██╗██║██╔══██╗██╔════╝ ██╔════╝██╔═══██╗████╗  ██║    ▒▒▒▒▒▒▒▒
          ░░░░░░░░   ██████╔╝██║██║  ██║██║  ███╗█████╗  ██║   ██║██╔██╗ ██║   ░░░░░░░░
           ████████  ██╔═══╝ ██║██║  ██║██║   ██║██╔══╝  ██║   ██║██║╚██╗██║  ████████
                     ██║     ██║██████╔╝╚██████╔╝███████╗╚██████╔╝██║ ╚████║
                     ╚═╝     ╚═╝╚═════╝  ╚═════╝ ╚══════╝ ╚═════╝ ╚═╝  ╚═══╝

                     Healthcare Message Testing Platform
                     HL7 • FHIR • NCPDP • X12
                     "Confident interoperability testing without PHI risk"

        """;

    /// <summary>
    /// Displays the Pidgeon banner with version and quick start information.
    /// </summary>
    /// <param name="showQuickStart">Whether to show quick start commands</param>
    public void DisplayBanner(bool showQuickStart = true)
    {
        Console.WriteLine();
        Console.WriteLine(BannerArt);
        Console.WriteLine();

        // Version and build info
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString(3) ?? "dev";
        var buildDate = GetBuildDate(assembly);

        Console.WriteLine($"    Version {version} • Built {buildDate:yyyy-MM-dd}");
        Console.WriteLine();

        if (showQuickStart)
        {
            DisplayQuickStart();
        }
    }

    /// <summary>
    /// Displays compact quick start commands for immediate productivity.
    /// </summary>
    public void DisplayQuickStart()
    {
        Console.WriteLine("    🚀 Quick Start Commands:");
        Console.WriteLine();
        Console.WriteLine("    pidgeon generate ADT^A01             # Generate HL7 admission message");
        Console.WriteLine("    pidgeon generate Patient --count 5   # Generate 5 FHIR patients");
        Console.WriteLine("    pidgeon validate message.hl7         # Validate healthcare message");
        Console.WriteLine("    pidgeon deident --in ./samples       # De-identify real data");
        Console.WriteLine();
        Console.WriteLine("    📖 More Commands:");
        Console.WriteLine("    pidgeon --help                       # Full command reference");
        Console.WriteLine("    pidgeon lookup MSH                   # HL7 standards reference");
        Console.WriteLine("    pidgeon workflow wizard              # Guided testing scenarios");
        Console.WriteLine("    pidgeon diff file1.hl7 file2.hl7    # Smart message comparison");
        Console.WriteLine();
    }

    /// <summary>
    /// Displays minimal banner for non-interactive scenarios.
    /// </summary>
    public void DisplayCompactBanner()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString(3) ?? "dev";

        Console.WriteLine($"Pidgeon Healthcare Platform v{version} | Testing without PHI nightmares");
        Console.WriteLine();
    }

    /// <summary>
    /// Displays first-time user welcome with enhanced guidance.
    /// </summary>
    public void DisplayWelcome()
    {
        DisplayBanner(showQuickStart: false);

        Console.WriteLine("    👋 Welcome to Pidgeon!");
        Console.WriteLine();
        Console.WriteLine("    Pidgeon transforms healthcare interoperability testing by providing:");
        Console.WriteLine("    • Synthetic data generation (HL7, FHIR, NCPDP)");
        Console.WriteLine("    • HIPAA-compliant de-identification");
        Console.WriteLine("    • Multi-standard validation & troubleshooting");
        Console.WriteLine("    • Vendor pattern detection & smart configuration");
        Console.WriteLine();
        Console.WriteLine("    🎯 Try These First:");
        Console.WriteLine();
        Console.WriteLine("    pidgeon welcome                      # Interactive demo");
        Console.WriteLine("    pidgeon --init                       # Machine setup");
        Console.WriteLine("    pidgeon generate ADT^A01             # Generate test message");
        Console.WriteLine();
    }

    /// <summary>
    /// Gets the build date from assembly metadata.
    /// </summary>
    private static DateTime GetBuildDate(Assembly assembly)
    {
        // Try to get build date from assembly attributes
        var buildAttribute = assembly.GetCustomAttribute<AssemblyMetadataAttribute>()
            ?.Value;

        if (DateTime.TryParse(buildAttribute, out var buildDate))
        {
            return buildDate;
        }

        // Fallback to app directory for single-file deployments
        var appDirectory = AppContext.BaseDirectory;
        if (!string.IsNullOrEmpty(appDirectory) && Directory.Exists(appDirectory))
        {
            return Directory.GetCreationTime(appDirectory);
        }

        return DateTime.Now;
    }
}