using System.Reflection;

namespace Solution.Testing.Common;

public static class AssemblyReference
{
    public static Assembly Assembly { get; } = typeof(AssemblyReference).Assembly;
}
