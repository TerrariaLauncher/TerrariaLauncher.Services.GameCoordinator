using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database;
using TerrariaLauncher.Commons.Database.CQS.Query;
using TerrariaLauncher.Commons.Database.Extensions;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.Database.Queries
{
    class GetBanByIdentityHandler : QueryHandler<GetBanByIdentityQuery, GetBanByIdentityQueryResult>
    {
        IUnitOfWorkFactory unitOfWorkFactory;
        public GetBanByIdentityHandler(
            ILogger<QueryHandler<GetBanByIdentityQuery, GetBanByIdentityQueryResult>> logger,
            IUnitOfWorkFactory unitOfWorkFactory) : base(logger)
        {
            this.unitOfWorkFactory = unitOfWorkFactory;
        }

        protected override async Task<GetBanByIdentityQueryResult> ImplementationAsync(GetBanByIdentityQuery query, CancellationToken cancellationToken)
        {
            var uow = await unitOfWorkFactory.CreateAsync().ConfigureAwait(false);
            await using (uow.ConfigureAwait(false))
            {
                string sql = "SELECT * FROM bans " +
                    "INNER JOIN banDetails ON bans.id = banDetails.banId " +
                    "WHERE type = @type AND identity = @identity AND expiration > utc_timestamp() " +
                    "ORDER BY expiration DESC";
                var command = uow.CreateDbCommand(sql);
                await using (command.ConfigureAwait(false))
                {
                    command.AddParameterWithValue("type", query.IdentityType);
                    command.AddParameterWithValue("identity", query.Identity);

                    var reader = await command.ExecuteReaderAsync(System.Data.CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
                    await using (reader.ConfigureAwait(false))
                    {
                        var result = new GetBanByIdentityQueryResult();
                        if (!await reader.ReadAsync(cancellationToken))
                        {
                            result.Found = false;
                            return result;
                        }

                        result.Id = await reader.GetFieldValueAsync<int>(reader.GetOrdinal("id"), cancellationToken).ConfigureAwait(false);
                        result.Reason = await reader.GetFieldValueAsync<string>(reader.GetOrdinal("string"), cancellationToken).ConfigureAwait(false);
                        return result;
                    }
                }
            }
        }
    }
}
