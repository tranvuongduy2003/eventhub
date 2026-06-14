using System.Reflection;

namespace Solution.Api.IntegrationTests;

public static class AssemblyReference
{
    public static Assembly Assembly { get; } = typeof(AssemblyReference).Assembly;
}
