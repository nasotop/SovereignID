using Microsoft.AspNetCore.Authorization;

namespace SovereignID.Authorization.Handlers;

public sealed class PlatformAdminRequirement : IAuthorizationRequirement;

public sealed class PlatformAdminAuthorizationHandler : AuthorizationHandler<PlatformAdminRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PlatformAdminRequirement requirement)
    {
        if (context.User.IsPlatformAdmin())
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
