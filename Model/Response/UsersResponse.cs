namespace RoadInfrastructureAssetManagementFrontend.Model.Response
{
    public class UsersResponse
    {

        public int user_id { get; set; }
        public string username { get; set; } = string.Empty;
        public string full_name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty;
        public DateTime? created_at { get; set; }
    }
}
