namespace WorkshopAdmin.Infrastructure.Persistence.TypeHandlers;

using System.Data;
using System.Text.Json;
using Dapper;
using Npgsql;
using NpgsqlTypes;

public class JsonbTypeHandler : SqlMapper.TypeHandler<Dictionary<string, string>>
{
    public override void SetValue(IDbDataParameter parameter, Dictionary<string, string>? value)
    {
        parameter.Value = value is null ? DBNull.Value : JsonSerializer.Serialize(value);
        parameter.DbType = DbType.String;

        if (parameter is NpgsqlParameter npgsqlParam)
        {
            npgsqlParam.NpgsqlDbType = NpgsqlDbType.Jsonb;
        }
    }

    public override Dictionary<string, string> Parse(object value)
    {
        return JsonSerializer.Deserialize<Dictionary<string, string>>(value.ToString()!)
            ?? new Dictionary<string, string>();
    }
}
