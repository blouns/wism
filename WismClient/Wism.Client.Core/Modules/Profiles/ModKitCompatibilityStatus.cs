namespace Wism.Client.Modules.Profiles
{
    public enum ModKitCompatibilityStatus
    {
        Verified,
        Invalid,
        Legacy,
        MissingDependency,
        Conflict,
        UnsupportedVersion,
        FingerprintMismatch
    }
}
