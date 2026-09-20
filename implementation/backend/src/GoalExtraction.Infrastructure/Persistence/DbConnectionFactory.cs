namespace GoalExtraction.Infrastructure.Persistence;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

public interface IDbConnectionFactory
{
    SqliteConnection CreateReadOnlyConnection();
    SqliteConnection CreateReadWriteConnection();
    string ReadOnlyConnectionString { get; }
    string ReadWriteConnectionString { get; }
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _readOnlyConnectionString;
    private readonly string _readWriteConnectionString;

    public DbConnectionFactory(IConfiguration? configuration = null)
    {
        var raw = configuration?.GetConnectionString("DefaultConnection") 
            ?? "Data Source=goals.db;Cache=Shared;";
        
        var baseBuilder = new SqliteConnectionStringBuilder(raw);

        var roBuilder = new SqliteConnectionStringBuilder(baseBuilder.ConnectionString)
        {
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Shared
        };
        _readOnlyConnectionString = roBuilder.ToString();

        var rwBuilder = new SqliteConnectionStringBuilder(baseBuilder.ConnectionString)
        {
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        };
        _readWriteConnectionString = rwBuilder.ToString();
    }

    public DbConnectionFactory(string connectionString)
    {
        var baseBuilder = new SqliteConnectionStringBuilder(connectionString);

        var roBuilder = new SqliteConnectionStringBuilder(baseBuilder.ConnectionString)
        {
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Shared
        };
        _readOnlyConnectionString = roBuilder.ToString();

        var rwBuilder = new SqliteConnectionStringBuilder(baseBuilder.ConnectionString)
        {
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        };
        _readWriteConnectionString = rwBuilder.ToString();
    }

    public string ReadOnlyConnectionString => _readOnlyConnectionString;
    public string ReadWriteConnectionString => _readWriteConnectionString;

    public SqliteConnection CreateReadOnlyConnection()
    {
        var connection = new SqliteConnection(_readOnlyConnectionString);
        connection.Open();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA query_only = ON;";
            cmd.ExecuteNonQuery();
        }

        return connection;
    }

    public SqliteConnection CreateReadWriteConnection()
    {
        var connection = new SqliteConnection(_readWriteConnectionString);
        connection.Open();
        return connection;
    }
}
