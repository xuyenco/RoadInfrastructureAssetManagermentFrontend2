using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Notifications
{
    public class InboxModel : PageModel
    {
        private readonly INotificationsService _notificationService;

        public InboxModel(INotificationsService notificationService)
        {
            _notificationService = notificationService;
        }

        public List<NotificationsResponse> Notifications { get; set; } = new List<NotificationsResponse>();

        public async Task OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("Id") ?? 0;
            if (userId != 0)
            {
                Notifications = (await _notificationService.GetAllNotificationsByUserIdAsync(userId))
                    .OrderByDescending(n => n.created_at)
                    .ToList();
            }
        }
    }
}
