using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace Colectica.Curation.Data
{
    public class NpgsqlConfiguration : DbConfiguration
    {
        public NpgsqlConfiguration()
        {
            SetMigrationSqlGenerator("Npgsql", () => new NpgsqlMigrationSqlGenerator());

            var name = "Npgsql";

            SetProviderFactory(providerInvariantName: name,
                               providerFactory: NpgsqlFactory.Instance);

            SetProviderServices(providerInvariantName: name,
                                provider: NpgsqlServices.Instance);

            //SetExecutionStrategy("Npgsql", )
            SetDefaultConnectionFactory(connectionFactory: new NpgsqlConnectionFactory());
        }
    }
}
