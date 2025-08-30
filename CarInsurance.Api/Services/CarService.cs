using CarInsurance.Api.Data;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services;

public class CarService(AppDbContext db)
{
    private readonly AppDbContext _db = db;

    public async Task<List<CarDto>> ListCarsAsync()
    {
        return await _db.Cars.Include(c => c.Owner)
            .Select(c => new CarDto(c.Id, c.Vin, c.Make, c.Model, c.YearOfManufacture,
                                    c.OwnerId, c.Owner.Name, c.Owner.Email))
            .ToListAsync();
    }

    public async Task<bool> IsInsuranceValidAsync(long carId, DateOnly date)
    {
        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists) throw new KeyNotFoundException($"Car {carId} not found");

        return await _db.Policies.AnyAsync(p =>
            p.CarId == carId &&
            p.StartDate <= date &&
            p.EndDate >= date
        );
    }

    public async Task<long?> CreateClaimAsync(long carId, CreateClaimRequest claimRequest)
    {
        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists) throw new KeyNotFoundException($"Car {carId} not found");

        if (!DateOnly.TryParse(claimRequest.ClaimDate, out var claimDate))
            return null;

        if (string.IsNullOrWhiteSpace(claimRequest.Description) || claimRequest.Amount <= 0)
            return null;

        var claim = new InsuranceClaim
        {
            CarId = carId,
            ClaimDate = claimDate,
            Description = claimRequest.Description,
            Amount = claimRequest.Amount
        };

        _db.Claims.Add(claim);
        await _db.SaveChangesAsync();

        return claim.Id;
    }
    
    public async Task<List<CarHistory>> GetHistory(long carId)
    {
        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists) throw new KeyNotFoundException($"Car {carId} not found");

        var policies = await _db.Policies
            .Where(p => p.CarId == carId)
            .Select(p => new CarHistory(
                "policy",
                p.StartDate.ToString("yyyy-MM-dd"),
                p.EndDate.ToString("yyyy-MM-dd"),
                p.Provider,
                null,
                null
            ))
            .ToListAsync();

        var claims= await _db.Claims
            .Where(c => c.CarId == carId)
            .Select (c=> new CarHistory(
                "claim",
                c.ClaimDate.ToString("yyyy-MM-dd"),
                null,
                null,
                c.Description,
                c.Amount
                )).ToListAsync();

        return policies.Concat(claims)
            .OrderBy(item => item.Start)
            .ToList();
    }

}
