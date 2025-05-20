using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend2.Interface
{
    public interface IUsersService
    {
        Task<List<UsersResponse>> GetAllUsersAsync();
        Task<UsersResponse?> GetUserByIdAsync(int id);
        Task<UsersResponse?> CreateUserAsync(UsersRequest request);
        Task<UsersResponse?> UpdateUserAsync(int id, UsersRequest request);
        Task<(List<UsersResponse> Users, int TotalCount)> GetUsersAsync(int page, int pageSize, string searchTerm, int searchField);
        Task<bool> DeleteUserAsync(int id);
        Task<LoginResponse?> LoginAsync(LoginRequest request);
        Task<LoginResponse?> RefreshTokenAsync(RefreshTokenRequest refreshTokenRequest);
    }
}
