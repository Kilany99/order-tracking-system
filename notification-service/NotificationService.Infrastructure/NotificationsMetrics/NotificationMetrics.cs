
using Prometheus;

namespace NotificationService.Infrastructure.NotificationsMetrics;

public class NotificationMetrics
{
    private readonly Counter _notificationsSentTotal;
    private readonly Counter _notificationsFailedTotal;
    private readonly Histogram _notificationLatency;
    private readonly Counter _emailsSentTotal;
    private readonly Counter _emailsFailedTotal;
    private readonly Counter _customerInfoRequestsTotal;
    private readonly Counter _customerInfoFailuresTotal;
    private readonly Counter _messageProcessedTotal;
    private readonly Counter _messageProcessingErrorsTotal;

    public NotificationMetrics()
    {
        _notificationsSentTotal = Metrics.CreateCounter(
            "notifications_sent_total",
            "Total number of notifications sent");

        _notificationsFailedTotal = Metrics.CreateCounter(
            "notifications_failed_total",
            "Total number of failed notifications");

        _notificationLatency = Metrics.CreateHistogram(
            "notification_processing_duration_seconds",
            "Time taken to process notifications",
            new HistogramConfiguration
            {
                Buckets = Histogram.ExponentialBuckets(0.01, 2, 10)
            });

        _emailsSentTotal = Metrics.CreateCounter(
            "emails_sent_total",
            "Total number of emails sent");

        _emailsFailedTotal = Metrics.CreateCounter(
            "emails_failed_total",
            "Total number of failed email sends");

        _customerInfoRequestsTotal = Metrics.CreateCounter(
            "customer_info_requests_total",
            "Total number of customer info requests");

        _customerInfoFailuresTotal = Metrics.CreateCounter(
            "customer_info_failures_total",
            "Total number of failed customer info requests");

        _messageProcessedTotal = Metrics.CreateCounter(
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
    }

    public Prometheus.ITimer BeginNotificationProcessing()
    {
        return _notificationLatency.NewTimer();
    }

    public void RecordNotificationSent(bool success)
    {
        if (success)
            _notificationsSentTotal.Inc();
        else
            _notificationsFailedTotal.Inc();
    }

    public void RecordEmailSent(bool success)
    {
        if (success)
            _emailsSentTotal.Inc();
        else
            _emailsFailedTotal.Inc();
    }

    public void RecordCustomerInfoRequest(bool success)
    {
        _customerInfoRequestsTotal.Inc();
        if (!success)
            _customerInfoFailuresTotal.Inc();
    }

    public void RecordMessageProcessed(string messageType)
    {
        _messageProcessedTotal.WithLabels(messageType).Inc();
    }

    public void RecordMessageError(string messageType)
    {
        _messageProcessingErrorsTotal.WithLabels(messageType).Inc();
    }
}