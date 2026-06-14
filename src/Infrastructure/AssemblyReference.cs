using System.Reflection;

namespace Solution.Infrastructure;

public static class AssemblyReference
{
    public static Assembly Assembly { get; } = typeof(AssemblyReference).Assembly;
}
