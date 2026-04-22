using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace io.github.kiriumestand.animatorclipboard.editor
{
    public class ClipSet
    {
        public ClipBase[] Clips { get; private set; }

        public ClipSetType Type { get; private set; }

        public AnimatorController ParentController { get; private set; }

        public AnimatorStateMachine AncestorStateMachine { get; private set; }

        public ClipSet(AnimatorControllerLayer layer, AnimatorController parentController) : this(new AnimatorControllerLayer[] { layer }, parentController) { }

        public ClipSet(IEnumerable<AnimatorControllerLayer> layers, AnimatorController parentController)
        {
            ClipSetInit(layers);
            if (Type != ClipSetType.Layers)
            {
                throw new Exception("指定されたオブジェクトが不正です");
            }

            AncestorSetting(layers, parentController);
            ContextsSetting(parentController);
        }

        public ClipSet(object obj, AnimatorControllerLayer parentLayer) : this(new object[] { obj }, parentLayer.stateMachine) { }

        public ClipSet(IEnumerable<object> objs, AnimatorControllerLayer parentLayer) : this(objs, parentLayer.stateMachine) { }

        public ClipSet(object obj, AnimatorStateMachine ancestorStateMachine) : this(new object[] { obj }, ancestorStateMachine) { }

        public ClipSet(IEnumerable<object> objs, AnimatorStateMachine ancestorStateMachine)
        {
            ClipSetInit(objs);
            if (Type != ClipSetType.ChildState &&
                Type != ClipSetType.ChildStateMachine &&
                Type != ClipSetType.Transition &&
                Type != ClipSetType.StateTransition &&
                Type != ClipSetType.InStateMachineObjects)
            {
                throw new Exception("指定されたオブジェクトが不正です");
            }

            AncestorSetting(objs, ancestorStateMachine);
            ContextsSetting(ancestorStateMachine);
        }

        public ClipSet(Behaviour behaviour) : this(new Behaviour[] { behaviour }) { }

        public ClipSet(IEnumerable<Behaviour> behaviours)
        {
            ClipSetInit(behaviours);
            if (Type != ClipSetType.Behaviours)
            {
                throw new Exception("指定されたオブジェクトが不正です");
            }

            ContextsSetting();
        }

        public ClipSet(object obj) : this(new object[] { obj }) { }

        public ClipSet(IEnumerable<object> objs)
        {
            ClipSetInit(objs);

            ContextsSetting();
        }

        private void ClipSetInit(IEnumerable<object> objs)
        {
            Clips = objs.Select(o => CreateClipBase(o)).ToArray();
            Type = GetClipSetType();
        }

        private void AncestorSetting(IEnumerable<AnimatorControllerLayer> layers, AnimatorController parentController)
        {
            if (parentController != null)
            {
                if (layers.All(l => parentController.layers.Contains(l)))
                {
                    ParentController = parentController;
                }
                else
                {
                    Debug.LogWarning("指定された親AnimatorControllerに含まれていないAnimatorControllerLayerがコピーされました。\n親AnimatorControllerは未指定状態になります");
                }
            }
        }

        private void AncestorSetting(IEnumerable<object> objs, AnimatorStateMachine ancestorStateMachine)
        {
            if (ancestorStateMachine != null)
            {
                HashSet<object> descendantObjs = AnimatorClipboardUtility.ListupInStateMachineObjectsAndSelf(ancestorStateMachine);

                if (objs.All(o => descendantObjs.Contains(o)))
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
            HashSet<object> objs = new(parentController.layers) { parentController };
            ContextsSettingInternal(objs);
        }

        private void ContextsSetting(AnimatorStateMachine ancestorStateMachine)
        {
            HashSet<object> objs = AnimatorClipboardUtility.ListupInStateMachineObjectsAndSelf(ancestorStateMachine);
            ContextsSettingInternal(objs);
        }

        private void ContextsSetting()
        {
            ContextsSettingInternal(Array.Empty<object>());
        }

        private void ContextsSettingInternal(IEnumerable<object> objs)
        {
            var groupedClips = Clips.GroupBy(c => c.Type);
            Clip<ChildAnimatorState>[] stateClips = (Clip<ChildAnimatorState>[])groupedClips.Where(g => g.Key == typeof(ChildAnimatorState)).SelectMany(g => g).ToArray();
            Clip<ChildAnimatorStateMachine>[] stateMachineClips = (Clip<ChildAnimatorStateMachine>[])groupedClips.Where(g => g.Key == typeof(ChildAnimatorStateMachine)).SelectMany(g => g).ToArray();
            Clip<AnimatorTransition>[] transitionClips = (Clip<AnimatorTransition>[])groupedClips.Where(g => g.Key == typeof(AnimatorTransition)).SelectMany(g => g).ToArray();
            Clip<AnimatorStateTransition>[] stateTransitionClips = (Clip<AnimatorStateTransition>[])groupedClips.Where(g => g.Key == typeof(AnimatorStateTransition)).SelectMany(g => g).ToArray();
            Clip<AnimatorControllerLayer>[] layerClips = (Clip<AnimatorControllerLayer>[])groupedClips.Where(g => g.Key == typeof(AnimatorControllerLayer)).SelectMany(g => g).ToArray();

            IEnumerable<object> clipObjs = Clips.Select(static x => x.GenericClipObject switch
                {
                    ChildAnimatorState cas => cas.state,
                    ChildAnimatorStateMachine csam => csam.stateMachine,
                    _ => x.GenericClipObject,
                });

            HashSet<object> totalObjHashSet = clipObjs.Union(objs).ToHashSet();
            var groupedObjs = totalObjHashSet.GroupBy(c => c.GetType());
            AnimatorState[] stateObjs = (AnimatorState[])groupedObjs.Where(g => g.Key == typeof(AnimatorState)).SelectMany(g => g).ToArray();
            AnimatorStateMachine[] stateMachineObjs = (AnimatorStateMachine[])groupedObjs.Where(g => g.Key == typeof(AnimatorStateMachine)).SelectMany(g => g).ToArray();
            AnimatorTransition[] transitionObjs = (AnimatorTransition[])groupedObjs.Where(g => g.Key == typeof(AnimatorTransition)).SelectMany(g => g).ToArray();
            AnimatorStateTransition[] stateTransitionObjs = (AnimatorStateTransition[])groupedObjs.Where(g => g.Key == typeof(AnimatorStateTransition)).SelectMany(g => g).ToArray();
            AnimatorController[] animatorControllerObjs = (AnimatorController[])groupedObjs.Where(g => g.Key == typeof(AnimatorController)).SelectMany(g => g).ToArray();

            foreach (Clip<AnimatorTransition> transitionClip in transitionClips)
            {
                foreach (AnimatorStateMachine stateMachineObj in stateMachineObjs)
                {
                    if (stateMachineObj.entryTransitions.Contains(transitionClip.ClipObject))
                    {
                        transitionClip.SetContext(ClipBase.ContextKey.Parent, stateMachineObj);
                        break;
                    }
                }
            }

            foreach (Clip<AnimatorStateTransition> stateTransitionClip in stateTransitionClips)
            {
                foreach (AnimatorStateMachine stateMachineObj in stateMachineObjs)
                {
                    if (stateMachineObj.anyStateTransitions.Contains(stateTransitionClip.ClipObject))
                    {
                        stateTransitionClip.SetContext(ClipBase.ContextKey.Parent, stateMachineObj);
                        break;
                    }
                }
                foreach (AnimatorState stateObj in stateObjs)
                {
                    if (stateObj.transitions.Contains(stateTransitionClip.ClipObject))
                    {
                        stateTransitionClip.SetContext(ClipBase.ContextKey.Parent, stateObj);
                        break;
                    }
                }
            }

            foreach (Clip<AnimatorControllerLayer> layerClip in layerClips)
            {
                foreach (AnimatorController animatorControllerObj in animatorControllerObjs)
                {
                    if (animatorControllerObj.layers.Contains(layerClip.ClipObject))
                    {
                        layerClip.SetContext(ClipBase.ContextKey.Parent, animatorControllerObj);
                        break;
                    }
                }
            }
        }

        private ClipBase CreateClipBase(object obj) => obj switch
        {
            AnimatorControllerLayer castedObj => new Clip<AnimatorControllerLayer>(castedObj),
            AnimatorState castedObj => new Clip<ChildAnimatorState>(new() { state = castedObj }),
            AnimatorStateMachine castedObj => new Clip<ChildAnimatorStateMachine>(new() { stateMachine = castedObj }),
            ChildAnimatorState castedObj => new Clip<ChildAnimatorState>(castedObj),
            ChildAnimatorStateMachine castedObj => new Clip<ChildAnimatorStateMachine>(castedObj),
            AnimatorTransition castedObj => new Clip<AnimatorTransition>(castedObj),
            AnimatorStateTransition castedObj => new Clip<AnimatorStateTransition>(castedObj),
            Behaviour castedObj => new Clip<Behaviour>(castedObj),
            _ => new Clip<object>(obj),
        };

        private ClipSetType GetClipSetType()
        {
            Type[] containTypes = Clips.Select(x => x.Type).Distinct().ToArray();
            if (containTypes.Length == 1)
            {
                if (containTypes[0] == typeof(AnimatorControllerLayer))
                    return ClipSetType.Layers;
                if (containTypes[0] == typeof(Behaviour))
                    return ClipSetType.Behaviours;
                if (containTypes[0] == typeof(ChildAnimatorState) ||
                    containTypes[0] == typeof(ChildAnimatorStateMachine) ||
                    containTypes[0] == typeof(AnimatorTransition) ||
                    containTypes[0] == typeof(AnimatorStateTransition))
                {
                    if (Clips.Length >= 2)
                        return ClipSetType.InStateMachineObjects;
                    if (containTypes[0] == typeof(ChildAnimatorState))
                        return ClipSetType.ChildState;
                    if (containTypes[0] == typeof(ChildAnimatorStateMachine))
                        return ClipSetType.ChildStateMachine;
                    if (containTypes[0] == typeof(AnimatorTransition))
                        return ClipSetType.Transition;
                    if (containTypes[0] == typeof(AnimatorStateTransition))
                        return ClipSetType.StateTransition;
                }

                return ClipSetType.Other;
            }

            Type[] inLayerTypes = new Type[] { typeof(ChildAnimatorState), typeof(ChildAnimatorStateMachine), typeof(AnimatorTransition), typeof(AnimatorStateTransition) };
            if (2 <= containTypes.Length && containTypes.Length <= inLayerTypes.Length)
            {
                bool allContainInLayerTypes = containTypes.All(t => inLayerTypes.Contains(t));
                if (allContainInLayerTypes)
                    return ClipSetType.InStateMachineObjects;
                return ClipSetType.Other;
            }

            return ClipSetType.Other;
        }

        public enum ClipSetType
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