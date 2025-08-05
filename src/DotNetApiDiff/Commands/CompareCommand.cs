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
    public string? OutputFormat { get; set; }

    [CommandOption("-p|--output-file <path>")]
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
    private readonly IExitCodeManager _exitCodeManager;
    private readonly IGlobalExceptionHandler _exceptionHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompareCommand"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="exitCodeManager">The exit code manager.</param>
    /// <param name="exceptionHandler">The global exception handler.</param>
    public CompareCommand(
        IServiceProvider serviceProvider,
        ILogger<CompareCommand> logger,
        IExitCodeManager exitCodeManager,
        IGlobalExceptionHandler exceptionHandler)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _exitCodeManager = exitCodeManager;
        _exceptionHandler = exceptionHandler;
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

        // Validate output format if provided
        if (!string.IsNullOrEmpty(settings.OutputFormat))
        {
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
                            return _exitCodeManager.GetExitCodeForException(ex);
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

                // NOW CREATE THE COMMAND-SPECIFIC CONTAINER
                _logger.LogInformation("Creating command-specific service container with loaded configuration");

                var commandServices = new ServiceCollection();

                // Reuse shared services from root container
                var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
                commandServices.AddSingleton(loggerFactory);
                commandServices.AddLogging(); // This adds ILogger<T> services

                // Add the loaded configuration
                commandServices.AddSingleton(config);                // Add all business logic services with configuration-aware instances
                commandServices.AddScoped<IAssemblyLoader, AssemblyLoading.AssemblyLoader>();
                commandServices.AddScoped<IApiExtractor, ApiExtraction.ApiExtractor>();
                commandServices.AddScoped<IMemberSignatureBuilder, ApiExtraction.MemberSignatureBuilder>();
                commandServices.AddScoped<ITypeAnalyzer, ApiExtraction.TypeAnalyzer>();
                commandServices.AddScoped<IDifferenceCalculator, ApiExtraction.DifferenceCalculator>();
                commandServices.AddScoped<IReportGenerator, Reporting.ReportGenerator>();

                // Add configuration-specific services
                commandServices.AddScoped<INameMapper>(provider =>
                {
                    return new ApiExtraction.NameMapper(
                        config.Mappings,
                        loggerFactory.CreateLogger<ApiExtraction.NameMapper>());
                });

                commandServices.AddScoped<IChangeClassifier>(provider =>
                    new ApiExtraction.ChangeClassifier(
                        config.BreakingChangeRules,
                        config.Exclusions,
                        loggerFactory.CreateLogger<ApiExtraction.ChangeClassifier>()));

                // Add the main comparison service that depends on configured services
                commandServices.AddScoped<IApiComparer>(provider =>
                    new ApiExtraction.ApiComparer(
                        provider.GetRequiredService<IApiExtractor>(),
                        provider.GetRequiredService<IDifferenceCalculator>(),
                        provider.GetRequiredService<INameMapper>(),
                        provider.GetRequiredService<IChangeClassifier>(),
                        config,
                        provider.GetRequiredService<ILogger<ApiExtraction.ApiComparer>>()));

                // Execute the command with the configured services
                using (var commandProvider = commandServices.BuildServiceProvider())
                {
                    return ExecuteWithConfiguredServices(settings, config, commandProvider);
                }
            }
        }
        catch (Exception ex)
        {
            // Use our centralized exception handler for any unhandled exceptions
            return _exceptionHandler.HandleException(ex, "Compare command execution");
        }
    }

    /// <summary>
    /// Executes the comparison logic using the configured services
    /// </summary>
    /// <param name="settings">Command settings</param>
    /// <param name="config">Loaded configuration</param>
    /// <param name="serviceProvider">Command-specific service provider</param>
    /// <returns>Exit code</returns>
    private int ExecuteWithConfiguredServices(CompareCommandSettings settings, ComparisonConfiguration config, IServiceProvider serviceProvider)
    {
        // Load assemblies
        Assembly sourceAssembly;
        Assembly targetAssembly;

        using (_logger.BeginScope("Assembly loading"))
        {
            _logger.LogInformation("Loading source assembly: {Path}", settings.SourceAssemblyPath);
            _logger.LogInformation("Loading target assembly: {Path}", settings.TargetAssemblyPath);

            var assemblyLoader = serviceProvider.GetRequiredService<IAssemblyLoader>();

            try
            {
                sourceAssembly = assemblyLoader.LoadAssembly(settings.SourceAssemblyPath!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load source assembly: {Path}", settings.SourceAssemblyPath);
                AnsiConsole.MarkupLine($"[red]Error loading source assembly:[/] {ex.Message}");

                return _exitCodeManager.GetExitCodeForException(ex);
            }

            try
            {
                targetAssembly = assemblyLoader.LoadAssembly(settings.TargetAssemblyPath!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load target assembly: {Path}", settings.TargetAssemblyPath);
                AnsiConsole.MarkupLine($"[red]Error loading target assembly:[/] {ex.Message}");

                return _exitCodeManager.GetExitCodeForException(ex);
            }
        }

        // Extract API information
        using (_logger.BeginScope("API extraction"))
        {
            _logger.LogInformation("Extracting API information from assemblies");
            var apiExtractor = serviceProvider.GetRequiredService<IApiExtractor>();

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
            var apiComparer = serviceProvider.GetRequiredService<IApiComparer>();

            try
            {
                // Use the single CompareAssemblies method - configuration is now injected into dependencies
                comparisonResult = apiComparer.CompareAssemblies(sourceAssembly, targetAssembly);

                // Update configuration with actual command-line values ONLY if explicitly provided by user
                if (!string.IsNullOrEmpty(settings.OutputFormat) && Enum.TryParse<ReportFormat>(settings.OutputFormat, true, out var outputFormat))
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

                return _exitCodeManager.GetExitCodeForException(ex);
            }
        }

        // Create ApiComparison from ComparisonResult
        var comparison = CreateApiComparisonFromResult(comparisonResult);

        // Generate report
        using (_logger.BeginScope("Report generation"))
        {
            // Use the configuration from the comparison result, which now has the correct precedence applied
            var effectiveFormat = comparisonResult.Configuration.OutputFormat;
            var effectiveOutputFile = comparisonResult.Configuration.OutputPath;

            _logger.LogInformation("Generating {Format} report", effectiveFormat);
            var reportGenerator = serviceProvider.GetRequiredService<IReportGenerator>();

            string report;
            try
            {
                if (string.IsNullOrEmpty(effectiveOutputFile))
                {
                    // No output file specified - output to console regardless of format
                    report = reportGenerator.GenerateReport(comparisonResult, effectiveFormat);

                    // Output the formatted report to the console
                    // Use Console.Write to avoid format string interpretation issues
                    Console.Write(report);
                }
                else
                {
                    // Output file specified - save to the specified file
                    reportGenerator.SaveReportAsync(comparisonResult, effectiveFormat, effectiveOutputFile).GetAwaiter().GetResult();
                    _logger.LogInformation("Report saved to {OutputFile}", effectiveOutputFile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating {Format} report", effectiveFormat);
                AnsiConsole.MarkupLine($"[red]Error generating report:[/] {ex.Message}");

                return _exitCodeManager.GetExitCodeForException(ex);
            }
        }

        // Use the ExitCodeManager to determine the appropriate exit code
        int exitCode = _exitCodeManager.GetExitCode(comparison);

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
            _exitCodeManager.GetExitCodeDescription(exitCode));

        return exitCode;
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
                    Description = d.Description,
                    TargetMember = new Models.ApiMember
                    {
                        Name = ExtractMemberName(d.ElementName),
                        FullName = d.ElementName,
                        Signature = d.NewSignature ?? "Unknown"
                    },
                    IsBreakingChange = d.IsBreakingChange
                }).ToList(),
            Removals = comparisonResult.Differences
                .Where(d => d.ChangeType == Models.ChangeType.Removed)
                .Select(d => new Models.ApiChange
                {
                    Type = Models.ChangeType.Removed,
                    Description = d.Description,
                    SourceMember = new Models.ApiMember
                    {
                        Name = ExtractMemberName(d.ElementName),
                        FullName = d.ElementName,
                        Signature = d.OldSignature ?? "Unknown"
                    },
                    IsBreakingChange = d.IsBreakingChange
                }).ToList(),
            Modifications = comparisonResult.Differences
                .Where(d => d.ChangeType == Models.ChangeType.Modified)
                .Select(d => new Models.ApiChange
                {
                    Type = Models.ChangeType.Modified,
                    Description = d.Description,
                    SourceMember = new Models.ApiMember
                    {
                        Name = ExtractMemberName(d.ElementName),
                        FullName = d.ElementName,
                        Signature = d.OldSignature ?? "Unknown"
                    },
                    TargetMember = new Models.ApiMember
                    {
                        Name = ExtractMemberName(d.ElementName),
                        FullName = d.ElementName,
                        Signature = d.NewSignature ?? "Unknown"
                    },
                    IsBreakingChange = d.IsBreakingChange
                }).ToList(),
            Excluded = comparisonResult.Differences
                .Where(d => d.ChangeType == Models.ChangeType.Excluded)
                .Select(d => new Models.ApiChange
                {
                    Type = Models.ChangeType.Excluded,
                    Description = d.Description,
                    SourceMember = new Models.ApiMember
                    {
                        Name = ExtractMemberName(d.ElementName),
                        FullName = d.ElementName,
                        Signature = "Unknown"
                    },
                    IsBreakingChange = false
                }).ToList()
        };
    }

    /// <summary>
    /// Extracts the member name from a full element name
    /// </summary>
    /// <param name="elementName">The full element name</param>
    /// <returns>The member name</returns>
    private static string ExtractMemberName(string elementName)
    {
        if (string.IsNullOrEmpty(elementName))
            return "Unknown";

        // For full names like "Namespace.Class.Method", extract just "Method"
        var lastDotIndex = elementName.LastIndexOf('.');
        return lastDotIndex >= 0 ? elementName.Substring(lastDotIndex + 1) : elementName;
    }
}
