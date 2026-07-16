using System.Reflection;
using System.Text;

namespace RCommon.ApiReferenceGenerator;

/// <summary>
/// Computes the C# compiler's XML-doc-comment member ID (e.g. <c>T:Namespace.Type</c>,
/// <c>M:Namespace.Type.Member(ParamType)</c>) for a reflected type or member, so it can be looked up
/// in the corresponding .xml doc-comment file. Covers the common cases in this codebase (generic
/// types/methods, nested types, constructors) -- not a fully exhaustive implementation of the ECMA-334
/// Annex D ID format (e.g. pointer types and some exotic array-of-generic combinations are
/// approximate), which is acceptable for this tool's functional-spot-check testing bar.
/// </summary>
internal static class MemberIdBuilder
{
    public static string ForType(Type type) => "T:" + TypeIdName(type, forDeclaration: true);

    public static string ForMember(MemberInfo member)
    {
        return member switch
        {
            Type t => ForType(t),
            MethodInfo m => "M:" + MethodOrConstructorId(m.DeclaringType!, m.Name, m.GetParameters(), m.IsGenericMethod ? m.GetGenericArguments().Length : 0),
            ConstructorInfo c => "M:" + MethodOrConstructorId(c.DeclaringType!, "#ctor", c.GetParameters(), 0),
            PropertyInfo p => "P:" + DeclaringPrefix(p.DeclaringType!) + p.Name,
            FieldInfo f => "F:" + DeclaringPrefix(f.DeclaringType!) + f.Name,
            EventInfo e => "E:" + DeclaringPrefix(e.DeclaringType!) + e.Name,
            _ => "?:" + member.Name,
        };
    }

    private static string DeclaringPrefix(Type declaringType) => TypeIdName(declaringType, forDeclaration: false) + ".";

    private static string MethodOrConstructorId(Type declaringType, string name, ParameterInfo[] parameters, int genericArity)
    {
        var sb = new StringBuilder();
        sb.Append(TypeIdName(declaringType, forDeclaration: false)).Append('.').Append(name);
        if (genericArity > 0)
        {
            sb.Append("``").Append(genericArity);
        }
        if (parameters.Length > 0)
        {
            sb.Append('(');
            for (var i = 0; i < parameters.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(ParameterTypeIdName(parameters[i].ParameterType));
            }
            sb.Append(')');
        }
        return sb.ToString();
    }

    /// <summary>
    /// Renders a type name for use inside a member ID. <paramref name="forDeclaration"/> selects
    /// between the "T:" form (generic arity suffix, e.g. `1) and the "containing type" form used as a
    /// prefix for members (also arity-suffixed) -- both forms are the same in this simplified builder.
    /// </summary>
    private static string TypeIdName(Type type, bool forDeclaration)
    {
        if (type.IsGenericParameter)
        {
            // Method type parameters use ``N; type type parameters use `N. Reflection alone can't
            // always distinguish these outside a member context, so this path is only reached for
            // top-level generic parameters, which do not occur for TypeIdName's callers here.
            return "`" + type.GenericParameterPosition;
        }

        var declaringChain = new List<string>();
        var current = type;
        while (current != null)
        {
            declaringChain.Insert(0, SimpleNameWithArity(current));
            current = current.DeclaringType;
        }

        var name = string.IsNullOrEmpty(type.Namespace)
            ? string.Join('.', declaringChain)
            : type.Namespace + "." + string.Join('.', declaringChain);

        return name;
    }

    private static string SimpleNameWithArity(Type type)
    {
        var name = type.Name;
        var tickIndex = name.IndexOf('`');
        return tickIndex >= 0 ? name[..tickIndex] + "`" + (type.GetGenericArguments().Length) : name;
    }

    private static string ParameterTypeIdName(Type type)
    {
        if (type.IsByRef)
        {
            return ParameterTypeIdName(type.GetElementType()!) + "@";
        }
        if (type.IsArray)
        {
            var rank = type.GetArrayRank();
            var suffix = rank == 1 ? "[]" : "[" + string.Join(",", Enumerable.Repeat("0:", rank)) + "]";
            return ParameterTypeIdName(type.GetElementType()!) + suffix;
        }
        if (type.IsGenericParameter)
        {
            // Distinguish type-level (`N) vs method-level (``N) generic parameters by their declaring
            // member kind.
            var prefix = type.DeclaringMethod != null ? "``" : "`";
            return prefix + type.GenericParameterPosition;
        }
        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var baseName = TypeIdName(definition, forDeclaration: false);
            var args = string.Join(",", type.GetGenericArguments().Select(ParameterTypeIdName));
            return baseName + "{" + args + "}";
        }
        return TypeIdName(type, forDeclaration: false);
    }
}
