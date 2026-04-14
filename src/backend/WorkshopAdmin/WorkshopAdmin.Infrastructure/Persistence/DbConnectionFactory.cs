namespace WorkshopAdmin.Infrastructure.Persistence;

using System.Data;
using Npgsql;
using WorkshopAdmin.Application.Common.Interfaces;

public class DbConnectionFactory(string connectionString) : IDbConnectionFactory
{
    private readonly string _connectionString = connectionString;

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}
