using Colectica.Curation.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

            // ---- License fields ----
            if (record.TermsOfUse == "CC0 1.0")
            {
                LicenseDto license = new();
                datasetVersion.License = license;
                license.Name = "CC0 1.0";
                license.Uri = "http://creativecommons.org/publicdomain/zero/1.0";
                license.RightsIdentifier = "CC0-1.0";
            }
            else if (record.TermsOfUse == "CC BY 4.0")
            {
                LicenseDto license = new();
                datasetVersion.License = license;
                license.Name = "CC BY 4.0";
                license.Uri = "http://creativecommons.org/licenses/by/4.0";
            }
            else if (record.TermsOfUse == "Custom Dataset Terms")
            {
                datasetVersion.TermsOfUse = record.RelatedDatabase;
            }

            // ---- Terms fields ----
            datasetVersion.TermsOfAccess = record.AccessStatement;
            datasetVersion.Restrictionions = record.AccessStatement;

            // ---- Custom ISPS block ----
            GenericBlockDto ispsBlock = new();
            metadataBlocks.CustomISPS = ispsBlock;
            ispsBlock.DisplayName = "ISPS Custom Metadata";
            ispsBlock.Name = "customISPS";
            ispsBlock.Fields = [];

            if (record.ArchiveDate != null)
            {
                ispsBlock.Fields.Add(new("ispsArchiveDate", record.ArchiveDate.Value.ToString("yyyy-MM-dd")));
            }

            if (record.CertifiedDate != null)
            {
                ispsBlock.Fields.Add(new("ispsCertifiedDate", record.CertifiedDate.Value.ToString("yyyy-MM-dd")));
            }

            string[] measures = record.OutcomeMeasures.Split(",", StringSplitOptions.RemoveEmptyEntries);
            ispsBlock.Fields.Add(new("ispsOutcomeMeasures", measures, multiple: true));

            ispsBlock.Fields.Add(new("ispsRandomizationProcedure", new List<string>() { record.RandomizationProcedure }, multiple:true));

            string[] modes = record.ModeOfDataCollection.Split(",");
            List<string> modesToAdd = [];
            foreach (string mode in modes)
            {
                modesToAdd.Add(MapModeOfDataCollection(mode));
            }
            ispsBlock.Fields.Add(new FieldDto("ispsModeOfDataCollection", modesToAdd, multiple: true, typeClass: "controlledVocabulary"));

            AddMultipleControlledVocabularyField(ispsBlock, "ispsResearchDesign", record.ResearchDesign, splitOnComma: true);

            if (!string.IsNullOrWhiteSpace(record.ResearchDesignOther))
            {
                ispsBlock.Fields.Add(new("ispsOtherResearchDesign", new List<string>() { record.ResearchDesignOther }, multiple: true));
            }

            string reviewType = "";
            if (record.ReviewType == "Full")
            {
                reviewType = "Full - YARD data and code review";
            }
            else if (record.ReviewType == "Partial")
            {
                reviewType = "Partial - YARD data or code review";
            }
            else if (record.ReviewType == "None")
            {
                reviewType = "None";
            }

            ispsBlock.Fields.Add(new("ispsReviewType", reviewType, typeClass: "controlledVocabulary"));
            ispsBlock.Fields.Add(new("ispsTreatment", new List<string>() { record.Treatment }, multiple: true));

            AddMultipleControlledVocabularyField(ispsBlock, "ispsTreatmentAdministration", record.TreatmentAdministration, splitOnComma: true);
            if (!string.IsNullOrWhiteSpace(record.TreatmentAdministrationOther))
            {
                ispsBlock.Fields.Add(new("ispsOtherTreatmentAdministration", new List<string>() { record.TreatmentAdministrationOther }, multiple: true));
            }

            AddMultipleControlledVocabularyField(ispsBlock, "ispsUnitOfObservation", record.UnitOfObservation, splitOnComma: true);
            if (!string.IsNullOrWhiteSpace(record.UnitOfObservationOther))
            {
                ispsBlock.Fields.Add(new("ispsOtherUnitOfObservation", new List<string>() { record.UnitOfObservationOther }, multiple: true));
            }

            AddMultipleControlledVocabularyField(ispsBlock, "ispsUnitOfRandomization", record.UnitOfRandomization, splitOnComma: true);

            ispsBlock.Fields.Add(new("ispsVersion", record.Version.ToString()));

            // ---- Geospatial fields -----
            List<object> geoCoverageSubFields = [];
            FieldDto geoCoverageField = new();
            geoCoverageField.TypeName = "geographicCoverage";
            geoCoverageField.Multiple = true;
            geoCoverageField.TypeClass = "compound";
            geoCoverageField.Value = geoCoverageSubFields;

            GeospatialDto geoBlock = new();
            metadataBlocks.Geospatial = geoBlock;
            geoBlock.DisplayName = "Geospatial Metadata";
            geoBlock.Fields = [geoCoverageField];

            string locationTrimmed = record.Location?.Trim().Trim(',') ?? "";

            if (record.Location == "United States")
            {
                geoCoverageSubFields.Add(
                    new
                    {
                        Country = new FieldDto("country", locationTrimmed, false, "controlledVocabulary")
                    });
            }
            else
            {
                geoCoverageSubFields.Add(
                    new
                    {
                        OtherGeographicCoverage = new FieldDto("otherGeographicCoverage", locationTrimmed)
                    });
            }

            if (!string.IsNullOrWhiteSpace(record.LocationDetails))
            {
                geoBlock.Fields.Add(new FieldDto("geographicUnit", new List<string>() { record.LocationDetails }, true));
            }

            // ---- Social Science fields ----
            SocialScienceDto socialScienceBlock = new();
            metadataBlocks.SocialScience = socialScienceBlock;
            socialScienceBlock.DisplayName = "Social Science Metadata";
            socialScienceBlock.Fields = [];

            socialScienceBlock.Fields.Add(new("samplingProcedure", record.InclusionExclusionCriteria));

            if (int.TryParse(record.SampleSize.Replace(",", ""), out int sampleSize))
            {
                FieldDto actualSampleSizeField = new("targetSampleActualSize", sampleSize.ToString());
                FieldDto targetSampleSizeField = new("targetSampleSize", new { TargetActualSampleSize = actualSampleSizeField }, typeClass: "compound");
                socialScienceBlock.Fields.Add(targetSampleSizeField);
            }
            else
            {
                // When sample size is not numeric, use the formula field.
                FieldDto formulaField = new("targetSampleSizeFormula", record.SampleSize);
                FieldDto targetSampleSizeField = new("targetSampleSize", new { TargetSampleSizeFormula = formulaField }, typeClass: "compound");
                socialScienceBlock.Fields.Add(targetSampleSizeField);
            }

            if (!string.IsNullOrWhiteSpace(record.FieldDates))
            {
                DateJsonModel? dateModel = JsonSerializer.Deserialize<DateJsonModel>(record.FieldDates);
                if (dateModel != null)
                {
                    FieldDto dataCollectionField = new(); 
                    dataCollectionField.TypeName = "dateOfCollection";
                    dataCollectionField.Multiple = true;
                    dataCollectionField.TypeClass = "compound";

                    if (!string.IsNullOrWhiteSpace(dateModel.endDate) && !dateModel.endDate.Contains("-mm"))
                    {
                        dataCollectionField.Value = new List<object>()
                        {
                            new
                            {
                                DateOfCollectionStart = new FieldDto("dateOfCollectionStart", dateModel.date),
                                DateOfCollectionEnd = new FieldDto("dateOfCollectionEnd", dateModel.endDate)
                            }
                        };
                    }
                    else
                    {
                        dataCollectionField.Value = new List<object>()
                        {
                            new
                            {
                                DateOfCollectionStart = new FieldDto("dateOfCollectionStart", dateModel.date),
                            }
                        };
                    }

                    socialScienceBlock.Fields.Add(dataCollectionField);
                }
            }


            // ---- Citation fields ----
            CitationDto citationBlock = new();
            metadataBlocks.Citation = citationBlock;
            citationBlock.DisplayName = "Citation Metadata";
            citationBlock.Fields = [];

            // Title
            citationBlock.Fields.Add(new("title", record.Title));

            FieldDto otherIdValueField = new()
            {
                TypeName = "otherId",
                Multiple = true,
                TypeClass = "compound",
                Value = new List<object>()
                {
                    new
                    {
                        OtherIdAgency = new FieldDto("otherIdAgency", "ISPS"),
                        OtherIdValue = new FieldDto("otherIdValue", record.Number)
                    }
                }
            };
            citationBlock.Fields.Add(otherIdValueField);

            // citationBlock.Fields.Add(new("otherIdValue", record.Number));

            // Deposit Date
            if (record.CreatedDate != null)
            {
                citationBlock.Fields.Add(new("dateOfDeposit", record.CreatedDate.Value.ToString("yyyy-MM-dd")));
            }

            // Time period covered
            if (!string.IsNullOrWhiteSpace(record.StudyTimePeriod))
            {
                DateJsonModel? dateModel = JsonSerializer.Deserialize<DateJsonModel>(record.StudyTimePeriod);
                if (dateModel != null)
                {
                    object timePeriodObj = null;
                    if (!string.IsNullOrWhiteSpace(dateModel.endDate) && !dateModel.endDate.Contains("-mm"))
                    {
                        timePeriodObj = new
                        {
                            TimePeriodCoveredStart = new FieldDto("timePeriodCoveredStart", dateModel.date),
                            TimePeriodCoveredEnd = new FieldDto("timePeriodCoveredEnd", dateModel.endDate)
                        };
                    }
                    else
                    {
                        timePeriodObj = new
                        {
                            TimePeriodCoveredStart = new FieldDto("timePeriodCoveredStart", dateModel.date)
                        };
                    }

                    FieldDto timePeriodCoveredField = new("timePeriodCovered", new List<object>() { timePeriodObj }, true, "compound");;
                    citationBlock.Fields.Add(timePeriodCoveredField);
                }
            }


            if (!string.IsNullOrWhiteSpace(record.Funding) && record.Funding.ToLower() != "none known")
            {
                FieldDto contactField = new();
                contactField.TypeName = "grantNumber";
                contactField.Multiple = true;
                contactField.TypeClass = "compound";

                List<object> agencyEntries = [];
                string[] fundingAgencies = record.Funding.Split(new[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string agency in fundingAgencies)
                {
                    agencyEntries.Add(new
                    {
                        GrantNumberAgency = new FieldDto("grantNumberAgency", agency.Trim())
                    });
                }

                contactField.Value = agencyEntries;
                citationBlock.Fields.Add(contactField);
            }
            

            // Authors
            if (record.Authors != null && record.Authors.Any())
            {
                FieldDto authorsField = new();
                authorsField.TypeName = "author";
                authorsField.Multiple = true;
                authorsField.TypeClass = "compound";

                List<object> authorObjs = [];
                foreach (var author in record.Authors)
                {
                    if (!string.IsNullOrWhiteSpace(author.Orcid))
                    {
                        authorObjs.Add(new
                        {
                            AuthorName = new FieldDto("authorName", author.FullName),
                            AuthorAffiliation = author.Affiliation == null ? null : new FieldDto("authorAffiliation", author.Affiliation),
                            AuthorIdentifierScheme = new FieldDto("authorIdentifierScheme", "ORCID", typeClass: "controlledVocabulary"),
                            AuthorIdentifier = new FieldDto("authorIdentifier", author.Orcid)
                        });
                    }
                    else
                    {
                        authorObjs.Add(new
                        {
                            AuthorName = new FieldDto("authorName", author.FullName),
                            AuthorAffiliation = author.Affiliation == null ? null : new FieldDto("authorAffiliation", author.Affiliation),
                        });
                    }
                }

                authorsField.Value = authorObjs;
                citationBlock.Fields.Add(authorsField);
            }

            // Distributor
            if (!string.IsNullOrWhiteSpace(record.Organization?.Name))
            {
                string distName = record.Organization.Name;

                if (distName == "ISPS")
                {
                    distName = "Institution for Social and Policy Studies";
                }

                string distAffiliation = distName == "Institution for Social and Policy Studies" ? "Yale University" : "";
                string distAbbreviation = distName == "Institution for Social and Policy Studies" ? "ISPS" : "";
                string distUrl = distName == "Institution for Social and Policy Studies" ? "https://isps.yale.edu" : "";


                FieldDto distributorField = new();
                distributorField.TypeName = "distributor";
                distributorField.Multiple = true;
                distributorField.TypeClass = "compound";
                distributorField.Value = new List<object>
                {
                    new
                    {
                        DistributorName = new FieldDto("distributorName", distName),
                        DistributorAffiliation = new FieldDto("distributorAffiliation", distAffiliation),
                        DistributorAbbreviation = new FieldDto("distributorAbbreviation", distAbbreviation),
                        DistributorUrl = new FieldDto("distributorURL", distUrl)
                    }
                };
                citationBlock.Fields.Add(distributorField);
            }

            // Contact
            if (record.Organization?.ContactInformation != null)
            {
                string orgName = record.Organization.Name;
                if (orgName == "ISPS")
                {
                    orgName = "Institution for Social and Policy Studies";
                }

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
                            Value = "isps@yale.edu"
                        },
                        DatasetContactName = new FieldDto
                        {
                            TypeName = "datasetContactName",
                            Multiple = false,
                            TypeClass = "primitive",
                            Value = orgName
                        },
                        DatasetContactAffiliation = new FieldDto
                        {
                            TypeName = "datasetContactAffiliation",
                            Multiple = false,
                            TypeClass = "primitive",
                            Value = "Yale University"
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

            citationBlock.Fields.Add(new("relatedDatasets", new List<string>() { record.RelatedDatabase ?? "" }, multiple: true));
            citationBlock.Fields.Add(new("relatedMaterial", new List<string>() {record.RelatedPublications }, multiple: true));

            return datasetDto;
        }


        private static void AddMultipleControlledVocabularyField(GenericBlockDto block, string fieldName, string value, bool splitOnComma = false)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                if (splitOnComma)
                {
                    string[] strings = value.Split(",", StringSplitOptions.RemoveEmptyEntries);
                    block.Fields.Add(new FieldDto(fieldName, strings.Select(s => s.Trim()).ToList(), multiple: true, typeClass: "controlledVocabulary"));
                }
                else
                {
                    block.Fields.Add(new FieldDto(fieldName, new List<string> { value }, multiple: true, typeClass: "controlledVocabulary"));
                }
            }
        }

        private static string MapModeOfDataCollection(string originalMode)
        {
            string result = originalMode switch
            {
                "Survey: Web based" => "Survey - Web",
                "Content coding" => "Content Coding",
                "Observation" => "Observation - Field",
                "Self administered questionnaire" => "Self-Administered Questionnaire",
                "Interview: web based" => "Interview - Web",
                "Focus group" => "Focus Group",
                "Interview: email" => "Interview - Email",
                "Interview" => "Interview - Face to Face",
                "Self-administered writing or diaries" => "Self-Administered Writing or Diaries",

                _ => originalMode
            };

            result = result.Replace(": ", " - ");
            return result;
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
        public FieldDto? DatasetContactAffiliation { get; set; }
    }


    public class DatasetVersionDto
    {
        public LicenseDto? License { get; set; }
        public MetadataBlocksDto? MetadataBlocks { get; set; }
        public string? TermsOfUse { get; set; }
        public string? TermsOfAccess { get; set; }
        public string? Restrictionions { get; set; }
        public string? DepositorRequirements { get; set; }
    }

    public class LicenseDto
    {
        public string? Name { get; set; }
        public string? Uri { get; set; }
        public string? RightsIdentifier { get; set; }
    }

    public class MetadataBlocksDto
    {
        public GenericBlockDto? Terms { get; set; }
        public GenericBlockDto? CustomISPS { get; set; }
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
        public string? Name { get; set; }
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

    public class GeospatialDto
    {
        public string? DisplayName { get; set; }
        public string Name => "geospatial";
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

    public class DateJsonModel
    {
        public string dateType { get; set; }
        public string date { get; set; }
        public bool isRange { get; set; }
        public string endDate { get; set; }
    }
}
