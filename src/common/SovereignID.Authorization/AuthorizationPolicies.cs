namespace SovereignID.Authorization;

public static class AuthorizationPolicies
{
    public const string PlatformAdmin = "PlatformAdmin";
    public const string PlatformOrInstitutionMember = "PlatformOrInstitutionMember";
    public const string InstitutionAdmin = "InstitutionAdmin";
    public const string InstitutionIssuer = "InstitutionIssuer";
    public const string HolderAuthenticated = "HolderAuthenticated";
}
