using System;
using System.Collections.Generic;
using UnityEditor.Animations;

namespace com.github.k_stand.ksanimatorclipboard.editor.Copying
{
    internal sealed class AnimatorCopyObjectKindRegistry
    {
        internal static AnimatorCopyObjectKindRegistry Shared { get; } = CreateDefault();

        private readonly Dictionary<Type, IAnimatorCopyObjectKind> _kinds = new();
        private readonly Dictionary<Type, Func<object, object>> _normalizers = new();

        internal void Register(IAnimatorCopyObjectKind kind) => _kinds[kind.ObjectType] = kind;

        // Normalizeは登録型のexact matchのみで、Resolveのように基底型を辿らない。
        // 現状はAnimatorState/AnimatorStateMachineが対象で、いずれもUnity側の非継承クラスのため問題にならないが、
        // 将来サブクラスを持つ型を正規化対象に加える場合はここを見直すこと。
        internal void RegisterNormalizer(Type sourceType, Func<object, object> normalize) => _normalizers[sourceType] = normalize;

        internal IAnimatorCopyObjectKind Resolve(Type type)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                if (_kinds.TryGetValue(current, out IAnimatorCopyObjectKind kind))
                {
                    return kind;
                }
            }

            return null;
        }

        internal object Normalize(object obj)
        {
            if (obj == null) return null;

            return _normalizers.TryGetValue(obj.GetType(), out Func<object, object> normalize) ? normalize(obj) : obj;
        }

        private static AnimatorCopyObjectKindRegistry CreateDefault()
        {
            AnimatorCopyObjectKindRegistry registry = new();

            registry.Register(new LayerCopyObjectKind());
            registry.Register(new ChildStateCopyObjectKind());
            registry.Register(new ChildStateMachineCopyObjectKind());
            registry.Register(new TransitionCopyObjectKind());
            registry.Register(new StateTransitionCopyObjectKind());
            registry.Register(new StateMachineBehaviourCopyObjectKind());
            registry.Register(new GenericUnityObjectCopyObjectKind());

            registry.RegisterNormalizer(typeof(AnimatorState), obj => new ChildAnimatorState { state = (AnimatorState)obj });
            registry.RegisterNormalizer(typeof(AnimatorStateMachine), obj => new ChildAnimatorStateMachine { stateMachine = (AnimatorStateMachine)obj });

            return registry;
        }
    }
}
