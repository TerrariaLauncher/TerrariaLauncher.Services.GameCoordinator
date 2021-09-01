using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database.CQS.Query;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.Database.Queries
{
    class GetUserByNameQuery: Query
    {
        public string Name { get; set; }
    }
}
