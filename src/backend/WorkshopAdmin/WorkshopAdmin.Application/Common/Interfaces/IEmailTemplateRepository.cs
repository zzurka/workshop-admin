namespace WorkshopAdmin.Application.Common.Interfaces;

using System.Data;
using WorkshopAdmin.Application.Common.Models;

public interface IEmailTemplateRepository
{
    Task<EmailTemplate?> GetByCodeAsync(
        string code,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken);
}
