using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database.CQS.Commons;
using TerrariaLauncher.Commons.Database.CQS.Query;
using TerrariaLauncher.Commons.DomainObjects;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.Database.Queries
{
    class GetUserByUUIDQuery: Query
    {
        public string UUID { get; set; }
    }

    class GetUserByUUIDQueryResult: IResult
    {
        public User User { get; set; }
    }
}
