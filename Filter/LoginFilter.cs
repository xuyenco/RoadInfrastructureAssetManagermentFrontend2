using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class LoginFilter : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
    {
        return Task.CompletedTask;
    }

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var token = context.HttpContext.Session.GetString("AccessToken");
        var path = context.HttpContext.Request.Path;
        if (string.IsNullOrEmpty(token) &&
            !path.StartsWithSegments("/Users/Login") &&
            !path.StartsWithSegments("/Map") &&
            !path.StartsWithSegments("/Incidents") &&
            path != "/")
        {
            Console.WriteLine($"Redirecting to /Users/Login from {path}");
            context.Result = new RedirectToPageResult("/Users/Login");
            return;
        }
        Console.WriteLine($"Allowing access to {path}, Token: {token}");
        await next();
    }
}