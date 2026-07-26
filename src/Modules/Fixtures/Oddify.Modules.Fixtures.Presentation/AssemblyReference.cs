using System.Reflection;

namespace Oddify.Modules.Fixtures.Presentation;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
