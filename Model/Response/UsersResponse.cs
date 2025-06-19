namespace RoadInfrastructureAssetManagementFrontend2.Model.Response
{
    public class UsersResponse
    {
        public int user_id { get; set; }
        public string username { get; set; }
        public string full_name { get; set; }
        public string email { get; set; }
        public string role { get; set; }
        public string department_company_unit { get; set; }
        public string image_url { get; set; }
        public string image_name { get; set; }
        public string image_public_id { get; set; }
        public DateTime? created_at { get; set; }
        public string? refresh_token { get; set; }
        public DateTime? refresh_token_expiry { get; set; }
    }
}
