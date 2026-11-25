namespace Lakasabasz.Nginx.Sdk.Tests.Infrastructure;

public static class Fixture
{
    public static string PathOf(string relativePath) => Path.Combine(AppContext.BaseDirectory, Consts.Fixtures.Root, relativePath);

    public static string Read(string relativePath) => File.ReadAllText(PathOf(relativePath));
}

public static class Expected
{
    public static string SiteAdded => Fixture.Read(Consts.Fixtures.SiteAdded);

    public static string SiteSet => Fixture.Read(Consts.Fixtures.SiteSet);

    public static string SiteSetDuplicates => Fixture.Read(Consts.Fixtures.SiteSetDuplicates);

    public static string SiteRemoved => Fixture.Read(Consts.Fixtures.SiteRemoved);

    public static string SiteBlock => Fixture.Read(Consts.Fixtures.SiteBlock);

    public static string SiteQuoted => Fixture.Read(Consts.Fixtures.SiteQuoted);

    public static string SiteTopLevel => Fixture.Read(Consts.Fixtures.SiteTopLevel);

    public static string InlineAdded => Fixture.Read(Consts.Fixtures.InlineAdded);

    public static string CrlfAdded => Fixture.Read(Consts.Fixtures.CrlfAdded);

    public static string Properties => Fixture.Read(Consts.Fixtures.Properties);

    public static string PropertiesRemoved => Fixture.Read(Consts.Fixtures.PropertiesRemoved);

    public static string NamesChanged => Fixture.Read(Consts.Fixtures.NamesChanged);

    public static string ListenReplaced => Fixture.Read(Consts.Fixtures.ListenReplaced);

    public static string CustomKept => Fixture.Read(Consts.Fixtures.CustomKept);

    public static string MixedChanged => Fixture.Read(Consts.Fixtures.MixedChanged);

    public static string MixedRelaid => Fixture.Read(Consts.Fixtures.MixedRelaid);

    public static string MixedRestyled => Fixture.Read(Consts.Fixtures.MixedRestyled);

    public static string MixedOneLineAdded => Fixture.Read(Consts.Fixtures.MixedOneLineAdded);


    public static string AttachAdded => Fixture.Read(Consts.Fixtures.AttachAdded);

    public static string AttachChanged => Fixture.Read(Consts.Fixtures.AttachChanged);

    public static string TextServer => Fixture.Read(Consts.Fixtures.TextServer);

    public static string TextLocationAndUpstream => Fixture.Read(Consts.Fixtures.TextLocationAndUpstream);

    public static string Switched => Fixture.Read(Consts.Fixtures.Switched);

    public static string IndexExtended => Fixture.Read(Consts.Fixtures.IndexExtended);

    public static string PropertiesRenamed => Fixture.Read(Consts.Fixtures.PropertiesRenamed);

    public static string MainAdded => Fixture.Read(Consts.Fixtures.MainAdded);

    public static string MixedLooseAdded => Fixture.Read(Consts.Fixtures.MixedLooseAdded);

    public static string Spaced => Fixture.Read(Consts.Fixtures.Spaced);

    public static string CommentsChanged => Fixture.Read(Consts.Fixtures.CommentsChanged);

    public static string CommentsMoved => Fixture.Read(Consts.Fixtures.CommentsMoved);

    public static string CommentsUnfolded => Fixture.Read(Consts.Fixtures.CommentsUnfolded);
}
