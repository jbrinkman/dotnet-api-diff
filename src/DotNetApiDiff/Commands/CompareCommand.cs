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

namespace DotNetApiDiff.Commands;

/// <summary>
/// Settings for the compare command
/// </summary>
public class CompareCommandSettings : CommandSettings
{
    [CommandArgument(0, "<sourceAssembly>")]
    [Description("Path to the source/baseline assembly")]
    required public string SourceAssemblyPath { get; init; }

    [CommandArgument(1, "<targetAssembly>")]
    [Description("Path to the target/current assembly")]
    required public string TargetAssemblyPath { get; init; }

    [CommandOption("-c|--config <configFile>")]
    [Description("Path to configuration file")]
    public string? ConfigFile { get; init; }

    [CommandOption("-o|--output <format>")]
    [Description("Output format (console, json, markdown)")]
    [DefaultValue("console")]
    public string OutputFormat { get; init; } = "console";

    [CommandOption("-f|--filter <namespace>")]
    [Description("Filter to specific namespaces (can be specified multiple times)")]
    public string[]? NamespaceFilters { get; init; }

    [CommandOption("-e|--exclude <pattern>")]
    [Description("Exclude types matching pattern (can be specified multiple times)")]
    public string[]? ExcludePatterns { get; init; }

    [CommandOption("-t|--type <pattern>")]
    [Description("Filter to specific type patterns (can be specified multiple times)")]
    public string[]? TypePatterns { get; init; }

    [CommandOption("--include-internals")]
    [Description("Include internal types in the comparison")]
    [DefaultValue(false)]
    public bool IncludeInternals { get; init; }

    [CommandOption("--include-compiler-generated")]
    [Description("Include compiler-generated types in the comparison")]
    [DefaultValue(false)]
    public bool IncludeCompilerGenerated { get; init; }

    [CommandOption("--no-color")]
    [Description("Disable colored output")]
    [DefaultValue(false)]
    public bool NoColor { get; init; }

    [CommandOption("-v|--verbose")]
    [Description("Enable verbose output")]
    [DefaultValue(false)]
    public bool Verbose { get; init; }
}

/// <summary>
/// Command to compare two assemblies
/// </summary>
public class CompareCommand : Command<CompareCommandSettings>
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompareCommand"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public CompareCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
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
        if (!File.Exists(settings.SourceAssemblyPath))
        {
            return ValidationResult.Error($"Source assembly file not found: {settings.SourceAssemblyPath}");
        }

        // Validate target assembly path
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
        if (format != "console" && format != "json" && format != "markdown")
        {
            return ValidationResult.Error($"Invalid output format: {settings.OutputFormat}. Valid formats are: console, json, markdown");
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
        var logger = _serviceProvider.GetRequiredService<ILogger<CompareCommand>>();

        try
        {
            // Set up logging level based on verbose flag
            if (settings.Verbose)
            {
                // This is a placeholder - in a real implementation we would configure the logging level
                logger.LogInformation("Verbose logging enabled");
            }

            // Load configuration
            ComparisonConfiguration config;
            if (!string.IsNullOrEmpty(settings.ConfigFile))
            {
                logger.LogInformation("Loading configuration from {ConfigFile}", settings.ConfigFile);

                try
                {
                    config = ComparisonConfiguration.LoadFromJsonFile(settings.ConfigFile);
                    logger.LogInformation("Configuration loaded successfully");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error loading configuration from {ConfigFile}", settings.ConfigFile);
                    AnsiConsole.MarkupLine($"[red]Error loading configuration:[/] {ex.Message}");

                    // Use the ExitCodeManager to determine the appropriate exit code for errors
                    var configErrorExitCodeManager = _serviceProvider.GetRequiredService<IExitCodeManager>();
                    return configErrorExitCodeManager.GetExitCode(false, true);
                }
            }
            else
            {
                logger.LogInformation("Using default configuration");
                config = ComparisonConfiguration.CreateDefault();
            }

            // Apply command-line filters if specified
            if (settings.NamespaceFilters != null && settings.NamespaceFilters.Length > 0)
            {
                logger.LogInformation("Applying namespace filters: {Filters}", string.Join(", ", settings.NamespaceFilters));

                // Add namespace filters to the configuration
                config.Filters.IncludeNamespaces.AddRange(settings.NamespaceFilters);

                // If we have explicit includes, we're filtering to only those namespaces
                if (config.Filters.IncludeNamespaces.Count > 0)
                {
                    logger.LogInformation("Filtering to only include specified namespaces");
                }
            }

            // Apply type pattern filters if specified
            if (settings.TypePatterns != null && settings.TypePatterns.Length > 0)
            {
                logger.LogInformation("Applying type pattern filters: {Patterns}", string.Join(", ", settings.TypePatterns));

                // Add type pattern filters to the configuration
                config.Filters.IncludeTypes.AddRange(settings.TypePatterns);

                logger.LogInformation("Filtering to only include types matching specified patterns");
            }

            // Apply command-line exclusions if specified
            if (settings.ExcludePatterns != null && settings.ExcludePatterns.Length > 0)
            {
                logger.LogInformation("Applying exclusion patterns: {Patterns}", string.Join(", ", settings.ExcludePatterns));

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
                logger.LogInformation("Including internal types in comparison");
                config.Filters.IncludeInternals = true;
            }

            // Apply compiler-generated types inclusion if specified
            if (settings.IncludeCompilerGenerated)
            {
                logger.LogInformation("Including compiler-generated types in comparison");
                config.Filters.IncludeCompilerGenerated = true;
            }

            // Load assemblies
            logger.LogInformation("Loading source assembly: {Path}", settings.SourceAssemblyPath);
            logger.LogInformation("Loading target assembly: {Path}", settings.TargetAssemblyPath);

            var assemblyLoader = _serviceProvider.GetRequiredService<IAssemblyLoader>();
            var sourceAssembly = assemblyLoader.LoadAssembly(settings.SourceAssemblyPath);
            var targetAssembly = assemblyLoader.LoadAssembly(settings.TargetAssemblyPath);

            // Extract API information
            logger.LogInformation("Extracting API information from assemblies");
            var apiExtractor = _serviceProvider.GetRequiredService<IApiExtractor>();

            // Pass the filter configuration to the API extractor
            var sourceApi = apiExtractor.ExtractApiMembers(sourceAssembly, config.Filters);
            var targetApi = apiExtractor.ExtractApiMembers(targetAssembly, config.Filters);

            // Compare APIs
            logger.LogInformation("Comparing APIs");
            var apiComparer = _serviceProvider.GetRequiredService<IApiComparer>();
            var comparisonResult = apiComparer.CompareAssemblies(sourceAssembly, targetAssembly);

            // Create ApiComparison from ComparisonResult
            var comparison = new Models.ApiComparison
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

            // Generate report
            logger.LogInformation("Generating {Format} report", settings.OutputFormat);
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

            var report = reportGenerator.GenerateReport(comparisonResult, format);

            // Output the formatted report to the console using the AnsiConsole library
            AnsiConsole.Write(report);

            // Use the ExitCodeManager to determine the appropriate exit code
            var exitCodeManager = _serviceProvider.GetRequiredService<IExitCodeManager>();
            int exitCode = exitCodeManager.GetExitCode(comparison);

            if (comparison.HasBreakingChanges)
            {
                logger.LogWarning("{Count} breaking changes detected", comparison.BreakingChangesCount);
            }
            else
            {
                logger.LogInformation("Comparison completed successfully with no breaking changes");
            }

            logger.LogInformation(
                "Exiting with code {ExitCode}: {Description}",
                exitCode,
                exitCodeManager.GetExitCodeDescription(exitCode));

            return exitCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during comparison");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");

            // Use the ExitCodeManager to determine the appropriate exit code for errors
            var exitCodeManager = _serviceProvider.GetRequiredService<IExitCodeManager>();
            int exitCode = exitCodeManager.GetExitCodeForException(ex);

            logger.LogInformation(
                "Exiting with code {ExitCode}: {Description}",
                exitCode,
                exitCodeManager.GetExitCodeDescription(exitCode));

            return exitCode;
        }
    }
}
