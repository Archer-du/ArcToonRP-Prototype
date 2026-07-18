using System;

namespace ArcToon.Settings.Attributes
{
    /// <summary>
    /// Shows this field only when a sibling enum field equals one of the given values.
    /// Always pass the condition field name via nameof() for compile-time safety.
    /// Example: [ShowIfEnum(nameof(filterQuality), FilterQuality.PoissonDisk, FilterQuality.PCSS)]
    /// Consumed only by the Editor-side AttributeInspectorDrawer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ShowIfEnumAttribute : Attribute
    {
        public string ConditionField { get; }
        public object[] Values { get; }

        public ShowIfEnumAttribute(string conditionField, params object[] values)
        {
            ConditionField = conditionField;
            Values = values;
        }
    }
}
