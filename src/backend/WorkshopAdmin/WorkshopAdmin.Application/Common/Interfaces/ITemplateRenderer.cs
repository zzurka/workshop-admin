namespace WorkshopAdmin.Application.Common.Interfaces;

/// <summary>
/// Renders a template string by substituting <c>{{Placeholder}}</c> tokens.
/// </summary>
public interface ITemplateRenderer
{
    string Render(string template, IReadOnlyDictionary<string, string> placeholders);
}
