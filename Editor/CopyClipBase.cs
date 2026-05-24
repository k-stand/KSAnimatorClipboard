using System;
using System.Collections.Generic;

namespace com.github.k_stand.ksanimatorclipboard.editor
{
    public abstract class CopyClipBase
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
    }
}