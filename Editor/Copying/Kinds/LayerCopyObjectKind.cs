using System;
using System.Collections.Generic;
using UnityEditor.Animations;

namespace com.github.k_stand.ksanimatorcopyengine.editor.Copying
{
    internal sealed class LayerCopyObjectKind : IAnimatorCopyObjectKind
    {
        public Type ObjectType => typeof(AnimatorControllerLayer);

        public AnimatorCopyClipSet.AnimatorCopyClipSetType SingleClipSetType => AnimatorCopyClipSet.AnimatorCopyClipSetType.Layers;

        public bool IsInStateMachineObject => false;

        public IEnumerable<UnityEngine.Object> GetCloneScope(object wrappedObject)
        {
            AnimatorControllerLayer layer = (AnimatorControllerLayer)wrappedObject;
            if (layer.stateMachine == null) return Array.Empty<UnityEngine.Object>();

            return AnimatorGraphTraversal.ListupObjectsInLayer(layer);
        }
    }
}
