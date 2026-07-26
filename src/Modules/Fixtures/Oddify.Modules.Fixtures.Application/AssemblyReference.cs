using System.Reflection;

namespace Oddify.Modules.Fixtures.Application;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
