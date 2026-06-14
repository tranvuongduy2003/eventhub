using System.Reflection;

namespace Solution.ServiceDefaults;

public static class AssemblyReference
{
    public static Assembly Assembly { get; } = typeof(AssemblyReference).Assembly;
}
