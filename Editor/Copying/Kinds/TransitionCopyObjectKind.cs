using System;
using System.Collections.Generic;
using UnityEditor.Animations;

namespace com.github.k_stand.ksanimatorcopyengine.editor.Copying
{
    internal sealed class TransitionCopyObjectKind : IAnimatorCopyObjectKind
    {
        public Type ObjectType => typeof(AnimatorTransition);

        public AnimatorCopyClipSet.AnimatorCopyClipSetType SingleClipSetType => AnimatorCopyClipSet.AnimatorCopyClipSetType.Transition;

        public bool IsInStateMachineObject => true;

        public IEnumerable<UnityEngine.Object> GetCloneScope(object wrappedObject) => new UnityEngine.Object[] { (AnimatorTransition)wrappedObject };
    }
}
