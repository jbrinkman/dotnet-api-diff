// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Commands;
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

        try
        {
            logger.LogInformation("DotNet API Diff Tool started");

            // Configure Spectre.Console command app
            var app = new CommandApp(new TypeRegistrar(serviceProvider));

            app.Configure(config =>
            {
                config.SetApplicationName("dotnet-api-diff");

                config.AddExample(new[] { "compare", "source.dll", "target.dll" });
                config.AddExample(new[] { "compare", "source.dll", "target.dll", "--output", "json" });
                config.AddExample(new[] { "compare", "source.dll", "target.dll", "--config", "config.json" });

                config.SetExceptionHandler(ex =>
                {
                    logger.LogError(ex, "An unhandled exception occurred");
                    return 1;
                });

                // Register the compare command
                config.AddCommand<CompareCommand>("compare")
                    .WithDescription("Compare two .NET assemblies and report API differences")
                    .WithExample(new[] { "compare", "source.dll", "target.dll" })
                    .WithExample(new[] { "compare", "source.dll", "target.dll", "--output", "json" })
                    .WithExample(new[] { "compare", "source.dll", "target.dll", "--filter", "System.Collections" });
            });

            return app.Run(args);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during execution");
            return 1;
        }
    }

    /// <summary>
    /// Configures dependency injection services
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    private static void ConfigureServices(IServiceCollection services)
    {
        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Register interfaces - implementations will be added in subsequent tasks
        // services.AddScoped<IAssemblyLoader, AssemblyLoader>();
        // services.AddScoped<IApiExtractor, ApiExtractor>();
        // services.AddScoped<ITypeAnalyzer, TypeAnalyzer>();
        // services.AddScoped<IMemberSignatureBuilder, MemberSignatureBuilder>();
        // services.AddScoped<IDifferenceCalculator, DifferenceCalculator>();

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
        // services.AddScoped<IApiComparer, ApiComparer>();

        // Register the ReportGenerator
        services.AddScoped<IReportGenerator, Reporting.ReportGenerator>();
    }
}
