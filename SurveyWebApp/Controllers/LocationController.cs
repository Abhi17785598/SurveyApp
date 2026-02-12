using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace SurveyWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public LocationController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        [HttpPost("save")]
        public async Task<IActionResult> SaveLocation([FromForm] LocationRequest request)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    string sql = @"
                        INSERT INTO UserLocations (UserId, Latitude, Longitude, Accuracy, [Timestamp], EventType)
                        VALUES (@UserId, @Latitude, @Longitude, @Accuracy, @Timestamp, @EventType)";
                    
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", request.UserId);
                        command.Parameters.AddWithValue("@Latitude", request.Latitude);
                        command.Parameters.AddWithValue("@Longitude", request.Longitude);
                        command.Parameters.AddWithValue("@Accuracy", request.Accuracy);
                        command.Parameters.AddWithValue("@Timestamp", DateTimeOffset.FromUnixTimeMilliseconds(request.Timestamp).DateTime);
                        command.Parameters.AddWithValue("@EventType", request.EventType ?? "MANUAL");
                        
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        
                        if (rowsAffected > 0)
                        {
                            return Ok(new { success = true, message = "Location saved successfully" });
                        }
                        else
                        {
                            return BadRequest(new { success = false, message = "Failed to save location" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error saving location: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("list/{userId:int}")]
        public async Task<IActionResult> GetUserLocations(int userId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    string sql = @"
                        SELECT Id, UserId, Latitude, Longitude, Accuracy, Timestamp, EventType
                        FROM UserLocations
                        WHERE UserId = @UserId
                        ORDER BY Timestamp DESC";
                    
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            var locations = new List<LocationResponse>();
                            
                            while (await reader.ReadAsync())
                            {
                                locations.Add(new LocationResponse
                                {
                                    Id = reader.GetInt32("Id"),
                                    UserId = reader.GetInt32("UserId").ToString(),
                                    Latitude = Convert.ToDouble(reader.GetDecimal("Latitude")),
                                    Longitude = Convert.ToDouble(reader.GetDecimal("Longitude")),
                                    Accuracy = reader.GetDouble("Accuracy"),
                                    Timestamp = reader.GetString("Timestamp"),
                                    EventType = reader.GetString("EventType")
                                });
                            }
                            
                            return Ok(new { success = true, locations = locations });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting locations: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }
    }

    public class LocationRequest
    {
        public int UserId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public float Accuracy { get; set; }
        public long Timestamp { get; set; }
        public string EventType { get; set; } = "MANUAL";
    }

    public class LocationResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = "";
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
        [JsonPropertyName("accuracy")]
        public double Accuracy { get; set; }
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = "";
        [JsonPropertyName("eventType")]
        public string EventType { get; set; } = "";
    }
}
