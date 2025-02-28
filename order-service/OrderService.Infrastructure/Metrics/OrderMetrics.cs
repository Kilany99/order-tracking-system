
namespace OrderService.Infrastructure.Metrics;

using Prometheus;
using StackExchange.Redis;
using System.Diagnostics.Metrics;

public class OrderMetrics
{
    // Order Processing Metrics
    private readonly Histogram _orderProcessingDuration;
    private readonly Counter _ordersCreatedTotal;
    private readonly Counter _ordersCompletedTotal;

    // Driver Assignment Metrics
    private readonly Counter _driverAssignmentAttemptsTotal;
    private readonly Counter _driverAssignmentSuccessTotal;

    // Real-time Tracking Metrics
    private readonly Histogram _locationUpdateLatency;
    private readonly Counter _locationUpdatesTotal;

    // Cache Metrics
    private readonly Counter _cacheHitsTotal;
    private readonly Counter _cacheMissesTotal;

    // Message Processing Metrics
    private readonly Counter _messagesProcessedTotal;
    private readonly Counter _messageProcessingErrorsTotal;
    private readonly Histogram _messageProcessingDuration;

    public OrderMetrics()
    {
        // Order Processing Metrics
        _orderProcessingDuration = Metrics.CreateHistogram(
            "order_processing_duration_seconds",
            "Time taken to process orders",
            new HistogramConfiguration
            {
                Buckets = Histogram.ExponentialBuckets(0.1, 2, 10)
            });

        _ordersCreatedTotal = Metrics.CreateCounter(
            "orders_created_total",
            "Total number of orders created");

        _ordersCompletedTotal = Metrics.CreateCounter(
            "orders_completed_total",
            "Total number of orders completed");

        // Driver Assignment Metrics
        _driverAssignmentAttemptsTotal = Metrics.CreateCounter(
            "driver_assignment_attempts_total",
            "Total number of driver assignment attempts");

        _driverAssignmentSuccessTotal = Metrics.CreateCounter(
            "driver_assignment_success_total",
            "Total number of successful driver assignments");

        // Real-time Tracking Metrics
        _locationUpdateLatency = Metrics.CreateHistogram(
            "location_update_latency_seconds",
            "Latency of location updates");

        _locationUpdatesTotal = Metrics.CreateCounter(
            "location_updates_total",
            "Total number of location updates");

        // Cache Metrics
        _cacheHitsTotal = Metrics.CreateCounter(
            "cache_hits_total",
            "Total number of cache hits");

        _cacheMissesTotal = Metrics.CreateCounter(
            "cache_misses_total",
            "Total number of cache misses");

        // Message Processing Metrics
        _messagesProcessedTotal = Metrics.CreateCounter(
            "messages_processed_total",
            "Total number of messages processed",
            new CounterConfiguration
            {
                LabelNames = new[] { "type" }
            });

        _messageProcessingErrorsTotal = Metrics.CreateCounter(
            "message_processing_errors_total",
            "Total number of message processing errors",
            new CounterConfiguration
            {
                LabelNames = new[] { "type" }
            });

        _messageProcessingDuration = Metrics.CreateHistogram(
            "message_processing_duration_seconds",
            "Time taken to process messages",
            new HistogramConfiguration
            {
                LabelNames = new[] { "type" },
                Buckets = Histogram.ExponentialBuckets(0.1, 2, 10)
            });
    }

    public ITimer BeginOrderProcessing() => _orderProcessingDuration.NewTimer();

    public void RecordOrderCreated() => _ordersCreatedTotal.Inc();

    public void RecordOrderCompleted() => _ordersCompletedTotal.Inc();

    public void RecordDriverAssignmentAttempt(bool success)
    {
        _driverAssignmentAttemptsTotal.Inc();
        if (success) _driverAssignmentSuccessTotal.Inc();
    }

    public void RecordLocationUpdate(double latencySeconds)
    {
        _locationUpdateLatency.Observe(latencySeconds);
        _locationUpdatesTotal.Inc();
    }

    public void RecordCacheAccess(bool hit)
    {
        if (hit)
            _cacheHitsTotal.Inc();
        else
            _cacheMissesTotal.Inc();
    }

    public ITimer BeginMessageProcessing(string messageType) =>
        _messageProcessingDuration.WithLabels(messageType).NewTimer();

    public void RecordMessageProcessed(string messageType) =>
        _messagesProcessedTotal.WithLabels(messageType).Inc();

    public void RecordMessageError(string messageType) =>
        _messageProcessingErrorsTotal.WithLabels(messageType).Inc();
}