# Communication Flow: Service, Elasticsearch, and Kibana

## EventGenerator-Service → Elasticsearch
* Uses HTTP/HTTPS protocol (REST API)
* Default ports:
   * HTTP: 9200
   * HTTPS: 9243
* Communication happens through Elasticsearch's REST APIs

```csharp
var elasticClient = new ElasticClient(settings);
await _elasticClient.BulkAsync(b => b
    .Index(indexName)
    .CreateMany(events));
```

## Elasticsearch → Kibana
* Uses HTTP/HTTPS protocol
* Default port for Kibana: 5601
* Kibana acts as a client to Elasticsearch
* Kibana makes REST API calls to Elasticsearch to:
   * Query data
   * Retrieve index patterns
   * Fetch aggregations for visualizations
   * Monitor cluster health

## Detailed Flow Diagram
```
    Event Generator
         ↓ HTTP/HTTPS (POST/PUT)
         ↓ Port 9200/9243
    Elasticsearch
         ↑ HTTP/HTTPS (GET/POST)
         ↑ Port 9200/9243
       Kibana
```

## Event Processing
When you send events:
1. Event-Generator service sends events to Elasticsearch using bulk API
2. Elasticsearch stores the data in indices
3. Kibana queries Elasticsearch to:
   * Display real-time data
   * Create visualizations
   * Show dashboards
   * Monitor metrics

## Configuration Details
In your `appsettings.json`:
* Elasticsearch is running on http://localhost:9200
* Using basic authentication (username/password)
* HTTP protocol is being used

## In Kibana
1. Go to Stack Management → Index Patterns
2. Create an index pattern for "camera-events*"
3. Go to Discover to see your events in real-time
4. Create visualizations to monitor event flow

## Architecture Benefits
* Scalable (you can add more Elasticsearch nodes)
* Uses standard HTTP/HTTPS protocols
* Supports secure communication
* Allows real-time monitoring through Kibana
* Enables complex queries and aggregations

## Supported Protocols

### Transport Protocol
* A native Elasticsearch protocol
* Uses port 9300 by default
* Used for node-to-node communication in Elasticsearch cluster
* More efficient than HTTP for internal cluster communication
* Uses TCP for reliable data transfer

### Binary Protocol
* A high-performance binary protocol
* Used between Elasticsearch nodes
* More efficient than HTTP for large data transfers
* Handles cluster state updates and shard operations

### gRPC Protocol
* Supported in newer versions of Elasticsearch
* High-performance RPC framework
* Used for certain internal communications
* Better performance than REST for some operations

### WebSocket Protocol
* Supported for real-time updates
* Used by Kibana for features such as:
   * Real-time monitoring
   * Live dashboard updates
   * Console updates

## Protocol Usage Diagram
```
Elasticsearch Cluster:
Node 1 ←→ Node 2 ←→ Node 3
    ↑ Transport Protocol (9300)
    ↑ Binary Protocol
    ↑ gRPC
    
Kibana ←→ Elasticsearch
    ↑ HTTP/HTTPS (9200)
    ↑ WebSocket
    
Event-Generator Service ←→ Elasticsearch
    ↑ HTTP/HTTPS (9200)
```

