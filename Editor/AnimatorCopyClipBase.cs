namespace com.github.k_stand.ksanimatorclipboard.editor
{
    public abstract class AnimatorCopyClipBase : CopyClipBase
    {
        internal static class ContextKey
        {
            internal static readonly string Parent = "Parent";
            internal static readonly string PropertyName = "PropertyName";
        }

        internal static class ContextValue
        {
            internal static class PropertyName
            {
                internal static readonly string m_EntryTransitions = "m_EntryTransitions";
                internal static readonly string m_StateMachineTransitions = "m_StateMachineTransitions";
                internal static readonly string m_AnyStateTransitions = "m_AnyStateTransitions";
            }
        }
    }
}