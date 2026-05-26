using System;
using System.Collections.Generic;
using System.Linq;

namespace com.github.k_stand.ksanimatorclipboard.editor
{
    public abstract class CopyClipBase<T> where T : CopyClipBase<T>
    {
        public object Object { get; private protected set; }

        public virtual Type Type => Object.GetType();

        private protected Dictionary<string, object> Contexts { get; set; } = new();

        private protected CopyClipBase(object obj)
        {
            Object = obj;
        }

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

        internal KeyValuePair<string, object>[] GetAllContext()
        {
            return Contexts.ToArray();
        }
    }
}