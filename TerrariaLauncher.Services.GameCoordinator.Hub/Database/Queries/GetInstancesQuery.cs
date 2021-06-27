using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database.CQS.Commons;
using TerrariaLauncher.Commons.Database.CQS.Query;
using TerrariaLauncher.Commons.DomainObjects;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.Database.Queries
{
    class GetInstancesQuery: Query
    {

    }

    class GetInstancesQueryResult: IResult
    {
        public IList<Instance> Instances { get; set; }
    }
}
