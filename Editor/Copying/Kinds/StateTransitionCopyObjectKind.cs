using System;
using System.Collections.Generic;
using UnityEditor.Animations;

namespace com.github.k_stand.ksanimatorcopyengine.editor.Copying
{
    internal sealed class StateTransitionCopyObjectKind : IAnimatorCopyObjectKind
    {
        public Type ObjectType => typeof(AnimatorStateTransition);

        public AnimatorCopyClipSet.AnimatorCopyClipSetType SingleClipSetType => AnimatorCopyClipSet.AnimatorCopyClipSetType.StateTransition;

        public bool IsInStateMachineObject => true;

        public IEnumerable<UnityEngine.Object> GetCloneScope(object wrappedObject) => new UnityEngine.Object[] { (AnimatorStateTransition)wrappedObject };
    }
}
