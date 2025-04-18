using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RoadInfrastructureAssetManagementFrontend2.Filter;

namespace RoadInfrastructureAssetManagementFrontend2.Filter
{
    public class AuthorizeRoleFilter : IAsyncPageFilter
    {
        private readonly string[] _allowedRoles;
        public AuthorizeRoleFilter(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
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
                context.Result = new RedirectToPageResult("/Index")
                {
                    PageHandler = null,
                    RouteValues = new RouteValueDictionary
                    {
                        { "Error", "Access denied. Insufficient permissions." }
                    }
                };
                return;
            }
            await next();
        }
    }
}
public class AuthorizeRoleAttribute : TypeFilterAttribute
{
    public AuthorizeRoleAttribute(params string[] roles) : base(typeof(AuthorizeRoleFilter))
    {
        Arguments = new object[] { roles };
    }
}
