using System.Collections.Generic;

namespace com.github.k_stand.ksanimatorclipboard.editor
{
    public class AnimatorCopyClip : CopyClipBase<AnimatorCopyClip>
    {
        internal AnimatorCopyClip(object obj) : base(obj) { }

        public AnimatorCopyClip Clone()
        {
            return Clone(Object);
        }

        public AnimatorCopyClip Clone(object obj)
        {
            return new(obj) { Contexts = new(Contexts) };
        }

        public AnimatorCopyClip Clone(AnimatorCloner cloner)
        {
            AnimatorCopyClip cloneClip = cloner.TryCloneObject(Object, out object cloneObj) ? Clone(cloneObj) : Clone();

            KeyValuePair<string, object>[] allContext = cloneClip.GetAllContext();
            foreach (KeyValuePair<string, object> context in allContext)
            {
                object cloneContextVal = cloner.TryCloneObject(context.Value, out object tempClone) ? tempClone : context.Value;
                cloneClip.SetContext(context.Key, cloneContextVal);
            }

            return cloneClip;
        }

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