using System;

namespace com.github.k_stand.ksanimatorcopyengine.editor.CrossController
{
    internal interface ICrossControllerPostProcessor
    {
        Type ObjectType { get; }

        void PostProcess(object clonedObject);
    }
}
