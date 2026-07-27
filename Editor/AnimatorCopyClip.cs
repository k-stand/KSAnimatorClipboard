using System.Collections.Generic;
using System.Linq;

namespace com.github.k_stand.ksanimatorcopyengine.editor
{
    /// <summary>
    /// AnimatorController関連オブジェクト1件分のコピー内容を表すクリップです。
    /// </summary>
    public class AnimatorCopyClip : CopyClipBase
    {
        private protected Dictionary<ContextKey, object> AnimatorContexts { get; set; } = new();

        internal AnimatorCopyClip(object obj) : base(obj) { }

        /// <summary>
        /// 保持しているObjectをそのまま使って自身の複製を作成します。
        /// </summary>
        /// <returns>複製されたAnimatorCopyClip。</returns>
        public AnimatorCopyClip Clone()
        {
            return Clone(Object);
        }

        /// <summary>
        /// Objectを指定したオブジェクトに差し替えた上で自身の複製を作成します。コンテキストの内容はそのまま引き継がれます。
        /// </summary>
        /// <param name="obj">複製後のクリップに設定するオブジェクト。</param>
        /// <returns>複製されたAnimatorCopyClip。</returns>
        public AnimatorCopyClip Clone(object obj)
        {
            return new(obj) { Contexts = new(Contexts) };
        }

        /// <summary>
        /// 指定したAnimatorClonerでObjectとコンテキスト内のオブジェクトをクローンした上で、自身の複製を作成します。
        /// クローンできなかったオブジェクトは元の値のまま引き継がれます。
        /// </summary>
        /// <param name="cloner">クローンに使用するAnimatorCloner。</param>
        /// <returns>複製されたAnimatorCopyClip。</returns>
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