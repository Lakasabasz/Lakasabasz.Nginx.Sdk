using System.Reflection;
using NUnit.Framework.Internal;

namespace Lakasabasz.Nginx.Sdk.Tests.Infrastructure;

[CancelAfter(TimeLimitMilliseconds)]
public abstract class TestTemplate
{
    private const int TimeLimitMilliseconds = 5 * 60 * 1000;

    private readonly Lazy<TestArrange> _arrange;
    private Lazy<string> _workDirectory;
    private NginxAccess? _nginx;

    protected TestTemplate()
    {
        _arrange = new Lazy<TestArrange>(() => new TestArrange(Consts.Paths.ConfigRoot, () => WorkDirectory));
        _workDirectory = NewWorkDirectory();
    }

    protected static TimeSpan TimeLimit => TimeSpan.FromMilliseconds(TimeLimitMilliseconds);

    protected static CancellationToken Cancellation => TestContext.CurrentContext.CancellationToken;

    protected TestArrange Arrange => _arrange.Value;

    protected string WorkDirectory => _workDirectory.Value;

    [OneTimeSetUp]
    public async Task AcquireNginxAsync()
    {
        if (GetType().GetCustomAttribute<NginxRequiredAttribute>() is not { } required)
            return;
        if (!TestConfiguration.InTestImage)
            Assert.Ignore(Consts.Messages.NeedsTestImage);
        Assume.That(await NginxRegistry.CheckRequirementsAsync(), Is.Empty, Consts.Messages.NginxNotUsable);

        _nginx = await NginxRegistry.AcquireAsync(required.Mode, GetType().FullName!);
    }

    [TearDown]
    public async Task Cleanup()
    {
        if (_nginx is { Mode: Mode.Modify })
            await NginxRegistry.RestoreAsync();

        if (_workDirectory.IsValueCreated && Directory.Exists(_workDirectory.Value))
            Directory.Delete(_workDirectory.Value, recursive: true);
        _workDirectory = NewWorkDirectory();

        var elapsed = TimeSpan.FromSeconds(TestExecutionContext.CurrentContext.Duration);
        if (elapsed > TimeLimit)
            Assert.Fail(Consts.Messages.TimeLimitExceeded(elapsed, TimeLimit));
    }

    [OneTimeTearDown]
    public void ReleaseNginx()
    {
        _nginx?.Dispose();
        _nginx = null;
    }

    private Lazy<string> NewWorkDirectory() => new(() =>
        Directory.CreateDirectory(Path.Combine(Consts.Paths.WorkRoot, GetType().Name, Guid.NewGuid().ToString(Consts.Formats.CompactGuid))).FullName);
}
