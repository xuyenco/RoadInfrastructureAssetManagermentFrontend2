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
        if (string.IsNullOrEmpty(token) && !context.HttpContext.Request.Path.StartsWithSegments("/Users/Login"))
        {
            context.Result = new RedirectToPageResult("/Users/Login");
            return;
        }
        await next();
    }
}