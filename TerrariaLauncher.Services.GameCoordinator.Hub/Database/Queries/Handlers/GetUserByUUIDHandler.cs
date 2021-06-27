using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database;
using TerrariaLauncher.Commons.Database.CQS.Query;
using TerrariaLauncher.Commons.DomainObjects;
using TerrariaLauncher.Commons.Database.Extensions;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.Database.Queries.Handlers
{
    class GetUserByUUIDHandler : QueryHandler<GetUserByUUIDQuery, GetUserByUUIDQueryResult>
    {
        IUnitOfWorkFactory unitOfWorkFactory;
        public GetUserByUUIDHandler(
            ILogger<QueryHandler<GetUserByUUIDQuery, GetUserByUUIDQueryResult>> logger, 
            IUnitOfWorkFactory unitOfWorkFactory) : base(logger)
        {
            this.unitOfWorkFactory = unitOfWorkFactory;
        }

        protected override GetUserByUUIDQueryResult Implementation(GetUserByUUIDQuery query)
        {
            throw new NotImplementedException();
        }

        protected override async Task<GetUserByUUIDQueryResult> ImplementationAsync(GetUserByUUIDQuery query, CancellationToken cancellationToken)
        {
            var uow = await this.unitOfWorkFactory.CreateAsync().ConfigureAwait(false);
            await using (uow.ConfigureAwait(false))
            {
                var command = uow.Connection.CreateCommand();
                await using (command.ConfigureAwait(false))
                {
                    command.CommandText = "SELECT * FROM users WHERE uuid = @uuid";

                    command.AddParameterWithValue("uuid", query.UUID);
                    var reader = await command.ExecuteReaderAsync(System.Data.CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);

                    await using (reader.ConfigureAwait(false))
                    {
                        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            return null;
                        }

                        var result = new GetUserByUUIDQueryResult();
                        result.User = new User()
                        {
                            Id = await reader.GetFieldValueAsync<int>(reader.GetOrdinal("id"), cancellationToken).ConfigureAwait(false),
                            Name = await reader.GetFieldValueAsync<string>(reader.GetOrdinal("name"), cancellationToken).ConfigureAwait(false),
                            Password = await reader.GetFieldValueAsync<string>(reader.GetOrdinal("password"), cancellationToken).ConfigureAwait(false),
                            Group = await reader.GetFieldValueAsync<string>(reader.GetOrdinal("group"), cancellationToken).ConfigureAwait(false),
                            UUID = await reader.GetFieldValueAsync<string>(reader.GetOrdinal("uuid"), cancellationToken).ConfigureAwait(false)
                        };

                        return result;
                    }
                }
            }
        }
    }
}
