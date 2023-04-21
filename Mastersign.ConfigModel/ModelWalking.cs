using System;
using System.Collections.Generic;
using System.Reflection;

namespace Mastersign.ConfigModel
{
    internal static class ModelWalking
    {
        private static void WalkModelList<TTarget, TItem>(IList<TItem> list, Func<TTarget, TTarget> visitor)
        {
            if (ReflectionHelper.IsAtomic(typeof(TItem))) return;
            for (var i = 0; i < list.Count; i++)
            {
                list[i] = (TItem)WalkConfigModel(list[i], visitor);
            }
        }

        private static void WalkModelDictionary<TTarget, TItem>(IDictionary<string, TItem> dictionary, Func<TTarget, TTarget> visitor)
        {
            if (ReflectionHelper.IsAtomic(typeof(TItem))) return;
            foreach (var kvp in dictionary)
            {
                dictionary[kvp.Key] = (TItem)WalkConfigModel(kvp.Value, visitor);
            }
        }

        private static void WalkProperties<TTarget>(object model, Type t, Func<TTarget, TTarget> visitor)
        {
            foreach (var p in t.GetModelProperties())
            {
                if (ReflectionHelper.IsAtomic(p.PropertyType)) continue;
                var pv = p.GetValue(model);
                if (pv == null) continue;
                p.SetValue(model, WalkConfigModel(pv, visitor));
            }
        }

        public static object WalkConfigModel<TTarget>(object model, Func<TTarget, TTarget> visitor)
        {
            if (model == null) return null;
            var t = model.GetType();

            var mapValueType = ReflectionHelper.GetDictionaryValueType(t);
            if (mapValueType != null)
            {
                var dictWalkerMethodInfo = typeof(ModelWalking).GetMethod(
                    nameof(WalkModelDictionary), BindingFlags.Static | BindingFlags.NonPublic);
                var dictWalker = dictWalkerMethodInfo.MakeGenericMethod(typeof(TTarget), mapValueType);
                dictWalker.Invoke(null, new[] { model, visitor });
                return model;
            }

            var listElementType = ReflectionHelper.GetListElementType(t);
            if (listElementType != null)
            {
                var listWalkerMethodInfo = typeof(ModelWalking).GetMethod(
                    nameof(WalkModelList), BindingFlags.Static | BindingFlags.NonPublic);
                var listWalker = listWalkerMethodInfo.MakeGenericMethod(typeof(TTarget), listElementType);
                listWalker.Invoke(null, new[] { model, visitor });
                return model;
            }

            WalkProperties(model, t, visitor);

            if (typeof(TTarget).IsAssignableFrom(t))
            {
                model = visitor((TTarget)model);
            }

            return model;
        }
    }
}
