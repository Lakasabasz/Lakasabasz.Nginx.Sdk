namespace Lakasabasz.Nginx.Sdk.Tests.Infrastructure;

public static class TestConfiguration
{
    public static bool InTestImage => Read(Consts.Variables.TestImage) == Consts.Variables.TestImageValue;

    public static string? BuddyUnitTestsToken => Read(Consts.Variables.BuddyUnitTestsToken);

    public static Uri? BuddyUnitTestsServiceUrl => Read(Consts.Variables.BuddyUnitTestsServiceUrl) is { } url ? new Uri(url) : null;

    public static BuddyRun? BuddyRun => Read(Consts.Variables.Buddy) != Consts.Variables.BuddyValue
        ? null
        : new BuddyRun(
            Branch: Read(Consts.Variables.BuddyRunBranch),
            Tag: Read(Consts.Variables.BuddyRunTag),
            RefType: Read(Consts.Variables.BuddyRunRefType),
            RefName: Read(Consts.Variables.BuddyRunRefName),
            Commit: Read(Consts.Variables.BuddyRunCommit),
            ExecutionId: Read(Consts.Variables.BuddyRunHash),
            ActionExecutionId: Read(Consts.Variables.BuddyActionRunHash));

    private static string? Read(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : null;
}

public sealed record BuddyRun(
    string? Branch,
    string? Tag,
    string? RefType,
    string? RefName,
    string? Commit,
    string? ExecutionId,
    string? ActionExecutionId);
