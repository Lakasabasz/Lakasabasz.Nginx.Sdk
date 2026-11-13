namespace Lakasabasz.Nginx.Sdk.Tests.Config;

[NginxRequired(Mode.ReadOnly)]
public sealed class ParseOracleTests : TestTemplate
{
    #region Helpers

    private static Task<NginxProcessResult> TestWithNginxAsync(string config) => NginxProcess.RunAsync(Consts.Paths.Sudo,
        Consts.Arguments.NoPrompt, Consts.Paths.Binary, Consts.NginxOutput.TestOption, Consts.NginxOutput.ConfigOption, config);

    #endregion

    [Test]
    [Description("A config with arguments in every form passes nginx -t and loads without errors.")]
    public async Task AcceptsWhatNginxAccepts()
    {
        var path = Arrange.ArgumentsConfig();

        var nginxTest = await TestWithNginxAsync(path);

        Assert.Multiple(() =>
        {
            Assert.That(nginxTest.StandardError, Does.Contain(Consts.NginxOutput.TestSuccessful), "nginx -t should accept the config");
            Assert.That(() => Nginx.Load(path), Throws.Nothing, "The package should load the config nginx accepts");
        });
    }

    [Test]
    [Description("For every broken fixture the package reports the same syntax error, message and line, as nginx -t.")]
    public async Task ErrorsMatchNginx()
    {
        var reports = new List<(NginxParseError Error, string Nginx)>();
        foreach (var path in Arrange.BrokenConfigs())
        {
            var error = Assert.Throws<NginxParseException>(() => Nginx.Load(path), $"Loading {path} should fail").Errors.Single();
            reports.Add((error, (await TestWithNginxAsync(path)).StandardError));
        }

        Assert.Multiple(() =>
        {
            foreach (var (error, nginx) in reports)
                Assert.That(nginx, Does.Contain(Consts.ParseErrors.NginxReport(error)),
                    $"nginx -t should report the same error as the package for {error.File}");
        });
    }
}
