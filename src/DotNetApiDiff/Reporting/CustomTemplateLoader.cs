using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace DotNetApiDiff.Reporting;

/// <summary>
/// Custom template loader for Scriban that loads templates from embedded resources
/// </summary>
public class CustomTemplateLoader : ITemplateLoader
{
    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
    {
        return templateName;
    }

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        try
        {
            // Load template source directly from embedded resources
            return EmbeddedTemplateLoader.LoadTemplate($"{templatePath}.scriban");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Template '{templatePath}' not found: {ex.Message}", ex);
        }
    }

    public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        return new ValueTask<string>(Load(context, callerSpan, templatePath));
    }
}
