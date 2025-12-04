using Colectica.Curation.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.Dataverse;

public class FileDto
{
    public static FileDto FromManagedFile(ManagedFile managedFile)
    {
        FileDto fileDto = new();

        string sourceInformationSegment = "";
        if (managedFile.Source == "Curation System")
        {
            sourceInformationSegment = $"Source information: {managedFile.CatalogRecord.Organization.ContactInformation}; ";
        }

        string publishedSegment = "";
        if (managedFile.CatalogRecord.ArchiveDate.HasValue)
        {
            publishedSegment = $"Published {managedFile.CatalogRecord.ArchiveDate.Value.ToShortDateString()}; ";
        }

        fileDto.Title = managedFile.Title;
        fileDto.Label = managedFile.Name;
        fileDto.Description = $"{managedFile.Title}; ISPS number {managedFile.Number}; {publishedSegment}Source: {managedFile.Source}; {sourceInformationSegment}Created with: {managedFile.Software} {managedFile.SoftwareVersion}";
        fileDto.Source = managedFile.Source;
        fileDto.Restricted = !managedFile.IsPublicAccess;

        if (managedFile.Type == "Data")
        {
            if (!string.IsNullOrWhiteSpace(managedFile.KindOfData) && string.Compare(managedFile.KindOfData, "Not Selected", StringComparison.OrdinalIgnoreCase) != 0)
            {
                fileDto.Categories.Add(managedFile.KindOfData);
            }
        }

        if (!string.IsNullOrWhiteSpace(managedFile.Type))
        {
            fileDto.Categories.Add(managedFile.Type);
        }

        if (managedFile.CertifiedDate.HasValue)
        {
            fileDto.DataFile.PublicationDate = new DateOnly(managedFile.CertifiedDate.Value.Year, managedFile.CertifiedDate.Value.Month, managedFile.CertifiedDate.Value.Day);
        }

        return fileDto;
    }

    public string? DataFileId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public bool Restricted { get; set; }
    public List<string> Categories { get; set; } = [];

    public string TabIngest => "true";

    public DataFileDto DataFile { get; set; } = new();
}

public class DataFileDto
{
    public DateOnly? PublicationDate { get; set; }
}