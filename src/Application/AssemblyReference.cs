using System.Reflection;

namespace Solution.Application;

public static class AssemblyReference
{
    public static Assembly Assembly { get; } = typeof(AssemblyReference).Assembly;
}
