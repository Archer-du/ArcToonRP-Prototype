using System;

namespace ArcToon.Settings.Attributes
{
    /// <summary>
    /// Draws a nested [Serializable] field as a titled sub-section box (bold label + boxed body),
    /// matching the shadow sub-section style. Non-collapsible.
    /// Consumed only by the Editor-side AttributeInspectorDrawer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class BoxGroupAttribute : Attribute
    {
        public string Title { get; }

        public BoxGroupAttribute(string title) => Title = title;
    }
}
