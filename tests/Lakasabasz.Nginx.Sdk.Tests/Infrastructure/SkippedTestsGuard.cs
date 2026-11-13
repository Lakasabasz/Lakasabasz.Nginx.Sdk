using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestHost;

namespace Lakasabasz.Nginx.Sdk.Tests.Infrastructure;

public sealed class SkippedTestsGuard : IDataConsumer
{
    private readonly object _fileLock = new();

    public static void AddExtensions(ITestApplicationBuilder builder, string[] _)
    {
        if (TestConfiguration.InTestImage)
            builder.TestHost.AddDataConsumer(_ => new SkippedTestsGuard());
    }

    public string Uid => nameof(SkippedTestsGuard);

    public string Version => Consts.Guard.ExtensionVersion;

    public string DisplayName => Consts.Guard.ExtensionName;

    public string Description => Consts.Guard.ExtensionDescription;

    public Type[] DataTypesConsumed => [typeof(TestNodeUpdateMessage)];

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public Task ConsumeAsync(IDataProducer dataProducer, IData value, CancellationToken cancellationToken)
    {
        if (value is TestNodeUpdateMessage { TestNode: var node }
            && node.Properties.SingleOrDefault<TestNodeStateProperty>() is SkippedTestNodeStateProperty skipped)
        {
            lock (_fileLock)
                File.AppendAllText(Consts.Paths.SkippedTests, Consts.Messages.SkippedTest(node.DisplayName, skipped.Explanation));
        }
        return Task.CompletedTask;
    }
}
