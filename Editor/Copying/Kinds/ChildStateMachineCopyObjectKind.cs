using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;

namespace com.github.k_stand.ksanimatorclipboard.editor.Copying
{
    internal sealed class ChildStateMachineCopyObjectKind : IAnimatorCopyObjectKind
    {
        public Type ObjectType => typeof(ChildAnimatorStateMachine);

        public AnimatorCopyClipSet.AnimatorCopyClipSetType SingleClipSetType => AnimatorCopyClipSet.AnimatorCopyClipSetType.ChildStateMachine;

        public bool IsInStateMachineObject => true;

        public IEnumerable<UnityEngine.Object> GetCloneScope(object wrappedObject)
        {
            AnimatorStateMachine stateMachine = ((ChildAnimatorStateMachine)wrappedObject).stateMachine;
            // stateMachineが未設定(壊れたChildAnimatorStateMachine)の場合は例外を出さず空スコープを返し、呼び出し元は静かに無登録で終わる
            if (stateMachine == null) return Array.Empty<UnityEngine.Object>();

            return new UnityEngine.Object[] { stateMachine }.Concat(AnimatorGraphTraversal.ListupObjectsInStateMachine(stateMachine));
        }
    }
}
