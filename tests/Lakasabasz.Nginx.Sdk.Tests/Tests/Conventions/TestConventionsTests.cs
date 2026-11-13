using System.Reflection;

namespace Lakasabasz.Nginx.Sdk.Tests.Conventions;

public sealed class TestConventionsTests : TestTemplate
{
    private static readonly Type[] Types = typeof(TestConventionsTests).Assembly.GetTypes();

    #region Helpers

    private static bool HasTests(Type type) => type
        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Any(IsTest);

    private static bool IsTest(MethodInfo method) =>
        method.IsDefined(typeof(TestAttribute)) || method.IsDefined(typeof(TestCaseAttribute)) || method.IsDefined(typeof(TestCaseSourceAttribute));

    #endregion

    [Test]
    [Description("Every test class derives from TestTemplate directly or through the _template of its own folder.")]
    public void AllUseTemplate()
    {
        var wrong = Types.Where(HasTests).Where(type =>
            type.BaseType != typeof(TestTemplate)
            && !(type.BaseType is { Name: Consts.Formats.Template } template && template.Namespace == type.Namespace));

        Assert.That(wrong.Select(Consts.Messages.TypeWithBase), Is.Empty, "Test classes that derive neither from TestTemplate nor from the _template of their folder");
    }

    [Test]
    [Description("Every _template is abstract, derives directly from TestTemplate and contains no tests.")]
    public void TemplatesAreAbstract()
    {
        var templates = Types.Where(type => type.Name == Consts.Formats.Template);

        Assert.That(templates.Where(type => !type.IsAbstract || type.BaseType != typeof(TestTemplate) || HasTests(type))
            .Select(type => type.FullName), Is.Empty, "_template classes that are not abstract, do not derive directly from TestTemplate or contain tests");
    }

    [Test]
    [Description("Every test has a non-empty [Description] saying what it checks; it is reported to Buddy with the result.")]
    public void AllHaveDescription()
    {
        var undescribed = Types.Where(HasTests)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(IsTest)
                .Where(method => string.IsNullOrWhiteSpace(
                    method.GetCustomAttribute<DescriptionAttribute>()?.Properties.Get(Consts.Formats.Description) as string))
                .Select(method => Consts.Messages.Method(type, method.Name)));

        Assert.That(undescribed, Is.Empty, "Tests without a [Description]");
    }
}
