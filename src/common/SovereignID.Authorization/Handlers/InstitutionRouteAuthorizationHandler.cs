using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace SovereignID.Authorization.Handlers;

public sealed class InstitutionRouteRequirement : IAuthorizationRequirement
{
    public InstitutionRouteRequirement(params string[] allowedRoles)
    {
        AllowedRoles = allowedRoles
            .Select(role => role.Trim().ToLowerInvariant())
            .ToArray();
    }

    public string[] AllowedRoles { get; }

    public bool AllowPlatformAdmin { get; init; } = true;
}

public sealed class InstitutionRouteAuthorizationHandler : AuthorizationHandler<InstitutionRouteRequirement>
{
  private readonly IHttpContextAccessor _httpContextAccessor;

  public InstitutionRouteAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
  {
    _httpContextAccessor = httpContextAccessor;
  }

  protected override Task HandleRequirementAsync(
      AuthorizationHandlerContext context,
      InstitutionRouteRequirement requirement)
  {
    var principal = context.User;
    if (requirement.AllowPlatformAdmin && principal.IsPlatformAdmin())
    {
      context.Succeed(requirement);
      return Task.CompletedTask;
    }

    var institutionId = ResolveInstitutionId(context);

    if (institutionId is null)
    {
      if (principal.HasAnyInstitutionRole(requirement.AllowedRoles))
      {
        context.Succeed(requirement);
      }

      return Task.CompletedTask;
    }

    if (principal.HasInstitutionRole(institutionId.Value, requirement.AllowedRoles))
    {
      context.Succeed(requirement);
    }

    return Task.CompletedTask;
  }

  private Guid? ResolveInstitutionId(AuthorizationHandlerContext context)
  {
    if (context.Resource is HttpContext httpContext)
    {
      return TryGetRouteInstitutionId(httpContext);
    }

    if (_httpContextAccessor.HttpContext is { } accessorContext)
    {
      return TryGetRouteInstitutionId(accessorContext);
    }

    return null;
  }

  private static Guid? TryGetRouteInstitutionId(HttpContext httpContext)
  {
    if (httpContext.Request.RouteValues.TryGetValue("institutionId", out var value)
        && Guid.TryParse(value?.ToString(), out var institutionId))
    {
      return institutionId;
    }

    return null;
  }
}
