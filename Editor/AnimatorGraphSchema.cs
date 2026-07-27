using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

namespace com.github.k_stand.ksanimatorcopyengine.editor
{
    // AnimatorController関連オブジェクトグラフの「形」(どのノードがどの子を持つか)を1箇所に集約する。
    // AnimatorCloner.ValidateRegistration*とAnimatorCloneResultValidator.Validate*CloneResultが
    // 個別に手書きしていた子要素の列挙を共通化する。列挙範囲は両者の既存実装の和集合を厳密に踏襲しており、
    // AnimatorState.motion(AnimationClip/BlendTree)はいずれの既存実装も辿っていなかったため対象外のまま。
    internal static class AnimatorGraphSchema
    {
        internal static IEnumerable<(string MemberName, object Child)> GetChildren(AnimatorController target)
        {
            AnimatorControllerLayer[] layers = target.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                yield return ($"{nameof(target.layers)}[{i}]", layers[i]);
            }
        }

        internal static IEnumerable<(string MemberName, object Child)> GetChildren(AnimatorControllerLayer target)
        {
            yield return (nameof(target.stateMachine), target.stateMachine);

            StateMotionPair[] overrideStateMotionPairs = AnimatorInternalsAdapterProvider.Current.GetAllOverrideStateMotionPairs(target) ?? Array.Empty<StateMotionPair>();
            foreach (StateMotionPair pair in overrideStateMotionPairs)
            {
                yield return ("m_Motions.m_State", pair.State);
            }

            StateBehavioursPair[] overrideBehavioursPairs = AnimatorInternalsAdapterProvider.Current.GetAllOverrideBehavioursPairs(target) ?? Array.Empty<StateBehavioursPair>();
            foreach (StateBehavioursPair pair in overrideBehavioursPairs)
            {
                yield return ("m_Behaviours.m_State", pair.State);
                foreach (StateMachineBehaviour behaviour in pair.Behaviours)
                {
                    yield return ("m_Behaviours.m_Behaviours", behaviour);
                }
            }
        }

        internal static IEnumerable<(string MemberName, object Child)> GetChildren(AnimatorStateMachine target)
        {
            ChildAnimatorState[] states = target.states;
            for (int i = 0; i < states.Length; i++)
            {
                yield return ($"{nameof(target.states)}[{i}].{nameof(ChildAnimatorState.state)}", states[i].state);
            }

            ChildAnimatorStateMachine[] stateMachines = target.stateMachines;
            for (int i = 0; i < stateMachines.Length; i++)
            {
                yield return ($"{nameof(target.stateMachines)}[{i}].{nameof(ChildAnimatorStateMachine.stateMachine)}", stateMachines[i].stateMachine);
            }

            yield return (nameof(target.defaultState), target.defaultState);

            AnimatorTransition[] entryTransitions = target.entryTransitions;
            for (int i = 0; i < entryTransitions.Length; i++)
            {
                yield return ($"{nameof(target.entryTransitions)}[{i}]", entryTransitions[i]);
            }

            AnimatorStateTransition[] anyStateTransitions = target.anyStateTransitions;
            for (int i = 0; i < anyStateTransitions.Length; i++)
            {
                yield return ($"{nameof(target.anyStateTransitions)}[{i}]", anyStateTransitions[i]);
            }

            foreach (ChildAnimatorStateMachine curCASM in stateMachines)
            {
                AnimatorTransition[] transitions = target.GetStateMachineTransitions(curCASM.stateMachine);
                for (int i = 0; i < transitions.Length; i++)
                {
                    yield return ($"StateMachineTransitions[{curCASM.stateMachine?.name}][{i}]", transitions[i]);
                }
            }

            StateMachineBehaviour[] behaviours = target.behaviours;
            for (int i = 0; i < behaviours.Length; i++)
            {
                yield return ($"{nameof(target.behaviours)}[{i}]", behaviours[i]);
            }
        }

        internal static IEnumerable<(string MemberName, object Child)> GetChildren(AnimatorState target)
        {
            AnimatorStateTransition[] transitions = target.transitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                yield return ($"{nameof(target.transitions)}[{i}]", transitions[i]);
            }

            StateMachineBehaviour[] behaviours = target.behaviours;
            for (int i = 0; i < behaviours.Length; i++)
            {
                yield return ($"{nameof(target.behaviours)}[{i}]", behaviours[i]);
            }
        }

        internal static IEnumerable<(string MemberName, object Child)> GetChildren(AnimatorTransition target)
        {
            yield return (nameof(target.destinationState), target.destinationState);
            yield return (nameof(target.destinationStateMachine), target.destinationStateMachine);
        }

        // isExit時はdestinationState/destinationStateMachineが未設定(null)で正常なため子として列挙しない。
        // (ValidateRegistration*側もnullターゲットは無条件で問題なし扱いのため、この省略で観測可能な挙動は変わらない)
        internal static IEnumerable<(string MemberName, object Child)> GetChildren(AnimatorStateTransition target)
        {
            if (target.isExit) yield break;

            yield return (nameof(target.destinationState), target.destinationState);
            yield return (nameof(target.destinationStateMachine), target.destinationStateMachine);
        }
    }
}
