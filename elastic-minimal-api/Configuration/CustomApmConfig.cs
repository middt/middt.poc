namespace ElasticMinimalApi.Configuration;

public class CustomApmConfig
{
    public HeaderCaptureConfig HeaderCapture { get; set; } = new();
    public UrlPattern[] UrlGroupingPatterns { get; set; } = Array.Empty<UrlPattern>();
}

public class HeaderCaptureConfig
{
    public HeaderMapping[] Headers { get; set; } = Array.Empty<HeaderMapping>();
}

public class HeaderMapping
{
    public string HeaderName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
}

public class UrlPattern
{
    public string Pattern { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public string Method { get; set; } = "*";  // "*" means match any method
}
