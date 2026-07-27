using System;
using UnityEditor.Animations;

namespace com.github.k_stand.ksanimatorcopyengine.editor.CrossController
{
    internal sealed class LayerSyncedIndexPostProcessor : ICrossControllerPostProcessor
    {
        public Type ObjectType => typeof(AnimatorControllerLayer);

        public void PostProcess(object clonedObject) => ((AnimatorControllerLayer)clonedObject).syncedLayerIndex = -1;
    }
}
