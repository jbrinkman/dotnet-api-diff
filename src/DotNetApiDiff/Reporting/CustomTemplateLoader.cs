using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace DotNetApiDiff.Reporting;

/// <summary>
/// Custom template loader for Scriban that loads templates from a dictionary
/// </summary>
public class CustomTemplateLoader : ITemplateLoader
{
    private readonly Dictionary<string, Template> _templates;

    public CustomTemplateLoader(Dictionary<string, Template> templates)
    {
        _templates = templates;
    }

    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
    {
        return templateName;
    }

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        if (_templates.TryGetValue(templatePath, out var template))
        {
            // Get the source code of the template
            return template.Page.Body.ToString();
        }

        // Try loading from embedded resources directly
        try
        {
            return EmbeddedTemplateLoader.LoadTemplate($"{templatePath}.scriban");
        }
        catch
        {
            throw new InvalidOperationException($"Template '{templatePath}' not found");
        }
    }

    public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        return new ValueTask<string>(Load(context, callerSpan, templatePath));
    }
}
