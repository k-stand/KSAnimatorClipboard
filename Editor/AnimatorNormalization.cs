using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;

namespace com.github.k_stand.ksanimatorclipboard.editor
{
    /// <summary>
    /// AnimatorControllerの構造をコピー&貼り付け処理が扱いやすい形へ正規化するためのユーティリティです。
    /// </summary>
    public static class AnimatorNormalization
    {
        /// <summary>
        /// AnimatorController内の全レイヤーに対してNormalizeAnyStateTransitionsを行い、アセットに含まれる未使用のサブアセットを整理します。
        /// </summary>
        /// <param name="animator">正規化対象のAnimatorController。</param>
        /// <param name="muteLogs">trueの場合、未使用サブアセット削除時のログ出力を抑制します。</param>
        public static void NormalizeAnimator(AnimatorController animator, bool muteLogs = false)
        {
            Array.ForEach(animator.layers, x => NormalizeAnyStateTransitions(x));

            string path = AssetDatabase.GetAssetPath(animator);
            if (!string.IsNullOrEmpty(path))
            {
                AnimatorAssetPersistence.AddObjectToAssetRecursively(animator, path);
                AnimatorAssetPersistence.RemoveUnusedSubAssets(animator, muteLogs);
            }
        }

        /// <summary>
        /// レイヤー内の子AnimatorStateMachineが持つAnyStateTransitionsを、すべてレイヤー直下のAnyStateTransitionsへ集約します。
        /// </summary>
        /// <param name="layer">正規化対象のAnimatorControllerLayer。</param>
        public static void NormalizeAnyStateTransitions(AnimatorControllerLayer layer)
        {
            AnimatorStateMachine[] innerStateMachines = GetAllStateMachineRecursively(layer.stateMachine);

            List<AnimatorStateTransition> anyStateTransitions = new();
            anyStateTransitions.AddRange(layer.stateMachine.anyStateTransitions);
            foreach (AnimatorStateMachine curStateMachine in innerStateMachines)
            {
                anyStateTransitions.AddRange(curStateMachine.anyStateTransitions);
                curStateMachine.anyStateTransitions = Array.Empty<AnimatorStateTransition>();
            }

            layer.stateMachine.anyStateTransitions = anyStateTransitions.ToArray();
        }

        private static AnimatorStateMachine[] GetAllStateMachineRecursively(AnimatorStateMachine stateMachine)
        {
            List<AnimatorStateMachine> stateMachines = new();
            foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines)
            {
                stateMachines.Add(childStateMachine.stateMachine);
                stateMachines.AddRange(GetAllStateMachineRecursively(childStateMachine.stateMachine));
            }
            return stateMachines.ToArray();
        }
    }
}
