using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database.CQS.Query;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.Database.Queries
{
    class GetUserByUUIDQuery: Query
    {
        public string UUID { get; set; }
    }
}
