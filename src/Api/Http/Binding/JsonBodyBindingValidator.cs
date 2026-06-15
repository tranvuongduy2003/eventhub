using System.Reflection;

namespace EventHub.Api.Http.Binding;

internal static class JsonBodyBindingValidator
{
    public static bool IsIncomplete<TBody>(TBody? body)
        where TBody : class
    {
        if (body is null)
        {
            return true;
        }

        foreach (var property in typeof(TBody).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (IsOptional(property))
            {
                continue;
            }

            if (IsNullReferenceTypeValue(property, body))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsOptional(PropertyInfo property) =>
        property.GetCustomAttribute<BindOptionalAttribute>() is not null
        || Nullable.GetUnderlyingType(property.PropertyType) is not null
        || !property.PropertyType.IsValueType && IsNullableReferenceType(property);

    private static bool IsNullableReferenceType(PropertyInfo property)
    {
        var nullability = new NullabilityInfoContext().Create(property);
        return nullability.ReadState == NullabilityState.Nullable;
    }

    private static bool IsNullReferenceTypeValue(PropertyInfo property, object body)
    {
        if (property.PropertyType.IsValueType)
        {
            return false;
        }

        return property.GetValue(body) is null;
    }
}

[AttributeUsage(AttributeTargets.Property)]
internal sealed class BindOptionalAttribute : Attribute;
