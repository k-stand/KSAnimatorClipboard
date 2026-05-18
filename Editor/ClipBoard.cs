using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace com.github.k_stand.ksanimatorclipboard.editor
{
    public static class ClipBoard
    {
        public static void Paste(ClipSet clipSet, AnimatorController destAnimatorController)
        {
            if (clipSet.Type != ClipSet.ClipSetType.Layers)
            {
                ThrowPasteInvalidClipSetTypeException();
            }

            AnimatorCloner cloner = new();
            var castedClips = clipSet.Clips.Select(c => (Clip<AnimatorControllerLayer>)c).ToArray();
            foreach (Clip<AnimatorControllerLayer> clip in castedClips)
            {
                cloner.AddRangeCloneWhiteList(AnimatorClipboardUtility.ListupInLayer(clip.ClipObject));
            }

            foreach (AnimatorControllerLayer layer in destAnimatorController.layers)
            {
                cloner.AddRangeReferenceHoldingList(AnimatorClipboardUtility.ListupInLayer(layer));
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
        }

        public static void Paste(ClipSet clipSet, AnimatorControllerLayer destLayer)
        {
            Paste(clipSet, destLayer.stateMachine);
        }

        public static void Paste(ClipSet clipSet, AnimatorStateMachine destStateMachine)
        {
            if (clipSet.Type != ClipSet.ClipSetType.ChildState &&
                clipSet.Type != ClipSet.ClipSetType.ChildStateMachine &&
                clipSet.Type != ClipSet.ClipSetType.Transition &&
                clipSet.Type != ClipSet.ClipSetType.StateTransition &&
                clipSet.Type != ClipSet.ClipSetType.InStateMachineObjects)
            {
                ThrowPasteInvalidClipSetTypeException();
            }

            HashSet<object> inScopeObjs = AnimatorClipboardUtility.ListupInStateMachineObjectsAndSelf(clipSet.AncestorStateMachine);

            AnimatorCloner cloner = new();
            foreach (ClipBase clip in clipSet.Clips)
            {
                switch (clip)
                {
                    case Clip<ChildAnimatorState> castedClip:
                        cloner.AddCloneWhiteList(castedClip.ClipObject.state);
                        break;
                    case Clip<ChildAnimatorStateMachine> castedClip:
                        cloner.AddRangeCloneWhiteList(AnimatorClipboardUtility.ListupInStateMachineObjectsAndSelf(castedClip.ClipObject.stateMachine));
                        break;
                    case Clip<AnimatorTransition> castedClip:
                        cloner.AddCloneWhiteList(castedClip.ClipObject);
                        break;
                    case Clip<AnimatorStateTransition> castedClip:
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
                cloner.AddRangeReferenceHoldingList(AnimatorClipboardUtility.ListupInStateMachineObjectsAndSelf(destStateMachine));
            }

            // クリップとそのデータのクローン
            List<Clip<ChildAnimatorState>> cloneChildAnimatorState = new();
            List<Clip<ChildAnimatorStateMachine>> cloneChildAnimatorStateMachine = new();
            List<Clip<AnimatorTransition>> cloneAnimatorTransition = new();
            List<Clip<AnimatorStateTransition>> cloneAnimatorStateTransition = new();
            foreach (ClipBase clip in clipSet.Clips)
            {
                switch (clip)
                {
                    case Clip<ChildAnimatorState> castedClip:
                        cloneChildAnimatorState.Add(CloneClip(castedClip, cloner));
                        break;
                    case Clip<ChildAnimatorStateMachine> castedClip:
                        cloneChildAnimatorStateMachine.Add(CloneClip(castedClip, cloner));
                        break;
                    case Clip<AnimatorTransition> castedClip:
                        cloneAnimatorTransition.Add(CloneClip(castedClip, cloner));
                        break;
                    case Clip<AnimatorStateTransition> castedClip:
                        cloneAnimatorStateTransition.Add(CloneClip(castedClip, cloner));
                        break;
                }
            }

            // ペースト処理
            string destAssetPath = AssetDatabase.GetAssetPath(destStateMachine);
            List<UnityEngine.Object> pastedObjs = new();
            foreach (Clip<ChildAnimatorStateMachine> cloneClip in cloneChildAnimatorStateMachine)
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

            foreach (Clip<ChildAnimatorState> cloneClip in cloneChildAnimatorState)
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

            foreach (Clip<AnimatorTransition> cloneClip in cloneAnimatorTransition)
            {
                if (cloneClip.ClipObject.destinationState == null && cloneClip.ClipObject.destinationStateMachine == null && !cloneClip.ClipObject.isExit)
                {
                    // Transition先が設定できていないなら
                    continue;
                }

                if (cloneClip.TryGetContext(ClipBase.ContextKey.PropertyName, out object objPropName))
                {
                    string propName = (string)objPropName;

                    // 元がm_StateMachineTransitionsに登録されていたものなら同様に設定する
                    if (propName == ClipBase.ContextValue.PropertyName.m_StateMachineTransitions &&
                        cloneClip.TryGetContext(ClipBase.ContextKey.Parent, out object parent) &&
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
                    if (propName == ClipBase.ContextValue.PropertyName.m_EntryTransitions &&
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

            foreach (Clip<AnimatorStateTransition> cloneClip in cloneAnimatorStateTransition)
            {
                if (cloneClip.ClipObject.destinationState == null && cloneClip.ClipObject.destinationStateMachine == null && !cloneClip.ClipObject.isExit)
                {
                    // Transition先が設定できていないなら
                    continue;
                }

                if (cloneClip.TryGetContext(ClipBase.ContextKey.Parent, out object parent) && parent != null)
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
                else if (cloneClip.TryGetContext(ClipBase.ContextKey.PropertyName, out object propName) && (string)propName == ClipBase.ContextValue.PropertyName.m_AnyStateTransitions)
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
        }

        private static Clip<T> CloneClip<T>(Clip<T> origClip, AnimatorCloner cloner)
        {
            T cloneCAS = cloner.CloneObject(origClip.ClipObject);
            Clip<T> cloneClip = origClip.Clone(cloneCAS);
            if (cloneClip.TryGetContext(ClipBase.ContextKey.Parent, out object parent))
            {
                cloneClip.SetContext(ClipBase.ContextKey.Parent, cloner.CloneObject(parent));
            }
            return cloneClip;
        }

        public static void PasteBehaviours(ClipSet clipSet, AnimatorStateMachine destStateMachine)
        {
            if (clipSet.Type != ClipSet.ClipSetType.Behaviours)
            {
                ThrowPasteInvalidClipSetTypeException();
            }

            AnimatorCloner cloner = new();
            foreach (Clip<StateMachineBehaviour> clip in clipSet.Clips.Cast<Clip<StateMachineBehaviour>>())
            {
                if (clip.GenericClipObject != null)
                {
                    cloner.AddCloneWhiteList(clip.ClipObject);
                }
            }

            foreach (Clip<StateMachineBehaviour> clip in clipSet.Clips.Cast<Clip<StateMachineBehaviour>>())
            {
                if (clip.GenericClipObject != null)
                {
                    StateMachineBehaviour clone = cloner.CloneStateMachineBehaviour(clip.ClipObject);
                    destStateMachine.behaviours = new List<StateMachineBehaviour>(destStateMachine.behaviours) { clone }.ToArray();
                }
            }
        }

        public static void PasteBehaviours(ClipSet clipSet, AnimatorState destState)
        {
            if (clipSet.Type != ClipSet.ClipSetType.Behaviours)
            {
                ThrowPasteInvalidClipSetTypeException();
            }

            AnimatorCloner cloner = new();
            foreach (Clip<StateMachineBehaviour> clip in clipSet.Clips.Cast<Clip<StateMachineBehaviour>>())
            {
                if (clip.GenericClipObject != null)
                {
                    cloner.AddCloneWhiteList(clip.ClipObject);
                }
            }

            foreach (Clip<StateMachineBehaviour> clip in clipSet.Clips.Cast<Clip<StateMachineBehaviour>>())
            {
                if (clip.GenericClipObject != null)
                {
                    StateMachineBehaviour clone = cloner.CloneStateMachineBehaviour(clip.ClipObject);
                    destState.behaviours = new List<StateMachineBehaviour>(destState.behaviours) { clone }.ToArray();
                }
            }
        }

        public static void PasteSettings(ClipSet clipSet, AnimatorState destState)
        {
            if (clipSet.Type != ClipSet.ClipSetType.ChildState)
            {
                ThrowPasteInvalidClipSetTypeException();
            }

            AnimatorState state = ((Clip<ChildAnimatorState>)clipSet.Clips.First()).ClipObject.state;

            destState.cycleOffset = state.cycleOffset;
            destState.cycleOffsetParameter = state.cycleOffsetParameter;
            destState.cycleOffsetParameterActive = state.cycleOffsetParameterActive;
            destState.iKOnFeet = state.iKOnFeet;
            destState.mirror = state.mirror;
            destState.mirrorParameter = state.mirrorParameter;
            destState.mirrorParameterActive = state.mirrorParameterActive;
            destState.motion = state.motion;
            destState.speed = state.speed;
            destState.speedParameter = state.speedParameter;
            destState.speedParameterActive = state.speedParameterActive;
            destState.tag = state.tag;
            destState.timeParameter = state.timeParameter;
            destState.timeParameterActive = state.timeParameterActive;
            destState.writeDefaultValues = state.writeDefaultValues;
        }

        public static void PasteSettings(ClipSet clipSet, AnimatorTransition destTransition)
        {
            AnimatorTransition srcTransition = GetAnimatorTransition(clipSet);
            PasteSettings(srcTransition, destTransition);
        }

        public static void PasteSettings(AnimatorTransition srcTransition, AnimatorTransition destTransition)
        {
            destTransition.mute = srcTransition.mute;
            destTransition.solo = srcTransition.solo;
        }

        public static void PasteConditions(ClipSet clipSet, AnimatorTransition destTransition)
        {
            AnimatorTransition srcTransition = GetAnimatorTransition(clipSet);
            PasteConditions(srcTransition, destTransition);
        }

        public static void PasteConditions(AnimatorTransition srcTransition, AnimatorTransition destTransition)
        {
            destTransition.conditions = srcTransition.conditions.ToArray();
        }

        public static void PasteSettingsAndConditions(ClipSet clipSet, AnimatorTransition destTransition)
        {
            AnimatorTransition srcTransition = GetAnimatorTransition(clipSet);
            PasteSettingsAndConditions(srcTransition, destTransition);
        }

        public static void PasteSettingsAndConditions(AnimatorTransition srcTransition, AnimatorTransition destTransition)
        {
            PasteSettings(srcTransition, destTransition);
            PasteConditions(srcTransition, destTransition);
        }

        public static AnimatorTransition GetAnimatorTransition(ClipSet clipSet)
        {
            if (clipSet.Type != ClipSet.ClipSetType.Transition)
            {
                ThrowPasteInvalidClipSetTypeException();
            }
            return ((Clip<AnimatorTransition>)clipSet.Clips.First()).ClipObject;
        }

        public static void PasteSettings(ClipSet clipSet, AnimatorStateTransition destStateTransition)
        {
            AnimatorStateTransition srcStateTransition = GetAnimatorStateTransition(clipSet);
            PasteSettings(srcStateTransition, destStateTransition);
        }

        public static void PasteSettings(AnimatorStateTransition srcStateTransition, AnimatorStateTransition destStateTransition)
        {
            destStateTransition.canTransitionToSelf = srcStateTransition.canTransitionToSelf;
            destStateTransition.duration = srcStateTransition.duration;
            destStateTransition.exitTime = srcStateTransition.exitTime;
            destStateTransition.hasExitTime = srcStateTransition.hasExitTime;
            destStateTransition.hasFixedDuration = srcStateTransition.hasFixedDuration;
            destStateTransition.interruptionSource = srcStateTransition.interruptionSource;
            destStateTransition.mute = srcStateTransition.mute;
            destStateTransition.offset = srcStateTransition.offset;
            destStateTransition.orderedInterruption = srcStateTransition.orderedInterruption;
            destStateTransition.solo = srcStateTransition.solo;
        }

        public static void PasteConditions(ClipSet clipSet, AnimatorStateTransition destStateTransition)
        {
            AnimatorStateTransition srcStateTransition = GetAnimatorStateTransition(clipSet);
            PasteConditions(srcStateTransition, destStateTransition);
        }

        public static void PasteConditions(AnimatorStateTransition srcStateTransition, AnimatorStateTransition destStateTransition)
        {
            destStateTransition.conditions = srcStateTransition.conditions.ToArray();
        }

        public static void PasteSettingsAndConditions(ClipSet clipSet, AnimatorStateTransition destStateTransition)
        {
            AnimatorStateTransition srcStateTransition = GetAnimatorStateTransition(clipSet);
            PasteSettingsAndConditions(srcStateTransition, destStateTransition);
        }

        public static void PasteSettingsAndConditions(AnimatorStateTransition srcStateTransition, AnimatorStateTransition destStateTransition)
        {
            PasteSettings(srcStateTransition, destStateTransition);
            PasteConditions(srcStateTransition, destStateTransition);
        }

        private static AnimatorStateTransition GetAnimatorStateTransition(ClipSet clipSet)
        {
            if (clipSet.Type != ClipSet.ClipSetType.StateTransition)
            {
                ThrowPasteInvalidClipSetTypeException();
            }
            return ((Clip<AnimatorStateTransition>)clipSet.Clips.First()).ClipObject;
        }

        private static void ThrowPasteInvalidClipSetTypeException()
        {
            throw new Exception("貼り付け先に対して貼り付けるデータのタイプが不正です");
        }
    }
}