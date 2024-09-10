using System.Diagnostics;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;


public class XRequestIdPropagator : TextMapPropagator
{
    private const string RequestIdHeader = "X-Request-Id";

    public override ISet<string> Fields => new HashSet<string> { RequestIdHeader };

    public override PropagationContext Extract<T>(PropagationContext context, T carrier, Func<T, string, IEnumerable<string>> getter)
    {
        var requestId = getter(carrier, RequestIdHeader)?.FirstOrDefault();
        if (!string.IsNullOrEmpty(requestId))
        {
            requestId = requestId.ToString().Replace("-", "");
            var activityContext = new ActivityContext(
                ActivityTraceId.CreateFromString(requestId),
                ActivitySpanId.CreateRandom(),
                ActivityTraceFlags.Recorded);
            return new PropagationContext(activityContext, context.Baggage);
        }
        return context;
    }

    public override void Inject<T>(PropagationContext context, T carrier, Action<T, string, string> setter)
    {
        var requestId = context.ActivityContext.TraceId.ToString();
        setter(carrier, RequestIdHeader, requestId);
    }
}

