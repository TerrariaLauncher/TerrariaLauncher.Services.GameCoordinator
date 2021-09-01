using TerrariaLauncher.Commons.Database.CQS.Request;
using TerrariaLauncher.Commons.DomainObjects;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.Database.Queries
{
    class GetUserByUUIDQueryResult : IResult
    {
        public User User { get; set; }
    }
}
