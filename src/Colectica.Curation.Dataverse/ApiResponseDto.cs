using System.Text.Json.Serialization;

namespace Colectica.Curation.Dataverse
{
    public class ApiResponseDto
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("data")]
        public ApiResponseDataDto? Data { get; set; }
    }

    public class ApiResponseDataDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("persistentId")]
        public string? PersistentId { get; set; }
    }
}
