namespace Lakasabasz.Nginx.Sdk.Tests.Infrastructure;

public enum RegistryRequirement
{
    NginxBinary,
    ConfigRoot,
    WritableConfigRoot,
    PasswordlessSudo,
}

public sealed record UnmetRequirement(RegistryRequirement Requirement, string Detail);
