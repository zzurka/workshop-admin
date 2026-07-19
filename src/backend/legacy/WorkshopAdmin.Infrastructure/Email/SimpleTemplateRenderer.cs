namespace WorkshopAdmin.Infrastructure.Email;

using WorkshopAdmin.Application.Common.Interfaces;

/// <summary>
/// Replaces <c>{{Placeholder}}</c> tokens in a template with values from the
/// supplied dictionary. Unknown placeholders are left intact so they are
/// visible in QA rather than silently dropped.
/// </summary>
public sealed class SimpleTemplateRenderer : ITemplateRenderer
{
    public string Render(string template, IReadOnlyDictionary<string, string> placeholders)
    {
        if (string.IsNullOrEmpty(template) || placeholders.Count == 0)
        {
            return template;
        }

        string rendered = template;
        foreach (KeyValuePair<string, string> pair in placeholders)
        {
            rendered = rendered.Replace("{{" + pair.Key + "}}", pair.Value, StringComparison.Ordinal);
        }
        return rendered;
    }
}
