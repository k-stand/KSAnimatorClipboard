using System;

namespace com.github.k_stand.ksanimatorclipboard.editor
{

    public class AnimatorCopyClip_obsorate<T> : AnimatorCopyClip
    {
        public override Type Type => typeof(T);

        public T ClipObject
        {
            get => (T)Object;
            private set => Object = value;
        }

        public AnimatorCopyClip_obsorate(T obj) : base(obj) { }

        public AnimatorCopyClip_obsorate<T> Clone(T obj) => new(obj) { Contexts = new(Contexts) };

    }
}