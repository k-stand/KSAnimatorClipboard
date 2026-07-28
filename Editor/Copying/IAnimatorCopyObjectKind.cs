using System;
using System.Collections.Generic;

namespace com.github.k_stand.ksanimatorcopyengine.editor.Copying
{
    internal interface IAnimatorCopyObjectKind
    {
        Type ObjectType { get; }

        AnimatorCopyClipSet.AnimatorCopyClipSetType SingleClipSetType { get; }

        bool IsInStateMachineObject { get; }

        // wrappedObjectをコピー範囲のルートとしてClone登録する際に、明示的にClonePolicy.Cloneを
        // 登録すべきUnityEngine.Object一式を返す。所有関係(AnimatorCloner.RegisterChildrenRecursively
        // が辿る範囲)に限定し、Transition/StateTransitionのdestinationState/destinationStateMachine
        // のような参照先は含めない。
        IEnumerable<UnityEngine.Object> GetCloneScope(object wrappedObject);
    }
}
