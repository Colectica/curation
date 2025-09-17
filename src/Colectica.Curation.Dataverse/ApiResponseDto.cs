using System.Text.Json;
using System.Text.Json.Serialization;

namespace Colectica.Curation.Dataverse
{
    public class ApiResponseDto
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("message")]
        public JsonElement Message { get; set; }

        [JsonPropertyName("data")]
        public ApiResponseDataDto? Data { get; set; }

        public string MessageText
        {
            get
            {
                if (Message.ValueKind == JsonValueKind.String)
                {
                    return Message.GetString() ?? "";
                }
                else if (Message.ValueKind == JsonValueKind.Object &&
                        Message.TryGetProperty("message", out JsonElement messageProperty))
                {
                    return messageProperty.GetString() ?? "";
                }
                return "";
            }
        }
    }

    public class ApiResponseDataDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("persistentId")]
        public string? PersistentId { get; set; }

        [JsonPropertyName("datasetPersistentId")]
        public string? DatasetPersistentId { get; set; }
    }
}
