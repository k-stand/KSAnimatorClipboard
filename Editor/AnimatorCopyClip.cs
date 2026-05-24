using System;

namespace com.github.k_stand.ksanimatorclipboard.editor
{

    public class AnimatorCopyClip<T> : AnimatorCopyClipBase
    {
        public override Type Type => typeof(T);

        public T ClipObject
        {
            get => (T)GenericClipObject;
            private set => GenericClipObject = value;
        }

        public AnimatorCopyClip(T obj)
        {
            ClipObject = obj;
        }

        public AnimatorCopyClip<T> Clone(T obj) => new(obj) { Contexts = new(Contexts) };

    }
}