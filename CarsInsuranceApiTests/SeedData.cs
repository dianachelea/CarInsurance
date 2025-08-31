using System;
using System.Linq;
using CarInsurance.Api.Data;
using CarInsurance.Api.Models;

namespace CarInsurance.Api.Tests;

public static class SeedData
{
    public static void EnsureSeeded(AppDbContext db)
    {
        if (db.Owners.Any()) return;

        var owner1 = new Owner { Name = "Popescu", Email = "popescu@test.com" };
        var owner2 = new Owner { Name = "Ciobanu", Email = "ciobanu@test.com" };
        db.Owners.AddRange(owner1, owner2);
        db.SaveChanges();

        var car1 = new Car { Vin = "VIN11111", Make = "Chevrolet", Model = "Malibu", YearOfManufacture = 2015, OwnerId = owner1.Id };
        var car2 = new Car { Vin = "VIN22222", Make = "Hyundai", Model = "Tucson", YearOfManufacture = 2020, OwnerId = owner2.Id };
        db.Cars.AddRange(car1, car2);
        db.SaveChanges();

        db.Policies.Add(new InsurancePolicy
        {
            CarId = car1.Id,
            Provider = "Groupama",
            StartDate = new DateOnly(2023, 1, 1),
            EndDate = new DateOnly(2024, 12, 31)
        });

        db.Policies.AddRange(
            new InsurancePolicy
            {
                CarId = car2.Id,
                Provider = "Allianz",
                StartDate = new DateOnly(2024, 1, 1),
                EndDate = new DateOnly(2024, 12, 31)
            },
            new InsurancePolicy
            {
                CarId = car2.Id,
                Provider = "Allianz",
                StartDate = new DateOnly(2025, 1, 1),
                EndDate = new DateOnly(2025, 12, 31)
            }
        );

        db.Claims.Add(new InsuranceClaim
        {
            CarId = car1.Id,
            ClaimDate = new DateOnly(2023, 2, 10),
            Description = "Bara fata",
            Amount = 1200m
        });

        db.SaveChanges();
    }
}
