namespace WorkshopAdmin.Infrastructure.Email;

using Microsoft.Extensions.Options;
using WorkshopAdmin.Application.Common.Interfaces;

public sealed class FrontendUrlProvider(IOptions<FrontendOptions> options) : IFrontendUrlProvider
{
    private readonly string _baseUrl = (options.Value.BaseUrl ?? string.Empty).TrimEnd('/');

    public string LoginUrl => _baseUrl + "/login";
}
