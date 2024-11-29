using Microsoft.AspNetCore.Http;

namespace elastic_minimal_api.Services
{
    public interface IApmHeaderService
    {
        void CaptureHeaders(HttpContext context);
    }
}