using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database;
using TerrariaLauncher.Commons.DomainObjects;
using TerrariaLauncher.Commons.Database.Extensions;
using TerrariaLauncher.Commons.Database.CQS.Query;
using Microsoft.Extensions.Logging;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.Database.Queries
{
    class GetUserByNameHandler : QueryHandler<GetUserByNameQuery, GetUserByNameQueryResult>
    {
        IUnitOfWorkFactory unitOfWorkFactory;
        public GetUserByNameHandler(
            ILogger<QueryHandler<GetUserByNameQuery, GetUserByNameQueryResult>> logger,
            IUnitOfWorkFactory unitOfWorkFactory)
            : base(logger)
        {
            this.unitOfWorkFactory = unitOfWorkFactory;
        }

        protected override async Task<GetUserByNameQueryResult> ImplementationAsync(GetUserByNameQuery query, CancellationToken cancellationToken)
        {
            var uow = await unitOfWorkFactory.CreateAsync().ConfigureAwait(false);
            await using (uow.ConfigureAwait(false))
            {
                var command = uow.Connection.CreateCommand();
                await using (command.ConfigureAwait(false))
                {
                    command.CommandText = "SELECT * FROM users WHERE name = @name";
                    command.Transaction = uow.Transaction;

                    command.AddParameterWithValue("name", query.Name);
                    var reader = await command.ExecuteReaderAsync(System.Data.CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);

                    var result = new GetUserByNameQueryResult();
                    await using (reader.ConfigureAwait(false))
                    {
                        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            result.User = new User()
                            {
                                Id = await reader.GetFieldValueAsync<int>(reader.GetOrdinal("id"), cancellationToken).ConfigureAwait(false),
                                Name = await reader.GetFieldValueAsync<string>(reader.GetOrdinal("name"), cancellationToken).ConfigureAwait(false),
                                Password = await reader.GetFieldValueAsync<string>(reader.GetOrdinal("password"), cancellationToken).ConfigureAwait(false),
                                Group = await reader.GetFieldValueAsync<string>(reader.GetOrdinal("group"), cancellationToken).ConfigureAwait(false),
                                UUID = await reader.GetFieldValueAsync<string>(reader.GetOrdinal("uuid"), cancellationToken).ConfigureAwait(false)
                            };
                        }
                    }
                    return result;
                }
            }
        }
    }
}
