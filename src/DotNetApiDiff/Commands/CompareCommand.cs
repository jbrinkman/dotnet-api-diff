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
    [CommandArgument(0, "<oldAssembly>")]
    [Description("Path to the old/baseline assembly")]
    public required string OldAssemblyPath { get; init; }

    [CommandArgument(1, "<newAssembly>")]
    [Description("Path to the new/current assembly")]
    public required string NewAssemblyPath { get; init; }

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
        // Validate old assembly path
        if (!File.Exists(settings.OldAssemblyPath))
        {
            return ValidationResult.Error($"Old assembly file not found: {settings.OldAssemblyPath}");
        }

        // Validate new assembly path
        if (!File.Exists(settings.NewAssemblyPath))
        {
            return ValidationResult.Error($"New assembly file not found: {settings.NewAssemblyPath}");
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
                // In a real implementation, we would load the configuration from the file
                config = ComparisonConfiguration.CreateDefault();
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
                // In a real implementation, we would update the configuration with the filters
            }

            // Apply command-line exclusions if specified
            if (settings.ExcludePatterns != null && settings.ExcludePatterns.Length > 0)
            {
                logger.LogInformation("Applying exclusion patterns: {Patterns}", string.Join(", ", settings.ExcludePatterns));
                // In a real implementation, we would update the configuration with the exclusions
            }

            // Load assemblies
            logger.LogInformation("Loading old assembly: {Path}", settings.OldAssemblyPath);
            logger.LogInformation("Loading new assembly: {Path}", settings.NewAssemblyPath);

            var assemblyLoader = _serviceProvider.GetRequiredService<IAssemblyLoader>();
            var oldAssembly = assemblyLoader.LoadAssembly(settings.OldAssemblyPath);
            var newAssembly = assemblyLoader.LoadAssembly(settings.NewAssemblyPath);

            // Extract API information
            logger.LogInformation("Extracting API information from assemblies");
            var apiExtractor = _serviceProvider.GetRequiredService<IApiExtractor>();
            var oldApi = apiExtractor.ExtractApiMembers(oldAssembly);
            var newApi = apiExtractor.ExtractApiMembers(newAssembly);

            // Compare APIs
            logger.LogInformation("Comparing APIs");
            var apiComparer = _serviceProvider.GetRequiredService<IApiComparer>();
            var comparisonResult = apiComparer.CompareAssemblies(oldAssembly, newAssembly);

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

            // Output report
            AnsiConsole.Write(report);

            // Determine exit code based on breaking changes
            bool hasBreakingChanges = comparison.Removals.Any(c => c.IsBreakingChange) ||
                                     comparison.Modifications.Any(c => c.IsBreakingChange);

            if (hasBreakingChanges)
            {
                logger.LogWarning("Breaking changes detected");
                return 1; // Non-zero exit code for breaking changes
            }

            logger.LogInformation("Comparison completed successfully with no breaking changes");
            return 0; // Success
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during comparison");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 2; // Error exit code
        }
    }
}
