using UnityEditor.Animations;
using UnityEngine;

namespace com.github.k_stand.ksanimatorclipboard.editor
{
    internal interface IAnimatorInternalsAdapter
    {
        StateMotionPair[] GetAllOverrideStateMotionPairs(AnimatorControllerLayer acl);

        StateBehavioursPair[] GetAllOverrideBehavioursPairs(AnimatorControllerLayer acl);

        void InitOverrideStateMotionPairs(AnimatorControllerLayer acl);

        void InitOverrideStateBehavioursPairs(AnimatorControllerLayer acl);

        /// <summary>
        /// 型・フィールドの解決および実際の読み書きが可能かを検証します。失敗時は例外を投げます。
        /// </summary>
        void Validate();
    }

    internal struct StateMotionPair
    {
        internal AnimatorState State;

        internal Motion Motion;
    }

    internal struct StateBehavioursPair
    {
        internal AnimatorState State;

        internal StateMachineBehaviour[] Behaviours;
    }
}
