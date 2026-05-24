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
        public ReadOnlyCollection<AnimatorCopyClipBase> Clips { get; private set; }

        public AnimatorCopyClipSetType Type { get; private set; }

        public AnimatorController ParentController { get; private set; }

        public AnimatorStateMachine AncestorStateMachine { get; private set; }

        internal AnimatorCopyClipSet(AnimatorControllerLayer layer, AnimatorController parentController) : this(new AnimatorControllerLayer[] { layer }, parentController) { }

        internal AnimatorCopyClipSet(IEnumerable<AnimatorControllerLayer> layers, AnimatorController parentController)
        {
            ClipSetInit(layers);
            if (Type != AnimatorCopyClipSetType.Layers)
            {
                throw new Exception("指定されたオブジェクトが不正です");
            }

            AncestorSetting(layers, parentController);
            ContextsSetting(parentController);
        }

        internal AnimatorCopyClipSet(object obj, AnimatorControllerLayer parentLayer) : this(new object[] { obj }, parentLayer.stateMachine) { }

        internal AnimatorCopyClipSet(IEnumerable<object> objs, AnimatorControllerLayer parentLayer) : this(objs, parentLayer.stateMachine) { }

        internal AnimatorCopyClipSet(object obj, AnimatorStateMachine ancestorStateMachine) : this(new object[] { obj }, ancestorStateMachine) { }

        internal AnimatorCopyClipSet(IEnumerable<object> objs, AnimatorStateMachine ancestorStateMachine)
        {
            ClipSetInit(objs);
            if (Type != AnimatorCopyClipSetType.ChildState &&
                Type != AnimatorCopyClipSetType.ChildStateMachine &&
                Type != AnimatorCopyClipSetType.Transition &&
                Type != AnimatorCopyClipSetType.StateTransition &&
                Type != AnimatorCopyClipSetType.InStateMachineObjects)
            {
                throw new Exception("指定されたオブジェクトが不正です");
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
                throw new Exception("指定されたオブジェクトが不正です");
            }

            ContextsSetting();
        }

        internal AnimatorCopyClipSet(object obj) : this(new object[] { obj }) { }

        internal AnimatorCopyClipSet(IEnumerable<object> objs)
        {
            ClipSetInit(objs);

            ContextsSetting();
        }

        private void ClipSetInit(IEnumerable<object> objs)
        {
            Clips = new(objs.Select(o => CreateClipBase(o)).ToList());
            Type = GetClipSetType();
        }

        private void AncestorSetting(IEnumerable<AnimatorControllerLayer> layers, AnimatorController parentController)
        {
            if (parentController != null)
            {
                if (layers.All(l => parentController.layers.Any(pl => IsEqualLayer(l, pl))))
                {
                    ParentController = parentController;
                }
                else
                {
                    Debug.LogWarning("指定された親AnimatorControllerに含まれていないAnimatorControllerLayerがコピーされました。\n親AnimatorControllerは未指定状態になります");
                }
            }
        }

        private bool IsEqualLayer(AnimatorControllerLayer layer1, AnimatorControllerLayer layer2)
        {
            return layer1.avatarMask == layer2.avatarMask &&
                layer1.blendingMode == layer2.blendingMode &&
                layer1.defaultWeight == layer2.defaultWeight &&
                layer1.iKPass == layer2.iKPass &&
                layer1.name == layer2.name &&
                layer1.stateMachine == layer2.stateMachine &&
                layer1.syncedLayerAffectsTiming == layer2.syncedLayerAffectsTiming &&
                layer1.syncedLayerIndex == layer2.syncedLayerIndex;
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
            AnimatorCopyClip<ChildAnimatorState>[] stateClips = groupedClips.Where(g => g.Key == typeof(ChildAnimatorState)).SelectMany(g => g.Select(cb => (AnimatorCopyClip<ChildAnimatorState>)cb)).ToArray();
            AnimatorCopyClip<ChildAnimatorStateMachine>[] stateMachineClips = groupedClips.Where(g => g.Key == typeof(ChildAnimatorStateMachine)).SelectMany(g => g.Select(cb => (AnimatorCopyClip<ChildAnimatorStateMachine>)cb)).ToArray();
            AnimatorCopyClip<AnimatorTransition>[] transitionClips = groupedClips.Where(g => g.Key == typeof(AnimatorTransition)).SelectMany(g => g.Select(cb => (AnimatorCopyClip<AnimatorTransition>)cb)).ToArray();
            AnimatorCopyClip<AnimatorStateTransition>[] stateTransitionClips = groupedClips.Where(g => g.Key == typeof(AnimatorStateTransition)).SelectMany(g => g.Select(cb => (AnimatorCopyClip<AnimatorStateTransition>)cb)).ToArray();
            AnimatorCopyClip<AnimatorControllerLayer>[] layerClips = groupedClips.Where(g => g.Key == typeof(AnimatorControllerLayer)).SelectMany(g => g.Select(cb => (AnimatorCopyClip<AnimatorControllerLayer>)cb)).ToArray();

            // Clipsの中身を取り出す
            IEnumerable<object> clipObjs = Clips.Select(static x => x.GenericClipObject switch
                {
                    ChildAnimatorState cas => cas.state,
                    ChildAnimatorStateMachine csam => csam.stateMachine,
                    _ => x.GenericClipObject,
                });

            // Clipsの中身を含めた全ての関連性のあるオブジェクト
            HashSet<object> totalRelatedObjHashSet = clipObjs.Union(relatedObjs).ToHashSet();
            var groupedObjs = totalRelatedObjHashSet.GroupBy(c => c.GetType());
            AnimatorState[] stateObjs = groupedObjs.Where(g => g.Key == typeof(AnimatorState)).SelectMany(g => g.Select(cb => (AnimatorState)cb)).ToArray();
            AnimatorStateMachine[] stateMachineObjs = groupedObjs.Where(g => g.Key == typeof(AnimatorStateMachine)).SelectMany(g => g.Select(cb => (AnimatorStateMachine)cb)).ToArray();
            AnimatorTransition[] transitionObjs = groupedObjs.Where(g => g.Key == typeof(AnimatorTransition)).SelectMany(g => g.Select(cb => (AnimatorTransition)cb)).ToArray();
            AnimatorStateTransition[] stateTransitionObjs = groupedObjs.Where(g => g.Key == typeof(AnimatorStateTransition)).SelectMany(g => g.Select(cb => (AnimatorStateTransition)cb)).ToArray();
            AnimatorController[] animatorControllerObjs = groupedObjs.Where(g => g.Key == typeof(AnimatorController)).SelectMany(g => g.Select(cb => (AnimatorController)cb)).ToArray();

            // 各Clipsに関連のあるオブジェクトや情報をコンテキストとして登録する
            foreach (AnimatorCopyClip<AnimatorTransition> transitionClip in transitionClips)
            {
                bool doBreak = false;
                foreach (AnimatorStateMachine stateMachineObj in stateMachineObjs)
                {
                    if (stateMachineObj.entryTransitions.Contains(transitionClip.ClipObject))
                    {
                        transitionClip.SetContext(AnimatorCopyClipBase.ContextKey.Parent, stateMachineObj);
                        transitionClip.SetContext(AnimatorCopyClipBase.ContextKey.PropertyName, AnimatorCopyClipBase.ContextValue.PropertyName.m_EntryTransitions);
                        break;
                    }

                    foreach (ChildAnimatorStateMachine innerChildStateMachine in stateMachineObj.stateMachines)
                    {
                        AnimatorTransition[] transitions = stateMachineObj.GetStateMachineTransitions(innerChildStateMachine.stateMachine);

                        if (transitions.Contains(transitionClip.ClipObject))
                        {
                            transitionClip.SetContext(AnimatorCopyClipBase.ContextKey.Parent, stateMachineObj);
                            transitionClip.SetContext(AnimatorCopyClipBase.ContextKey.PropertyName, AnimatorCopyClipBase.ContextValue.PropertyName.m_StateMachineTransitions);
                            doBreak = true;
                            break;
                        }
                    }
                    if (doBreak) break;
                }
            }

            foreach (AnimatorCopyClip<AnimatorStateTransition> stateTransitionClip in stateTransitionClips)
            {
                foreach (AnimatorStateMachine stateMachineObj in stateMachineObjs)
                {
                    if (stateMachineObj.anyStateTransitions.Contains(stateTransitionClip.ClipObject))
                    {
                        stateTransitionClip.SetContext(AnimatorCopyClipBase.ContextKey.Parent, stateMachineObj);
                        stateTransitionClip.SetContext(AnimatorCopyClipBase.ContextKey.PropertyName, AnimatorCopyClipBase.ContextValue.PropertyName.m_AnyStateTransitions);
                        break;
                    }
                }
                foreach (AnimatorState stateObj in stateObjs)
                {
                    if (stateObj.transitions.Contains(stateTransitionClip.ClipObject))
                    {
                        stateTransitionClip.SetContext(AnimatorCopyClipBase.ContextKey.Parent, stateObj);
                        break;
                    }
                }
            }

            foreach (AnimatorCopyClip<AnimatorControllerLayer> layerClip in layerClips)
            {
                foreach (AnimatorController animatorControllerObj in animatorControllerObjs)
                {
                    if (animatorControllerObj.layers.Contains(layerClip.ClipObject))
                    {
                        layerClip.SetContext(AnimatorCopyClipBase.ContextKey.Parent, animatorControllerObj);
                        break;
                    }
                }
            }
        }

        private AnimatorCopyClipBase CreateClipBase(object obj) => obj switch
        {
            AnimatorControllerLayer castedObj => new AnimatorCopyClip<AnimatorControllerLayer>(castedObj),
            AnimatorState castedObj => new AnimatorCopyClip<ChildAnimatorState>(new() { state = castedObj }),
            AnimatorStateMachine castedObj => new AnimatorCopyClip<ChildAnimatorStateMachine>(new() { stateMachine = castedObj }),
            ChildAnimatorState castedObj => new AnimatorCopyClip<ChildAnimatorState>(castedObj),
            ChildAnimatorStateMachine castedObj => new AnimatorCopyClip<ChildAnimatorStateMachine>(castedObj),
            AnimatorTransition castedObj => new AnimatorCopyClip<AnimatorTransition>(castedObj),
            AnimatorStateTransition castedObj => new AnimatorCopyClip<AnimatorStateTransition>(castedObj),
            Behaviour castedObj => new AnimatorCopyClip<Behaviour>(castedObj),
            _ => new AnimatorCopyClip<object>(obj),
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