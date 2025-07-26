// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using DotNetApiDiff.Models.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DotNetApiDiff.Commands;

/// <summary>
/// Settings for the compare command
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public class CompareCommandSettings : CommandSettings
{
    [CommandArgument(0, "<sourceAssembly>")]
    [Description("Path to the source/baseline assembly")]
    public string? SourceAssemblyPath { get; set; }

    [CommandArgument(1, "<targetAssembly>")]
    [Description("Path to the target/current assembly")]
    public string? TargetAssemblyPath { get; set; }

    [CommandOption("-c|--config <configFile>")]
    [Description("Path to configuration file")]
    public string? ConfigFile { get; set; }

    [CommandOption("-o|--output <format>")]
    [Description("Output format (console, json, html, markdown)")]
    [DefaultValue("console")]
    public string OutputFormat { get; set; } = "console";

    [CommandOption("--output-file <path>")]
    [Description("Output file path (required for json, html, markdown formats)")]
    public string? OutputFile { get; set; }

    [CommandOption("-f|--filter <namespace>")]
    [Description("Filter to specific namespaces (can be specified multiple times)")]
    public string[]? NamespaceFilters { get; set; }

    [CommandOption("-e|--exclude <pattern>")]
    [Description("Exclude types matching pattern (can be specified multiple times)")]
    public string[]? ExcludePatterns { get; set; }

    [CommandOption("-t|--type <pattern>")]
    [Description("Filter to specific type patterns (can be specified multiple times)")]
    public string[]? TypePatterns { get; set; }

    [CommandOption("--include-internals")]
    [Description("Include internal types in the comparison")]
    [DefaultValue(false)]
    public bool IncludeInternals { get; set; }

    [CommandOption("--include-compiler-generated")]
    [Description("Include compiler-generated types in the comparison")]
    [DefaultValue(false)]
    public bool IncludeCompilerGenerated { get; set; }

    [CommandOption("--no-color")]
    [Description("Disable colored output")]
    [DefaultValue(false)]
    public bool NoColor { get; set; }

    [CommandOption("-v|--verbose")]
    [Description("Enable verbose output")]
    [DefaultValue(false)]
    public bool Verbose { get; set; }
}

/// <summary>
/// Command to compare two assemblies
/// </summary>
public class CompareCommand : Command<CompareCommandSettings>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CompareCommand> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompareCommand"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">The logger.</param>
    public CompareCommand(IServiceProvider serviceProvider, ILogger<CompareCommand> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Validates the command settings
    /// </summary>
    /// <param name="context">The command context</param>
    /// <param name="settings">The command settings</param>
    /// <returns>ValidationResult indicating success or failure</returns>
    public override ValidationResult Validate([NotNull] CommandContext context, [NotNull] CompareCommandSettings settings)
    {
        // Validate source assembly path
        if (string.IsNullOrEmpty(settings.SourceAssemblyPath))
        {
            return ValidationResult.Error("Source assembly path is required");
        }

        if (!File.Exists(settings.SourceAssemblyPath))
        {
            return ValidationResult.Error($"Source assembly file not found: {settings.SourceAssemblyPath}");
        }

        // Validate target assembly path
        if (string.IsNullOrEmpty(settings.TargetAssemblyPath))
        {
            return ValidationResult.Error("Target assembly path is required");
        }

        if (!File.Exists(settings.TargetAssemblyPath))
        {
            return ValidationResult.Error($"Target assembly file not found: {settings.TargetAssemblyPath}");
        }

        // Validate config file if specified
        if (!string.IsNullOrEmpty(settings.ConfigFile) && !File.Exists(settings.ConfigFile))
        {
            return ValidationResult.Error($"Configuration file not found: {settings.ConfigFile}");
        }

        // Validate output format
        string format = settings.OutputFormat.ToLowerInvariant();
        if (format != "console" && format != "json" && format != "html" && format != "markdown")
        {
            return ValidationResult.Error($"Invalid output format: {settings.OutputFormat}. Valid formats are: console, json, html, markdown");
        }

        // Validate output file requirements
        if (format == "html")
        {
            // HTML format requires an output file
            if (string.IsNullOrEmpty(settings.OutputFile))
            {
                return ValidationResult.Error($"Output file is required for {settings.OutputFormat} format. Use --output-file to specify the output file path.");
            }

            // Validate output directory exists
            var outputDir = Path.GetDirectoryName(settings.OutputFile);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                return ValidationResult.Error($"Output directory does not exist: {outputDir}");
            }
        }
        else if (!string.IsNullOrEmpty(settings.OutputFile))
        {
            // If output file is specified for non-HTML formats, validate the directory exists
            var outputDir = Path.GetDirectoryName(settings.OutputFile);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                return ValidationResult.Error($"Output directory does not exist: {outputDir}");
            }
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Executes the command
    /// </summary>
    /// <param name="context">The command context</param>
    /// <param name="settings">The command settings</param>
    /// <returns>Exit code (0 for success, non-zero for failure)</returns>
    public override int Execute([NotNull] CommandContext context, [NotNull] CompareCommandSettings settings)
    {
        var exceptionHandler = _serviceProvider.GetRequiredService<IGlobalExceptionHandler>();

        try
        {
            // Create a logging scope for this command execution
            using (_logger.BeginScope("Compare command execution"))
            {
                // Set up logging level based on verbose flag
                if (settings.Verbose)
                {
                    _logger.LogInformation("Verbose logging enabled");
                }

                // Configure console output
                if (settings.NoColor)
                {
                    _logger.LogDebug("Disabling colored output");
                    AnsiConsole.Profile.Capabilities.ColorSystem = ColorSystem.NoColors;
                }

                // Load configuration
                ComparisonConfiguration config;
                if (!string.IsNullOrEmpty(settings.ConfigFile))
                {
                    using (_logger.BeginScope("Configuration loading"))
                    {
                        _logger.LogInformation("Loading configuration from {ConfigFile}", settings.ConfigFile);

                        try
                        {
                            // Verify the file exists and is accessible
                            if (!File.Exists(settings.ConfigFile))
                            {
                                throw new FileNotFoundException($"Configuration file not found: {settings.ConfigFile}", settings.ConfigFile);
                            }

                            // Try to load the configuration
                            config = ComparisonConfiguration.LoadFromJsonFile(settings.ConfigFile);
                            _logger.LogInformation("Configuration loaded successfully");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error loading configuration from {ConfigFile}", settings.ConfigFile);
                            AnsiConsole.MarkupLine($"[red]Error loading configuration:[/] {ex.Message}");

                            // Use the ExitCodeManager to determine the appropriate exit code for errors
                            var configErrorExitCodeManager = _serviceProvider.GetRequiredService<IExitCodeManager>();
                            return configErrorExitCodeManager.GetExitCodeForException(ex);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Using default configuration");
                    config = ComparisonConfiguration.CreateDefault();
                }

                // Apply command-line filters and options
                ApplyCommandLineOptions(settings, config);

                // Load assemblies
                Assembly sourceAssembly;
                Assembly targetAssembly;

                using (_logger.BeginScope("Assembly loading"))
                {
                    _logger.LogInformation("Loading source assembly: {Path}", settings.SourceAssemblyPath);
                    _logger.LogInformation("Loading target assembly: {Path}", settings.TargetAssemblyPath);

                    var assemblyLoader = _serviceProvider.GetRequiredService<IAssemblyLoader>();

                    try
                    {
                        sourceAssembly = assemblyLoader.LoadAssembly(settings.SourceAssemblyPath!);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load source assembly: {Path}", settings.SourceAssemblyPath);
                        AnsiConsole.MarkupLine($"[red]Error loading source assembly:[/] {ex.Message}");

                        var sourceAssemblyExitCodeManager = _serviceProvider.GetRequiredService<IExitCodeManager>();
                        return sourceAssemblyExitCodeManager.GetExitCodeForException(ex);
                    }

                    try
                    {
                        targetAssembly = assemblyLoader.LoadAssembly(settings.TargetAssemblyPath!);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load target assembly: {Path}", settings.TargetAssemblyPath);
                        AnsiConsole.MarkupLine($"[red]Error loading target assembly:[/] {ex.Message}");

                        var targetAssemblyExitCodeManager = _serviceProvider.GetRequiredService<IExitCodeManager>();
                        return targetAssemblyExitCodeManager.GetExitCodeForException(ex);
                    }
                }

                // Extract API information
                using (_logger.BeginScope("API extraction"))
                {
                    _logger.LogInformation("Extracting API information from assemblies");
                    var apiExtractor = _serviceProvider.GetRequiredService<IApiExtractor>();

                    // Pass the filter configuration to the API extractor
                    var sourceApi = apiExtractor.ExtractApiMembers(sourceAssembly, config.Filters);
                    var targetApi = apiExtractor.ExtractApiMembers(targetAssembly, config.Filters);

                    // Log the number of API members extracted
                    _logger.LogInformation(
                        "Extracted {SourceCount} API members from source and {TargetCount} API members from target",
                        sourceApi.Count(),
                        targetApi.Count());
                }

                // Compare APIs
                Models.ComparisonResult comparisonResult;
                using (_logger.BeginScope("API comparison"))
                {
                    _logger.LogInformation("Comparing APIs");
                    var apiComparer = _serviceProvider.GetRequiredService<IApiComparer>();

                    try
                    {
                        comparisonResult = apiComparer.CompareAssemblies(sourceAssembly, targetAssembly);

                        // Include the configuration in the result for reporting, updating it with actual runtime values
                        comparisonResult.Configuration = config;

                        // Update configuration with actual command-line values used for this comparison
                        if (Enum.TryParse<ReportFormat>(settings.OutputFormat, true, out var outputFormat))
                        {
                            comparisonResult.Configuration.OutputFormat = outputFormat;
                        }
                        if (!string.IsNullOrEmpty(settings.OutputFile))
                        {
                            comparisonResult.Configuration.OutputPath = settings.OutputFile;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error comparing assemblies");
                        AnsiConsole.MarkupLine($"[red]Error comparing assemblies:[/] {ex.Message}");

                        var comparisonExitCodeManager = _serviceProvider.GetRequiredService<IExitCodeManager>();
                        return comparisonExitCodeManager.GetExitCodeForException(ex);
                    }
                }

                // Create ApiComparison from ComparisonResult
                var comparison = CreateApiComparisonFromResult(comparisonResult);

                // Generate report
                using (_logger.BeginScope("Report generation"))
                {
                    _logger.LogInformation("Generating {Format} report", settings.OutputFormat);
                    var reportGenerator = _serviceProvider.GetRequiredService<IReportGenerator>();

                    // Convert string format to ReportFormat enum
                    ReportFormat format = settings.OutputFormat.ToLowerInvariant() switch
                    {
                        "json" => ReportFormat.Json,
                        "xml" => ReportFormat.Xml,
                        "html" => ReportFormat.Html,
                        "markdown" => ReportFormat.Markdown,
                        _ => ReportFormat.Console
                    };

                    string report;
                    try
                    {
                        if (string.IsNullOrEmpty(settings.OutputFile))
                        {
                            // No output file specified - output to console regardless of format
                            report = reportGenerator.GenerateReport(comparisonResult, format);

                            // Output the formatted report to the console
                            // Use Console.Write to avoid format string interpretation issues
                            Console.Write(report);
                        }
                        else
                        {
                            // Output file specified - save to the specified file
                            reportGenerator.SaveReportAsync(comparisonResult, format, settings.OutputFile).GetAwaiter().GetResult();
                            _logger.LogInformation("Report saved to {OutputFile}", settings.OutputFile);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generating {Format} report", format);
                        AnsiConsole.MarkupLine($"[red]Error generating report:[/] {ex.Message}");

                        var reportExitCodeManager = _serviceProvider.GetRequiredService<IExitCodeManager>();
                        return reportExitCodeManager.GetExitCodeForException(ex);
                    }
                }

                // Use the ExitCodeManager to determine the appropriate exit code
                var exitCodeManager = _serviceProvider.GetRequiredService<IExitCodeManager>();
                int exitCode = exitCodeManager.GetExitCode(comparison);

                if (comparison.HasBreakingChanges)
                {
                    _logger.LogWarning("{Count} breaking changes detected", comparison.BreakingChangesCount);
                }
                else
                {
                    _logger.LogInformation("Comparison completed successfully with no breaking changes");
                }

                _logger.LogInformation(
                    "Exiting with code {ExitCode}: {Description}",
                    exitCode,
                    exitCodeManager.GetExitCodeDescription(exitCode));

                return exitCode;
            }
        }
        catch (Exception ex)
        {
            // Use our centralized exception handler for any unhandled exceptions
            return exceptionHandler.HandleException(ex, "Compare command execution");
        }
    }

    /// <summary>
    /// Applies command-line options to the configuration
    /// </summary>
    /// <param name="settings">Command settings</param>
    /// <param name="config">Configuration to update</param>
    /// <param name="logger">Logger for diagnostic information</param>
    private void ApplyCommandLineOptions(CompareCommandSettings settings, Models.Configuration.ComparisonConfiguration config)
    {
        using (_logger.BeginScope("Applying command-line options"))
        {
            // Apply namespace filters if specified
            if (settings.NamespaceFilters != null && settings.NamespaceFilters.Length > 0)
            {
                _logger.LogInformation("Applying namespace filters: {Filters}", string.Join(", ", settings.NamespaceFilters));

                // Add namespace filters to the configuration
                config.Filters.IncludeNamespaces.AddRange(settings.NamespaceFilters);

                // If we have explicit includes, we're filtering to only those namespaces
                if (config.Filters.IncludeNamespaces.Count > 0)
                {
                    _logger.LogInformation("Filtering to only include specified namespaces");
                }
            }

            // Apply type pattern filters if specified
            if (settings.TypePatterns != null && settings.TypePatterns.Length > 0)
            {
                _logger.LogInformation("Applying type pattern filters: {Patterns}", string.Join(", ", settings.TypePatterns));

                // Add type pattern filters to the configuration
                config.Filters.IncludeTypes.AddRange(settings.TypePatterns);

                _logger.LogInformation("Filtering to only include types matching specified patterns");
            }

            // Apply command-line exclusions if specified
            if (settings.ExcludePatterns != null && settings.ExcludePatterns.Length > 0)
            {
                _logger.LogInformation("Applying exclusion patterns: {Patterns}", string.Join(", ", settings.ExcludePatterns));

                // Add exclusion patterns to the configuration
                foreach (var pattern in settings.ExcludePatterns)
                {
                    // Determine if this is a namespace or type pattern based on presence of dot
                    if (pattern.Contains('.'))
                    {
                        // Assume it's a type pattern if it contains a dot
                        config.Exclusions.ExcludedTypePatterns.Add(pattern);
                    }
                    else
                    {
                        // Otherwise assume it's a namespace pattern
                        config.Filters.ExcludeNamespaces.Add(pattern);
                    }
                }
            }

            // Apply internal types inclusion if specified
            if (settings.IncludeInternals)
            {
                _logger.LogInformation("Including internal types in comparison");
                config.Filters.IncludeInternals = true;
            }

            // Apply compiler-generated types inclusion if specified
            if (settings.IncludeCompilerGenerated)
            {
                _logger.LogInformation("Including compiler-generated types in comparison");
                config.Filters.IncludeCompilerGenerated = true;
            }
        }
    }

    /// <summary>
    /// Creates an ApiComparison object from a ComparisonResult
    /// </summary>
    /// <param name="comparisonResult">The comparison result to convert</param>
    /// <returns>An ApiComparison object</returns>
    private Models.ApiComparison CreateApiComparisonFromResult(Models.ComparisonResult comparisonResult)
    {
        return new Models.ApiComparison
        {
            Additions = comparisonResult.Differences
                .Where(d => d.ChangeType == Models.ChangeType.Added)
                .Select(d => new Models.ApiChange
                {
                    Type = Models.ChangeType.Added,
                    TargetMember = new Models.ApiMember { Name = d.ElementName },
                    IsBreakingChange = d.IsBreakingChange
                }).ToList(),
            Removals = comparisonResult.Differences
                .Where(d => d.ChangeType == Models.ChangeType.Removed)
                .Select(d => new Models.ApiChange
                {
                    Type = Models.ChangeType.Removed,
                    SourceMember = new Models.ApiMember { Name = d.ElementName },
                    IsBreakingChange = d.IsBreakingChange
                }).ToList(),
            Modifications = comparisonResult.Differences
                .Where(d => d.ChangeType == Models.ChangeType.Modified)
                .Select(d => new Models.ApiChange
                {
                    Type = Models.ChangeType.Modified,
                    SourceMember = new Models.ApiMember { Name = d.ElementName },
                    TargetMember = new Models.ApiMember { Name = d.ElementName },
                    IsBreakingChange = d.IsBreakingChange
                }).ToList(),
            Excluded = comparisonResult.Differences
                .Where(d => d.ChangeType == Models.ChangeType.Excluded)
                .Select(d => new Models.ApiChange
                {
                    Type = Models.ChangeType.Excluded,
                    SourceMember = new Models.ApiMember { Name = d.ElementName },
                    IsBreakingChange = false
                }).ToList()
        };
    }
}
