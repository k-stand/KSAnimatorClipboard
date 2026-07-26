using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;

namespace com.github.k_stand.ksanimatorclipboard.editor
{
    /// <summary>
    /// コピーしたオブジェクトが参照しているAnimatorControllerParameterのうち、貼り付け先のAnimatorControllerに存在しないものを検出します。
    /// </summary>
    public static class AnimatorClipboardParameterConsistency
    {
        /// <summary>
        /// clipSetが参照しているパラメーター名のうち、destControllerに存在しないものを列挙します。
        /// StateMachineBehaviourが参照するパラメーターは検出対象に含まれません(条件式(AnimatorCondition)由来の参照のみを収集します)。
        /// </summary>
        /// <param name="clipSet">検証対象のAnimatorCopyClipSet。</param>
        /// <param name="destController">存在確認の基準にする貼り付け先のAnimatorController。</param>
        /// <returns>destControllerに存在しないパラメーター名の一覧。</returns>
        /// <exception cref="ArgumentNullException">clipSetまたはdestControllerがnullの場合。</exception>
        public static IReadOnlyList<string> FindMissingParameters(AnimatorCopyClipSet clipSet, AnimatorController destController)
        {
            if (clipSet == null) throw new ArgumentNullException(nameof(clipSet));
            if (destController == null) throw new ArgumentNullException(nameof(destController));

            HashSet<string> referencedParameterNames = new();
            foreach (AnimatorCopyClip clip in clipSet.Clips)
            {
                CollectReferencedParameterNames(clip.Object, referencedParameterNames);
            }

            HashSet<string> existingParameterNames = destController.parameters.Select(p => p.name).ToHashSet();
            return referencedParameterNames.Where(name => !existingParameterNames.Contains(name)).ToList();
        }

        private static void CollectReferencedParameterNames(object obj, HashSet<string> result)
        {
            switch (obj)
            {
                case AnimatorControllerLayer layer:
                    CollectFromStateMachine(layer.stateMachine, result);
                    break;
                case ChildAnimatorStateMachine childStateMachine:
                    CollectFromStateMachine(childStateMachine.stateMachine, result);
                    break;
                case ChildAnimatorState childState:
                    CollectFromState(childState.state, result);
                    break;
                case AnimatorStateTransition stateTransition:
                    CollectFromConditions(stateTransition.conditions, result);
                    break;
                case AnimatorTransition transition:
                    CollectFromConditions(transition.conditions, result);
                    break;
            }
        }

        private static void CollectFromStateMachine(AnimatorStateMachine stateMachine, HashSet<string> result)
        {
            if (stateMachine == null) return;

            Queue<AnimatorStateMachine> searchQueue = new();
            searchQueue.Enqueue(stateMachine);
            HashSet<AnimatorStateMachine> visited = new();

            while (searchQueue.Count > 0)
            {
                AnimatorStateMachine current = searchQueue.Dequeue();
                if (!visited.Add(current)) continue;

                foreach (AnimatorTransition entryTransition in current.entryTransitions)
                {
                    CollectFromConditions(entryTransition.conditions, result);
                }

                foreach (AnimatorStateTransition anyStateTransition in current.anyStateTransitions)
                {
                    CollectFromConditions(anyStateTransition.conditions, result);
                }

                foreach (ChildAnimatorState childState in current.states)
                {
                    CollectFromState(childState.state, result);
                }

                foreach (ChildAnimatorStateMachine childStateMachine in current.stateMachines)
                {
                    foreach (AnimatorTransition subMachineTransition in current.GetStateMachineTransitions(childStateMachine.stateMachine))
                    {
                        CollectFromConditions(subMachineTransition.conditions, result);
                    }

                    searchQueue.Enqueue(childStateMachine.stateMachine);
                }
            }
        }

        private static void CollectFromState(AnimatorState state, HashSet<string> result)
        {
            if (state == null) return;

            foreach (AnimatorStateTransition transition in state.transitions)
            {
                CollectFromConditions(transition.conditions, result);
            }
        }

        private static void CollectFromConditions(AnimatorCondition[] conditions, HashSet<string> result)
        {
            foreach (AnimatorCondition condition in conditions)
            {
                result.Add(condition.parameter);
            }
        }
    }
}
