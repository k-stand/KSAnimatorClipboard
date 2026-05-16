using System;
using System.Collections.Generic;

namespace io.github.kiriumestand.animatorclipboard.editor
{
    public class Clip<T> : ClipBase
    {
        public override Type Type => typeof(T);

        public T ClipObject
        {
            get => (T)GenericClipObject;
            private set => GenericClipObject = value;
        }

        public Clip(T obj)
        {
            ClipObject = obj;
        }

        public Clip<T> Clone(T obj) => new(obj) { Contexts = new(Contexts) };
    }
}