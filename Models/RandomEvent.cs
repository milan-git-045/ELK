using System;

namespace EventGenerator.Models
{
    public class RandomEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string EventType { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public int Severity { get; set; }
        public string Source { get; set; }
    }
}
