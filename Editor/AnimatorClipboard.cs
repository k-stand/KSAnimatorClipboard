using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace com.github.k_stand.ksanimatorclipboard.editor
{
    public static class AnimatorClipboard
    {
        public static AnimatorCopyClipSet Copy(AnimatorControllerLayer layer, AnimatorController parentController) => new(layer, parentController);
        public static AnimatorCopyClipSet Copy(IEnumerable<AnimatorControllerLayer> layers, AnimatorController parentController) => new(layers, parentController);
        public static AnimatorCopyClipSet Copy(object obj, AnimatorControllerLayer parentLayer) => new(obj, parentLayer);
        public static AnimatorCopyClipSet Copy(IEnumerable<object> objs, AnimatorControllerLayer parentLayer) => new(objs, parentLayer);
        public static AnimatorCopyClipSet Copy(object obj, AnimatorStateMachine ancestorStateMachine) => new(obj, ancestorStateMachine);
        public static AnimatorCopyClipSet Copy(IEnumerable<object> objs, AnimatorStateMachine ancestorStateMachine) => new(objs, ancestorStateMachine);
        public static AnimatorCopyClipSet Copy(Behaviour behaviour) => new(behaviour);
        public static AnimatorCopyClipSet Copy(IEnumerable<Behaviour> behaviours) => new(behaviours);
        public static AnimatorCopyClipSet Copy(object obj) => new(obj);
        public static AnimatorCopyClipSet Copy(IEnumerable<object> objs) => new(objs);

        public static AnimatorControllerLayer[] PasteLayers(AnimatorCopyClipSet clipSet, AnimatorController destAnimatorController)
        {
            if (clipSet.Type != AnimatorCopyClipSet.AnimatorCopyClipSetType.Layers)
            {
                ThrowInvalidClipSetTypeException(AnimatorCopyClipSet.AnimatorCopyClipSetType.Layers, clipSet.Type);
            }

            AnimatorCloner cloner = new();
            foreach (AnimatorCopyClip clip in clipSet.Clips)
            {
                cloner.SetRangeClonePolicy(AnimatorClipboardUtility.ListupObjectsInLayer((AnimatorControllerLayer)clip.Object), AnimatorCloner.ClonePolicy.Clone);
            }

            foreach (AnimatorControllerLayer layer in destAnimatorController.layers)
            {
                cloner.SetRangeClonePolicyIfAbsent(AnimatorClipboardUtility.ListupObjectsInLayer(layer), AnimatorCloner.ClonePolicy.KeepReference);
            }

            AnimatorControllerLayer[] cloneLayers = cloner.CloneAnimatorControllerLayers(clipSet.Clips.Select(x => (AnimatorControllerLayer)x.Object));

            if (clipSet.ParentController != destAnimatorController)
            {
                foreach (AnimatorControllerLayer cloneLayer in cloneLayers)
                {
                    cloneLayer.syncedLayerIndex = -1;
                }
            }

            string destAssetPath = AssetDatabase.GetAssetPath(destAnimatorController);

            List<AnimatorControllerLayer> layerList = new(destAnimatorController.layers);
            layerList.AddRange(cloneLayers);
            destAnimatorController.layers = layerList.ToArray();

            if (destAssetPath != "")
            {
                cloneLayers.ToList().ForEach(x => AnimatorClipboardUtility.AddObjectToAssetRecursively(x.stateMachine, destAssetPath));
            }

            return cloneLayers;
        }

        public static UnityEngine.Object[] PasteIntoLayer(AnimatorCopyClipSet clipSet, AnimatorControllerLayer destLayer) => PasteIntoStateMachine(clipSet, destLayer.stateMachine);

        public static UnityEngine.Object[] PasteIntoStateMachine(AnimatorCopyClipSet clipSet, AnimatorStateMachine destStateMachine)
        {
            if (clipSet.Type != AnimatorCopyClipSet.AnimatorCopyClipSetType.ChildState &&
                clipSet.Type != AnimatorCopyClipSet.AnimatorCopyClipSetType.ChildStateMachine &&
                clipSet.Type != AnimatorCopyClipSet.AnimatorCopyClipSetType.Transition &&
                clipSet.Type != AnimatorCopyClipSet.AnimatorCopyClipSetType.StateTransition &&
                clipSet.Type != AnimatorCopyClipSet.AnimatorCopyClipSetType.InStateMachineObjects)
            {
                ThrowInvalidClipSetTypeException(AnimatorCopyClipSet.AnimatorCopyClipSetType.InStateMachineObjects, clipSet.Type);
            }

            HashSet<UnityEngine.Object> inScopeObjs = AnimatorClipboardUtility.ListupObjectsInStateMachine(clipSet.AncestorStateMachine);
            inScopeObjs.Add(clipSet.AncestorStateMachine);

            AnimatorCloner cloner = new();
            foreach (AnimatorCopyClip clip in clipSet.Clips)
            {
                if (clip.Type == typeof(ChildAnimatorState))
                {
                    cloner.SetClonePolicy(((ChildAnimatorState)clip.Object).state, AnimatorCloner.ClonePolicy.Clone);
                }
                else if (clip.Type == typeof(ChildAnimatorStateMachine))
                {
                    AnimatorStateMachine stateMachine = ((ChildAnimatorStateMachine)clip.Object).stateMachine;
                    cloner.SetClonePolicy(stateMachine, AnimatorCloner.ClonePolicy.Clone);
                    cloner.SetRangeClonePolicy(AnimatorClipboardUtility.ListupObjectsInStateMachine(stateMachine), AnimatorCloner.ClonePolicy.Clone);
                }
                else if (clip.Type == typeof(AnimatorTransition) || clip.Type == typeof(AnimatorStateTransition))
                {
                    cloner.SetClonePolicy((UnityEngine.Object)clip.Object, AnimatorCloner.ClonePolicy.Clone);
                }
            }

            // 貼り付け先がコピー元の祖先自身、もしくはその子孫であるかを確認
            if (inScopeObjs.Contains(destStateMachine))
            {
                // 同レイヤー間でのコピペのはずなので、コピー元との参照を保持できる
                cloner.SetRangeClonePolicyIfAbsent(inScopeObjs, AnimatorCloner.ClonePolicy.KeepReference);
            }
            else
            {
                // 同レイヤー間のコピペである保証が無いので貼り付け先及びその子孫のみを参照を保持する
                cloner.SetClonePolicyIfAbsent(destStateMachine, AnimatorCloner.ClonePolicy.KeepReference);
                cloner.SetRangeClonePolicyIfAbsent(AnimatorClipboardUtility.ListupObjectsInStateMachine(destStateMachine), AnimatorCloner.ClonePolicy.KeepReference);
            }

            // クリップとそのデータのクローン
            List<AnimatorCopyClip> cloneChildAnimatorState = new();
            List<AnimatorCopyClip> cloneChildAnimatorStateMachine = new();
            List<AnimatorCopyClip> cloneAnimatorTransition = new();
            List<AnimatorCopyClip> cloneAnimatorStateTransition = new();
            foreach (AnimatorCopyClip clip in clipSet.Clips)
            {
                AnimatorCopyClip cloneClip = clip.Clone(cloner);
                if (clip.Type == typeof(ChildAnimatorState)) cloneChildAnimatorState.Add(cloneClip);
                else if (clip.Type == typeof(ChildAnimatorStateMachine)) cloneChildAnimatorStateMachine.Add(cloneClip);
                else if (clip.Type == typeof(AnimatorTransition)) cloneAnimatorTransition.Add(cloneClip);
                else if (clip.Type == typeof(AnimatorStateTransition)) cloneAnimatorStateTransition.Add(cloneClip);
            }

            // ペースト処理
            string destAssetPath = AssetDatabase.GetAssetPath(destStateMachine);
            List<UnityEngine.Object> pastedObjs = new();

            List<ChildAnimatorStateMachine> tempStateMachines = new(destStateMachine.stateMachines);
            tempStateMachines.AddRange(cloneChildAnimatorStateMachine.Select(x => (ChildAnimatorStateMachine)x.Object));
            destStateMachine.stateMachines = tempStateMachines.ToArray();
            if (destAssetPath != "")
            {
                foreach (AnimatorStateMachine cloneASM in cloneChildAnimatorStateMachine.Select(x => ((ChildAnimatorStateMachine)x.Object).stateMachine))
                {
                    pastedObjs.AddRange(AnimatorClipboardUtility.AddObjectToAssetRecursively(cloneASM, destAssetPath));
                }
            }

            List<ChildAnimatorState> tempStates = new(destStateMachine.states);
            tempStates.AddRange(cloneChildAnimatorState.Select(x => (ChildAnimatorState)x.Object));
            destStateMachine.states = tempStates.ToArray();
            if (destAssetPath != "")
            {
                foreach (AnimatorState cloneAS in cloneChildAnimatorState.Select(x => ((ChildAnimatorState)x.Object).state))
                {
                    pastedObjs.AddRange(AnimatorClipboardUtility.AddObjectToAssetRecursively(cloneAS, destAssetPath));
                }
            }

            foreach (AnimatorCopyClip cloneClip in cloneAnimatorTransition)
            {
                AnimatorTransition cloneAT = (AnimatorTransition)cloneClip.Object;
                if (cloneAT.destinationState == null && cloneAT.destinationStateMachine == null && !cloneAT.isExit)
                {
                    // Transition先が設定できていないなら
                    continue;
                }

                if (cloneClip.TryGetAnimatorContext(AnimatorCopyClip.ContextKey.PropertyName, out object objPropName))
                {
                    AnimatorCopyClip.ContextValue.PropertyName propName = (AnimatorCopyClip.ContextValue.PropertyName)objPropName;

                    // 元がm_StateMachineTransitionsに登録されていたものなら同様に設定する
                    if (propName == AnimatorCopyClip.ContextValue.PropertyName.m_StateMachineTransitions &&
                        cloneClip.TryGetAnimatorContext(AnimatorCopyClip.ContextKey.Parent, out object parent) &&
                        destStateMachine.stateMachines.Select(x => x.stateMachine).Contains(parent))
                    {
                        AnimatorTransition[] smTranss = destStateMachine.GetStateMachineTransitions((AnimatorStateMachine)parent);
                        AnimatorTransition[] newSMTranss = new List<AnimatorTransition>(smTranss) { cloneAT }.ToArray();
                        destStateMachine.SetStateMachineTransitions((AnimatorStateMachine)parent, newSMTranss);
                        if (AnimatorClipboardUtility.CheckAndAddObjectToAsset(cloneAT, destAssetPath))
                        {
                            pastedObjs.Add(cloneAT);
                        }
                        continue;
                    }

                    // 元がEntryTransitionなら同様に登録する
                    if (propName == AnimatorCopyClip.ContextValue.PropertyName.m_EntryTransitions)
                    {
                        destStateMachine.entryTransitions = new List<AnimatorTransition>(destStateMachine.entryTransitions) { cloneAT }.ToArray();
                        if (AnimatorClipboardUtility.CheckAndAddObjectToAsset(cloneAT, destAssetPath))
                        {
                            pastedObjs.Add(cloneAT);
                        }
                        continue;
                    }
                }
            }

            foreach (AnimatorCopyClip cloneClip in cloneAnimatorStateTransition)
            {
                AnimatorStateTransition cloneAST = (AnimatorStateTransition)cloneClip.Object;
                if (cloneAST.destinationState == null && cloneAST.destinationStateMachine == null && !cloneAST.isExit)
                {
                    // Transition先が設定できていないなら
                    continue;
                }

                if (cloneClip.TryGetAnimatorContext(AnimatorCopyClip.ContextKey.Parent, out object parent) && parent != null)
                {
                    if (parent is AnimatorState parentState)
                    {
                        // 親がStateなら通常のTransitionと解釈
                        if (!parentState.transitions.Contains(cloneAST))
                        {
                            parentState.transitions = new List<AnimatorStateTransition>(parentState.transitions) { cloneAST }.ToArray();

                            if (AnimatorClipboardUtility.CheckAndAddObjectToAsset(cloneAST, destAssetPath))
                            {
                                pastedObjs.Add(cloneAST);
                            }
                        }
                    }
                    else if (parent is AnimatorStateMachine)
                    {
                        // 親がStateMachineならAnyStateTransitionsと解釈
                        if (!destStateMachine.anyStateTransitions.Contains(cloneAST))
                        {
                            destStateMachine.anyStateTransitions = new List<AnimatorStateTransition>(destStateMachine.anyStateTransitions) { cloneAST }.ToArray();

                            if (AnimatorClipboardUtility.CheckAndAddObjectToAsset(cloneAST, destAssetPath))
                            {
                                pastedObjs.Add(cloneAST);
                            }
                        }
                    }
                }
                else if (cloneClip.TryGetAnimatorContext(AnimatorCopyClip.ContextKey.PropertyName, out object propName) &&
                    (AnimatorCopyClip.ContextValue.PropertyName)propName == AnimatorCopyClip.ContextValue.PropertyName.m_AnyStateTransitions)
                {
                    // 親が取得できない(親のClonePolicyがDetachの場合)かつ、
                    // 元のプロパティがAnyStateTransitionだった場合
                    destStateMachine.anyStateTransitions = new List<AnimatorStateTransition>(destStateMachine.anyStateTransitions) { cloneAST }.ToArray();
                    if (AnimatorClipboardUtility.CheckAndAddObjectToAsset(cloneAST, destAssetPath))
                    {
                        pastedObjs.Add(cloneAST);
                    }
                }
            }

            return pastedObjs.ToArray();
        }

        public static StateMachineBehaviour[] PasteBehaviours(AnimatorCopyClipSet clipSet, AnimatorStateMachine destStateMachine)
        {
            StateMachineBehaviour[] cloneBehaviours = CloneBehaviours(clipSet);
            destStateMachine.behaviours = destStateMachine.behaviours.Concat(cloneBehaviours).ToArray();
            return cloneBehaviours;
        }

        public static StateMachineBehaviour[] PasteBehaviours(AnimatorCopyClipSet clipSet, AnimatorState destState)
        {
            StateMachineBehaviour[] cloneBehaviours = CloneBehaviours(clipSet);
            destState.behaviours = destState.behaviours.Concat(cloneBehaviours).ToArray();
            return cloneBehaviours;
        }

        private static StateMachineBehaviour[] CloneBehaviours(AnimatorCopyClipSet clipSet)
        {
            if (clipSet.Type != AnimatorCopyClipSet.AnimatorCopyClipSetType.Behaviours)
            {
                ThrowInvalidClipSetTypeException(AnimatorCopyClipSet.AnimatorCopyClipSetType.Behaviours, clipSet.Type);
            }

            AnimatorCloner cloner = new();
            foreach (AnimatorCopyClip clip in clipSet.Clips)
            {
                if (clip.Object != null)
                {
                    cloner.SetClonePolicy((UnityEngine.Object)clip.Object, AnimatorCloner.ClonePolicy.Clone);
                }
            }

            List<StateMachineBehaviour> cloneBehaviours = new();
            foreach (AnimatorCopyClip clip in clipSet.Clips)
            {
                if (clip.Object != null)
                {
                    StateMachineBehaviour clone = cloner.CloneStateMachineBehaviour((StateMachineBehaviour)clip.Object);
                    cloneBehaviours.Add(clone);
                }
            }

            return cloneBehaviours.ToArray();
        }

        public static void PasteSettings(AnimatorCopyClipSet clipSet, AnimatorState destState) => PasteSettings(ValidateAndGetSingleClipObjectType<ChildAnimatorState>(clipSet), destState);

        private static void PasteSettings(ChildAnimatorState srcChildState, AnimatorState destState) => PasteSettings(srcChildState.state, destState);

        private static void PasteSettings(AnimatorState srcState, AnimatorState destState)
        {
            var backupBehaviours = destState.behaviours;
            var backupName = destState.name;
            var backupTransitions = destState.transitions;

            EditorUtility.CopySerialized(srcState, destState);

            destState.behaviours = backupBehaviours;
            destState.name = backupName;
            destState.transitions = backupTransitions;
        }

        public static void PasteSettings(AnimatorCopyClipSet clipSet, AnimatorTransition destTransition) => PasteSettings(ValidateAndGetSingleClipObjectType<AnimatorTransition>(clipSet), destTransition);

        public static void PasteSettings(AnimatorTransition srcTransition, AnimatorTransition destTransition)
        {
            destTransition.hideFlags = srcTransition.hideFlags;
            destTransition.mute = srcTransition.mute;
            destTransition.solo = srcTransition.solo;
        }

        public static void PasteConditions(AnimatorCopyClipSet clipSet, AnimatorTransition destTransition) => PasteConditions(ValidateAndGetSingleClipObjectType<AnimatorTransition>(clipSet), destTransition);

        public static void PasteConditions(AnimatorTransition srcTransition, AnimatorTransition destTransition) => destTransition.conditions = srcTransition.conditions.ToArray();

        public static void PasteSettingsAndConditions(AnimatorCopyClipSet clipSet, AnimatorTransition destTransition) => PasteSettingsAndConditions(ValidateAndGetSingleClipObjectType<AnimatorTransition>(clipSet), destTransition);

        public static void PasteSettingsAndConditions(AnimatorTransition srcTransition, AnimatorTransition destTransition)
        {
            PasteSettings(srcTransition, destTransition);
            PasteConditions(srcTransition, destTransition);
        }

        public static void PasteSettings(AnimatorCopyClipSet clipSet, AnimatorStateTransition destStateTransition) => PasteSettings(ValidateAndGetSingleClipObjectType<AnimatorStateTransition>(clipSet), destStateTransition);

        public static void PasteSettings(AnimatorStateTransition srcStateTransition, AnimatorStateTransition destStateTransition)
        {
            var backupConditions = destStateTransition.conditions;
            var backupDestinationState = destStateTransition.destinationState;
            var backupDestinationStateMachine = destStateTransition.destinationStateMachine;
            var backupIsExit = destStateTransition.isExit;
            var backupName = destStateTransition.name;

            EditorUtility.CopySerialized(srcStateTransition, destStateTransition);

            destStateTransition.conditions = backupConditions;
            destStateTransition.destinationState = backupDestinationState;
            destStateTransition.destinationStateMachine = backupDestinationStateMachine;
            destStateTransition.isExit = backupIsExit;
            destStateTransition.name = backupName;
        }

        public static void PasteConditions(AnimatorCopyClipSet clipSet, AnimatorStateTransition destStateTransition) => PasteConditions(ValidateAndGetSingleClipObjectType<AnimatorStateTransition>(clipSet), destStateTransition);

        public static void PasteConditions(AnimatorStateTransition srcStateTransition, AnimatorStateTransition destStateTransition) => destStateTransition.conditions = srcStateTransition.conditions.ToArray();

        public static void PasteSettingsAndConditions(AnimatorCopyClipSet clipSet, AnimatorStateTransition destStateTransition) => PasteSettingsAndConditions(ValidateAndGetSingleClipObjectType<AnimatorStateTransition>(clipSet), destStateTransition);

        public static void PasteSettingsAndConditions(AnimatorStateTransition srcStateTransition, AnimatorStateTransition destStateTransition)
        {
            PasteSettings(srcStateTransition, destStateTransition);
            PasteConditions(srcStateTransition, destStateTransition);
        }

        private static T ValidateAndGetSingleClipObjectType<T>(AnimatorCopyClipSet clipSet)
        {
            Type tType = typeof(T);
            if (tType == typeof(ChildAnimatorState))
            {
                if (clipSet.Type != AnimatorCopyClipSet.AnimatorCopyClipSetType.ChildState)
                {
                    ThrowInvalidClipSetTypeException(tType, clipSet.Type);
                }
            }
            else if (tType == typeof(ChildAnimatorStateMachine))
            {
                if (clipSet.Type != AnimatorCopyClipSet.AnimatorCopyClipSetType.ChildStateMachine)
                {
                    ThrowInvalidClipSetTypeException(tType, clipSet.Type);
                }
            }
            else if (tType == typeof(AnimatorStateTransition))
            {
                if (clipSet.Type != AnimatorCopyClipSet.AnimatorCopyClipSetType.StateTransition)
                {
                    ThrowInvalidClipSetTypeException(tType, clipSet.Type);
                }
            }
            else if (tType == typeof(AnimatorTransition))
            {
                if (clipSet.Type != AnimatorCopyClipSet.AnimatorCopyClipSetType.Transition)
                {
                    ThrowInvalidClipSetTypeException(tType, clipSet.Type);
                }
            }

            return (T)clipSet.Clips.First().Object;
        }

        private static void ThrowInvalidClipSetTypeException(Type requestType, AnimatorCopyClipSet.AnimatorCopyClipSetType clipSetType) => throw new Exception($"要求された型({requestType.FullName})に対して、ClipSetのデータのタイプ({nameof(AnimatorCopyClipSet.AnimatorCopyClipSetType)}.{clipSetType})が一致しません");

        private static void ThrowInvalidClipSetTypeException(AnimatorCopyClipSet.AnimatorCopyClipSetType requestClipSetType, AnimatorCopyClipSet.AnimatorCopyClipSetType clipSetType) => throw new Exception($"要求されたClipSetのデータのタイプ({nameof(AnimatorCopyClipSet.AnimatorCopyClipSetType)}.{requestClipSetType})に対して、ClipSetのデータのタイプ({nameof(AnimatorCopyClipSet.AnimatorCopyClipSetType)}.{clipSetType})が一致しません");
    }
}