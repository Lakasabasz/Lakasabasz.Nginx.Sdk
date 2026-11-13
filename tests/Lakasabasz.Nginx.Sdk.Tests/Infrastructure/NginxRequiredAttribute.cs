namespace Lakasabasz.Nginx.Sdk.Tests.Infrastructure;

public enum Mode
{
    ReadOnly,

    Modify,
}

[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class NginxRequiredAttribute(Mode mode) : Attribute
{
    public Mode Mode { get; } = mode;
}
