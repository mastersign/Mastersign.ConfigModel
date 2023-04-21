using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mastersign.ConfigModel
{
    internal static class ReflectionHelper
    {
        public static bool HasCustomAttribute<T>(this MemberInfo type, bool inherit = false)
            where T : Attribute
            => type.GetCustomAttribute<T>(inherit) != null;

        public static bool IsAtomic(Type t)
            => t.IsPrimitive || t.IsEnum || t == typeof(string) 
                || t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);

        public static IEnumerable<PropertyInfo> GetModelProperties(this Type type)
            => type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0);

        public static Type GetListElementType(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>))
                return t.GetGenericArguments()[0];
            return t
                .FindInterfaces((i, c) => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>), null)
                .FirstOrDefault()
                ?.GetGenericArguments()[0];
        }

        public static Type GetDictionaryValueType(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                return t.GetGenericArguments()[1];
            return t
                .FindInterfaces(
                    (i, c) => i.IsGenericType && 
                        i.GetGenericTypeDefinition() == typeof(IDictionary<,>) &&
                        i.GetGenericArguments()[0] == typeof(string),
                    null)
                .FirstOrDefault()
                ?.GetGenericArguments()[1];
        }

        public static List<Type> FindAllDerivedTypes(Type baseType, Assembly assembly, bool returnAbstractClasses = false)
        {
            return assembly.GetTypes()
                .Where(t => t != baseType && baseType.IsAssignableFrom(t)
                    && (returnAbstractClasses || !t.IsAbstract))
                .ToList();
        }

        public static List<Type> FindAllDerivedTypesInTheSameAssembly(Type baseType, bool returnAbstractClasses = false)
            => FindAllDerivedTypes(baseType, Assembly.GetAssembly(baseType), returnAbstractClasses);

        public static IEnumerable<Type> GetAssociatedModelTypes(Type t)
        {
            foreach (var propInfo in t.GetModelProperties())
            {
                var at = propInfo.PropertyType;
                var listElementType = GetListElementType(at);
                var mapValueType = GetDictionaryValueType(at);
                if (listElementType != null) yield return listElementType;
                else if (mapValueType != null) yield return mapValueType;
                else yield return at;
            }
        }

        public static IEnumerable<Type> TraverseModelTypes(Type t, HashSet<Type> visited = null)
        {
            visited = visited ?? new HashSet<Type>();
            if (visited.Contains(t)) yield break;
            visited.Add(t);
            yield return t;
            foreach (var at in GetAssociatedModelTypes(t))
            {
                if (visited.Contains(at)) continue;
                foreach (var at2 in TraverseModelTypes(at, visited))
                {
                    yield return at2;
                }
            }
        }

        public static bool IsMergableByInterface(this Type type)
            => typeof(IMergableConfigModel).IsAssignableFrom(type);

        public static bool IsMergableByAttribute(this Type type)
            => type.HasCustomAttribute<MergableConfigModelAttribute>();

        public static bool IsMergable(this Type type) 
            => type.IsMergableByInterface() || type.IsMergableByAttribute();

        public static Dictionary<Type, Dictionary<string, Type>> GetTypeDiscriminationsByPropertyExistence(Type modelType)
        {
            var result = new Dictionary<Type, Dictionary<string, Type>>();
            foreach (var mt in TraverseModelTypes(modelType))
            {
                foreach (var st in FindAllDerivedTypesInTheSameAssembly(mt))
                {
                    foreach (var p in st.GetModelProperties())
                    {
                        if (p.HasCustomAttribute<TypeIndicatorAttribute>())
                        {
                            if (!result.TryGetValue(mt, out var uniqueKeys))
                            {
                                uniqueKeys = new Dictionary<string, Type>();
                                result.Add(mt, uniqueKeys);
                            }
                            uniqueKeys.Add(p.Name, st);
                            break;
                        }
                    }
                }
            }

            return result;
        }

        public static Dictionary<Type, Tuple<string, Dictionary<string, Type>>> GetTypeDiscriminationsByPropertyValue(Type modelType)
        {
            var result = new Dictionary<Type, Tuple<string, Dictionary<string, Type>>>();
            foreach (var mt in TraverseModelTypes(modelType))
            {
                foreach (var p in mt.GetModelProperties())
                {
                    if (p.HasCustomAttribute<TypeDiscriminatorAttribute>())
                    {
                        if (!result.TryGetValue(mt, out var valueIndicator))
                        {
                            valueIndicator = Tuple.Create(p.Name, new Dictionary<string, Type>());
                            result.Add(mt, valueIndicator);
                        }
                        foreach (var st in FindAllDerivedTypesInTheSameAssembly(mt))
                        {
                            var discriminationValue = st.GetCustomAttribute<TypeDiscriminationValueAttribute>()?.Value;
                            if (!string.IsNullOrWhiteSpace(discriminationValue))
                            {
                                valueIndicator.Item2.Add(discriminationValue, st);
                            }
                        }
                        break;
                    }
                }
            }
            return result;
        }
    }
}
