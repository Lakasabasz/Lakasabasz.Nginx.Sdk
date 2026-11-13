namespace Lakasabasz.Nginx.Sdk.Tests.Config;

[NginxRequired(Mode.ReadOnly)]
public sealed class CorpusTests : TestTemplate
{
    #region Helpers

    private static void AssertUnchanged(IReadOnlyList<string> files)
    {
        Assert.That(files, Is.Not.Empty, "The corpus should contain config files");
        Assert.Multiple(() =>
        {
            foreach (var file in files)
                Assert.That(Nginx.Load(file).Config.All.Single().Serialize(), Is.EqualTo(File.ReadAllText(file)),
                    $"{file} should be serialized exactly as it was loaded");
        });
    }

    #endregion

    [Test]
    [Description("Every config file of Ubuntu's nginx package (and the test preset) in /etc/nginx is serialized exactly as it was loaded.")]
    public void UbuntuUnchanged() => AssertUnchanged(NginxCorpus.Files(Consts.Paths.ConfigRoot));

    [Test]
    [Description("Every config file of the official nginx 1.28.3 image is serialized exactly as it was loaded.")]
    public void OfficialUnchanged() => AssertUnchanged(NginxCorpus.Files(Consts.Paths.OfficialCorpus));
}
