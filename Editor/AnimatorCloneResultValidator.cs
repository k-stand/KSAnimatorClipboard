using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

namespace com.github.k_stand.ksanimatorcopyengine.editor
{
    /// <summary>
    /// クローン結果のAnimatorController関連オブジェクトが、本来クローンされているべき箇所にnull参照を持っていないかを検証します。
    /// </summary>
    public static class AnimatorCloneResultValidator
    {
        /// <summary>
        /// 指定したオブジェクトを起点に、無効なnull参照を持つメンバーを再帰的に検出します。
        /// StateMachineBehaviourの子要素の検証はStateMachineBehaviourCloneResultValidatorRegistryに登録されたvalidatorに委ねられ、
        /// 未登録の型は検証対象外として無視されます。
        /// </summary>
        /// <param name="target">検証対象のオブジェクト。</param>
        /// <returns>検出された無効なnull参照メンバーの一覧。</returns>
        public static IReadOnlyCollection<InvalidNullMember> ValidateCloneResult(UnityEngine.Object target) => ValidateCloneResultInternal(target);

        /// <summary>
        /// 複数のオブジェクトに対して、まとめてValidateCloneResultを行います。
        /// </summary>
        /// <param name="targets">検証対象のオブジェクトの列挙。</param>
        /// <returns>検出された無効なnull参照メンバーの一覧。</returns>
        public static IReadOnlyCollection<InvalidNullMember> ValidateCloneResults(IEnumerable<UnityEngine.Object> targets) => targets.SelectMany(t => ValidateCloneResult(t)).ToHashSet();

        private static IReadOnlyCollection<InvalidNullMember> ValidateCloneResultInternal(object target)
        {
            if (target == null)
            {
                return new List<InvalidNullMember>();
            }

            HashSet<UnityEngine.Object> visitedObjSet = new();

            return ValidateCloneResultDispatch(target, null, "", ref visitedObjSet);
        }

        // AnimatorGraphSchema.GetChildrenが列挙した子要素を、実際の型に応じて対応するValidateXxxCloneResultへ振り分ける。
        // トップレベルのValidateCloneResultInternalと、各ノードの子要素再帰の両方から使う共通の入口。
        private static IReadOnlyCollection<InvalidNullMember> ValidateCloneResultDispatch(object target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet) => target switch
        {
            null => new InvalidNullMember[] { new(parent, memberName) },
            AnimatorController castedObj => ValidateAnimatorControllerCloneResult(castedObj, parent, memberName, ref visitedObjSet),
            AnimatorControllerLayer castedObj => ValidateAnimatorControllerLayerCloneResult(castedObj, parent, memberName, ref visitedObjSet),
            ChildAnimatorStateMachine castedObj => ValidateChildAnimatorStateMachineCloneResult(castedObj, parent, memberName, ref visitedObjSet),
            AnimatorStateMachine castedObj => ValidateAnimatorStateMachineCloneResult(castedObj, parent, memberName, ref visitedObjSet),
            ChildAnimatorState castedObj => ValidateChildAnimatorStateCloneResult(castedObj, parent, memberName, ref visitedObjSet),
            AnimatorState castedObj => ValidateAnimatorStateCloneResult(castedObj, parent, memberName, ref visitedObjSet),
            AnimatorTransition castedObj => ValidateAnimatorTransitionCloneResult(castedObj, parent, memberName, ref visitedObjSet),
            AnimatorStateTransition castedObj => ValidateAnimatorStateTransitionCloneResult(castedObj, parent, memberName, ref visitedObjSet),
            StateMachineBehaviour castedObj => ValidateStateMachineBehaviourCloneResult(castedObj, parent, memberName, ref visitedObjSet),
            _ => Array.Empty<InvalidNullMember>(),
        };

        private static IReadOnlyCollection<InvalidNullMember> ValidateAnimatorControllerCloneResult(AnimatorController target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (target == null) return new InvalidNullMember[] { new(parent, memberName) };

            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidNullMember>();
            visitedObjSet.Add(target);

            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach ((string childMemberName, object child) in AnimatorGraphSchema.GetChildren(target))
            {
                invalidNullMembers.UnionWith(ValidateCloneResultDispatch(child, target, childMemberName, ref visitedObjSet));
            }
            return invalidNullMembers;
        }

        // 複数形の一括検証版。ValidateAnimatorControllerCloneResult自体はAnimatorGraphSchema経由の
        // 再帰で完結するため内部からは呼ばれないが、既存の利用者(テスト等)向けにinternalとして残す。
        internal static IReadOnlyCollection<InvalidNullMember> ValidateAnimatorControllerLayersCloneResult(IEnumerable<AnimatorControllerLayer> target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach (AnimatorControllerLayer acl in target)
            {
                invalidNullMembers.UnionWith(ValidateAnimatorControllerLayerCloneResult(acl, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return invalidNullMembers;
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateAnimatorControllerLayerCloneResult(AnimatorControllerLayer target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (target == null) return new InvalidNullMember[] { new(parent, memberName) };

            // AnimatorControllerLayerはUnityEngine.Objectではないため、次の階層のparentにはなれない。
            // 受け取ったparent(このレイヤー自身の親)をそのまま子要素へ引き継ぐ。
            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach ((string childMemberName, object child) in AnimatorGraphSchema.GetChildren(target))
            {
                invalidNullMembers.UnionWith(ValidateCloneResultDispatch(child, parent, $"{memberName}.{childMemberName}", ref visitedObjSet));
            }
            return invalidNullMembers;
        }

        // 複数形の一括検証版。用途はValidateAnimatorControllerLayersCloneResultと同様(内部の再帰からは呼ばれない)。
        internal static IReadOnlyCollection<InvalidNullMember> ValidateChildAnimatorStateMachinesCloneResult(IEnumerable<ChildAnimatorStateMachine> targets, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach (ChildAnimatorStateMachine target in targets)
            {
                invalidNullMembers.UnionWith(ValidateChildAnimatorStateMachineCloneResult(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return invalidNullMembers;
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateChildAnimatorStateMachineCloneResult(ChildAnimatorStateMachine target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            return ValidateAnimatorStateMachineCloneResult(target.stateMachine, parent, $"{memberName}.{nameof(target.stateMachine)}", ref visitedObjSet);
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateAnimatorStateMachineCloneResult(AnimatorStateMachine target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (target == null) return new InvalidNullMember[] { new(parent, memberName) };

            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidNullMember>();
            visitedObjSet.Add(target);

            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach ((string childMemberName, object child) in AnimatorGraphSchema.GetChildren(target))
            {
                invalidNullMembers.UnionWith(ValidateCloneResultDispatch(child, target, childMemberName, ref visitedObjSet));
            }

            return invalidNullMembers;
        }

        // 複数形の一括検証版。用途はValidateAnimatorControllerLayersCloneResultと同様(内部の再帰からは呼ばれない)。
        internal static IReadOnlyCollection<InvalidNullMember> ValidateChildAnimatorStatesCloneResult(IEnumerable<ChildAnimatorState> targets, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach (ChildAnimatorState target in targets)
            {
                invalidNullMembers.UnionWith(ValidateChildAnimatorStateCloneResult(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return invalidNullMembers;
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateChildAnimatorStateCloneResult(ChildAnimatorState target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            return ValidateAnimatorStateCloneResult(target.state, parent, $"{memberName}.{nameof(target.state)}", ref visitedObjSet);
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateAnimatorStateCloneResult(AnimatorState target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (target == null) return new InvalidNullMember[] { new(parent, memberName) };

            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidNullMember>();
            visitedObjSet.Add(target);

            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach ((string childMemberName, object child) in AnimatorGraphSchema.GetChildren(target))
            {
                invalidNullMembers.UnionWith(ValidateCloneResultDispatch(child, target, childMemberName, ref visitedObjSet));
            }

            return invalidNullMembers;
        }

        // 複数形の一括検証版。用途はValidateAnimatorControllerLayersCloneResultと同様(内部の再帰からは呼ばれない)。
        internal static IReadOnlyCollection<InvalidNullMember> ValidateAnimatorTransitionsCloneResult(IEnumerable<AnimatorTransition> targets, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach (AnimatorTransition target in targets)
            {
                invalidNullMembers.UnionWith(ValidateAnimatorTransitionCloneResult(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return invalidNullMembers;
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateAnimatorTransitionCloneResult(AnimatorTransition target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (target == null) return new InvalidNullMember[] { new(parent, memberName) };

            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidNullMember>();
            visitedObjSet.Add(target);

            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach ((string childMemberName, object child) in AnimatorGraphSchema.GetChildren(target))
            {
                invalidNullMembers.UnionWith(ValidateCloneResultDispatch(child, target, childMemberName, ref visitedObjSet));
            }

            return invalidNullMembers;
        }

        // 複数形の一括検証版。用途はValidateAnimatorControllerLayersCloneResultと同様(内部の再帰からは呼ばれない)。
        internal static IReadOnlyCollection<InvalidNullMember> ValidateAnimatorStateTransitionsCloneResult(IEnumerable<AnimatorStateTransition> targets, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach (AnimatorStateTransition target in targets)
            {
                invalidNullMembers.UnionWith(ValidateAnimatorStateTransitionCloneResult(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return invalidNullMembers;
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateAnimatorStateTransitionCloneResult(AnimatorStateTransition target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (target == null) return new InvalidNullMember[] { new(parent, memberName) };

            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidNullMember>();
            visitedObjSet.Add(target);

            // isExit時はdestinationState/destinationStateMachineが未設定(null)で正常なため、
            // AnimatorGraphSchema.GetChildrenはこの場合子要素を返さない。
            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach ((string childMemberName, object child) in AnimatorGraphSchema.GetChildren(target))
            {
                invalidNullMembers.UnionWith(ValidateCloneResultDispatch(child, target, childMemberName, ref visitedObjSet));
            }

            return invalidNullMembers;
        }

        // 複数形の一括検証版。用途はValidateAnimatorControllerLayersCloneResultと同様(内部の再帰からは呼ばれない)。
        internal static IReadOnlyCollection<InvalidNullMember> ValidateStateMachineBehavioursCloneResult(IEnumerable<StateMachineBehaviour> targets, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach (StateMachineBehaviour target in targets)
            {
                invalidNullMembers.UnionWith(ValidateStateMachineBehaviourCloneResult(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return invalidNullMembers;
        }

        // コアはStateMachineBehaviourの具象型を知らないため、検証すべき子要素は
        // StateMachineBehaviourCloneResultValidatorRegistry経由のプラグインに委ねる。
        // 未登録の型は(プラグイン導入前と同じく)無害な素通りとする。
        private static IReadOnlyCollection<InvalidNullMember> ValidateStateMachineBehaviourCloneResult(StateMachineBehaviour target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (target == null) return new InvalidNullMember[] { new(parent, memberName) };

            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidNullMember>();
            visitedObjSet.Add(target);

            IStateMachineBehaviourCloneResultValidator validator = StateMachineBehaviourCloneResultValidatorRegistry.Shared.Resolve(target.GetType());
            if (validator == null) return Array.Empty<InvalidNullMember>();

            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach ((string childMemberName, object child) in validator.GetChildren(target))
            {
                invalidNullMembers.UnionWith(ValidateCloneResultDispatch(child, target, $"{memberName}.{childMemberName}", ref visitedObjSet));
            }
            return invalidNullMembers;
        }

        /// <summary>
        /// ValidateCloneResultで検出された、無効なnull参照を持つメンバー1件を表します。
        /// </summary>
        public record InvalidNullMember
        {
            /// <summary>無効なnull参照を持っていたメンバーの親オブジェクトを取得します。</summary>
            public UnityEngine.Object Parent { get; }
            /// <summary>無効なnull参照を持っていたメンバー名を取得します。</summary>
            public string MemberName { get; }

            /// <summary>
            /// InvalidNullMemberの新しいインスタンスを初期化します。
            /// </summary>
            /// <param name="parent">無効なnull参照を持っていたメンバーの親オブジェクト。</param>
            /// <param name="memberName">無効なnull参照を持っていたメンバー名。</param>
            public InvalidNullMember(UnityEngine.Object parent, string memberName)
            {
                Parent = parent;
                MemberName = memberName;
            }
        }
    }
}
