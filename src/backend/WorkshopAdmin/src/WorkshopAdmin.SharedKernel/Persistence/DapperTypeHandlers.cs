using System.Data;
using System.Text.Json;
using Dapper;
using Npgsql;
using NpgsqlTypes;

namespace WorkshopAdmin.SharedKernel.Persistence;

/// <summary>Global Dapper type handlers. Call <see cref="Register"/> once at startup (idempotent).</summary>
public static class DapperTypeHandlers
{
    private static bool _registered;
    private static readonly Lock RegistrationLock = new();

    public static void Register()
    {
        lock (RegistrationLock)
        {
            if (_registered)
            {
                return;
            }

            SqlMapper.AddTypeHandler(new JsonbTypeHandler());
            _registered = true;
        }
    }

    /// <summary>Port of the legacy JsonbTypeHandler: jsonb ↔ Dictionary&lt;string, string&gt;.</summary>
    private sealed class JsonbTypeHandler : SqlMapper.TypeHandler<Dictionary<string, string>>
    {
        public override void SetValue(IDbDataParameter parameter, Dictionary<string, string>? value)
        {
            parameter.Value = value is null ? DBNull.Value : JsonSerializer.Serialize(value);
            parameter.DbType = DbType.String;

            if (parameter is NpgsqlParameter npgsqlParameter)
            {
                npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Jsonb;
            }
        }

        public override Dictionary<string, string> Parse(object value) =>
            JsonSerializer.Deserialize<Dictionary<string, string>>(value.ToString()!)
                ?? new Dictionary<string, string>();
    }
}
