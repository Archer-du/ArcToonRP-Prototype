using System;

namespace ArcToon.Settings.Attributes
{
    /// <summary>
    /// Comparison operator used by <see cref="ShowIfAttribute"/>.
    /// Only the operators currently required by the settings classes are defined.
    /// </summary>
    public enum CompareOp
    {
        Equal,
        GreaterEqual,
    }

    /// <summary>
    /// Shows this field only when a sibling integer / bool / enum field satisfies the comparison.
    /// Multiple [ShowIf] on one field are AND-combined (all must pass).
    /// Always pass the condition field name via nameof() for compile-time safety.
    /// Example: [ShowIf(nameof(cascadeCount), 2, CompareOp.GreaterEqual)]
    /// Consumed only by the Editor-side AttributeInspectorDrawer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class ShowIfAttribute : Attribute
    {
        public string ConditionField { get; }
        public long CompareValue { get; }
        public CompareOp Operator { get; }

        public ShowIfAttribute(string conditionField, long compareValue, CompareOp op = CompareOp.Equal)
        {
            ConditionField = conditionField;
            CompareValue = compareValue;
            Operator = op;
        }
    }
}
