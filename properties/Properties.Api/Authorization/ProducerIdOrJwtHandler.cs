using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Properties.Api.Authorization;

public class ProducerIdOrJwtHandler : AuthorizationHandler<ProducerIdOrJwtRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProducerIdOrJwtRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var httpContext = context.Resource as DefaultHttpContext;
        var hasProducerId = httpContext?.HttpContext.Request.Headers["X-Producer-Id"].FirstOrDefault() is { Length: > 0 };
        if (hasProducerId)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
