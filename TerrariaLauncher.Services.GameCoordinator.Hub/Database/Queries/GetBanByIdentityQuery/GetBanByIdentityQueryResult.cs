using TerrariaLauncher.Commons.Database.CQS.Request;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.Database.Queries
{
    class GetBanByIdentityQueryResult : IResult
    {
        public bool Found { get; set; }
        public int Id { get; set; }
        public string Reason { get; set; }
    }
}
