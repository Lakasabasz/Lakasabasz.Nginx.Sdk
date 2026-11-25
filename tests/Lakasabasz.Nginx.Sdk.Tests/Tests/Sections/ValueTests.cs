namespace Lakasabasz.Nginx.Sdk.Tests.Sections;

using Server = Lakasabasz.Nginx.Sdk.Server;

public sealed class ValueTests : TestTemplate
{
    private Server _server;

    #region SetUp

    [SetUp]
    public void CreateServer() => _server = new Server();

    #endregion

    #region Helpers

    private TimeSpan? ReadTime(string text)
    {
        _server.Directives.Set(Consts.Values.KeepAliveTimeout, text);
        return _server.KeepAliveTimeout;
    }

    private bool TimeUnreadable(string text)
    {
        _server.Directives.Set(Consts.Values.KeepAliveTimeout, text);
        try
        {
            _ = _server.KeepAliveTimeout;
            return false;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    private string WrittenTime(TimeSpan time)
    {
        _server.KeepAliveTimeout = time;
        return _server.Directives.Single(d => d.Name == Consts.Values.KeepAliveTimeout).Arguments.Single();
    }

    private (LocationModifier Modifier, string Path) ReadLocation(string[] arguments)
    {
        var directive = _server.Directives.AddBlock(Consts.Values.Location, arguments);
        var location = _server.Locations.Single(l => l.Directive == directive);
        return (location.Modifier, location.Path);
    }

    private Listen ReadListen(string[] arguments)
    {
        _server.Directives.Set(Consts.Values.Listen, arguments);
        return _server.Listens.Single();
    }

    #endregion

    [Test]
    [Description("Times are read in every form nginx takes: no unit, ms, s, m, h, d, w, M, y, several parts, with spaces between them.")]
    public void ReadsTimes()
    {
        var read = Consts.Values.TimeTexts.Select(ReadTime).ToList();

        Assert.That(read, Is.EqualTo(Consts.Values.Times), "Each text should be read as its time");
    }

    [Test]
    [Description("A text that is no time (unknown unit, a unit without a number, a fraction, a time too long) cannot be read as a time.")]
    public void RejectsTimes()
    {
        var unreadable = Consts.Values.NotTimes.Select(TimeUnreadable).ToList();

        Assert.That(unreadable, Is.All.True, "Reading each text as a time should fail");
    }

    [Test]
    [Description("Times are written in the largest units first, days at most, down to milliseconds; zero is 0s.")]
    public void WritesTimes()
    {
        var written = Consts.Values.TimesToWrite.Select(WrittenTime).ToList();

        Assert.That(written, Is.EqualTo(Consts.Values.WrittenTimes), "Each time should be written in nginx's shortest form");
    }

    [Test]
    [Description("Sizes are read with or without k, m, g in either case; a sign, a fraction, an unknown unit or a size too large is refused.")]
    public void ParsesSizes()
    {
        var bytes = Consts.Values.SizeTexts.Select(text => Size.Parse(text).Bytes).ToList();
        var refused = Consts.Values.NotSizes.Select(text => !Size.TryParse(text, out _)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(bytes, Is.EqualTo(Consts.Values.SizeBytes), "Each text should be read as its number of bytes");
            Assert.That(refused, Is.All.True, "Each text that is no size should be refused");
        });
    }

    [Test]
    [Description("Sizes are written in the largest unit that holds them exactly, bytes otherwise, also for 0; a size too large to make throws.")]
    public void WritesSizes()
    {
        var written = Consts.Values.SizesToWrite.Select(size => size.ToString()).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(Consts.Values.WrittenSizes), "Each size should be written in its largest exact unit");
            Assert.Throws<OverflowException>(() => Size.FromGigabytes(long.MaxValue), "A size beyond the range of bytes should not wrap");
        });
    }

    [Test]
    [Description("A location key is read as a separate modifier and path, a modifier glued to the path (=, ^~, ~*, ~), a named location, or a lone modifier as the path.")]
    public void ReadsLocationKeys()
    {
        var read = Consts.Values.LocationArguments.Select(ReadLocation).ToList();

        Assert.That(read, Is.EqualTo(Consts.Values.LocationKeys), "Each location should give its modifier and path");
    }

    [Test]
    [Description("A listen is read with its flags in any order, the legacy default, ipv6only, unknown parameters kept, and an IPv6 address without a port.")]
    public void ReadsListenFlags()
    {
        var full = ReadListen(Consts.Values.FullListenArguments);
        var ipv6 = ReadListen(Consts.Values.Ipv6WithoutPortArguments);

        Assert.Multiple(() =>
        {
            Assert.That(full, Is.EqualTo(Consts.Values.FullListen), "The flags, ipv6only and backlog should be read");
            Assert.That(ipv6, Is.EqualTo(Consts.Values.Ipv6WithoutPort), "An IPv6 address without a port should keep its brackets and have no port");
            Assert.That((ipv6.EffectivePort, full.IsUnixSocket), Is.EqualTo((Consts.Values.DefaultPort, false)), "A listen without a port is on 80");
        });
    }

    [Test]
    [Description("A listen written back gives the address, the flags in a fixed order, ipv6only and the unknown parameters last.")]
    public void WritesListenFlags()
    {
        _server.Listens.Add(Consts.Values.FullListen);

        Assert.That(_server.Directives.Single().Arguments, Is.EqualTo(Consts.Values.WrittenFullListen), "The listen should be written in the fixed order");
    }
}
