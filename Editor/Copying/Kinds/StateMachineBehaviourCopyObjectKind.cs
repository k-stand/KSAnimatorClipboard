using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.github.k_stand.ksanimatorcopyengine.editor.Copying
{
    internal sealed class StateMachineBehaviourCopyObjectKind : IAnimatorCopyObjectKind
    {
        public Type ObjectType => typeof(StateMachineBehaviour);

        public AnimatorCopyClipSet.AnimatorCopyClipSetType SingleClipSetType => AnimatorCopyClipSet.AnimatorCopyClipSetType.Behaviours;

        public bool IsInStateMachineObject => false;

        // Behaviour自身のみをクローン範囲とし、Behaviourが内部で保持する参照先(パラメーター名など)は対象外とする。
        // それらの参照の妥当性検証は、別のプラグイン機構(IStateMachineBehaviourCloneResultValidator)が担う。
        public IEnumerable<UnityEngine.Object> GetCloneScope(object wrappedObject) => new UnityEngine.Object[] { (StateMachineBehaviour)wrappedObject };
    }
}
