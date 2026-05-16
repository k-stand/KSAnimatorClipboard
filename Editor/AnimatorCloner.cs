using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace io.github.kiriumestand.animatorclipboard.editor
{
    internal class AnimatorCloner
    {
        private readonly HashSet<object> CloneWhiteList = new();

        private readonly HashSet<object> ReferenceHoldingList = new();

        private readonly Dictionary<object, object> CloneMap = new();

        internal void AddCloneWhiteList(object obj) => CloneWhiteList.Add(obj);

        internal void AddRangeCloneWhiteList(IEnumerable<object> objs)
        {
            foreach (object obj in objs)
            {
                CloneWhiteList.Add(obj);
            }
        }

        internal void RemoveCloneWhiteList(object obj) => ReferenceHoldingList.Remove(obj);

        internal List<object> GetAllCloneWhiteList() => new(CloneWhiteList);

        internal void AddReferenceHoldingList(object obj) => ReferenceHoldingList.Add(obj);

        internal void AddRangeReferenceHoldingList(IEnumerable<object> objs)
        {
            foreach (object obj in objs)
            {
                ReferenceHoldingList.Add(obj);
            }
        }

        internal void RemoveReferenceHoldingList(object obj) => ReferenceHoldingList.Remove(obj);

        internal List<object> GetAllReferenceHoldingList() => new(ReferenceHoldingList);

        private bool GetOrCreateCloneInstance<T>(T orig, out T cloneIns) where T : new()
        {
            if (orig == null || orig is null)
            {
                cloneIns = default;
                return false;
            }
            if (CloneMap.TryGetValue(orig, out object outObj) && outObj is T clone)
            {
                cloneIns = clone;
                return false;
            }

            if (CloneWhiteList.Contains(orig))
            {
                clone = new();
                CloneMap[orig] = CloneMap[clone] = cloneIns = clone;
                return true;
            }
            else
            {
                if (ReferenceHoldingList.Contains(orig))
                {
                    CloneMap[orig] = cloneIns = orig;
                }
                else
                {
                    CloneMap[orig] = cloneIns = default;
                }
                return false;
            }
        }

        internal object[] CloneObjects(IEnumerable<object> objs)
        {
            return objs.Select(obj => CloneObject(obj)).ToArray();
        }

        internal T CloneObject<T>(T obj) => (T)CloneObjectInternal(obj);

        internal object CloneObjectInternal(object obj) => obj switch
        {
            AnimatorController castedObj => CloneAnimatorController(castedObj),
            AnimatorControllerParameter castedObj => CloneAnimatorControllerParameter(castedObj),
            AnimatorControllerLayer castedObj => CloneAnimatorControllerLayer(castedObj),
            ChildAnimatorStateMachine castedObj => CloneChildAnimatorStateMachine(castedObj),
            AnimatorStateMachine castedObj => CloneAnimatorStateMachine(castedObj),
            ChildAnimatorState castedObj => CloneChildAnimatorState(castedObj),
            AnimatorState castedObj => CloneAnimatorState(castedObj),
            AnimatorTransition castedObj => CloneAnimatorTransition(castedObj),
            AnimatorStateTransition castedObj => CloneAnimatorStateTransition(castedObj),
            AnimatorCondition castedObj => CloneAnimatorCondition(castedObj),
            StateMachineBehaviour castedObj => CloneStateMachineBehaviour(castedObj),
            _ => null,
        };


        internal AnimatorController CloneAnimatorController(AnimatorController ac)
        {
            bool isCreated = GetOrCreateCloneInstance(ac, out AnimatorController cloneAC);
            if (!isCreated) return cloneAC;

            cloneAC.hideFlags = ac.hideFlags;

            cloneAC.parameters = CloneAnimatorControllerParameters(ac.parameters);
            cloneAC.layers = CloneAnimatorControllerLayers(ac.layers);

            return cloneAC;
        }

        internal AnimatorControllerParameter[] CloneAnimatorControllerParameters(IEnumerable<AnimatorControllerParameter> acps)
        {
            return acps.Select(acp => CloneAnimatorControllerParameter(acp)).ToArray();
        }

        internal AnimatorControllerParameter CloneAnimatorControllerParameter(AnimatorControllerParameter acp)
        {
            return new()
            {
                defaultBool = acp.defaultBool,
                defaultFloat = acp.defaultFloat,
                defaultInt = acp.defaultInt,
                name = GetCloneObjName(acp.name),
                type = acp.type
            };
        }

        internal AnimatorControllerLayer[] CloneAnimatorControllerLayers(IEnumerable<AnimatorControllerLayer> acls)
        {
            return acls.Select(acl => CloneAnimatorControllerLayer(acl)).ToArray();
        }

        internal AnimatorControllerLayer CloneAnimatorControllerLayer(AnimatorControllerLayer acl)
        {
            AnimatorControllerLayer cloneACL = new()
            {
                avatarMask = acl.avatarMask,
                blendingMode = acl.blendingMode,
                defaultWeight = acl.defaultWeight,
                iKPass = acl.iKPass,
                name = GetCloneObjName(acl.name),
                syncedLayerAffectsTiming = acl.syncedLayerAffectsTiming,
                syncedLayerIndex = acl.syncedLayerIndex,
                stateMachine = CloneAnimatorStateMachine(acl.stateMachine)
            };

            AnimatorClipboardUtility.StateMotionPair[] overrideStateMotionPairs = AnimatorClipboardUtility.GetAllOverrideStateMotionPairs(acl);
            foreach (AnimatorClipboardUtility.StateMotionPair pair in overrideStateMotionPairs)
            {
                AnimatorState cloneAS = CloneAnimatorState(pair.State);
                cloneACL.SetOverrideMotion(cloneAS, pair.Motion);
            }
            AnimatorClipboardUtility.StateBehavioursPair[] overrideBehavioursPairs = AnimatorClipboardUtility.GetAllOverrideBehavioursPairs(acl);
            foreach (AnimatorClipboardUtility.StateBehavioursPair pair in overrideBehavioursPairs)
            {
                AnimatorState cloneAS = CloneAnimatorState(pair.State);
                StateMachineBehaviour[] cloneSMBs = CloneStateMachineBehaviours(pair.Behaviours);
                cloneACL.SetOverrideBehaviours(cloneAS, cloneSMBs);
            }

            return cloneACL;
        }

        internal ChildAnimatorStateMachine[] CloneChildAnimatorStateMachines(IEnumerable<ChildAnimatorStateMachine> casms)
        {
            return casms.Select(casm => CloneChildAnimatorStateMachine(casm)).ToArray();
        }

        internal ChildAnimatorStateMachine CloneChildAnimatorStateMachine(ChildAnimatorStateMachine casm)
        {
            ChildAnimatorStateMachine cloneCAS = new()
            {
                position = casm.position,
                stateMachine = CloneAnimatorStateMachine(casm.stateMachine)
            };

            return cloneCAS;
        }

        internal AnimatorStateMachine CloneAnimatorStateMachine(AnimatorStateMachine asm)
        {
            bool isCreated = GetOrCreateCloneInstance(asm, out AnimatorStateMachine cloneASM);
            if (!isCreated) return cloneASM;

            cloneASM.hideFlags = asm.hideFlags;

            cloneASM.anyStatePosition = asm.anyStatePosition;
            cloneASM.entryPosition = asm.entryPosition;
            cloneASM.exitPosition = asm.exitPosition;
            cloneASM.name = GetCloneObjName(asm.name);
            cloneASM.parentStateMachinePosition = asm.parentStateMachinePosition;

            cloneASM.states = CloneChildAnimatorStates(asm.states);
            cloneASM.stateMachines = CloneChildAnimatorStateMachines(asm.stateMachines);
            cloneASM.defaultState = CloneAnimatorState(asm.defaultState);

            cloneASM.entryTransitions = CloneAnimatorTransitions(asm.entryTransitions);
            cloneASM.anyStateTransitions = CloneAnimatorStateTransitions(asm.anyStateTransitions);
            foreach (ChildAnimatorStateMachine curCASM in asm.stateMachines)
            {
                AnimatorStateMachine cloneStateMachine = CloneAnimatorStateMachine(curCASM.stateMachine);

                AnimatorTransition[] transitions = asm.GetStateMachineTransitions(curCASM.stateMachine);
                AnimatorTransition[] cloneTransitions = CloneAnimatorTransitions(transitions);

                cloneASM.SetStateMachineTransitions(cloneStateMachine, cloneTransitions);
            }

            cloneASM.behaviours = CloneStateMachineBehaviours(asm.behaviours);

            return cloneASM;
        }

        internal ChildAnimatorState[] CloneChildAnimatorStates(IEnumerable<ChildAnimatorState> cass)
        {
            return cass.Select(cas => CloneChildAnimatorState(cas)).ToArray();
        }

        internal ChildAnimatorState CloneChildAnimatorState(ChildAnimatorState cas)
        {
            ChildAnimatorState cloneCAS = new()
            {
                position = cas.position,
                state = CloneAnimatorState(cas.state)
            };

            return cloneCAS;
        }

        internal AnimatorState CloneAnimatorState(AnimatorState aState)
        {
            bool isCreated = GetOrCreateCloneInstance(aState, out AnimatorState cloneAS);
            if (!isCreated) return cloneAS;

            cloneAS.hideFlags = aState.hideFlags;

            cloneAS.cycleOffset = aState.cycleOffset;
            cloneAS.cycleOffsetParameter = aState.cycleOffsetParameter;
            cloneAS.cycleOffsetParameterActive = aState.cycleOffsetParameterActive;
            cloneAS.iKOnFeet = aState.iKOnFeet;
            cloneAS.mirror = aState.mirror;
            cloneAS.mirrorParameter = aState.mirrorParameter;
            cloneAS.mirrorParameterActive = aState.mirrorParameterActive;
            cloneAS.motion = aState.motion;
            cloneAS.name = GetCloneObjName(aState.name);
            cloneAS.speed = aState.speed;
            cloneAS.speedParameter = aState.speedParameter;
            cloneAS.speedParameterActive = aState.speedParameterActive;
            cloneAS.tag = aState.tag;
            cloneAS.timeParameter = aState.timeParameter;
            cloneAS.timeParameterActive = aState.timeParameterActive;
            cloneAS.writeDefaultValues = aState.writeDefaultValues;

            cloneAS.transitions = CloneAnimatorStateTransitions(aState.transitions);

            cloneAS.behaviours = CloneStateMachineBehaviours(aState.behaviours);

            return cloneAS;
        }

        internal AnimatorTransition[] CloneAnimatorTransitions(IEnumerable<AnimatorTransition> ats)
        {
            return ats.Select(at => CloneAnimatorTransition(at)).ToArray();
        }

        internal AnimatorTransition CloneAnimatorTransition(AnimatorTransition at)
        {
            bool isCreated = GetOrCreateCloneInstance(at, out AnimatorTransition cloneAT);
            if (!isCreated) return cloneAT;

            cloneAT.hideFlags = at.hideFlags;

            cloneAT.isExit = at.isExit;
            cloneAT.mute = at.mute;
            cloneAT.name = GetCloneObjName(at.name);
            cloneAT.solo = at.solo;
            cloneAT.destinationState = CloneAnimatorState(at.destinationState);
            cloneAT.destinationStateMachine = CloneAnimatorStateMachine(at.destinationStateMachine);
            cloneAT.conditions = CloneAnimatorConditions(at.conditions);

            return cloneAT;
        }

        internal AnimatorStateTransition[] CloneAnimatorStateTransitions(IEnumerable<AnimatorStateTransition> asts)
        {
            return asts.Select(ast => CloneAnimatorStateTransition(ast)).ToArray();
        }

        internal AnimatorStateTransition CloneAnimatorStateTransition(AnimatorStateTransition ast)
        {
            bool isCreated = GetOrCreateCloneInstance(ast, out AnimatorStateTransition cloneAST);
            if (!isCreated) return cloneAST;

            cloneAST.hideFlags = ast.hideFlags;

            cloneAST.canTransitionToSelf = ast.canTransitionToSelf;
            cloneAST.duration = ast.duration;
            cloneAST.exitTime = ast.exitTime;
            cloneAST.hasExitTime = ast.hasExitTime;
            cloneAST.hasFixedDuration = ast.hasFixedDuration;
            cloneAST.interruptionSource = ast.interruptionSource;
            cloneAST.isExit = ast.isExit;
            cloneAST.mute = ast.mute;
            cloneAST.name = GetCloneObjName(ast.name);
            cloneAST.offset = ast.offset;
            cloneAST.orderedInterruption = ast.orderedInterruption;
            cloneAST.solo = ast.solo;
            cloneAST.destinationState = CloneAnimatorState(ast.destinationState);
            cloneAST.destinationStateMachine = CloneAnimatorStateMachine(ast.destinationStateMachine);
            cloneAST.conditions = CloneAnimatorConditions(ast.conditions);

            return cloneAST;
        }

        internal AnimatorCondition[] CloneAnimatorConditions(IEnumerable<AnimatorCondition> acs)
        {
            return acs.Select(ac => CloneAnimatorCondition(ac)).ToArray();
        }

        internal AnimatorCondition CloneAnimatorCondition(AnimatorCondition ac)
        {
            return ac;
        }

        internal StateMachineBehaviour[] CloneStateMachineBehaviours(IEnumerable<StateMachineBehaviour> smbs)
        {
            return smbs.Select(smb => CloneStateMachineBehaviour(smb)).ToArray();
        }

        internal StateMachineBehaviour CloneStateMachineBehaviour(StateMachineBehaviour smb)
        {
            StateMachineBehaviour cloneSMB = (StateMachineBehaviour)ScriptableObject.CreateInstance(smb.GetType());
            EditorUtility.CopySerialized(smb, cloneSMB);

            return cloneSMB;
        }

        internal string GetCloneObjName(string origName)
        {
            return string.IsNullOrEmpty(origName) ? "" : origName + " (Clone)";
        }
    }
}