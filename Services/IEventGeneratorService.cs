using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventGenerator.Models;

namespace EventGenerator.Services
{
    public interface IEventGeneratorService
    {
        Task<IEnumerable<RandomEvent>> GenerateEvents(int count);
        Task<bool> IndexEventsToElasticsearch(IEnumerable<RandomEvent> events);
        Task GenerateAndSendEventsAsync(int totalEvents, int batchSize, int intervalMs, CancellationToken cancellationToken);
    }
}
