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
        public DatasetVersionDto? DatasetVersion { get; set; }

        public static DatasetDto FromCatalogRecord(CatalogRecord record)
        {
            DatasetDto datasetDto = new();
            DatasetVersionDto datasetVersion = new();
            datasetDto.DatasetVersion = datasetVersion;

            MetadataBlocksDto metadataBlocks = new();
            datasetVersion.MetadataBlocks = metadataBlocks;

            // ---- Terms fields ----
            GenericBlockDto termsBlock = new();
            metadataBlocks.Terms = termsBlock;
            termsBlock.DisplayName = "Terms of Use and Access";
            termsBlock.Fields = [];

            termsBlock.Fields.Add(new("restrictions", record.AccessStatement));
            termsBlock.Fields.Add(new("confidentialitydeclaration", record.ConfidentialityStatement));
            termsBlock.Fields.Add(new("depositorrequirements", record.DepositAgreement));
            termsBlock.Fields.Add(new("availabilitystatus", record.EmbargoStatement));

            // ---- Custom ISPS block ----
            GenericBlockDto ispsBlock = new();
            metadataBlocks.Isps = ispsBlock;
            ispsBlock.DisplayName = "ISPS Custom Metadata";
            ispsBlock.Fields = [];

            if (record.ArchiveDate != null)
            {
                ispsBlock.Fields.Add(new("ispsArchiveDate", record.ArchiveDate.Value.ToString("yyyy-MM-dd")));
            }

            if (record.CertifiedDate != null)
            {
                ispsBlock.Fields.Add(new("ispsCertifiedDate", record.CertifiedDate.Value.ToString("yyyy-MM-dd")));
            }

            ispsBlock.Fields.Add(new("ispsOutcomeMeasures", new List<string>() { record.OutcomeMeasures }, multiple:true));
            ispsBlock.Fields.Add(new("randomizationProcedure", record.RandomizationProcedure));

            string researchDesign = GetResearchDesignTerm(record.ResearchDesign ?? "", out string researchDesignOtherSpecify);
            AddMultipleControlledVocabularyField(ispsBlock, "ispsResearchDesign", researchDesign);
            ispsBlock.Fields.Add(new("ispsOtherResearchDesign", new List<string>() { researchDesignOtherSpecify }, multiple: true));

            ispsBlock.Fields.Add(new("ispsReviewType", record.ReviewType, typeClass: "controlledVocabulary"));
            ispsBlock.Fields.Add(new("ispsTreatment", new List<string>() { record.Treatment }, multiple: true));

            string treatmentAdministration = GetTreatmentAdministrationTerm(record.TreatmentAdministration ?? "", out string treatmentAdministrationOtherSpecify);

            AddMultipleControlledVocabularyField(ispsBlock, "ispsTreatmentAdministration", treatmentAdministration);

            ispsBlock.Fields.Add(new("ispsOtherTreatmentAdministration", new List<string>() { treatmentAdministrationOtherSpecify }, multiple: true));

            string unitOfObservation = GetUnitOfTerm(record.UnitOfObservation ?? "", out string unitOfObservationOtherSpecify);

            AddMultipleControlledVocabularyField(ispsBlock, "ispsUnitOfObservation", unitOfObservation);
            ispsBlock.Fields.Add(new("ispsOtherUnitOfObservation", new List<string>() { unitOfObservationOtherSpecify }, multiple: true));

            string unitOfRandomization = GetUnitOfTerm(record.UnitOfRandomization ?? "", out string _);
            AddMultipleControlledVocabularyField(ispsBlock, "ispsUnitOfRandomization", unitOfRandomization);

            ispsBlock.Fields.Add(new("ispsVersion", record.Version.ToString()));

            // ---- Social Science fields ----
            SocialScienceDto socialScienceBlock = new();
            metadataBlocks.SocialScience = socialScienceBlock;
            socialScienceBlock.DisplayName = "Social Science Metadata";
            socialScienceBlock.Fields = [];

            socialScienceBlock.Fields.Add(new("samplingProcedure", record.InclusionExclusionCriteria));

            if (int.TryParse(record.SampleSize, out int sampleSize))
            {
                FieldDto actualSampleSizeField = new("targetSampleActualSize", sampleSize.ToString());
                FieldDto targetSampleSizeField = new("targetSampleSize", new { TargetActualSampleSize = actualSampleSizeField }, typeClass: "compound");
                socialScienceBlock.Fields.Add(targetSampleSizeField);
            }


            // ---- Citation fields ----
            CitationDto citationBlock = new();
            metadataBlocks.Citation = citationBlock;
            citationBlock.DisplayName = "Citation Metadata";
            citationBlock.Fields = [];

            // Title
            citationBlock.Fields.Add(new("title", record.Title));
            citationBlock.Fields.Add(new("otherIdValue", record.Number));

            if (record.CreatedDate != null)
            {
                citationBlock.Fields.Add(new("dateOfDeposit", record.CreatedDate.Value.ToString("yyyy-MM-dd")));
            }

            // Authors
            if (record.Authors != null && record.Authors.Any())
            {
                FieldDto authorsField = new();
                authorsField.TypeName = "author";
                authorsField.Multiple = true;
                authorsField.TypeClass = "compound";
                authorsField.Value = record.Authors.Select(author => new AuthorDto
                {
                    AuthorName = new FieldDto("authorName", author.FullName)
                    //AuthorAffiliation = author.Affiliation,
                    //AuthorIdentifierScheme = "ORCID",
                    //AuthorIdentifier = author.Orcid
                }).ToList();
                citationBlock.Fields.Add(authorsField);
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
                citationBlock.Fields.Add(contactField);
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
            citationBlock.Fields.Add(descriptionField);

            // Subject
            FieldDto subjectField = new();
            subjectField.TypeName = "subject";
            subjectField.Multiple = true;
            subjectField.TypeClass = "controlledVocabulary";
            citationBlock.Fields.Add(subjectField);
            subjectField.Value = new List<string> { "Social Sciences" };
            
            // Keywords
            FieldDto keywordField = new();
            keywordField.TypeName = "keyword";
            keywordField.Multiple = true;
            keywordField.TypeClass = "compound";
            keywordField.Value = record.Keywords.Split(",")
                .Select(str => new { KeywordValue = new FieldDto("keywordValue", str.Trim()) } )
                .ToArray();
            citationBlock.Fields.Add(keywordField);

            citationBlock.Fields.Add(new("relatedDatasets", new List<string>() { record.RelatedDatabase }, multiple: true));
            citationBlock.Fields.Add(new("relatedMaterial", new List<string>() { record.RelatedProjects }, multiple: true));
            citationBlock.Fields.Add(new("relatedMaterial", new List<string>() {record.RelatedPublications }, multiple: true));

            return datasetDto;
        }

        private static void AddMultipleControlledVocabularyField(GenericBlockDto block, string fieldName, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                block.Fields.Add(new FieldDto(fieldName, new List<string> { value }, multiple: true, typeClass: "controlledVocabulary"));
            }
        }

        private static string GetResearchDesignTerm(string input, out string otherSpecify)
        {
            otherSpecify = string.Empty;

            input = input.Trim().ToLowerInvariant();
            string harmonizedTerm = input switch
            {
                "" => "",
                "multiple" => string.Empty,
                "field experiment" => "Field Experiment",
                "lab experiment" => "Lab Experiment",
                "matching" => "Matching",
                "metaanalysis" => "Meta-analysis",
                "natural experiment" => "Natural Experiment",
                "observational" => "Observational",
                "regression discontinuity" => "Regression Discontinuity Design",
                "regression discontinuity design" => "Regression Discontinuity Design",
                "survey experiment" => "Survey Experiment",
                "other" => "Other",
                _ => "Other"
            };

            if (harmonizedTerm == "Other")
            {
                otherSpecify = input;
            }

            return harmonizedTerm;
        }

        private static string GetTreatmentAdministrationTerm(string input, out string otherSpecify)
        {
            otherSpecify = string.Empty;

            input = input.Trim().ToLowerInvariant();
            string harmonizedTerm = input switch
            {
                "not applicable" => "",
                "n/a" => "",
                "not selected" => "",
                "door to Door" => "Door to Door",
                "email" => "Email",
                "government-administered program" => "Government Program",
                "mail" => "Mail",
                "multiple" => "",
                "ngo-adminstered program" => "NGO Program",
                "ngo-administered program" => "NGO Program",
                "phone" => "Phone",
                "radio" => "Radio",
                "school-administered program" => "School Program",
                "television" => "Television",
                "mobile technology / Text messages" => "Text Messages",
                "web delivered" => "Web",
                "other" => "Other",
                _ => "Other"
            };

            if (harmonizedTerm == "Other")
            {
                otherSpecify = input;
            }

            return harmonizedTerm;
        }

        private static string GetUnitOfTerm(string input, out string otherSpecify)
        {
            otherSpecify = string.Empty;

            input = input.Trim().ToLowerInvariant();
            string harmonizedTerm = input switch
            {
                "event/process" => "Event or Process",
                "family" => "Family",
                "geo: census track" => "Geo - Census Track",
                "geo: country" => "Geo - Country",
                "geo: district" => "Geo - District",
                "geo: dma" => "Geo - DMA",
                "geo: other" => "Geo - Other",
                "geo: region" => "Geo - Region",
                "geo: school" => "Geo - School",
                "geo: village" => "Geo - Village",
                "household: family" => "Household - Family",
                "household: unit" => "Household - Unit",
                "housing unit" => "Housing Unit",
                "individual" => "Individual",
                "individuals" => "Individual",
                "multiple" => "",
                "organization" => "Organization",
                "other" => "Other",
                _ => "Other"
            };

            if (harmonizedTerm == "Other")
            {
                otherSpecify = input;
            }

            return harmonizedTerm;       
        }

    }

    public class DescriptionValueDto
    {
        public FieldDto? DsDescriptionValue { get; set; }
    }

    public class DatasetContactValueDto
    {
        public FieldDto? DatasetContactEmail { get; set; }
        public FieldDto? DatasetContactName { get; set; }
    }


    public class DatasetVersionDto
    {
        public LicenseDto? License { get; set; }
        public MetadataBlocksDto? MetadataBlocks { get; set; }
    }

    public class LicenseDto
    {
        public string? Name { get; set; }
        public string? Uri { get; set; }
    }

    public class MetadataBlocksDto
    {
        public GenericBlockDto? Terms { get; set; }
        public GenericBlockDto? Isps { get; set; }
        public CitationDto? Citation { get; set; }
        public GeospatialDto? Geospatial { get; set; }
        public SocialScienceDto? SocialScience { get; set; }
        public AstrophysicsDto? Astrophysics { get; set; }
        public BiomedicalDto? Biomedical { get; set; }
        public JournalDto? Journal { get; set; }
    }

    public class GenericBlockDto
    {
        public string? DisplayName { get; set; }
        public List<FieldDto> Fields { get; set; } = [];
    }

    public class CitationDto
    {
        public string? DisplayName { get; set; }
        public List<FieldDto> Fields { get; set; } = [];
    }

    public class FieldDto
    {
        public string? TypeName { get; set; }
        public bool Multiple { get; set; } = false;
        public string? TypeClass { get; set; }
        public object? Value { get; set; }

        public FieldDto()
        {

        }

        public FieldDto(string typeName, object value, bool multiple = false, string typeClass = "primitive")
        {
            TypeName = typeName;
            Multiple = multiple;
            TypeClass = typeClass;
            Value = value;
        }
    }

    public class AuthorDto
    {
        public FieldDto? AuthorName { get; set; }
        public string? AuthorAffiliation { get; set; }
        public string? AuthorIdentifierScheme { get; set; }
        public string? AuthorIdentifier { get; set; }
    }

    public class GeospatialDto
    {
        public string? DisplayName { get; set; }
        public List<FieldDto> Fields { get; set; } = [];
    }

    public class SocialScienceDto
    {
        public string? DisplayName { get; set; }
        public List<FieldDto> Fields { get; set; } = [];
    }

    public class AstrophysicsDto
    {
        public string? DisplayName { get; set; }
        public List<FieldDto> Fields { get; set; } = [];
    }

    public class BiomedicalDto
    {
        public string? DisplayName { get; set; }
        public List<FieldDto> Fields { get; set; } = [];
    }

    public class JournalDto
    {
        public string? DisplayName { get; set; }
        public List<FieldDto> Fields { get; set; } = [];
    }

}
