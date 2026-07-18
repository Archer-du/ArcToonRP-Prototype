using System;

namespace ArcToon.Settings.Attributes
{
    /// <summary>
    /// Runtime mirror of UnityEditor.MessageType so layout attributes carry no Editor dependency.
    /// The Editor-side drawer maps this to UnityEditor.MessageType.
    /// </summary>
    public enum MessageTypeLevel
    {
        None,
        Info,
        Warning,
        Error,
    }

    /// <summary>
    /// For a UnityEngine.Object reference field: draws a HelpBox below the field when the
    /// reference is null. Replaces the inline null-check that lived in the hand-written editor.
    /// Consumed only by the Editor-side AttributeInspectorDrawer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class HelpBoxIfNullAttribute : Attribute
    {
        public string Message { get; }
        public MessageTypeLevel Level { get; }

        public HelpBoxIfNullAttribute(string message, MessageTypeLevel level = MessageTypeLevel.Warning)
        {
            Message = message;
            Level = level;
        }
    }
}
