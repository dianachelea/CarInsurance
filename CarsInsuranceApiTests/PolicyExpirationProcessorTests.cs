using CarInsurance.Api.Data;
using CarInsurance.Api.Models;
using CarInsurance.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CarInsurance.Api.Tests;

public class PolicyExpirationProcessorTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly TimeZoneInfo _businessTz;

    public PolicyExpirationProcessorTests()
    {
        _loggerMock = new Mock<ILogger>();
        _businessTz = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
    }

    [Fact]
    public async Task ProcessOnceAsync_NoExpiredPolicies_ReturnsZero()
    {
        // Arrange
        using var dbHelper = DbHelper.CreateSeeded();
        var nowUtc = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);

        // Act
        var savedCount = await PolicyExpirationProcessor.ProcessOnceAsync(
            dbHelper.Db,
            _loggerMock.Object,
            nowUtc,
            _businessTz
        );

        // Assert
        Assert.Equal(0, savedCount);
        Assert.Equal(0, dbHelper.Db.PolicyExpiration.Count());
    }

    [Fact]
    public async Task ProcessOnceAsync_OneExpiredPolicy_AddsExpirationEntryAndLogs()
    {
        // Arrange
        using var dbHelper = DbHelper.CreateSeeded();

        var expiredPolicy = new InsurancePolicy
        {
            CarId = 1,
            Provider = "ExpiredProv",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 5, 31)
        };
        dbHelper.Db.Policies.Add(expiredPolicy);
        await dbHelper.Db.SaveChangesAsync();

        var nowUtc = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);

        // Act
        var savedCount = await PolicyExpirationProcessor.ProcessOnceAsync(
            dbHelper.Db,
            _loggerMock.Object,
            nowUtc,
            _businessTz
        );

        // Assert
        Assert.Equal(1, savedCount);
        var expiredEntry = await dbHelper.Db.PolicyExpiration.FirstOrDefaultAsync(pe => pe.PolicyId == expiredPolicy.Id);
        Assert.NotNull(expiredEntry);

        var localEnd = expiredPolicy.EndDate.ToDateTime(TimeOnly.MaxValue);
        var expectedExpiredAtUtc = new DateTimeOffset(
            TimeZoneInfo.ConvertTimeToUtc(localEnd, _businessTz)
        );
        Assert.Equal(expectedExpiredAtUtc, expiredEntry.ExpiredAt);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Policy {expiredPolicy.Id}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task ProcessOnceAsync_ExpiredPolicyAlreadyProcessed_DoesNotAddDuplicate()
    {
        // Arrange
        using var dbHelper = DbHelper.CreateSeeded();

        var expiredPolicy = new InsurancePolicy
        {
            CarId = 1,
            Provider = "ExpiredProv",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 5, 31)
        };
        dbHelper.Db.Policies.Add(expiredPolicy);
        await dbHelper.Db.SaveChangesAsync();

        var expiredAtUtc = new DateTimeOffset(
            TimeZoneInfo.ConvertTimeToUtc(expiredPolicy.EndDate.ToDateTime(TimeOnly.MaxValue), _businessTz)
        );
        dbHelper.Db.PolicyExpiration.Add(new PolicyExpiration
        {
            PolicyId = expiredPolicy.Id,
            ExpiredAt = expiredAtUtc
        });
        await dbHelper.Db.SaveChangesAsync();

        var nowUtc = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);

        // Act
        var savedCount = await PolicyExpirationProcessor.ProcessOnceAsync(
            dbHelper.Db,
            _loggerMock.Object,
            nowUtc,
            _businessTz
        );

        // Assert
        Assert.Equal(0, savedCount);
        Assert.Equal(1, dbHelper.Db.PolicyExpiration.Count());
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ),
            Times.Never
        );
    }

    [Fact]
    public async Task ProcessOnceAsync_MultipleExpiredPolicies_ProcessesAll()
    {
        // Arrange
        using var dbHelper = DbHelper.CreateSeeded();

        var expiredPolicy1 = new InsurancePolicy
        {
            CarId = 1,
            Provider = "Expired1",
            StartDate = new DateOnly(2023, 1, 1),
            EndDate = new DateOnly(2024, 4, 30)
        };
        var expiredPolicy2 = new InsurancePolicy
        {
            CarId = 2,
            Provider = "Expired2",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 5, 31)
        };
        dbHelper.Db.Policies.AddRange(expiredPolicy1, expiredPolicy2);
        await dbHelper.Db.SaveChangesAsync();

        var nowUtc = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);

        // Act
        var savedCount = await PolicyExpirationProcessor.ProcessOnceAsync(
            dbHelper.Db,
            _loggerMock.Object,
            nowUtc,
            _businessTz
        );

        // Assert
        Assert.Equal(2, savedCount);
        Assert.Equal(2, dbHelper.Db.PolicyExpiration.Count());
        Assert.Contains(dbHelper.Db.PolicyExpiration, pe => pe.PolicyId == expiredPolicy1.Id);
        Assert.Contains(dbHelper.Db.PolicyExpiration, pe => pe.PolicyId == expiredPolicy2.Id);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Policy {expiredPolicy1.Id}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ),
            Times.Once
        );
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Policy {expiredPolicy2.Id}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ),
            Times.Once
        );
    }
   

    [Fact]
    public async Task ProcessOnceAsync_AlreadyProcessed_ReturnsZero_NoWarning()
    {
        // Arrange
        using var dbHelper = DbHelper.CreateSeeded();

        var expiredPolicy = new InsurancePolicy
        {
            CarId = 1,
            Provider = "Expired",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 5, 31)
        };
        dbHelper.Db.Policies.Add(expiredPolicy);
        await dbHelper.Db.SaveChangesAsync();

        dbHelper.Db.PolicyExpiration.Add(new PolicyExpiration
        {
            PolicyId = expiredPolicy.Id,
            ExpiredAt = DateTimeOffset.UtcNow
        });
        await dbHelper.Db.SaveChangesAsync();
        dbHelper.Db.ChangeTracker.Clear();

        var nowUtc = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);

        var savedCount = await PolicyExpirationProcessor.ProcessOnceAsync(
            dbHelper.Db,
            _loggerMock.Object,
            nowUtc,
            _businessTz
        );

        // Assert
        Assert.Equal(0, savedCount);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<DbUpdateException>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ),
            Times.Never
        );
        Assert.Equal(1, dbHelper.Db.PolicyExpiration.Count()); 
    }
}