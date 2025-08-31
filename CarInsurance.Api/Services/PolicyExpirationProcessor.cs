using CarInsurance.Api.Data;
using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services
{
    public static class PolicyExpirationProcessor
    {
        public static async Task<int> ProcessOnceAsync(
            AppDbContext db,
            ILogger logger,
            DateTimeOffset nowUtc,
            TimeZoneInfo businessTz,
            CancellationToken ct = default)
        {

            var currentTime = TimeZoneInfo.ConvertTime(nowUtc, businessTz);
            var todayLocal = DateOnly.FromDateTime(currentTime.Date);

            var candidateRows = await db.Policies
                .Where(p => p.EndDate < todayLocal)
                .Where(p => !db.PolicyExpiration.Any(pe => pe.PolicyId == p.Id))
                .Select(p => new { p.Id, p.CarId, p.Provider, p.EndDate })
                .ToListAsync(ct);

            if (candidateRows.Count == 0) 
                return 0;

            foreach (var p in candidateRows)
            {
                var localEnd = p.EndDate.ToDateTime(TimeOnly.MaxValue); 
                var expiredAtUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localEnd, businessTz));
                db.PolicyExpiration.Add(new PolicyExpiration
                {
                    PolicyId = p.Id,
                    ExpiredAt = expiredAtUtc
                });
            }

            int saved;
            try
            {
                saved = await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                logger.LogWarning(ex, "Skipped duplicate expiration(s) due to unique index.");
                db.ChangeTracker.Clear();
                return 0;
            }

            foreach (var r in candidateRows)
            {
                var localEnd = r.EndDate.ToDateTime(TimeOnly.MaxValue);
                var expiredAtUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localEnd, businessTz));
                logger.LogInformation(
                    "Policy {PolicyId} for car {CarId} (provider {Provider}) expired at {ExpiredAtUtc}.",
                    r.Id, r.CarId, r.Provider, expiredAtUtc);
            }
            return saved;
        }
    }
}
