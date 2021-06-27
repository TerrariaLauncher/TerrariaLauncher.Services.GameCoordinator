using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database.CQS.Commons;
using TerrariaLauncher.Commons.Database.CQS.Query;
using TerrariaLauncher.Commons.DomainObjects;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.Database.Queries
{
    class GetUserByNameQuery: Query
    {
        public string Name { get; set; }
    }

    class GetUserByNameQueryResult: IResult
    {
        public User User { get; set; }
    }
}
