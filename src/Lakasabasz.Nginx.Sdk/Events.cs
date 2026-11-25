using Lakasabasz.Nginx.Sdk.Internal.Structure;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>The <c>events { }</c> block of <c>nginx.conf</c>: how worker processes handle connections.</summary>
public sealed class Events : Section
{
    internal const string Keyword = "events";

    /// <summary>A loose <c>events { }</c>, in no file until it is set as <see cref="NginxFile.Events"/>.</summary>
    public Events()
        : base(Keyword)
    {
    }

    internal Events(StructureDirective node)
        : base(node)
    {
    }
}
