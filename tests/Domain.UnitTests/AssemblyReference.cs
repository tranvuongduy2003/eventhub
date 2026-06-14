using System.Reflection;

namespace Solution.Domain.UnitTests;

public static class AssemblyReference
{
    public static Assembly Assembly { get; } = typeof(AssemblyReference).Assembly;
}
