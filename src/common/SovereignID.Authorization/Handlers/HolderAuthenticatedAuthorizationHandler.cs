using Microsoft.AspNetCore.Authorization;

namespace SovereignID.Authorization.Handlers;

public sealed class HolderAuthenticatedRequirement : IAuthorizationRequirement;

public sealed class HolderAuthenticatedAuthorizationHandler : AuthorizationHandler<HolderAuthenticatedRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HolderAuthenticatedRequirement requirement)
    {
        if (context.User.IsHolder()
            || !string.IsNullOrWhiteSpace(context.User.FindFirst("did")?.Value))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
