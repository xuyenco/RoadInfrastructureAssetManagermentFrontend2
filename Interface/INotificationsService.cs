using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend2.Interface
{
    public interface INotificationsService
    {
        Task<List<NotificationsResponse>> GetAllNotificationsAsync();
        Task<NotificationsResponse?> GetNotificationByIdAsync(int id);
        Task<List<NotificationsResponse>> GetAllNotificationsByUserIdAsync(int id);
        Task<NotificationsResponse?> CreateNotificationAsync(NotificationsRequest entity);
        Task<NotificationsResponse?> UpdateNotificationAsync(int id, NotificationsRequest entity);
        Task<bool> DeleteNotificationAsync(int id);
    }
}
