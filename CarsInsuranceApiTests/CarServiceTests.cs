using System;
using System.Linq;
using System.Threading.Tasks;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Services;
using CarInsurance.Api.Tests;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static CarInsurance.Api.Tests.Constants;
using static CarInsurance.Api.Tests.TestsHelper;
namespace CarInsurance.Api.CarInsurance.Api.Tests
{
    public class CarServiceTests
    {
        [Fact]
        public async Task ListCarsAsync_Should_ReturnOwnerFields_ForEachCar()
        {
            //Arange
            using var h = DbHelper.CreateSeeded();
            var sut = CreateSut(h.Db);
            //Act
            var list = await sut.ListCarsAsync();
            //Assert
            Assert.True(list.Count >= 2);
            var c1 = list.First(c => c.Vin == Vin1);
            Assert.Equal(Owner1Name, c1.OwnerName);
            Assert.Equal(Owner1Email, c1.OwnerEmail);

        }
        [Fact]
        public async Task IsInsuranceValidAsync_Should_BeTrue_WhenDateInsidePolicy()
        {
            //Arange
            using var h = DbHelper.CreateSeeded();
            var sut = CreateSut(h.Db);
            //Act
            var carId = await GetCarIdByVinAsync(h.Db, Constants.Vin1);
            var date = new DateOnly(2024, 6, 1);

            var isValid = await sut.IsInsuranceValidAsync(carId, date);
            //Assert
            Assert.True(isValid);
        }
        [Fact]
        public async Task IsInsuranceValidAsync_Should_Throw_WhenCarNotFound()
        {
            // Arrange
            using var h = DbHelper.CreateSeeded();
            var sut = CreateSut(h.Db);
            // Act + Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                sut.IsInsuranceValidAsync(999_999, new DateOnly(2024, 1, 1)));
        }
        [Fact]
        public async Task CreateCar_Should_Persist_AndReturnId_WhenPayloadIsValid()
        {
            // Arrange
            using var h = DbHelper.CreateSeeded();
            var sut = CreateSut(h.Db);
            var ownerId = await GetAnyOwnerIdAsync(h.Db);
            var req = new CreateCarRequest
            (
                "VIN33333",
                "Skoda",
                "Octavia",
                DateTime.UtcNow.Year,
                ownerId
            );
            // Act
            var id = await sut.CreateCar(req);
            // Assert
            Assert.NotNull(id);
            var saved = await h.Db.Cars.FindAsync(id);
            Assert.NotNull(saved);
            Assert.Equal("VIN33333", saved.Vin);
        }

        [Fact]
        public async Task CreateCar_Should_Throw_WhenVinIsEmpty()
        {
            //Arrange
            using var h = DbHelper.CreateSeeded();
            var sut = CreateSut(h.Db);
            var ownerId = await GetAnyOwnerIdAsync(h.Db);
            
            var req = new CreateCarRequest(
                "",   
                "Skoda",
                "Octavia",
                2020,
                ownerId
            );
            //Act + Assert
            await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateCar(req));
        }

        [Fact]
        public async Task CreateCar_Should_Throw_WhenVinDuplicate()
        {
            // Arrange
            using var h = DbHelper.CreateSeeded();
            var sut = CreateSut(h.Db);
            var ownerId = await GetAnyOwnerIdAsync(h.Db);

            var req = new CreateCarRequest(
                Vin1,      
                null,      
                null,      
                2020,      
                ownerId
            );

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateCar(req));
        }

        [Fact]
        public async Task CreateCar_Should_Throw_WhenYearInvalidOrTooFuture()
        {
            // Arrange
            using var h = DbHelper.CreateSeeded();
            var sut = CreateSut(h.Db);
            var ownerId = await GetAnyOwnerIdAsync(h.Db);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.CreateCar(new CreateCarRequest(
                    VinNew2,   
                    null,      
                    null,      
                    0,         
                    ownerId
                ))
            );

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.CreateCar(new CreateCarRequest(
                    VinNew3,
                    null,
                    null,
                    DateTime.UtcNow.Year + 2, 
                    ownerId
                ))
            );
        }
        [Fact]
        public async Task CreateCar_Should_Throw_WhenOwnerMissing()
        {
            // Arrange
            using var h = DbHelper.CreateSeeded();
            var sut = CreateSut(h.Db);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                sut.CreateCar(new CreateCarRequest(
                    VinNew4,
                    null,
                    null,
                    2020,
                    99
                ))
            );
        }

        [Fact]
        public async Task CreatePolicyAsync_Should_Persist_WhenValid()
        {
            // Arrange
            using var h = DbHelper.CreateSeeded();
            var sut = CreateSut(h.Db);
            var carId = await GetCarIdByVinAsync(h.Db, Vin1);
            var start = new DateOnly(2025, 1, 1);
            var end = new DateOnly(2025, 12, 31);

            // Act
            var id = await sut.CreatePolicyAsync(carId, start, end, ProviderNew);

            // Assert
            Assert.NotNull(id);
            var saved = await h.Db.Policies.FindAsync(id);
            Assert.NotNull(saved);
            Assert.Equal(carId, saved.CarId);
            Assert.Equal(ProviderNew, saved.Provider);
        }

        [Fact]
        public async Task CreatePolicyAsync_Should_Throw_WhenEndBeforeStart()
        {
            // Arrange
            using var h = DbHelper.CreateSeeded();
            var sut = CreateSut(h.Db);
            var carId = await GetCarIdByVinAsync(h.Db, Vin1);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.CreatePolicyAsync(
                    carId,
                    new DateOnly(2025, 5, 1),
                    new DateOnly(2025, 4, 30),
                    "X"
                )
            );
        }

        [Fact]
        public async Task CreatePolicyAsync_Should_Throw_WhenOverlapsExisting()
        {
            // Arrange
            using var h = DbHelper.CreateSeeded();
            var sut = CreateSut(h.Db);
            var carId = await GetCarIdByVinAsync(h.Db, Vin1);

            var start = new DateOnly(2024, 12, 1);
            var end = new DateOnly(2025, 1, 31);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.CreatePolicyAsync(carId, start, end, ProviderOverlap)
            );
        }

        [Fact]
        public async Task CreateClaimAsync_Should_Persist_WhenPayloadValid()
        {
            // Arrange
            using var h = DbHelper.CreateSeeded();
            var sut = CreateSut(h.Db);
            var carId = await GetCarIdByVinAsync(h.Db, Vin2);

            var req = new CreateClaimRequest(
                ValidClaimDate,
                ClaimDesc1,
                800
            );

            // Act
            var id = await sut.CreateClaimAsync(carId, req);

            // Assert
            Assert.NotNull(id);
            var saved = await h.Db.Claims.FindAsync(id);
            Assert.NotNull(saved);
            Assert.Equal(carId, saved!.CarId);
            Assert.Equal(800, saved.Amount);
            Assert.Equal(ClaimDesc1, saved.Description);
        }

        [Fact]
        public async Task CreateClaimAsync_Should_ReturnNull_WhenDateInvalid()
        {
            // Arrange
            using var h = DbHelper.CreateSeeded();
            var sut = CreateSut(h.Db);
            var carId = await GetCarIdByVinAsync(h.Db, Vin1);

            var req = new CreateClaimRequest(
                InvalidDate,
                "X",
                100m
            );

            // Act
            var id = await sut.CreateClaimAsync(carId, req);

            // Assert
            Assert.Null(id);
        }

        [Fact]
        public async Task CreateClaimAsync_Should_ReturnNull_WhenDescriptionEmpty()
        {
            // Arrange
            using var h = DbHelper.CreateSeeded();
            var sut = CreateSut(h.Db);
            var carId = await GetCarIdByVinAsync(h.Db, Vin1);

            var req = new CreateClaimRequest(
                "2024-01-01",
                "",
                100m
            );

            // Act
            var id = await sut.CreateClaimAsync(carId, req);

            // Assert
            Assert.Null(id);
        }

        [Fact]
        public async Task CreateClaimAsync_Should_ReturnNull_WhenAmountNonPositive()
        {
            // Arrange
            using var h = DbHelper.CreateSeeded();
            var sut = CreateSut(h.Db);
            var carId = await GetCarIdByVinAsync(h.Db, Vin1);

            var req = new CreateClaimRequest(
                "2024-01-01",
                "ok",
                0
            );

            // Act
            var id = await sut.CreateClaimAsync(carId, req);

            // Assert
            Assert.Null(id);
        }

        [Fact]
        public async Task CreateClaimAsync_Should_Throw_WhenCarNotFound()
        {
            // Arrange
            using var h = DbHelper.CreateSeeded();
            var sut = CreateSut(h.Db);

            var req = new CreateClaimRequest(
                "2024-01-01",
                "x",
                10
            );

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                sut.CreateClaimAsync(777_777, req)
            );
        }

        [Fact]
        public async Task GetHistory_Should_ReturnPoliciesAndClaims_InChronologicalOrder()
        {
            // Arrange
            using var h = DbHelper.CreateSeeded();
            var sut = CreateSut(h.Db);
            var carId = await GetCarIdByVinAsync(h.Db, Vin1);

            // add a future policy to test ordering
            await sut.CreatePolicyAsync(
                carId,
                new DateOnly(2025, 1, 1),
                new DateOnly(2025, 6, 30),
                "NewProv"
            );

            // Act
            var history = await sut.GetHistory(carId);

            // Assert
            Assert.True(history.Count >= 2);
            var starts = history.Select(hh => hh.Start).ToList();
            var sorted = starts.OrderBy(s => s).ToList();
            Assert.Equal(sorted, starts);
            Assert.Contains(history, hh => hh.Type == "policy");
            Assert.Contains(history, hh => hh.Type == "claim");
        }
    }
}
