namespace InnovaFlow.Projects.Data;

// ---------------------------------------------------------------------------
// Effective role resolution - the rule that must live in exactly one place
// ---------------------------------------------------------------------------

public static class AccessRules
{
    /// <summary>
    /// Permissions are additive. Adding a user directly to an idea never
    /// removes rights their team already granted.
    /// </summary>
    public static MemberRole? EffectiveRole(MemberRole? direct, MemberRole? viaTeam) =>
        (direct, viaTeam) switch
        {
            (null, null) => null,
            (var d, null) => d,
            (null, var t) => t,
            var (d, t)    => (MemberRole)Math.Max((int)d!, (int)t!)
        };

    public static bool CanView(MemberRole? role)      => role is not null;
    public static bool CanEdit(MemberRole? role)      => role >= MemberRole.Editor;
    public static bool CanStartJob(MemberRole? role)  => role >= MemberRole.Editor;
    public static bool CanDeleteFile(MemberRole? role)=> role >= MemberRole.Editor;
    public static bool CanManageMembers(MemberRole? r)=> r == MemberRole.Owner;
    public static bool CanDeleteIdea(MemberRole? role)=> role == MemberRole.Owner;
}
