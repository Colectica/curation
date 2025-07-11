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

        fileDto.Title = managedFile.Title;
        fileDto.Label = managedFile.PublicName ?? managedFile.Name;
        fileDto.Description = $"{managedFile.Title}; ISPS number {managedFile.Number}; Published {managedFile.CertifiedDate}; Source: {managedFile.Source}; Source information: {managedFile.SourceInformation}; Created with: {managedFile.Software} {managedFile.SoftwareVersion}";
        fileDto.Source = managedFile.Source;
        fileDto.Restricted = !managedFile.IsPublicAccess;

        if (managedFile.CertifiedDate.HasValue)
        {
            fileDto.DataFile.PublicationDate = new DateOnly(managedFile.CertifiedDate.Value.Year, managedFile.CertifiedDate.Value.Month, managedFile.CertifiedDate.Value.Day);
        }

        return fileDto;
    }

    public string Description { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public bool Restricted { get; set; }

    public string TabIngest => "false";

    public DataFileDto DataFile { get; set; } = new();
}

public class DataFileDto
{
    public DateOnly PublicationDate { get; set; }
}