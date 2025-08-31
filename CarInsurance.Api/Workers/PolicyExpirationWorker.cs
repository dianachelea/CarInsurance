using CarInsurance.Api.Data;
using CarInsurance.Api.Services;

namespace CarInsurance.Api.Workers
{
    public sealed class PolicyExpirationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PolicyExpirationWorker> _log;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);
        private readonly TimeZoneInfo _businessTz = TimeZoneInfo.Local; 

        public PolicyExpirationWorker(IServiceScopeFactory scopeFactory, ILogger<PolicyExpirationWorker> log)
        {
            _scopeFactory = scopeFactory;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_interval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var count = await PolicyExpirationProcessor.ProcessOnceAsync(db, _log, DateTimeOffset.UtcNow, _businessTz, stoppingToken);

                    if (count > 0)
                        _log.LogInformation("PolicyExpirationWorker processed {Count}", count);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error while running PolicyExpirationWorker.");
                }

                await timer.WaitForNextTickAsync(stoppingToken);
            }
        }
    }
}
