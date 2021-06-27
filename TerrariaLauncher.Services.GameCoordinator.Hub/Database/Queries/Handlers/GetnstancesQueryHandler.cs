using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database;
using TerrariaLauncher.Commons.Database.CQS.Query;
using TerrariaLauncher.Commons.DomainObjects;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.Database.Queries.Handlers
{
    class GetnstancesQueryHandler : QueryHandler<GetInstancesQuery, GetInstancesQueryResult>
    {
        IUnitOfWorkFactory unitOfWorkFactory;
        public GetnstancesQueryHandler(
            ILogger<QueryHandler<GetInstancesQuery, GetInstancesQueryResult>> logger, 
            IUnitOfWorkFactory unitOfWorkFactory) : base(logger)
        {
            this.unitOfWorkFactory = unitOfWorkFactory;
        }

        protected override GetInstancesQueryResult Implementation(GetInstancesQuery query)
        {
            throw new NotImplementedException();
        }

        protected override async Task<GetInstancesQueryResult> ImplementationAsync(GetInstancesQuery query, CancellationToken cancellationToken)
        {
            var uow = await this.unitOfWorkFactory.CreateAsync().ConfigureAwait(false);
            await using (uow.ConfigureAwait(false))
            {
                var command = uow.Connection.CreateCommand();
                command.CommandText = "SELECT * FROM instances";
                await using (command.ConfigureAwait(false))
                {
                    var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                    await using (reader.ConfigureAwait(false))
                    {
                        var result = new GetInstancesQueryResult();
                        result.Instances = new List<Instance>();
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            result.Instances.Add(new Instance()
                            {
                                Id = await reader.GetFieldValueAsync<int>(reader.GetOrdinal("id"), cancellationToken).ConfigureAwait(false),
                                Name = await reader.GetFieldValueAsync<string>(reader.GetOrdinal("name"), cancellationToken).ConfigureAwait(false),
                                Enabled = await reader.GetFieldValueAsync<bool>(reader.GetOrdinal("enabled"), cancellationToken).ConfigureAwait(false),
                                Host = await reader.GetFieldValueAsync<string>(reader.GetOrdinal("host"), cancellationToken).ConfigureAwait(false),
                                Port = await reader.GetFieldValueAsync<int>(reader.GetOrdinal("port"), cancellationToken).ConfigureAwait(false),
                                RestPort = await reader.GetFieldValueAsync<int>(reader.GetOrdinal("restPort"), cancellationToken).ConfigureAwait(false),
                                GrpcPort = await reader.GetFieldValueAsync<int>(reader.GetOrdinal("grpcPort"), cancellationToken).ConfigureAwait(false),
                                GrpcTls = await reader.GetFieldValueAsync<bool>(reader.GetOrdinal("grpcTls"), cancellationToken).ConfigureAwait(false),
                                Version = await reader.GetFieldValueAsync<string>(reader.GetOrdinal("version"), cancellationToken).ConfigureAwait(false),
                                Platform = await reader.GetFieldValueAsync<string>(reader.GetOrdinal("platform"), cancellationToken).ConfigureAwait(false),
                                Realm = await reader.GetFieldValueAsync<string>(reader.GetOrdinal("realm"), cancellationToken).ConfigureAwait(false)
                            });
                        }

                        return result;
                    }
                }
            }
        }
    }
}
