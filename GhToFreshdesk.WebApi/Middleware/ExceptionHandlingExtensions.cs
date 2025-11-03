using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace GhToFreshdesk.WebApi.Middleware;

public static class ExceptionHandlingExtensions
{
    public static void UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(builder =>
        {
            builder.Run(async context =>
            {
                var logger = context.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("GlobalExceptionHandler");

                var feature = context.Features.Get<IExceptionHandlerFeature>();
                var ex = feature?.Error;

                context.Response.ContentType = "application/json";

                if (ex is ValidationException ve)
                {
                    logger.LogWarning(ex, "Validation failed at {Path}", context.Request.Path);
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        title = "Validation failed",
                        errors = ve.Errors
                            .GroupBy(e => e.PropertyName)
                            .ToDictionary(
                                g => g.Key,
                                g => g.Select(e => e.ErrorMessage).ToArray()
                            )
                    });
                    return;
                }

                logger.LogError(ex, "Unhandled exception at {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    title = "Server error",
                    detail = ex?.Message
                });
            });
        });
    }
}