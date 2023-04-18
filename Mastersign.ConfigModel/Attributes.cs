using System;

namespace Mastersign.ConfigModel
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MergableConfigModelAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class TypeDescriminatorAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TypeDescriminationValueAttribute : Attribute
    {
        public string Value { get; }

        public TypeDescriminationValueAttribute(string value)
        {
            Value = value;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class TypeIndicatorAttribute : Attribute
    {
    }
}
