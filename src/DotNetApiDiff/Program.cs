// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Commands;
using DotNetApiDiff.ExitCodes;
using DotNetApiDiff.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace DotNetApiDiff;

/// <summary>
/// Main entry point for the DotNet API Diff tool
/// </summary>
public class Program
{
    /// <summary>
    /// Application entry point
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Exit code (0 for success, non-zero for failure)</returns>
    public static int Main(string[] args)
    {
        // Set up dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services);

        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var exceptionHandler = serviceProvider.GetRequiredService<IGlobalExceptionHandler>();

        // Set up global exception handling
        exceptionHandler.SetupGlobalExceptionHandling();

        try
        {
            // Log application start with version information
            var version = typeof(Program).Assembly.GetName().Version;
            var buildTime = GetBuildTime();

            logger.LogInformation(
                "DotNet API Diff Tool v{Version} started at {Time} (Build: {BuildTime})",
                version,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                buildTime);

            // Log environment information
            logger.LogDebug(
                "Environment: OS={OS}, Framework={Framework}, ProcessorCount={ProcessorCount}",
                Environment.OSVersion,
                Environment.Version,
                Environment.ProcessorCount);

            // Get the exit code manager
            var exitCodeManager = serviceProvider.GetRequiredService<IExitCodeManager>();

            // Configure Spectre.Console command app
            var app = new CommandApp(new TypeRegistrar(serviceProvider));

            app.Configure(config =>
            {
                config.SetApplicationName("dotnet-api-diff");
                config.PropagateExceptions();

                config.AddExample(new[] { "compare", "source.dll", "target.dll" });
                config.AddExample(new[] { "compare", "source.dll", "target.dll", "--output", "json" });
                config.AddExample(new[] { "compare", "source.dll", "target.dll", "--config", "config.json" });

                config.SetExceptionHandler(ex =>
                {
                    // Use our centralized exception handler
                    return exceptionHandler.HandleException(ex, "Command execution");
                });

                // Register the compare command
                config.AddCommand<CompareCommand>("compare")
                    .WithDescription("Compare two .NET assemblies and report API differences")
                    .WithExample(new[] { "compare", "source.dll", "target.dll" })
                    .WithExample(new[] { "compare", "source.dll", "target.dll", "--output", "json" })
                    .WithExample(new[] { "compare", "source.dll", "target.dll", "--filter", "System.Collections" });
            });

            // Run the command and return the exit code
            return app.Run(args);
        }
        catch (Exception ex)
        {
            // Use our centralized exception handler for any unhandled exceptions
            return exceptionHandler.HandleException(ex, "Application startup");
        }
    }

    /// <summary>
    /// Gets the build time of the assembly from the file timestamp
    /// </summary>
    /// <returns>Build time as a string</returns>
    private static string GetBuildTime()
    {
        try
        {
            var assembly = typeof(Program).Assembly;
            var assemblyLocation = assembly.Location;
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                var buildTime = File.GetLastWriteTime(assemblyLocation);
                return buildTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
        catch (Exception)
        {
            // Ignore errors getting build time
        }

        return "Unknown";
    }

    /// <summary>
    /// Configures dependency injection services
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    private static void ConfigureServices(IServiceCollection services)
    {
        // Configure logging with structured logging support
        services.AddLogging(builder =>
        {
            builder.AddConsole(options =>
            {
                // Use ConsoleFormatterOptions instead of deprecated ConsoleLoggerOptions
                options.FormatterName = "simple";
            });

            // Configure the simple console formatter
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                options.SingleLine = false;
                options.UseUtcTimestamp = false;
            });

            // Set minimum level based on environment variable if present
            var logLevelEnv = Environment.GetEnvironmentVariable("DOTNET_API_DIFF_LOG_LEVEL");
            var logLevel = LogLevel.Information; // Default level

            if (!string.IsNullOrEmpty(logLevelEnv) &&
                Enum.TryParse<LogLevel>(logLevelEnv, true, out var parsedLevel))
            {
                logLevel = parsedLevel;
            }

            builder.SetMinimumLevel(logLevel);
        });

        // Register core services
        services.AddScoped<IAssemblyLoader, AssemblyLoading.AssemblyLoader>();
        services.AddScoped<IApiExtractor, ApiExtraction.ApiExtractor>();
        services.AddScoped<ITypeAnalyzer, ApiExtraction.TypeAnalyzer>();
        services.AddScoped<IMemberSignatureBuilder, ApiExtraction.MemberSignatureBuilder>();
        services.AddScoped<IDifferenceCalculator, ApiExtraction.DifferenceCalculator>();

        // Register the NameMapper
        services.AddScoped<INameMapper>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<ApiExtraction.NameMapper>>();

            // Create a default mapping configuration - in real usage this would be loaded from config
            var mappingConfig = Models.Configuration.MappingConfiguration.CreateDefault();
            return new ApiExtraction.NameMapper(mappingConfig, logger);
        });

        // Register the ChangeClassifier
        services.AddScoped<IChangeClassifier>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<ApiExtraction.ChangeClassifier>>();

            // Create default configurations - in real usage these would be loaded from config
            var breakingChangeRules = Models.Configuration.BreakingChangeRules.CreateDefault();
            var exclusionConfig = Models.Configuration.ExclusionConfiguration.CreateDefault();

            return new ApiExtraction.ChangeClassifier(breakingChangeRules, exclusionConfig, logger);
        });

        // Register the ApiComparer with NameMapper
        services.AddScoped<IApiComparer, ApiExtraction.ApiComparer>();

        // Register the ReportGenerator
        services.AddScoped<IReportGenerator, Reporting.ReportGenerator>();

        // Register the ExitCodeManager
        services.AddSingleton<IExitCodeManager, ExitCodeManager>();

        // Register the GlobalExceptionHandler
        services.AddSingleton<IGlobalExceptionHandler, GlobalExceptionHandler>();
    }
}
