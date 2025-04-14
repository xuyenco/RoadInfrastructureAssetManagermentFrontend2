namespace RoadInfrastructureAssetManagementFrontend.Model.Response
{
    public class LoginResponse
    {
        public string accessToken { get; set; }
        public string refreshToken { get; set; }
        public string username { get; set; }
        public string role { get; set; }
    }
}
