using Colectica.Curation.Data;

namespace Colectica.Curation.Dataverse
{
    public class DataversePublisher
    {
        public CatalogRecord CatalogRecord { get; set; }

        public DataversePublisher(CatalogRecord catalogRecord)
        {
            CatalogRecord = catalogRecord;
        }

    }
}
