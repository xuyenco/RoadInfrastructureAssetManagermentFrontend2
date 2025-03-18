using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend.Interface
{
    public interface IUsersService
    {
        Task<List<UsersResponse>> GetAllUsersAsync();
        Task<UsersResponse?> GetUserByIdAsync(int id);
        Task<UsersResponse?> CreateUserAsync(UsersRequest request);
        Task<UsersResponse?> UpdateUserAsync(int id, UsersRequest request);
        Task<bool> DeleteUserAsync(int id);
    }
}
