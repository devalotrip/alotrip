using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Shared.BuildingBlocks.Logging;

/// <summary>
/// Enriches every log event with the current OpenTelemetry trace_id and span_id.
/// Enables log-to-trace correlation in Grafana (Loki → Tempo).
/// </summary>
public sealed class ActivityTraceEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity is null) return;

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("trace_id", activity.TraceId.ToString()));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("span_id",  activity.SpanId.ToString()));
    }
}
