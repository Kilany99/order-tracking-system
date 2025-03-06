
using Prometheus;

namespace DriverService.Infrastructure.DriversMetrics;



public class DriverMetrics
{
    private readonly Counter _locationUpdatesTotal;
    private readonly Histogram _locationUpdateLatency;
    private readonly Counter _driverAssignmentsTotal;
    private readonly Counter _assignmentFailuresTotal;
    private readonly Counter _ordersPickedUpTotal;
    private readonly Counter _ordersDeliveredTotal;
    private readonly Gauge _activeDriversCount;
    private readonly Counter _cacheHitsTotal;
    private readonly Counter _cacheMissesTotal;

    public DriverMetrics()
    {
        _locationUpdatesTotal = Metrics.CreateCounter(
            "driver_location_updates_total",
            "Total number of driver location updates");

        _locationUpdateLatency = Metrics.CreateHistogram(
            "driver_location_update_latency_seconds",
            "Latency of location updates",
            new HistogramConfiguration
            {
                Buckets = Histogram.ExponentialBuckets(0.01, 2, 10)
            });

        _driverAssignmentsTotal = Metrics.CreateCounter(
            "driver_assignments_total",
            "Total number of driver assignments");

        _assignmentFailuresTotal = Metrics.CreateCounter(
            "driver_assignment_failures_total",
            "Total number of failed driver assignments");

        _ordersPickedUpTotal = Metrics.CreateCounter(
            "orders_picked_up_total",
            "Total number of orders picked up");

        _ordersDeliveredTotal = Metrics.CreateCounter(
            "orders_delivered_total",
            "Total number of orders delivered");

        _activeDriversCount = Metrics.CreateGauge(
            "active_drivers_current",
            "Current number of active drivers");

        _cacheHitsTotal = Metrics.CreateCounter(
            "driver_cache_hits_total",
            "Total number of cache hits");

        _cacheMissesTotal = Metrics.CreateCounter(
            "driver_cache_misses_total",
            "Total number of cache misses");
    }

    public void RecordLocationUpdate(double latencySeconds)
    {
        _locationUpdatesTotal.Inc();
        _locationUpdateLatency.Observe(latencySeconds);
    }

    public void RecordDriverAssignment(bool success)
    {
        _driverAssignmentsTotal.Inc();
        if (!success)
        {
            _assignmentFailuresTotal.Inc();
        }
    }

    public void RecordOrderPickup()
    {
        _ordersPickedUpTotal.Inc();
    }

    public void RecordOrderDelivery()
    {
        _ordersDeliveredTotal.Inc();
    }

    public void RecordActiveDriver(bool active)
    {
        if (active)
            _activeDriversCount.Inc();
        else
            _activeDriversCount.Dec();
    }

    public void RecordCacheAccess(bool hit)
    {
        if (hit)
            _cacheHitsTotal.Inc();
        else
            _cacheMissesTotal.Inc();
    }
}