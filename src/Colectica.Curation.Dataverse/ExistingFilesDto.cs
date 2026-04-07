using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Colectica.Curation.Dataverse
{
    public class ExistingFilesDto
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("data")]
        public List<ExistingFileItemDto> Data { get; set; } = new List<ExistingFileItemDto>();
    }

    public class ExistingFileItemDto
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("restricted")]
        public bool Restricted { get; set; }

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("datasetVersionId")]
        public int DatasetVersionId { get; set; }

        [JsonPropertyName("categories")]
        public List<string> Categories { get; set; } = new List<string>();

        [JsonPropertyName("dataFile")]
        public ExistingDataFileDto DataFile { get; set; } = new ExistingDataFileDto();
    }

    public class ExistingDataFileDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("persistentId")]
        public string PersistentId { get; set; } = string.Empty;

        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty;

        [JsonPropertyName("originalFileName")]
        public string OriginalFileName { get; set; } = string.Empty;

        [JsonPropertyName("contentType")]
        public string ContentType { get; set; } = string.Empty;

        [JsonPropertyName("friendlyType")]
        public string FriendlyType { get; set; } = string.Empty;

        [JsonPropertyName("filesize")]
        public long Filesize { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("categories")]
        public List<string> Categories { get; set; } = new List<string>();

        [JsonPropertyName("storageIdentifier")]
        public string StorageIdentifier { get; set; } = string.Empty;

        [JsonPropertyName("rootDataFileId")]
        public int RootDataFileId { get; set; }

        [JsonPropertyName("md5")]
        public string Md5 { get; set; } = string.Empty;

        [JsonPropertyName("checksum")]
        public ChecksumDto Checksum { get; set; } = new ChecksumDto();

        [JsonPropertyName("tabularData")]
        public bool TabularData { get; set; }

        [JsonPropertyName("creationDate")]
        public string CreationDate { get; set; } = string.Empty;

        [JsonPropertyName("fileAccessRequest")]
        public bool FileAccessRequest { get; set; }
    }

    public class ChecksumDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }
}
