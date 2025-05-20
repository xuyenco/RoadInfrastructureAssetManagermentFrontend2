using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RoadInfrastructureAssetManagementFrontend2.Filter
{
    public class AuthorizeRoleFilter : IAsyncPageFilter
    {
        private readonly string[] _allowedRoles;

        public AuthorizeRoleFilter(params string[] allowedRoles)
        {
            // Tách chuỗi theo dấu phẩy và loại bỏ khoảng trắng
            _allowedRoles = allowedRoles
                .SelectMany(role => role.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(role => role.Trim())
                .Distinct() // Loại bỏ role trùng lặp (nếu có)
                .ToArray();
        }

        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        {
            return Task.CompletedTask;
        }

        public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            var role = context.HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(role) || !_allowedRoles.Contains(role))
            {
                // Lấy URL trang trước từ Referer
                var referer = context.HttpContext.Request.Headers["Referer"].ToString();
                var redirectUrl = string.IsNullOrEmpty(referer) ? "/Incidents" : referer;

                // Lưu thông báo lỗi vào Session
                context.HttpContext.Session.SetString("AuthorizationError", "Không đủ quyền hạn để thực hiện hành động này.");

                // Redirect về trang trước hoặc /Incidents
                context.Result = new RedirectResult(redirectUrl);
                return;
            }

            // Xóa thông báo lỗi nếu có quyền
            context.HttpContext.Session.Remove("AuthorizationError");
            await next();
        }
    }

    public class AuthorizeRoleAttribute : TypeFilterAttribute
    {
        public AuthorizeRoleAttribute(params string[] roles) : base(typeof(AuthorizeRoleFilter))
        {
            Arguments = new object[] { roles };
        }
    }
}