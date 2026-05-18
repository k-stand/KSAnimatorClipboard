using System;
using System.Collections.Generic;

namespace com.github.k_stand.ksanimatorclipboard.editor
{
    public abstract class ClipBase
    {
        public abstract Type Type { get; }

        public object GenericClipObject { get; private protected set; }

        private protected Dictionary<string, object> Contexts { get; set; } = new();

        internal void SetContext(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new Exception("コンテキストのキーはnullや空文字列であってはいけません");
            }
            Contexts[key] = value;
        }

        internal bool TryGetContext(string key, out object value)
        {
            return Contexts.TryGetValue(key, out value);
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