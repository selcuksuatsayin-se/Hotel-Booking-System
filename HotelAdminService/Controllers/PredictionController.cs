using Microsoft.AspNetCore.Mvc;
using HotelAdminService.Dtos;
using System.Text.Json;

namespace HotelAdminService.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PredictionController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public PredictionController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<ActionResult<PredictionResponseDto>> GetPrediction(PredictionRequestDto input)
        {
            // 1. Get the URL of the Python Service
            // In local dev, this is usually "http://localhost:5000/predict"
            // We read it from appsettings so you can change it easily when you deploy to Cloud.
            var mlServiceUrl = _configuration["MLServiceUrl"] ?? "http://localhost:5000/predict";

            try
            {
                // 2. Create Client
                var client = _httpClientFactory.CreateClient();

                // 3. Send Request to Python
                var response = await client.PostAsJsonAsync(mlServiceUrl, input);

                if (response.IsSuccessStatusCode)
                {
                    // 4. Read Response
                    var result = await response.Content.ReadFromJsonAsync<PredictionResponseDto>();
                    return Ok(result);
                }
                else
                {
                    // If Python script crashes or errors
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, $"ML Service Error: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error contacting ML Service: {ex.Message}");
            }
        }
    }
}