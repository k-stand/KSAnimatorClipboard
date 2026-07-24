using System;

namespace com.github.k_stand.ksanimatorclipboard.editor
{
    /// <summary>
    /// AnimatorCopyClipSetの実際のTypeが要求された型と一致しない場合に、Try接頭辞を持たないPaste系メソッドから送出される例外です。
    /// </summary>
    public sealed class AnimatorCopyClipSetTypeMismatchException : InvalidOperationException
    {
        /// <summary>
        /// AnimatorCopyClipSetTypeMismatchExceptionの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="message">例外の内容を説明するメッセージ。</param>
        public AnimatorCopyClipSetTypeMismatchException(string message) : base(message) { }
    }
}
