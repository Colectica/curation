using Colectica.Curation.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.Dataverse
{
    public class DatasetDto
    {
        public DatasetVersionDto DatasetVersion { get; set; }

        public static DatasetDto FromCatalogRecord(CatalogRecord record)
        {
            DatasetDto datasetDto = new();
            DatasetVersionDto datasetVersion = new();
            datasetDto.DatasetVersion = datasetVersion;

            MetadataBlocksDto metadataBlocks = new();
            datasetVersion.MetadataBlocks = metadataBlocks;

            // Citation
            CitationDto citation = new();
            metadataBlocks.Citation = citation;
            citation.DisplayName = "Citation Metadata";
            citation.Fields = new List<FieldDto>();

            // Title
            FieldDto titleField = new();
            titleField.TypeName = "title";
            titleField.Multiple = false;
            titleField.TypeClass = "primitive";
            titleField.Value = record.Title;
            citation.Fields.Add(titleField);

            // Authors
            if (record.Authors != null && record.Authors.Any())
            {
                FieldDto authorsField = new();
                authorsField.TypeName = "author";
                authorsField.Multiple = true;
                authorsField.TypeClass = "compound";
                authorsField.Value = record.Authors.Select(author => new AuthorDto
                {
                    AuthorName = new AuthorNameDto
                    {
                        Value = author.FullName,
                        TypeClass = "primitive",
                        Multiple = false,
                        TypeName = "authorName"
                    },
                    //AuthorAffiliation = author.Affiliation,
                    //AuthorIdentifierScheme = "ORCID",
                    //AuthorIdentifier = author.Orcid
                }).ToList();
                citation.Fields.Add(authorsField);
            }

            // Contact
            if (record.Organization?.ContactInformation != null)
            {
                FieldDto contactField = new();
                contactField.TypeName = "datasetContact";
                contactField.Multiple = true;
                contactField.TypeClass = "compound";
                contactField.Value = new List<object>
                {
                    new DatasetContactValueDto
                    {
                        DatasetContactEmail = new FieldDto
                        {
                            TypeName = "datasetContactEmail",
                            Multiple = false,
                            TypeClass = "primitive",
                            Value = record.Organization.ReplyToAddress
                        },
                        DatasetContactName = new FieldDto
                        {
                            TypeName = "datasetContactName",
                            Multiple = false,
                            TypeClass = "primitive",
                            Value = record.Organization.ContactInformation
                        }
                    }
                };
                citation.Fields.Add(contactField);
            }

            // Description
            FieldDto descriptionField = new();
            descriptionField.TypeName = "dsDescription";
            descriptionField.Multiple = true;
            descriptionField.TypeClass = "compound";
            descriptionField.Value = new List<DescriptionValueDto>
            {
                new DescriptionValueDto
                {
                    DsDescriptionValue = new FieldDto
                    {
                        TypeName = "dsDescriptionValue",
                        Multiple = false,
                        TypeClass = "primitive",
                        Value = record.Description
                    },
                }
            };
            citation.Fields.Add(descriptionField);

            // Subject
            FieldDto subjectField = new();
            subjectField.TypeName = "subject";
            subjectField.Multiple = true;
            subjectField.TypeClass = "controlledVocabulary";
            citation.Fields.Add(subjectField);
            subjectField.Value = new List<string> { "Social Sciences" };
            //subjectField.Value = record.Keywords.Split(",");


            return datasetDto;
        }
    }

    public class DescriptionValueDto
    {
        public FieldDto DsDescriptionValue { get; set; }
    }

    public class DatasetContactValueDto
    {
        public FieldDto DatasetContactEmail { get; set; }
        public FieldDto DatasetContactName { get; set; }
    }


    public class DatasetVersionDto
    {
        public LicenseDto License { get; set; }
        public MetadataBlocksDto MetadataBlocks { get; set; }
    }

    public class LicenseDto
    {
        public string Name { get; set; }
        public string Uri { get; set; }
    }

    public class MetadataBlocksDto
    {
        public CitationDto Citation { get; set; }
        public GeospatialDto Geospatial { get; set; }
        public SocialScienceDto SocialScience { get; set; }
        public AstrophysicsDto Astrophysics { get; set; }
        public BiomedicalDto Biomedical { get; set; }
        public JournalDto Journal { get; set; }
    }

    public class CitationDto
    {
        public string DisplayName { get; set; }
        public List<FieldDto> Fields { get; set; }
    }

    public class FieldDto
    {
        public string TypeName { get; set; }
        public bool Multiple { get; set; }
        public string TypeClass { get; set; }
        public object Value { get; set; }
    }

    public class AuthorDto
    {
        public AuthorNameDto AuthorName { get; set; }
        public string AuthorAffiliation { get; set; }
        public string AuthorIdentifierScheme { get; set; }
        public string AuthorIdentifier { get; set; }
    }

    public class AuthorNameDto
    {
        public string Value { get; set; }
        public string TypeClass { get; set; }
        public bool Multiple { get; set; }
        public string TypeName { get; set; }
    }

    public class GeospatialDto
    {
        public string DisplayName { get; set; }
        public List<FieldDto> Fields { get; set; }
    }

    public class SocialScienceDto
    {
        public string DisplayName { get; set; }
        public List<FieldDto> Fields { get; set; }
    }

    public class AstrophysicsDto
    {
        public string DisplayName { get; set; }
        public List<FieldDto> Fields { get; set; }
    }

    public class BiomedicalDto
    {
        public string DisplayName { get; set; }
        public List<FieldDto> Fields { get; set; }
    }

    public class JournalDto
    {
        public string DisplayName { get; set; }
        public List<FieldDto> Fields { get; set; }
    }

}
