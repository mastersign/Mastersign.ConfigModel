using System;

namespace Mastersign.ConfigModel
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MergableConfigModelAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class NoMergeAttribute : Attribute 
    {
    }

    public enum ListMergeMode
    {
        Clear,
        ReplaceItem,
        MergeItem,
        Append,
        Prepend,
        AppendDistinct,
        PrependDistinct,
    }

    public enum DictionaryMergeMode
    {
        Clear,
        ReplaceValue,
        MergeValue,
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class MergeListAttribute : Attribute
    {
        public ListMergeMode MergeMode { get; }

        public MergeListAttribute(ListMergeMode mergeMode)
        {
            MergeMode = mergeMode;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class MergeDictionaryAttribute : Attribute
    {
        public DictionaryMergeMode MergeMode { get; }

        public MergeDictionaryAttribute(DictionaryMergeMode mergeMode)
        {
            MergeMode = mergeMode;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class TypeDiscriminatorAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TypeDiscriminationValueAttribute : Attribute
    {
        public string Value { get; }

        public TypeDiscriminationValueAttribute(string value)
        {
            Value = value;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class TypeIndicatorAttribute : Attribute
    {
    }
}
