using System.Reflection;

namespace Solution.AppHost;

public static class AssemblyReference
{
    public static Assembly Assembly { get; } = typeof(AssemblyReference).Assembly;
}
