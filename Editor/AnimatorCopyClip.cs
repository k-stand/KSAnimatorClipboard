using System.Collections.Generic;
using System.Linq;

namespace com.github.k_stand.ksanimatorclipboard.editor
{
    public class AnimatorCopyClip : CopyClipBase
    {
        private protected Dictionary<ContextKey, object> AnimatorContexts { get; set; } = new();

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

            KeyValuePair<ContextKey, object>[] allContext = GetAllAnimatorContext();
            foreach (KeyValuePair<ContextKey, object> context in allContext)
            {
                object cloneContextVal = cloner.TryCloneObject(context.Value, out object tempClone) ? tempClone : context.Value;
                cloneClip.SetAnimatorContext(context.Key, cloneContextVal);
            }

            return cloneClip;
        }

        internal void SetAnimatorContext(ContextKey key, object value)
        {
            AnimatorContexts[key] = value;
        }

        internal bool TryGetAnimatorContext(ContextKey key, out object value)
        {
            return AnimatorContexts.TryGetValue(key, out value);
        }

        internal KeyValuePair<ContextKey, object>[] GetAllAnimatorContext()
        {
            return AnimatorContexts.ToArray();
        }

        internal enum ContextKey
        {
            Parent,
            PropertyName,
        }

        internal static class ContextValue
        {
            internal enum PropertyName
            {
                m_EntryTransitions,
                m_StateMachineTransitions,
                m_AnyStateTransitions,
                m_Transitions,
                m_AnimatorLayers,
            }
        }
    }
}