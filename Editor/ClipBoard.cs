using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

namespace io.github.kiriumestand.animatorclipboard.editor
{
    public class ClipBoard
    {
        public void Paste(ClipSet clipSet, AnimatorController destAnimatorController)
        {
            if (clipSet.Type != ClipSet.ClipSetType.Layers)
            {
                ThrowPasteInvalidClipSetTypeException();
            }

            AnimatorCloner cloner = new();
            var castedClips = (Clip<AnimatorControllerLayer>[])clipSet.Clips;
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

            List<AnimatorControllerLayer> layerList = new(destAnimatorController.layers);
            layerList.AddRange(cloneLayers);
            destAnimatorController.layers = layerList.ToArray();
        }

        public void Paste(ClipSet clipSet, AnimatorControllerLayer destLayer)
        {
            Paste(clipSet, destLayer.stateMachine);
        }

        public void Paste(ClipSet clipSet, AnimatorStateMachine destStateMachine)
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

            List<object> pastedObjs = new();
            foreach (Clip<ChildAnimatorState> cloneClip in cloneChildAnimatorState)
            {
                if (!destStateMachine.states.Contains(cloneClip.ClipObject))
                {
                    destStateMachine.states = new List<ChildAnimatorState>(destStateMachine.states) { cloneClip.ClipObject }.ToArray();
                    pastedObjs.Add(cloneClip.ClipObject);
                }
            }

            foreach (Clip<ChildAnimatorStateMachine> cloneClip in cloneChildAnimatorStateMachine)
            {
                if (!destStateMachine.stateMachines.Contains(cloneClip.ClipObject))
                {
                    destStateMachine.stateMachines = new List<ChildAnimatorStateMachine>(destStateMachine.stateMachines) { cloneClip.ClipObject }.ToArray();
                    pastedObjs.Add(cloneClip.ClipObject);
                }
            }

            foreach (Clip<AnimatorTransition> cloneClip in cloneAnimatorTransition)
            {
                if (!destStateMachine.entryTransitions.Contains(cloneClip.ClipObject) &&
                    cloneClip.ClipObject != null)
                {
                    destStateMachine.entryTransitions = new List<AnimatorTransition>(destStateMachine.entryTransitions) { cloneClip.ClipObject }.ToArray();
                    pastedObjs.Add(cloneClip.ClipObject);
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
                    switch (parent)
                    {
                        case AnimatorState parentState:
                            if (!parentState.transitions.Contains(cloneClip.ClipObject))
                            {
                                parentState.transitions = new List<AnimatorStateTransition>(parentState.transitions) { cloneClip.ClipObject }.ToArray();
                            }
                            pastedObjs.Contains(cloneClip.ClipObject);
                            break;
                        case AnimatorStateMachine parentStateMachine:
                            if (!parentStateMachine.anyStateTransitions.Contains(cloneClip.ClipObject))
                            {
                                parentStateMachine.anyStateTransitions = new List<AnimatorStateTransition>(parentStateMachine.anyStateTransitions) { cloneClip.ClipObject }.ToArray();
                            }
                            pastedObjs.Contains(cloneClip.ClipObject);
                            break;
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

        public void PasteBehaviours(ClipSet clipSet, AnimatorStateMachine destStateMachine)
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

        public void PasteBehaviours(ClipSet clipSet, AnimatorState destState)
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

        public void PasteSettings(ClipSet clipSet, AnimatorState destState)
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

        public void PasteSettings(ClipSet clipSet, AnimatorTransition destTransition)
        {
            AnimatorTransition srcTransition = GetAnimatorTransition(clipSet);
            PasteSettings(srcTransition, destTransition);
        }

        public void PasteSettings(AnimatorTransition srcTransition, AnimatorTransition destTransition)
        {
            destTransition.mute = srcTransition.mute;
            destTransition.solo = srcTransition.solo;
        }

        public void PasteConditions(ClipSet clipSet, AnimatorTransition destTransition)
        {
            AnimatorTransition srcTransition = GetAnimatorTransition(clipSet);
            PasteConditions(srcTransition, destTransition);
        }

        public void PasteConditions(AnimatorTransition srcTransition, AnimatorTransition destTransition)
        {
            destTransition.conditions = srcTransition.conditions.ToArray();
        }

        public void PasteSettingsAndConditions(ClipSet clipSet, AnimatorTransition destTransition)
        {
            AnimatorTransition srcTransition = GetAnimatorTransition(clipSet);
            PasteSettingsAndConditions(srcTransition, destTransition);
        }

        public void PasteSettingsAndConditions(AnimatorTransition srcTransition, AnimatorTransition destTransition)
        {
            PasteSettings(srcTransition, destTransition);
            PasteConditions(srcTransition, destTransition);
        }

        public AnimatorTransition GetAnimatorTransition(ClipSet clipSet)
        {
            if (clipSet.Type != ClipSet.ClipSetType.Transition)
            {
                ThrowPasteInvalidClipSetTypeException();
            }
            return ((Clip<AnimatorTransition>)clipSet.Clips.First()).ClipObject;
        }

        public void PasteSettings(ClipSet clipSet, AnimatorStateTransition destStateTransition)
        {
            AnimatorStateTransition srcStateTransition = GetAnimatorStateTransition(clipSet);
            PasteSettings(srcStateTransition, destStateTransition);
        }

        public void PasteSettings(AnimatorStateTransition srcStateTransition, AnimatorStateTransition destStateTransition)
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

        public void PasteConditions(ClipSet clipSet, AnimatorStateTransition destStateTransition)
        {
            AnimatorStateTransition srcStateTransition = GetAnimatorStateTransition(clipSet);
            PasteConditions(srcStateTransition, destStateTransition);
        }

        public void PasteConditions(AnimatorStateTransition srcStateTransition, AnimatorStateTransition destStateTransition)
        {
            destStateTransition.conditions = srcStateTransition.conditions.ToArray();
        }

        public void PasteSettingsAndConditions(ClipSet clipSet, AnimatorStateTransition destStateTransition)
        {
            AnimatorStateTransition srcStateTransition = GetAnimatorStateTransition(clipSet);
            PasteSettingsAndConditions(srcStateTransition, destStateTransition);
        }

        public void PasteSettingsAndConditions(AnimatorStateTransition srcStateTransition, AnimatorStateTransition destStateTransition)
        {
            PasteSettings(srcStateTransition, destStateTransition);
            PasteConditions(srcStateTransition, destStateTransition);
        }

        private AnimatorStateTransition GetAnimatorStateTransition(ClipSet clipSet)
        {
            if (clipSet.Type != ClipSet.ClipSetType.StateTransition)
            {
                ThrowPasteInvalidClipSetTypeException();
            }
            return ((Clip<AnimatorStateTransition>)clipSet.Clips.First()).ClipObject;
        }

        private void ThrowPasteInvalidClipSetTypeException()
        {
            throw new Exception("貼り付け先に対して貼り付けるデータのタイプが不正です");
        }
    }
}