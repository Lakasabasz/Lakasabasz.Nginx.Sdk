using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestHost;
using Microsoft.Testing.Platform.Services;
using NUnit.Framework.Internal;

namespace Lakasabasz.Nginx.Sdk.Tests.Infrastructure.Reporting;

public sealed class BuddyReportingExtension : IDataConsumer, ITestSessionLifetimeHandler, IAsyncDisposable
{
    private readonly BuddyTestSink _sink;
    private readonly string _testGroupName;

    private BuddyReportingExtension(BuddyTestSink sink, string testGroupName)
    {
        _sink = sink;
        _testGroupName = testGroupName;
    }

    public static void AddExtensions(ITestApplicationBuilder builder, string[] _)
    {
        if (TestConfiguration.BuddyUnitTestsToken is not { } token)
            return;

        var factory = new CompositeExtensionFactory<BuddyReportingExtension>(() => new BuddyReportingExtension(
            new BuddyTestSink(new BuddyUnitTestsClient(token), BuddyTestSessionInfo.From(TestConfiguration.BuddyRun)),
            TestGroupName(Assembly.GetEntryAssembly())));
        builder.TestHost.AddDataConsumer(factory);
        builder.TestHost.AddTestSessionLifetimeHandler(factory);
    }

    public string Uid => nameof(BuddyReportingExtension);

    public string Version => Consts.Buddy.ExtensionVersion;

    public string DisplayName => Consts.Buddy.ExtensionName;

    public string Description => Consts.Buddy.ExtensionDescription;

    public Type[] DataTypesConsumed => [typeof(TestNodeUpdateMessage)];

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public Task OnTestSessionStartingAsync(ITestSessionContext testSessionContext) =>
        _sink.OpenAsync(testSessionContext.CancellationToken);

    public Task OnTestSessionFinishingAsync(ITestSessionContext testSessionContext) =>
        _sink.CloseAsync(CancellationToken.None);

    public Task ConsumeAsync(IDataProducer dataProducer, IData value, CancellationToken cancellationToken)
    {
        if (value is TestNodeUpdateMessage { TestNode: var node } && ToBuddyCase(node, _testGroupName) is { } testCase)
            _sink.Add(testCase);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => _sink.DisposeAsync();

    public static BuddyTestCase? ToBuddyCase(TestNode node, string testGroupName)
    {
        if (node.Properties.SingleOrDefault<TestMethodIdentifierProperty>() is not { } method)
            return null;

        var (status, result) = node.Properties.SingleOrDefault<TestNodeStateProperty>() switch
        {
            PassedTestNodeStateProperty => (BuddyTestStatus.PASSED, (string?)null),
            SkippedTestNodeStateProperty skipped => (BuddyTestStatus.SKIPPED, skipped.Explanation),
            FailedTestNodeStateProperty failed => (BuddyTestStatus.FAILED, Describe(failed.Exception, failed.Explanation)),
            ErrorTestNodeStateProperty error => (BuddyTestStatus.ERROR, Describe(error.Exception, error.Explanation)),
            TimeoutTestNodeStateProperty timeout => (BuddyTestStatus.ERROR, Describe(timeout.Exception, timeout.Explanation ?? Consts.Buddy.TimeoutResult)),
            _ => ((BuddyTestStatus?)null, (string?)null),
        };
        if (status is null)
            return null;

        var classname = string.IsNullOrEmpty(method.Namespace) ? method.TypeName : Consts.Buddy.Classname(method.Namespace, method.TypeName);
        if (status == BuddyTestStatus.SKIPPED && TestConfiguration.InTestImage)
            status = BuddyTestStatus.ERROR;

        var duration = node.Properties.SingleOrDefault<TimingProperty>()?.GlobalTiming.Duration ?? TimeSpan.Zero;
        var data = string.Join(Consts.Formats.Paragraph, new[] { DescriptionOf(method, classname), result }.Where(part => !string.IsNullOrWhiteSpace(part)));
        return new BuddyTestCase(node.DisplayName, testGroupName, classname, status.Value, duration.TotalSeconds,
            data.Length == 0 ? null : data);
    }

    public static string? DescriptionOf(TestMethodIdentifierProperty method, string classname) => AppDomain.CurrentDomain
        .GetAssemblies()
        .FirstOrDefault(assembly => assembly.FullName == method.AssemblyFullName || assembly.GetName().Name == method.AssemblyFullName)
        ?.GetType(classname)
        ?.GetMethod(method.MethodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
        ?.GetCustomAttribute<DescriptionAttribute>()
        ?.Properties.Get(PropertyNames.Description) as string;

    public static string TestGroupName(Assembly? assembly)
    {
        var name = assembly?.GetName().Name ?? Consts.Buddy.DefaultTestGroup;
        var framework = assembly?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName is { } targetFramework
            ? new FrameworkName(targetFramework)
            : null;
        return framework is null ? name : Consts.Buddy.TestGroup(name, framework.Version);
    }

    private static string? Describe(Exception? exception, string? explanation) => exception switch
    {
        null => explanation,
        _ when string.IsNullOrEmpty(explanation) || exception.Message.Contains(explanation, StringComparison.Ordinal)
            => Consts.Buddy.Failure(exception.Message, exception.StackTrace),
        _ => Consts.Buddy.Failure(explanation, exception.Message, exception.StackTrace),
    };
}
