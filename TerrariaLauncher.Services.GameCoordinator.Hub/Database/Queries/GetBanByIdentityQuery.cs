using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database.CQS.Commons;
using TerrariaLauncher.Commons.Database.CQS.Query;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.Database.Queries
{
    class GetBanByIdentityQuery: Query
    {
        public string IdentityType { get; set; }
        public string Identity { get; set; }
    }

    class GetBanByIdentityQueryResult: IResult
    {
        public bool Found { get; set; }
        public int Id { get; set; }
        public string Reason { get; set; }
    }
}
