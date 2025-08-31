using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using CarInsurance.Api.Data;

namespace CarInsurance.Api.Tests;

public sealed class DbHelper : IDisposable
{
    private readonly SqliteConnection _connection;
    public AppDbContext Db { get; }

    private DbHelper()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .EnableSensitiveDataLogging()
            .Options;

        Db = new AppDbContext(options);
        Db.Database.EnsureCreated();
    }

    public static DbHelper CreateEmpty() => new();

    public static DbHelper CreateSeeded()
    {
        var h = new DbHelper();
        SeedData.EnsureSeeded(h.Db);
        return h;
    }

    public void Dispose()
    {
        Db?.Dispose();
        _connection.Close();
        _connection.Dispose();
    }
}
