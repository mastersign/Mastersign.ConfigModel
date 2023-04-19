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

        public static IEnumerable<PropertyInfo> GetModelProperties(this Type type)
            => type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0);

        public static Type GetListElementType(Type t)
        {
            var listInterface = t.FindInterfaces(
                (i, c) => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(List<>),
                null)
                .FirstOrDefault();
            return listInterface?.GetGenericArguments()[0];
        }

        public static Type GetMapValueType(Type t)
        {
            var dictionaryInterfaces = t.FindInterfaces(
                (i, c) => i.IsGenericType && 
                    i.GetGenericTypeDefinition() == typeof(IDictionary<,>) &&
                    i.GetGenericArguments()[0] == typeof(string),
                null)
                .FirstOrDefault();
            return dictionaryInterfaces?.GetGenericArguments()[1];
        }

        public static List<Type> FindAllDerivedTypes(Type baseType, Assembly assembly)
        {
            return assembly.GetTypes()
                .Where(t => t != baseType && t.IsAssignableFrom(baseType))
                .ToList();
        }

        public static List<Type> FindAllDerivedTypesInTheSameAssembly(Type baseType)
            => FindAllDerivedTypes(baseType, Assembly.GetAssembly(baseType));

        public static IEnumerable<Type> GetAssociatedModelTypes(Type t)
        {
            foreach (var propInfo in t.GetModelProperties())
            {
                var at = propInfo.PropertyType;
                var listElementType = GetListElementType(at);
                var mapValueType = GetMapValueType(at);
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
                visited.Add(at);
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
    }
}
