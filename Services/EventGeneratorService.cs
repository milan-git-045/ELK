using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using EventGenerator.Models;
using Microsoft.Extensions.Logging;
using Nest;

namespace EventGenerator.Services
{
    public class EventGeneratorService : IEventGeneratorService
    {
        private readonly ILogger<EventGeneratorService> _logger;
        private readonly IElasticClient _elasticClient;
        private readonly string[] _eventTypes = { "CameraOfflineEvent", "RecordingStoppedEvent", "MotionDetectedEvent" };
        private readonly Random _random = new Random();

        public EventGeneratorService(ILogger<EventGeneratorService> logger, IElasticClient elasticClient)
        {
            _logger = logger;
            _elasticClient = elasticClient;
        }

        public async Task GenerateAndSendEventsAsync(int totalEvents, int batchSize, int intervalMs, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting batch event generation. Total Events: {TotalEvents}, Batch Size: {BatchSize}, Interval: {IntervalMs}ms",
                totalEvents, batchSize, intervalMs);

            var remainingEvents = totalEvents;
            var processedEvents = 0;

            while (remainingEvents > 0 && !cancellationToken.IsCancellationRequested)
            {
                var currentBatchSize = Math.Min(batchSize, remainingEvents);
                var events = await GenerateEvents(currentBatchSize);
                
                processedEvents += currentBatchSize;
                remainingEvents -= currentBatchSize;

                _logger.LogInformation("Progress: {ProcessedEvents}/{TotalEvents} events processed", 
                    processedEvents, totalEvents);

                if (remainingEvents > 0)
                {
                    await Task.Delay(intervalMs, cancellationToken);
                }
            }

            _logger.LogInformation("Completed generating and sending all events. Total processed: {ProcessedEvents}", 
                processedEvents);
        }
        
        public async Task<IEnumerable<RandomEvent>> GenerateEvents(int count)
        {
            _logger.LogInformation("Generating {Count} random events", count);
            
            try
            {
                var events = Enumerable.Range(0, count).Select(_ => GenerateSingleEvent()).ToList();
                _logger.LogInformation("Successfully generated {Count} events", events.Count);

                // Index events to Elasticsearch
                var indexed = await IndexEventsToElasticsearch(events);
                if (!indexed)
                {
                    _logger.LogWarning("Failed to index some events to Elasticsearch");
                }

                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating events");
                throw;
            }
        }

        public async Task<bool> IndexEventsToElasticsearch(IEnumerable<RandomEvent> events)
        {
            try
            {
                var indexName = $"camera-events-{DateTime.UtcNow:yyyy-MM}";
                
                var bulkResponse = await _elasticClient.BulkAsync(b => b
                    .Index(indexName)
                    .CreateMany(events)
                );

                if (!bulkResponse.IsValid)
                {
                    _logger.LogError("Failed to index events to Elasticsearch: {ErrorMessage}", 
                        bulkResponse.DebugInformation);
                    return false;
                }

                _logger.LogInformation("Successfully indexed {Count} events to Elasticsearch index {IndexName}", 
                    events.Count(), indexName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while indexing events to Elasticsearch");
                return false;
            }
        }

        private RandomEvent GenerateSingleEvent()
        {
            var eventType = _eventTypes[_random.Next(_eventTypes.Length)];
            var severity = _random.Next(1, 6);

            var description = eventType switch
            {
                "CameraOfflineEvent" => $"Camera {_random.Next(1, 10)} went offline",
                "RecordingStoppedEvent" => $"Recording stopped for Camera {_random.Next(1, 10)}",
                "MotionDetectedEvent" => $"Motion detected in Camera {_random.Next(1, 10)}",
                _ => $"Unknown event occurred"
            };

            return new RandomEvent
            {
                EventType = eventType,
                Description = description,
                Timestamp = DateTime.UtcNow,
                Severity = severity,
                Source = $"Camera_{_random.Next(1, 10)}"
            };
        }
    }
}
