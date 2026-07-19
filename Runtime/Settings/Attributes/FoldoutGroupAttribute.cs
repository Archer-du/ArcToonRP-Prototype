using System;

namespace ArcToon.Settings.Attributes
{
    /// <summary>
    /// Draws this field (and its serialized children) inside a collapsible foldout panel,
    /// styled like the hand-written pipeline asset panels.
    /// Top-level fields with no [FoldoutGroup] fall into an implicit "General" group.
    /// Consumed only by the Editor-side AttributeInspectorDrawer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class FoldoutGroupAttribute : Attribute
    {
        public string Title { get; }

        public FoldoutGroupAttribute(string title) => Title = title;
    }
}
