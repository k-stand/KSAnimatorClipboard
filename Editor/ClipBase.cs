using System;
using System.Collections.Generic;

namespace io.github.kiriumestand.animatorclipboard.editor
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
        }
    }
}