using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace EventGenerator.Services
{
    public class AutoEventGeneratorService : BackgroundService
    {
        private readonly IEventGeneratorService _eventGeneratorService;
        private readonly ILogger<AutoEventGeneratorService> _logger;
        private const int TotalEvents = 100000;
        private const int BatchSize = 1000;
        private const int IntervalMs = 100;

        public AutoEventGeneratorService(
            IEventGeneratorService eventGeneratorService,
            ILogger<AutoEventGeneratorService> logger)
        {
            _eventGeneratorService = eventGeneratorService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Starting automatic event generation: Total Events: {TotalEvents}, Batch Size: {BatchSize}, Interval: {IntervalMs}ms",
                    TotalEvents, BatchSize, IntervalMs);

                await _eventGeneratorService.GenerateAndSendEventsAsync(TotalEvents, BatchSize, IntervalMs, stoppingToken);
                
                _logger.LogInformation("Completed generating {TotalEvents} events", TotalEvents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating events automatically");
            }
        }
    }
}
