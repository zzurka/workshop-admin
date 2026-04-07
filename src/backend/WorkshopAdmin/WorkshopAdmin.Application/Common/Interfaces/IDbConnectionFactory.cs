namespace WorkshopAdmin.Application.Common.Interfaces;

using System.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
