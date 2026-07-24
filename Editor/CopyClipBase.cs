using System;
using System.Collections.Generic;
using System.Linq;

namespace com.github.k_stand.ksanimatorclipboard.editor
{
    /// <summary>
    /// コピー対象のオブジェクトと、貼り付け時に必要な付随情報(コンテキスト)を保持する基底クラスです。
    /// </summary>
    public abstract class CopyClipBase
    {
        /// <summary>
        /// コピー対象のオブジェクト本体を取得します。
        /// </summary>
        public object Object { get; private protected set; }

        /// <summary>
        /// Objectの実際の型を取得します。
        /// </summary>
        public virtual Type Type => Object.GetType();

        private protected Dictionary<string, object> Contexts { get; set; } = new();

        private protected CopyClipBase(object obj)
        {
            Object = obj;
        }

        /// <summary>
        /// Objectがstruct型の場合に、その内容を差し替えます。
        /// </summary>
        /// <param name="obj">差し替える新しい値。</param>
        /// <exception cref="InvalidCastException">現在のObjectと型が一致しないstructが指定された場合。</exception>
        public void SetStructObject<T>(T obj) where T : struct
        {
            if (Object is T) { Object = obj; }
            else { throw new InvalidCastException("型が一致しないstructをCopyClipにセットしようとしました"); }
        }

        internal void SetContext(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("コンテキストのキーはnullや空文字列であってはいけません");
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