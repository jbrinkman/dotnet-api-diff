// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
    public static async Task<int> Main(string[] args)
    {
        // Set up dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services);

        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("DotNet API Diff Tool started");

            // TODO: Parse command line arguments and execute comparison
            // This will be implemented in subsequent tasks

            // Add a minimal await to satisfy the async method requirement
            await Task.Delay(0);

            logger.LogInformation("DotNet API Diff Tool completed successfully");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during execution");
            return 1;
        }
        finally
        {
            serviceProvider.Dispose();
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
        // services.AddScoped<IApiComparer, ApiComparer>();
        // services.AddScoped<IReportGenerator, ReportGenerator>();
    }
}
