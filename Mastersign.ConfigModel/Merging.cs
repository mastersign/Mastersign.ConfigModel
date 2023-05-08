using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Mastersign.ConfigModel.ReflectionHelper;

namespace Mastersign.ConfigModel
{
    internal static class Merging
    {
        private static void MergeListGeneric<T>(IList<T> target, IList<T> source, ListMergeMode mode, bool forceDeepMerge)
        {
            switch (mode)
            {
                case ListMergeMode.Clear:
                    target.Clear();
                    if (source == null) break;
                    foreach (var x in source) target.Add(x);
                    break;
                case ListMergeMode.Append:
                    if (source == null) break;
                    foreach (var x in source) target.Add(x);
                    break;
                case ListMergeMode.Prepend:
                    if (source == null) break;
                    // inefficient, performance can definitely be improved
                    foreach (var x in source.Reverse()) target.Insert(0, x);
                    break;
                case ListMergeMode.AppendDistinct:
                    if (source == null) break;
                    foreach (var x in source)
                    {
                        if (!target.Contains(x)) target.Add(x);
                    }
                    break;
                case ListMergeMode.PrependDistinct:
                    if (source == null) break;
                    // inefficient, performance can definitely be improved
                    foreach (var x in source.Reverse())
                    {
                        if (!target.Contains(x)) target.Insert(0, x);
                    }
                    break;
                case ListMergeMode.ReplaceItem:
                    if (source == null) break;
                    for (var i = 0; i < Math.Min(source.Count, target.Count); i++)
                    {
                        target[i] = source[i];
                    }
                    if (source.Count > target.Count)
                    {
                        for (var i = target.Count; i < source.Count; i++)
                        {
                            target.Add(source[i]);
                        }
                    }
                    break;
                case ListMergeMode.MergeItem:
                    if (source == null) break;
                    for (var i = 0; i < Math.Min(source.Count, target.Count); i++)
                    {
                        var tv = target[i];
                        MergeObject(tv, source[i], forceRootMerge: true, forceDeepMerge);
                        if (typeof(T).IsValueType) target[i] = tv;
                    }
                    if (source.Count > target.Count)
                    {
                        for (var i = target.Count; i < source.Count; i++)
                        {
                            target.Add(source[i]);
                        }
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public static void MergeList(object target, object source, Type itemType, ListMergeMode mode, bool forceDeepMerge = false)
        {
            var mergeMethodInfo = typeof(Merging).GetMethod(nameof(MergeListGeneric), BindingFlags.NonPublic | BindingFlags.Static);
            var genericMergeMethodInfo = mergeMethodInfo.MakeGenericMethod(itemType);
            genericMergeMethodInfo.Invoke(null, new object[] { target, source, mode, forceDeepMerge });
        }

        private static void MergeDictionaryGeneric<T>(IDictionary<string, T> target, IDictionary<string, T> source, DictionaryMergeMode mode, bool forceDeepMerge)
        {
            switch (mode)
            {
                case DictionaryMergeMode.Clear:
                    target.Clear();
                    if (source == null) break;
                    foreach (var kvp in source) target.Add(kvp.Key, kvp.Value);
                    break;
                case DictionaryMergeMode.ReplaceValue:
                    if (source == null) break;
                    foreach (var kvp in source) target[kvp.Key] = kvp.Value;
                    break;
                case DictionaryMergeMode.MergeValue:
                    if (source == null) break;
                    foreach (var kvp in source)
                    {
                        if (target.TryGetValue(kvp.Key, out var v))
                        {
                            MergeObject(v, kvp.Value, forceRootMerge: true, forceDeepMerge);
                            if (typeof(T).IsValueType) target[kvp.Key] = v;
                        }
                        else
                            target[kvp.Key] = kvp.Value;
                    }
                    break;
                case DictionaryMergeMode.Complement:
                    if (source == null) break;
                    foreach (var kvp in source)
                    {
                        if (!target.ContainsKey(kvp.Key))
                        {
                            target.Add(kvp.Key, kvp.Value);
                        }
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public static void MergeDictionary(object target, object source, Type valueType, DictionaryMergeMode mode, bool forceDeepMerge = false)
        {
            var mergeMethodInfo = typeof(Merging).GetMethod(nameof(MergeDictionaryGeneric), BindingFlags.NonPublic | BindingFlags.Static);
            var genericMergeMethodInfo = mergeMethodInfo.MakeGenericMethod(valueType);
            genericMergeMethodInfo.Invoke(null, new object[] { target, source, mode, forceDeepMerge });
        }

        private static void MergeObjectProperties(object target, object source, bool forceRootMerge = false, bool forceDeepMerge = false)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (source == null) return;
            var t = target.GetType();
            if (source.GetType() != t) throw new ArgumentException("Different types in source and target");

            foreach (var p in t.GetModelProperties())
            {
                if (p.GetCustomAttribute<NoMergeAttribute>() != null) continue;
                var sv = p.GetValue(source);
                if (sv == null) continue;
                var tv = p.GetValue(target);
                if (tv == null || IsAtomic(p.PropertyType))
                {
                    p.SetValue(target, sv);
                    continue;
                }
                var propertyMapValueType = GetDictionaryValueType(p.PropertyType);
                if (propertyMapValueType != null)
                {
                    var mapMergeMode = p.GetCustomAttribute<MergeDictionaryAttribute>()?.MergeMode
                        ?? (propertyMapValueType.IsMergable()
                            ? DictionaryMergeMode.MergeValue
                            : DictionaryMergeMode.ReplaceValue);
                    MergeDictionary(tv, sv, propertyMapValueType, mapMergeMode);
                    continue;
                }
                var propertyListElementType = GetListElementType(p.PropertyType);
                if (propertyListElementType != null)
                {
                    var listMergeMode = p.GetCustomAttribute<MergeListAttribute>()?.MergeMode ?? ListMergeMode.Clear;
                    MergeList(tv, sv, propertyListElementType, listMergeMode);
                    continue;
                }
                if (forceRootMerge || forceDeepMerge || p.PropertyType.IsMergable())
                {
                    MergeObject(tv, sv, forceRootMerge: false, forceDeepMerge);
                    if (t.IsValueType) p.SetValue(target, tv);
                    continue;
                }

                p.SetValue(target, sv);
            }
        }

        public static void MergeObject(object target, object source, bool forceRootMerge = false, bool forceDeepMerge = false)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var t = target.GetType();

            if (t.IsMergableByInterface())
            {
                ((IMergableConfigModel)target).UpdateWith(source, forceDeepMerge);
                return;
            }

            if (source != null && source.GetType() != t)
            {
                throw new ArgumentException("Different types in source and target");
            }

            var mapValueType = GetDictionaryValueType(t);
            if (mapValueType != null)
            {
                MergeDictionary(target, source, mapValueType, DictionaryMergeMode.ReplaceValue, forceDeepMerge);
                return;
            }

            var listElementType = GetListElementType(t);
            if (listElementType != null)
            {
                MergeList(target, source, listElementType, ListMergeMode.Clear, forceDeepMerge);
                return;
            }

            MergeObjectProperties(target, source, forceRootMerge, forceDeepMerge);
        }

    }
}
