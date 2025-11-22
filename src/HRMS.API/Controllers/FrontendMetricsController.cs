using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace HRMS.API.Controllers;

/// <summary>
/// Frontend Performance Metrics Collection Endpoint
/// Receives Real User Monitoring (RUM) metrics from Angular frontend
/// Optimized for high throughput (millions of requests/min)
/// </summary>
[ApiController]
[Route("api/frontend-metrics")]
[AllowAnonymous] // Allow metrics to be sent without authentication
public class FrontendMetricsController : ControllerBase
{
    private readonly ILogger<FrontendMetricsController> _logger;

    // In-memory storage for metrics (will be scraped by Prometheus)
    // ConcurrentDictionary for thread-safe access under high load
    private static readonly ConcurrentDictionary<string, FrontendMetricValue> _metrics = new();

    // Track total metrics received for debugging
    private static long _totalMetricsReceived = 0;

    public FrontendMetricsController(ILogger<FrontendMetricsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Receive frontend performance metrics
    /// POST /api/frontend-metrics
    /// Accepts batches of metrics from client-side monitoring
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult PostMetrics([FromBody] List<FrontendMetricDto> metrics)
    {
        if (metrics == null || metrics.Count == 0)
        {
            return BadRequest("No metrics provided");
        }

        try
        {
            // Process metrics in batch for performance
            foreach (var metric in metrics)
            {
                ProcessMetric(metric);
            }

            Interlocked.Add(ref _totalMetricsReceived, metrics.Count);

            // Return minimal response for performance
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing frontend metrics");
            return StatusCode(500, "Error processing metrics");
        }
    }

    /// <summary>
    /// Prometheus metrics endpoint - Exposes metrics in Prometheus format
    /// GET /api/frontend-metrics/prometheus
    /// </summary>
    [HttpGet("prometheus")]
    [Produces("text/plain")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetPrometheusMetrics()
    {
        var prometheusOutput = new System.Text.StringBuilder();

        // Add total metrics received counter
        prometheusOutput.AppendLine("# HELP frontend_metrics_received_total Total number of frontend metrics received");
        prometheusOutput.AppendLine("# TYPE frontend_metrics_received_total counter");
        prometheusOutput.AppendLine($"frontend_metrics_received_total {_totalMetricsReceived}");

        prometheusOutput.AppendLine();

        // Group metrics by name
        var groupedMetrics = _metrics.GroupBy(m => m.Value.Name);

        foreach (var group in groupedMetrics)
        {
            var metricName = SanitizeMetricName(group.Key);
            var firstMetric = group.First().Value;

            // Determine metric type based on name
            var metricType = DetermineMetricType(metricName);

            // Add help and type headers
            prometheusOutput.AppendLine($"# HELP {metricName} Frontend metric: {group.Key}");
            prometheusOutput.AppendLine($"# TYPE {metricName} {metricType}");

            // Add metric values with labels
            foreach (var metric in group)
            {
                var labels = FormatLabels(metric.Value.Labels);
                var value = metric.Value.Value;

                if (labels.Length > 0)
                {
                    prometheusOutput.AppendLine($"{metricName}{{{labels}}} {value}");
                }
                else
                {
                    prometheusOutput.AppendLine($"{metricName} {value}");
                }
            }

            prometheusOutput.AppendLine();
        }

        return Content(prometheusOutput.ToString(), "text/plain; version=0.0.4");
    }

    /// <summary>
    /// Health check endpoint
    /// GET /api/frontend-metrics/health
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            metricsCount = _metrics.Count,
            totalReceived = _totalMetricsReceived
        });
    }

    /// <summary>
    /// Clear all metrics (for testing/debugging only)
    /// DELETE /api/frontend-metrics
    /// </summary>
    [HttpDelete]
    [Authorize(Roles = "SuperAdmin")] // Only SuperAdmin can clear metrics
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ClearMetrics()
    {
        _metrics.Clear();
        Interlocked.Exchange(ref _totalMetricsReceived, 0);

        _logger.LogWarning("All frontend metrics cleared by {User}", User.Identity?.Name);

        return Ok(new { message = "All metrics cleared" });
    }

    /// <summary>
    /// Process a single metric and update in-memory storage
    /// </summary>
    private void ProcessMetric(FrontendMetricDto dto)
    {
        var key = GenerateMetricKey(dto.Name, dto.Labels);

        _metrics.AddOrUpdate(
            key,
            // Add new metric
            new FrontendMetricValue
            {
                Name = dto.Name,
                Value = dto.Value,
                Labels = dto.Labels ?? new Dictionary<string, string>(),
                LastUpdated = DateTimeOffset.UtcNow,
                Count = 1
            },
            // Update existing metric (aggregate)
            (existingKey, existingValue) =>
            {
                // For counters, add values; for gauges, update to latest
                if (dto.Name.EndsWith("_total"))
                {
                    existingValue.Value += dto.Value;
                }
                else if (dto.Name.Contains("duration") || dto.Name.Contains("_ms"))
                {
                    // For durations, calculate running average
                    var totalCount = existingValue.Count + 1;
                    existingValue.Value = ((existingValue.Value * existingValue.Count) + dto.Value) / totalCount;
                    existingValue.Count = totalCount;
                }
                else
                {
                    // For other metrics (like scores), use latest value
                    existingValue.Value = dto.Value;
                }

                existingValue.LastUpdated = DateTimeOffset.UtcNow;
                return existingValue;
            }
        );
    }

    /// <summary>
    /// Generate unique key for metric + labels combination
    /// </summary>
    private string GenerateMetricKey(string name, Dictionary<string, string>? labels)
    {
        if (labels == null || labels.Count == 0)
        {
            return name;
        }

        var sortedLabels = labels.OrderBy(l => l.Key);
        var labelsString = string.Join(",", sortedLabels.Select(l => $"{l.Key}={l.Value}"));

        return $"{name}|{labelsString}";
    }

    /// <summary>
    /// Sanitize metric name for Prometheus format
    /// </summary>
    private string SanitizeMetricName(string name)
    {
        // Replace invalid characters with underscores
        return System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9_:]", "_");
    }

    /// <summary>
    /// Format labels for Prometheus output
    /// </summary>
    private string FormatLabels(Dictionary<string, string> labels)
    {
        if (labels == null || labels.Count == 0)
        {
            return string.Empty;
        }

        var formattedLabels = labels.Select(l =>
            $"{SanitizeMetricName(l.Key)}=\"{EscapeLabelValue(l.Value)}\"");

        return string.Join(",", formattedLabels);
    }

    /// <summary>
    /// Escape label values for Prometheus format
    /// </summary>
    private string EscapeLabelValue(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n");
    }

    /// <summary>
    /// Determine Prometheus metric type based on metric name
    /// </summary>
    private string DetermineMetricType(string metricName)
    {
        if (metricName.EndsWith("_total"))
            return "counter";

        if (metricName.Contains("duration") || metricName.Contains("_ms"))
            return "histogram";

        if (metricName.Contains("_ratio") || metricName.Contains("_percent") || metricName.Contains("_score"))
            return "gauge";

        return "gauge"; // Default to gauge
    }
}

/// <summary>
/// Frontend metric data transfer object
/// </summary>
public class FrontendMetricDto
{
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
    public Dictionary<string, string>? Labels { get; set; }
    public long Timestamp { get; set; }
}

/// <summary>
/// Internal metric value storage
/// </summary>
internal class FrontendMetricValue
{
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
    public Dictionary<string, string> Labels { get; set; } = new();
    public DateTimeOffset LastUpdated { get; set; }
    public long Count { get; set; } // For averaging
}
