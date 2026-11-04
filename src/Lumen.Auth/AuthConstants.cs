namespace Lumen.Auth;

public static class LumenRoles
{
    public const string Learner = "learner";
    public const string Admin = "admin";
    public const string Scim = "scim";
}

public static class LumenPolicies
{
    public const string OrgAdmin = "OrgAdmin";
    public const string Scim = "Scim";
}

public static class LumenClaims
{
    public const string Subject = "sub";
    public const string Organisation = "org";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";
}

public static class LumenSchemes
{
    public const string InternalKey = "InternalKey";
}
