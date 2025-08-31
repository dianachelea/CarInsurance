using System.Threading.Tasks;
using CarInsurance.Api.Services;
using Microsoft.EntityFrameworkCore;
using CarInsurance.Api.Data;

namespace CarInsurance.Api.Tests;

internal static class TestsHelper
{
    public static CarService CreateSut(AppDbContext db) => new(db);

    public static async Task<long> GetCarIdByVinAsync(AppDbContext db, string vin)
        => (await db.Cars.FirstAsync(c => c.Vin == vin)).Id;

    public static async Task<long> GetAnyOwnerIdAsync(AppDbContext db)
        => (await db.Owners.FirstAsync()).Id;
}
