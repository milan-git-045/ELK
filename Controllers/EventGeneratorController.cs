using Microsoft.AspNetCore.Mvc;
using EventGenerator.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Diagnostics;

namespace EventGenerator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventGeneratorController : ControllerBase
    {
        private readonly IEventGeneratorService _eventGeneratorService;
        private readonly ILogger<EventGeneratorController> _logger;
        private int successCount;
        private int failureCount;

        public EventGeneratorController(IEventGeneratorService eventGeneratorService, ILogger<EventGeneratorController> logger)
        {
            _eventGeneratorService = eventGeneratorService;
            _logger = logger;
        }

        [HttpGet("generate/{count}")]
        public async Task<IActionResult> GenerateEvents(int count)
        {
            _logger.LogInformation("Received request to generate {Count} events", count);

            if (count <= 0) 
            {
                _logger.LogWarning("Invalid count parameter: {Count}", count);
                return BadRequest("Count must be between 1 and 100000");
            }

            try
            {
                var events = await _eventGeneratorService.GenerateEvents(count);
                _logger.LogInformation("Successfully generated and returned {Count} events", count);
                return Ok(events);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error occurred while generating events");
                return StatusCode(500, "An error occurred while generating events");
            }
        }

        [HttpPost("loadtest")]
        public async Task<IActionResult> LoadTest([FromBody] LoadTestRequest request)
        {
            if (request.BatchSize <= 0 || request.TotalEvents <= 0)
            {
                return BadRequest("BatchSize and TotalEvents must be greater than 0");
            }

            // Reset counters at the start of each test
            successCount = 0;
            failureCount = 0;

            var sw = Stopwatch.StartNew();
            var totalProcessed = 0;

            try
            {
                var batches = (int)Math.Ceiling((double)request.TotalEvents / request.BatchSize);
                var tasks = new List<Task>();

                for (int i = 0; i < batches; i++)
                {
                    var remainingEvents = request.TotalEvents - totalProcessed;
                    var currentBatchSize = Math.Min(request.BatchSize, remainingEvents);

                    var task = ProcessBatchAsync(currentBatchSize);
                    tasks.Add(task);
                    totalProcessed += currentBatchSize;

                    if (request.DelayBetweenBatchesMs > 0)
                    {
                        await Task.Delay(request.DelayBetweenBatchesMs);
                    }
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Load test failed");
                return StatusCode(500, "Load test failed");
            }

            sw.Stop();
            
            var result = new LoadTestResult
            {
                TotalEvents = request.TotalEvents,
                TotalTimeMs = sw.ElapsedMilliseconds,
                EventsPerSecond = (double)request.TotalEvents / (sw.ElapsedMilliseconds / 1000.0),
                SuccessCount = successCount,
                FailureCount = failureCount
            };

            return Ok(result);
        }

        private async Task ProcessBatchAsync(int batchSize)
        {
            try
            {
                var events = await _eventGeneratorService.GenerateEvents(batchSize);
                await _eventGeneratorService.IndexEventsToElasticsearch(events);
                Interlocked.Add(ref successCount, batchSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process batch of {Count} events", batchSize);
                Interlocked.Add(ref failureCount, batchSize);
            }
        }
    }

    public class LoadTestRequest
    {
        public int BatchSize { get; set; }
        public int TotalEvents { get; set; }
        public int DelayBetweenBatchesMs { get; set; }
    }

    public class LoadTestResult
    {
        public int TotalEvents { get; set; }
        public long TotalTimeMs { get; set; }
        public double EventsPerSecond { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
    }
}
