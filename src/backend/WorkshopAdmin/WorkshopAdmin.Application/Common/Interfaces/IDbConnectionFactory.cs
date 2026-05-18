namespace WorkshopAdmin.Application.Common.Interfaces;

using System.Data;
using System.Data.Common;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();

    /// <summary>Creates and opens a connection ready for async use (services own its lifetime).</summary>
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken);
}
