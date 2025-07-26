using System.Reflection;

namespace DotNetApiDiff.Reporting;

/// <summary>
/// Helper class to load embedded HTML templates and assets
/// </summary>
public static class EmbeddedTemplateLoader
{
    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private static readonly Dictionary<string, string> _templateCache = new();

    /// <summary>
    /// Loads a template by name from embedded resources
    /// </summary>
    /// <param name="templateName">Name of the template file (without extension)</param>
    /// <returns>Template content as string</returns>
    public static string LoadTemplate(string templateName)
    {
        if (_templateCache.TryGetValue(templateName, out var cached))
        {
            return cached;
        }

        var resourceName = $"DotNetApiDiff.Reporting.HtmlTemplates.{templateName}";
        var content = LoadEmbeddedResource(resourceName);
        _templateCache[templateName] = content;
        return content;
    }

    /// <summary>
    /// Loads CSS styles from embedded resources
    /// </summary>
    /// <returns>CSS content as string</returns>
    public static string LoadStyles()
    {
        return LoadTemplate("styles.css");
    }

    /// <summary>
    /// Loads JavaScript code from embedded resources
    /// </summary>
    /// <returns>JavaScript content as string</returns>
    public static string LoadScripts()
    {
        return LoadTemplate("scripts.js");
    }

    /// <summary>
    /// Gets all available template names
    /// </summary>
    /// <returns>List of template names</returns>
    public static List<string> GetAvailableTemplates()
    {
        var templateNames = new List<string>();
        var resourceNames = _assembly.GetManifestResourceNames();

        foreach (var resourceName in resourceNames)
        {
            if (resourceName.StartsWith("DotNetApiDiff.Reporting.HtmlTemplates."))
            {
                var templateName = resourceName.Substring("DotNetApiDiff.Reporting.HtmlTemplates.".Length);
                templateNames.Add(templateName);
            }
        }

        return templateNames;
    }

    private static string LoadEmbeddedResource(string resourceName)
    {
        using var stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
