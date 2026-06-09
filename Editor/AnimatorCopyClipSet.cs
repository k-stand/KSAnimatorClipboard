using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace com.github.k_stand.ksanimatorclipboard.editor
{
    public class AnimatorCopyClipSet
    {
        public ReadOnlyCollection<AnimatorCopyClip> Clips { get; private set; }

        private readonly Lazy<AnimatorCopyClipSetType> _type;
        public AnimatorCopyClipSetType Type => _type.Value;

        public AnimatorController ParentController { get; private set; }

        public AnimatorStateMachine AncestorStateMachine { get; private set; }

        internal AnimatorCopyClipSet(AnimatorControllerLayer layer, AnimatorController parentController) : this(new AnimatorControllerLayer[] { layer }, parentController) { }

        internal AnimatorCopyClipSet(IEnumerable<AnimatorControllerLayer> layers, AnimatorController parentController)
        {
            _type = new Lazy<AnimatorCopyClipSetType>(GetClipSetType);

            ClipSetInit(layers);
            if (Type != AnimatorCopyClipSetType.Layers)
            {
                throw new ArgumentException("指定されたオブジェクトが不正です");
            }

            AncestorSetting(layers, parentController);
            ContextsSetting(parentController);
        }

        internal AnimatorCopyClipSet(object obj, AnimatorControllerLayer parentLayer) : this(new object[] { obj }, parentLayer.stateMachine) { }

        internal AnimatorCopyClipSet(IEnumerable<object> objs, AnimatorControllerLayer parentLayer) : this(objs, parentLayer.stateMachine) { }

        internal AnimatorCopyClipSet(object obj, AnimatorStateMachine ancestorStateMachine) : this(new object[] { obj }, ancestorStateMachine) { }

        internal AnimatorCopyClipSet(IEnumerable<object> objs, AnimatorStateMachine ancestorStateMachine)
        {
            _type = new Lazy<AnimatorCopyClipSetType>(GetClipSetType);

            ClipSetInit(objs);
            if (Type != AnimatorCopyClipSetType.ChildState &&
                Type != AnimatorCopyClipSetType.ChildStateMachine &&
                Type != AnimatorCopyClipSetType.Transition &&
                Type != AnimatorCopyClipSetType.StateTransition &&
                Type != AnimatorCopyClipSetType.InStateMachineObjects)
            {
                throw new ArgumentException("指定されたオブジェクトが不正です");
            }

            AncestorSetting(objs, ancestorStateMachine);
            ContextsSetting(ancestorStateMachine);
        }

        internal AnimatorCopyClipSet(Behaviour behaviour) : this(new Behaviour[] { behaviour }) { }

        internal AnimatorCopyClipSet(IEnumerable<Behaviour> behaviours)
        {
            ClipSetInit(behaviours);
            if (Type != AnimatorCopyClipSetType.Behaviours)
            {
                throw new ArgumentException("指定されたオブジェクトが不正です");
            }

            ContextsSetting();
        }

        internal AnimatorCopyClipSet(object obj) : this(new object[] { obj }) { }

        internal AnimatorCopyClipSet(IEnumerable<object> objs)
        {
            ClipSetInit(objs);

            ContextsSetting();
        }

        private AnimatorCopyClipSet(
            IEnumerable<AnimatorCopyClip> clips,
            AnimatorController parentController,
            AnimatorStateMachine ancestorStateMachine)
        {
            Clips = new(clips.ToList());
            ParentController = parentController;
            AncestorStateMachine = ancestorStateMachine;
        }

        public AnimatorCopyClipSet Clone()
        {
            AnimatorCloner cloner = new() { DefaultPolicy = AnimatorCloner.ClonePolicy.KeepReference };
            cloner.SetRangeClonePolicy(Clips.Select(x => x.Object).OfType<UnityEngine.Object>(), AnimatorCloner.ClonePolicy.Clone);
            return Clone(cloner);
        }

        public AnimatorCopyClipSet Clone(AnimatorCloner cloner)
        {
            List<AnimatorCopyClip> cloneClips = new();
            foreach (AnimatorCopyClip clip in Clips)
            {
                AnimatorCopyClip cloneClip = clip.Clone(cloner);
                cloneClips.Add(cloneClip);
            }

            AnimatorController assignParentController = cloner.TryCloneObject(ParentController, out object cloneParentController) ? (AnimatorController)cloneParentController : ParentController;
            AnimatorStateMachine assignAncestorStateMachine = cloner.TryCloneObject(AncestorStateMachine, out object cloneAncestorStateMachine) ? (AnimatorStateMachine)cloneAncestorStateMachine : AncestorStateMachine;
            AnimatorCopyClipSet cloneClipSet = new(cloneClips, assignParentController, assignAncestorStateMachine);

            return cloneClipSet;
        }

        private void ClipSetInit(IEnumerable<object> objs)
        {
            Clips = new(objs.Select(o => CreateClipBase(o)).ToList());
        }

        private void AncestorSetting(IEnumerable<AnimatorControllerLayer> layers, AnimatorController parentController)
        {
            if (parentController != null)
            {
                if (layers.All(l => parentController.layers.Any(pcl => l.stateMachine == pcl.stateMachine)))
                {
                    ParentController = parentController;
                }
                else
                {
                    // TODO:警告を出しつつ処理を続行するパターンで、呼び出し側が気づかない可能性があります。例外にするか、戻り値で検出できる設計にするか検討の余地があります。
                    Debug.LogWarning("指定された親AnimatorControllerに含まれていないAnimatorControllerLayerがコピーされました。\n親AnimatorControllerは未指定状態になります");
                }
            }
        }

        private void AncestorSetting(IEnumerable<object> objs, AnimatorStateMachine ancestorStateMachine)
        {
            if (ancestorStateMachine != null)
            {
                HashSet<object> descendantObjs = new() { ancestorStateMachine };
                descendantObjs.UnionWith(AnimatorClipboardUtility.ListupObjectsInStateMachine(ancestorStateMachine));

                if (
                    objs.All(o => descendantObjs.Contains(o) ||
                        (o is ChildAnimatorState cas && descendantObjs.Contains(cas.state)) ||
                        (o is ChildAnimatorStateMachine casm && descendantObjs.Contains(casm.stateMachine))
                    )
                )
                {
                    AncestorStateMachine = ancestorStateMachine;
                }
                else
                {
                    // TODO:警告を出しつつ処理を続行するパターンで、呼び出し側が気づかない可能性があります。例外にするか、戻り値で検出できる設計にするか検討の余地があります。
                    Debug.LogWarning("指定されたAnimatorStateMachineの子孫に含まれていないオブジェクトがコピーされました。\n先祖AnimatorStateMachineは未指定状態になります");
                }
            }
        }

        private void ContextsSetting(AnimatorController parentController)
        {
            HashSet<object> relatedObjs = new(parentController.layers) { parentController };
            ContextsSettingInternal(relatedObjs);
        }

        private void ContextsSetting(AnimatorStateMachine ancestorStateMachine)
        {
            HashSet<object> relatedObjs = new() { ancestorStateMachine };
            relatedObjs.UnionWith(AnimatorClipboardUtility.ListupObjectsInStateMachine(ancestorStateMachine));
            ContextsSettingInternal(relatedObjs);
        }

        private void ContextsSetting()
        {
            ContextsSettingInternal(Array.Empty<object>());
        }

        private void ContextsSettingInternal(IEnumerable<object> relatedObjs)
        {
            // Clipsを型ごとに仕分ける
            var groupedClips = Clips.GroupBy(c => c.Type);
            //AnimatorCopyClip[] stateClips = groupedClips.Where(g => g.Key == typeof(ChildAnimatorState)).SelectMany(g => g.Select(cb => (AnimatorCopyClip)cb)).ToArray();
            //AnimatorCopyClip[] stateMachineClips = groupedClips.Where(g => g.Key == typeof(ChildAnimatorStateMachine)).SelectMany(g => g.Select(cb => (AnimatorCopyClip)cb)).ToArray();
            AnimatorCopyClip[] transitionClips = groupedClips.Where(g => g.Key == typeof(AnimatorTransition)).SelectMany(g => g.Select(cb => (AnimatorCopyClip)cb)).ToArray();
            AnimatorCopyClip[] stateTransitionClips = groupedClips.Where(g => g.Key == typeof(AnimatorStateTransition)).SelectMany(g => g.Select(cb => (AnimatorCopyClip)cb)).ToArray();
            AnimatorCopyClip[] layerClips = groupedClips.Where(g => g.Key == typeof(AnimatorControllerLayer)).SelectMany(g => g.Select(cb => (AnimatorCopyClip)cb)).ToArray();

            // Clipsの中身を取り出す
            IEnumerable<object> clipObjs = Clips.Select(static x => x.Object switch
                {
                    ChildAnimatorState cas => cas.state,
                    ChildAnimatorStateMachine csam => csam.stateMachine,
                    _ => x.Object,
                });
            // Clipsの中身を含めた全ての関連性のあるオブジェクト
            HashSet<object> totalRelatedObjHashSet = clipObjs.Union(relatedObjs).ToHashSet();
            var groupedObjs = totalRelatedObjHashSet.GroupBy(c => c.GetType());

            AnimatorState[] stateObjs = groupedObjs.Where(g => g.Key == typeof(AnimatorState)).SelectMany(g => g.Select(cb => (AnimatorState)cb)).ToArray();
            AnimatorStateMachine[] stateMachineObjs = groupedObjs.Where(g => g.Key == typeof(AnimatorStateMachine)).SelectMany(g => g.Select(cb => (AnimatorStateMachine)cb)).ToArray();
            //AnimatorTransition[] transitionObjs = groupedObjs.Where(g => g.Key == typeof(AnimatorTransition)).SelectMany(g => g.Select(cb => (AnimatorTransition)cb)).ToArray();
            //AnimatorStateTransition[] stateTransitionObjs = groupedObjs.Where(g => g.Key == typeof(AnimatorStateTransition)).SelectMany(g => g.Select(cb => (AnimatorStateTransition)cb)).ToArray();
            AnimatorController[] animatorControllerObjs = groupedObjs.Where(g => g.Key == typeof(AnimatorController)).SelectMany(g => g.Select(cb => (AnimatorController)cb)).ToArray();

            // 各Clipsに関連のあるオブジェクトや情報をコンテキストとして登録する
            // AnimatorTransition → (親StateMachine, PropertyName) のインデックス
            var transitionParentIndex = new Dictionary<AnimatorTransition, (AnimatorStateMachine Parent, AnimatorCopyClip.ContextValue.PropertyName PropertyName)>();
            foreach (AnimatorStateMachine asm in stateMachineObjs)
            {
                foreach (AnimatorTransition at in asm.entryTransitions)
                {
                    transitionParentIndex[at] = (asm, AnimatorCopyClip.ContextValue.PropertyName.m_EntryTransitions);
                }
                foreach (ChildAnimatorStateMachine csm in asm.stateMachines)
                {
                    foreach (AnimatorTransition at in asm.GetStateMachineTransitions(csm.stateMachine))
                    {
                        transitionParentIndex[at] = (asm, AnimatorCopyClip.ContextValue.PropertyName.m_StateMachineTransitions);
                    }
                }
            }

            // AnimatorStateTransition → 親(StateMachine or State) のインデックス
            var stateTransitionParentIndex = new Dictionary<AnimatorStateTransition, (UnityEngine.Object Parent, AnimatorCopyClip.ContextValue.PropertyName PropertyName)>();
            foreach (AnimatorStateMachine asm in stateMachineObjs)
            {
                foreach (AnimatorStateTransition ast in asm.anyStateTransitions)
                {
                    stateTransitionParentIndex[ast] = (asm, AnimatorCopyClip.ContextValue.PropertyName.m_AnyStateTransitions);
                }
            }
            foreach (AnimatorState state in stateObjs)
            {
                foreach (AnimatorStateTransition ast in state.transitions)
                {
                    stateTransitionParentIndex[ast] = (state, AnimatorCopyClip.ContextValue.PropertyName.m_Transitions);
                }
            }

            // AnimatorControllerLayer → 親AnimatorController のインデックス
            var layerParentIndex = new Dictionary<AnimatorControllerLayer, (AnimatorController Parent, AnimatorCopyClip.ContextValue.PropertyName PropertyName)>();
            foreach (AnimatorController ac in animatorControllerObjs)
            {
                foreach (AnimatorControllerLayer acl in ac.layers)
                {
                    layerParentIndex[acl] = (ac, AnimatorCopyClip.ContextValue.PropertyName.m_AnimatorLayers);
                }
            }


            foreach (AnimatorCopyClip transitionClip in transitionClips)
            {
                if (transitionParentIndex.TryGetValue((AnimatorTransition)transitionClip.Object, out var entry))
                {
                    transitionClip.SetAnimatorContext(AnimatorCopyClip.ContextKey.Parent, entry.Parent);
                    transitionClip.SetAnimatorContext(AnimatorCopyClip.ContextKey.PropertyName, entry.PropertyName);
                }
            }

            foreach (AnimatorCopyClip stateTransitionClip in stateTransitionClips)
            {
                if (stateTransitionParentIndex.TryGetValue((AnimatorStateTransition)stateTransitionClip.Object, out var entry))
                {
                    stateTransitionClip.SetAnimatorContext(AnimatorCopyClip.ContextKey.Parent, entry.Parent);
                    stateTransitionClip.SetAnimatorContext(AnimatorCopyClip.ContextKey.PropertyName, entry.PropertyName);
                }
            }

            foreach (AnimatorCopyClip layerClip in layerClips)
            {
                if (layerParentIndex.TryGetValue((AnimatorControllerLayer)layerClip.Object, out var entry))
                {
                    layerClip.SetAnimatorContext(AnimatorCopyClip.ContextKey.Parent, entry.Parent);
                    layerClip.SetAnimatorContext(AnimatorCopyClip.ContextKey.PropertyName, entry.PropertyName);
                }
            }
        }

        private AnimatorCopyClip CreateClipBase(object obj) => obj switch
        {
            AnimatorControllerLayer castedObj => new AnimatorCopyClip(castedObj),
            AnimatorState castedObj => new AnimatorCopyClip(new ChildAnimatorState() { state = castedObj }),
            AnimatorStateMachine castedObj => new AnimatorCopyClip(new ChildAnimatorStateMachine() { stateMachine = castedObj }),
            ChildAnimatorState castedObj => new AnimatorCopyClip(castedObj),
            ChildAnimatorStateMachine castedObj => new AnimatorCopyClip(castedObj),
            AnimatorTransition castedObj => new AnimatorCopyClip(castedObj),
            AnimatorStateTransition castedObj => new AnimatorCopyClip(castedObj),
            Behaviour castedObj => new AnimatorCopyClip(castedObj),
            _ => new AnimatorCopyClip(obj),
        };

        private AnimatorCopyClipSetType GetClipSetType()
        {
            Type[] containTypes = Clips.Select(x => x.Type).Distinct().ToArray();
            if (containTypes.Length == 1)
            {
                if (containTypes[0] == typeof(AnimatorControllerLayer))
                    return AnimatorCopyClipSetType.Layers;
                if (containTypes[0] == typeof(Behaviour))
                    return AnimatorCopyClipSetType.Behaviours;
                if (containTypes[0] == typeof(ChildAnimatorState) ||
                    containTypes[0] == typeof(ChildAnimatorStateMachine) ||
                    containTypes[0] == typeof(AnimatorTransition) ||
                    containTypes[0] == typeof(AnimatorStateTransition))
                {
                    if (Clips.Count >= 2)
                        // 二つ以上のClipでInStateMachineObjectsになるのはCopySettingなどに対応しているかの区別のため
                        return AnimatorCopyClipSetType.InStateMachineObjects;
                    if (containTypes[0] == typeof(ChildAnimatorState))
                        return AnimatorCopyClipSetType.ChildState;
                    if (containTypes[0] == typeof(ChildAnimatorStateMachine))
                        return AnimatorCopyClipSetType.ChildStateMachine;
                    if (containTypes[0] == typeof(AnimatorTransition))
                        return AnimatorCopyClipSetType.Transition;
                    if (containTypes[0] == typeof(AnimatorStateTransition))
                        return AnimatorCopyClipSetType.StateTransition;
                }

                return AnimatorCopyClipSetType.Other;
            }

            Type[] inLayerTypes = new Type[] { typeof(ChildAnimatorState), typeof(ChildAnimatorStateMachine), typeof(AnimatorTransition), typeof(AnimatorStateTransition) };
            if (2 <= containTypes.Length && containTypes.Length <= inLayerTypes.Length)
            {
                bool allContainInLayerTypes = containTypes.All(t => inLayerTypes.Contains(t));
                if (allContainInLayerTypes)
                    return AnimatorCopyClipSetType.InStateMachineObjects;
                return AnimatorCopyClipSetType.Other;
            }

            return AnimatorCopyClipSetType.Other;
        }

        public enum AnimatorCopyClipSetType
        {
            Layers,
            Transition,
            StateTransition,
            ChildState,
            ChildStateMachine,
            InStateMachineObjects,
            Behaviours,
            Other
        }
    }
}