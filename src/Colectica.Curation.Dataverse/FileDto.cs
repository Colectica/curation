using Colectica.Curation.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.Dataverse
{
    public class FileDto
    {
        public static FileDto FromManagedFile(ManagedFile managedFile)
        {
            FileDto fileDto = new();

            fileDto.Label = managedFile.PublicName ?? managedFile.Name;
            //fileDto.Title = managedFile.Title;
            fileDto.Source = managedFile.Source;

            if (managedFile.CertifiedDate.HasValue)
            {
                fileDto.PublicationDate = new DateOnly(managedFile.CertifiedDate.Value.Year, managedFile.CertifiedDate.Value.Month, managedFile.CertifiedDate.Value.Day);
            }

            return fileDto;
        }

        public string Label { get; set; }
        public string Title { get; set; }
        public string Source { get; set; }
        public DateOnly PublicationDate { get; set; }

        public string TabIngest => "false";
    }
}
