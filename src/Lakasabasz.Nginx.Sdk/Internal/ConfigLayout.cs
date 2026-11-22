namespace Lakasabasz.Nginx.Sdk.Internal;

/// <summary>The layout of nginx's config directory on Debian and Ubuntu, relative to it.</summary>
internal static class ConfigLayout
{
    public static readonly NginxPath MainFile = "nginx.conf";
    public static readonly NginxPath SitesAvailable = "sites-available";
    public static readonly NginxPath SitesEnabled = "sites-enabled";
    public static readonly NginxPath ConfigDirectory = "conf.d";
    public static readonly NginxPath Snippets = "snippets";
}
