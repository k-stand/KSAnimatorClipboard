using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;

namespace com.github.k_stand.ksanimatorclipboard.editor.Copying
{
    internal sealed class ChildStateCopyObjectKind : IAnimatorCopyObjectKind
    {
        public Type ObjectType => typeof(ChildAnimatorState);

        public AnimatorCopyClipSet.AnimatorCopyClipSetType SingleClipSetType => AnimatorCopyClipSet.AnimatorCopyClipSetType.ChildState;

        public bool IsInStateMachineObject => true;

        public IEnumerable<UnityEngine.Object> GetCloneScope(object wrappedObject)
        {
            AnimatorState state = ((ChildAnimatorState)wrappedObject).state;
            // stateが未設定(壊れたChildAnimatorState)の場合は例外を出さず空スコープを返し、呼び出し元は静かに無登録で終わる
            if (state == null) return Array.Empty<UnityEngine.Object>();

            return new UnityEngine.Object[] { state }.Concat(state.transitions).Concat(state.behaviours);
        }
    }
}
