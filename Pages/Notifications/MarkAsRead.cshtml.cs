using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using System.Threading.Tasks;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Notifications
{
    public class MarkAsReadModel : PageModel
    {
        private readonly INotificationsService _notificationService;

        public MarkAsReadModel(INotificationsService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            try
            {
                var currentNotification = await _notificationService.GetNotificationByIdAsync(id);
                
                var request = new NotificationsRequest
                {
                    user_id = currentNotification.user_id, 
                    task_id = currentNotification.task_id,
                    message = currentNotification.message,
                    is_read = true,
                    notification_type = currentNotification.notification_type,
                };
                var updated = await _notificationService.UpdateNotificationAsync(id, request);
                if (updated == null)
                {
                    return new JsonResult(new { success = false, message = "Notification not found" });
                }
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
    }
}