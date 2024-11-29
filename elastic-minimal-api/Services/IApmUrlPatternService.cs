using Microsoft.AspNetCore.Http;

namespace elastic_minimal_api.Services;

public interface IApmUrlPatternService
{
    void SetTransactionName(HttpContext context);
}
