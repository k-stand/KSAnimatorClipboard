using System;
using System.Collections.Generic;

namespace com.github.k_stand.ksanimatorclipboard.editor.Copying
{
    internal sealed class GenericUnityObjectCopyObjectKind : IAnimatorCopyObjectKind
    {
        // 他のKindが対応しない任意のUnityEngine.Object派生型に対するフォールバック。
        // AnimatorCopyObjectKindRegistry.Resolveの基底型探索により、専用Kindが未登録の型はここに解決される。
        public Type ObjectType => typeof(UnityEngine.Object);

        public AnimatorCopyClipSet.AnimatorCopyClipSetType SingleClipSetType => AnimatorCopyClipSet.AnimatorCopyClipSetType.Other;

        public bool IsInStateMachineObject => false;

        public IEnumerable<UnityEngine.Object> GetCloneScope(object wrappedObject) =>
            wrappedObject is UnityEngine.Object obj ? new[] { obj } : Array.Empty<UnityEngine.Object>();
    }
}
