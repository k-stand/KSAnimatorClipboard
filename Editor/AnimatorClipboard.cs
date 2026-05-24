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
            AnimatorCopyClip<AnimatorControllerLayer>[] castedClips = clipSet.Clips.Select(c => (AnimatorCopyClip<AnimatorControllerLayer>)c).ToArray();
            foreach (AnimatorCopyClip<AnimatorControllerLayer> clip in castedClips)
            {
                cloner.AddRangeCloneWhiteList(AnimatorClipboardUtility.ListupObjectsInLayer(clip.ClipObject));
            }

            foreach (AnimatorControllerLayer layer in destAnimatorController.layers)
            {
                cloner.AddRangeReferenceHoldingList(AnimatorClipboardUtility.ListupObjectsInLayer(layer));
            }

            AnimatorControllerLayer[] cloneLayers = cloner.CloneAnimatorControllerLayers(castedClips.Select(x => x.ClipObject));

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

            HashSet<object> inScopeObjs = AnimatorClipboardUtility.ListupObjectsInStateMachine(clipSet.AncestorStateMachine);
            inScopeObjs.Add(clipSet.AncestorStateMachine);

            AnimatorCloner cloner = new();
            foreach (AnimatorCopyClipBase clip in clipSet.Clips)
            {
                switch (clip)
                {
                    case AnimatorCopyClip<ChildAnimatorState> castedClip:
                        cloner.AddCloneWhiteList(castedClip.ClipObject.state);
                        break;
                    case AnimatorCopyClip<ChildAnimatorStateMachine> castedClip:
                        cloner.AddCloneWhiteList(castedClip.ClipObject.stateMachine);
                        cloner.AddRangeCloneWhiteList(AnimatorClipboardUtility.ListupObjectsInStateMachine(castedClip.ClipObject.stateMachine));
                        break;
                    case AnimatorCopyClip<AnimatorTransition> castedClip:
                        cloner.AddCloneWhiteList(castedClip.ClipObject);
                        break;
                    case AnimatorCopyClip<AnimatorStateTransition> castedClip:
                        cloner.AddCloneWhiteList(castedClip.ClipObject);
                        break;
                }
            }

            // 貼り付け先がコピー元の祖先のの子孫であるかで処理を変える
            if (inScopeObjs.Contains(destStateMachine))
            {
                cloner.AddRangeReferenceHoldingList(inScopeObjs);
            }
            else
            {
                cloner.AddReferenceHoldingList(destStateMachine);
                cloner.AddRangeReferenceHoldingList(AnimatorClipboardUtility.ListupObjectsInStateMachine(destStateMachine));
            }

            // クリップとそのデータのクローン
            List<AnimatorCopyClip<ChildAnimatorState>> cloneChildAnimatorState = new();
            List<AnimatorCopyClip<ChildAnimatorStateMachine>> cloneChildAnimatorStateMachine = new();
            List<AnimatorCopyClip<AnimatorTransition>> cloneAnimatorTransition = new();
            List<AnimatorCopyClip<AnimatorStateTransition>> cloneAnimatorStateTransition = new();
            foreach (AnimatorCopyClipBase clip in clipSet.Clips)
            {
                switch (clip)
                {
                    case AnimatorCopyClip<ChildAnimatorState> castedClip:
                        cloneChildAnimatorState.Add(CloneClip(castedClip, cloner));
                        break;
                    case AnimatorCopyClip<ChildAnimatorStateMachine> castedClip:
                        cloneChildAnimatorStateMachine.Add(CloneClip(castedClip, cloner));
                        break;
                    case AnimatorCopyClip<AnimatorTransition> castedClip:
                        cloneAnimatorTransition.Add(CloneClip(castedClip, cloner));
                        break;
                    case AnimatorCopyClip<AnimatorStateTransition> castedClip:
                        cloneAnimatorStateTransition.Add(CloneClip(castedClip, cloner));
                        break;
                }
            }

            // ペースト処理
            string destAssetPath = AssetDatabase.GetAssetPath(destStateMachine);
            List<UnityEngine.Object> pastedObjs = new();
            foreach (AnimatorCopyClip<ChildAnimatorStateMachine> cloneClip in cloneChildAnimatorStateMachine)
            {
                if (!destStateMachine.stateMachines.Contains(cloneClip.ClipObject))
                {
                    destStateMachine.stateMachines = new List<ChildAnimatorStateMachine>(destStateMachine.stateMachines) { cloneClip.ClipObject }.ToArray();
                    if (destAssetPath != "")
                    {
                        pastedObjs.AddRange(AnimatorClipboardUtility.AddObjectToAssetRecursively(cloneClip.ClipObject.stateMachine, destAssetPath));
                    }
                }
            }

            foreach (AnimatorCopyClip<ChildAnimatorState> cloneClip in cloneChildAnimatorState)
            {
                if (!destStateMachine.states.Contains(cloneClip.ClipObject))
                {
                    destStateMachine.states = new List<ChildAnimatorState>(destStateMachine.states) { cloneClip.ClipObject }.ToArray();
                    if (destAssetPath != "")
                    {
                        pastedObjs.AddRange(AnimatorClipboardUtility.AddObjectToAssetRecursively(cloneClip.ClipObject.state, destAssetPath));
                    }
                }
            }

            foreach (AnimatorCopyClip<AnimatorTransition> cloneClip in cloneAnimatorTransition)
            {
                if (cloneClip.ClipObject.destinationState == null && cloneClip.ClipObject.destinationStateMachine == null && !cloneClip.ClipObject.isExit)
                {
                    // Transition先が設定できていないなら
                    continue;
                }

                if (cloneClip.TryGetContext(AnimatorCopyClipBase.ContextKey.PropertyName, out object objPropName))
                {
                    string propName = (string)objPropName;

                    // 元がm_StateMachineTransitionsに登録されていたものなら同様に設定する
                    if (propName == AnimatorCopyClipBase.ContextValue.PropertyName.m_StateMachineTransitions &&
                        cloneClip.TryGetContext(AnimatorCopyClipBase.ContextKey.Parent, out object parent) &&
                        destStateMachine.stateMachines.Select(x => x.stateMachine).Contains(parent))
                    {
                        AnimatorTransition[] smTranss = destStateMachine.GetStateMachineTransitions((AnimatorStateMachine)parent);
                        if (!smTranss.Contains(cloneClip.ClipObject))
                        {
                            AnimatorTransition[] newSMTranss = new List<AnimatorTransition>(smTranss) { cloneClip.ClipObject }.ToArray();
                            destStateMachine.SetStateMachineTransitions((AnimatorStateMachine)parent, newSMTranss);
                            if (AnimatorClipboardUtility.CheckAndAddObjectToAsset(cloneClip.ClipObject, destAssetPath))
                            {
                                pastedObjs.Add(cloneClip.ClipObject);
                            }
                            continue;
                        }
                    }

                    // 元がEntryTransitionなら同様に登録する
                    if (propName == AnimatorCopyClipBase.ContextValue.PropertyName.m_EntryTransitions &&
                        !destStateMachine.entryTransitions.Contains(cloneClip.ClipObject))
                    {
                        destStateMachine.entryTransitions = new List<AnimatorTransition>(destStateMachine.entryTransitions) { cloneClip.ClipObject }.ToArray();
                        if (AnimatorClipboardUtility.CheckAndAddObjectToAsset(cloneClip.ClipObject, destAssetPath))
                        {
                            pastedObjs.Add(cloneClip.ClipObject);
                        }
                        continue;
                    }
                }
            }

            foreach (AnimatorCopyClip<AnimatorStateTransition> cloneClip in cloneAnimatorStateTransition)
            {
                if (cloneClip.ClipObject.destinationState == null && cloneClip.ClipObject.destinationStateMachine == null && !cloneClip.ClipObject.isExit)
                {
                    // Transition先が設定できていないなら
                    continue;
                }

                if (cloneClip.TryGetContext(AnimatorCopyClipBase.ContextKey.Parent, out object parent) && parent != null)
                {
                    if (parent is AnimatorState parentState)
                    {
                        if (!parentState.transitions.Contains(cloneClip.ClipObject))
                        {
                            parentState.transitions = new List<AnimatorStateTransition>(parentState.transitions) { cloneClip.ClipObject }.ToArray();
                        }

                        if (AnimatorClipboardUtility.CheckAndAddObjectToAsset(cloneClip.ClipObject, destAssetPath))
                        {
                            pastedObjs.Add(cloneClip.ClipObject);
                        }
                    }
                    else if (parent is AnimatorStateMachine parentStateMachine)
                    {
                        if (!parentStateMachine.anyStateTransitions.Contains(cloneClip.ClipObject))
                        {
                            parentStateMachine.anyStateTransitions = new List<AnimatorStateTransition>(parentStateMachine.anyStateTransitions) { cloneClip.ClipObject }.ToArray();
                        }

                        if (AnimatorClipboardUtility.CheckAndAddObjectToAsset(cloneClip.ClipObject, destAssetPath))
                        {
                            pastedObjs.Add(cloneClip.ClipObject);
                        }
                    }
                }
                else if (cloneClip.TryGetContext(AnimatorCopyClipBase.ContextKey.PropertyName, out object propName) && (string)propName == AnimatorCopyClipBase.ContextValue.PropertyName.m_AnyStateTransitions)
                {
                    if (!destStateMachine.anyStateTransitions.Contains(cloneClip.ClipObject))
                    {
                        destStateMachine.anyStateTransitions = new List<AnimatorStateTransition>(destStateMachine.anyStateTransitions) { cloneClip.ClipObject }.ToArray();
                    }

                    if (AnimatorClipboardUtility.CheckAndAddObjectToAsset(cloneClip.ClipObject, destAssetPath))
                    {
                        pastedObjs.Add(cloneClip.ClipObject);
                    }
                }
            }

            return pastedObjs.ToArray();
        }

        private static AnimatorCopyClip<T> CloneClip<T>(AnimatorCopyClip<T> origClip, AnimatorCloner cloner)
        {
            T cloneCAS = cloner.CloneObject(origClip.ClipObject);
            AnimatorCopyClip<T> cloneClip = origClip.Clone(cloneCAS);
            if (cloneClip.TryGetContext(AnimatorCopyClipBase.ContextKey.Parent, out object parent))
            {
                cloneClip.SetContext(AnimatorCopyClipBase.ContextKey.Parent, cloner.CloneObject(parent));
            }
            return cloneClip;
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
            List<AnimatorCopyClip<StateMachineBehaviour>> castedClips = clipSet.Clips.Cast<AnimatorCopyClip<StateMachineBehaviour>>().ToList();
            foreach (AnimatorCopyClip<StateMachineBehaviour> clip in castedClips)
            {
                if (clip.GenericClipObject != null)
                {
                    cloner.AddCloneWhiteList(clip.ClipObject);
                }
            }

            List<StateMachineBehaviour> cloneBehaviours = new();
            foreach (AnimatorCopyClip<StateMachineBehaviour> clip in castedClips)
            {
                if (clip.GenericClipObject != null)
                {
                    StateMachineBehaviour clone = cloner.CloneStateMachineBehaviour(clip.ClipObject);
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
            if (tType == typeof(ChildAnimatorStateMachine))
            {
                if (clipSet.Type != AnimatorCopyClipSet.AnimatorCopyClipSetType.ChildStateMachine)
                {
                    ThrowInvalidClipSetTypeException(tType, clipSet.Type);
                }
            }
            if (tType == typeof(AnimatorStateTransition))
            {
                if (clipSet.Type != AnimatorCopyClipSet.AnimatorCopyClipSetType.StateTransition)
                {
                    ThrowInvalidClipSetTypeException(tType, clipSet.Type);
                }
            }
            if (tType == typeof(AnimatorTransition))
            {
                if (clipSet.Type != AnimatorCopyClipSet.AnimatorCopyClipSetType.Transition)
                {
                    ThrowInvalidClipSetTypeException(tType, clipSet.Type);
                }
            }

            return ((AnimatorCopyClip<T>)clipSet.Clips.First()).ClipObject;
        }

        private static void ThrowInvalidClipSetTypeException(Type requestType, AnimatorCopyClipSet.AnimatorCopyClipSetType clipSetType) => throw new Exception($"要求された型({requestType.FullName})に対して、ClipSetのデータのタイプ({nameof(AnimatorCopyClipSet.AnimatorCopyClipSetType)}.{clipSetType})が一致しません");

        private static void ThrowInvalidClipSetTypeException(AnimatorCopyClipSet.AnimatorCopyClipSetType requestClipSetType, AnimatorCopyClipSet.AnimatorCopyClipSetType clipSetType) => throw new Exception($"要求されたClipSetのデータのタイプ({nameof(AnimatorCopyClipSet.AnimatorCopyClipSetType)}.{requestClipSetType})に対して、ClipSetのデータのタイプ({nameof(AnimatorCopyClipSet.AnimatorCopyClipSetType)}.{clipSetType})が一致しません");
    }
}