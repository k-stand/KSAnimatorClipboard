using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace com.github.k_stand.ksanimatorclipboard.editor
{
    public class AnimatorCloner
    {
        public ClonePolicy DefaultPolicy { get; set; } = ClonePolicy.Detach;

        private readonly Dictionary<UnityEngine.Object, ClonePolicy> _policyMap = new();

        private readonly Dictionary<UnityEngine.Object, UnityEngine.Object> _cloneMap = new();

        public static readonly IReadOnlyCollection<Type> CloneableTypes = new HashSet<Type>
        {
            typeof(AnimatorController),
            typeof(AnimatorControllerParameter),
            typeof(AnimatorControllerLayer),
            typeof(ChildAnimatorStateMachine),
            typeof(AnimatorStateMachine),
            typeof(ChildAnimatorState),
            typeof(AnimatorState),
            typeof(AnimatorTransition),
            typeof(AnimatorStateTransition),
            typeof(AnimatorCondition),
            typeof(StateMachineBehaviour),
        };

        public Func<string, string> NameTransformer { get; set; } = static origName => string.IsNullOrEmpty(origName) ? "" : origName + " (Clone)";

        public void SetClonePolicy(UnityEngine.Object obj, ClonePolicy policy) => _policyMap[obj] = policy;

        public void SetRangeClonePolicy(IEnumerable<UnityEngine.Object> objs, ClonePolicy policy)
        {
            foreach (UnityEngine.Object obj in objs) SetClonePolicy(obj, policy);
        }

        public void SetClonePolicyIfAbsent(UnityEngine.Object obj, ClonePolicy policy)
        {
            if (!_policyMap.TryGetValue(obj, out ClonePolicy current) || current < policy)
            {
                _policyMap[obj] = policy;
            }
        }

        public void SetRangeClonePolicyIfAbsent(IEnumerable<UnityEngine.Object> objs, ClonePolicy policy)
        {
            foreach (UnityEngine.Object obj in objs) SetClonePolicyIfAbsent(obj, policy);
        }

        public void RemoveClonePolicy(UnityEngine.Object obj) => _policyMap.Remove(obj);

        public bool TryGetClonePolicy(UnityEngine.Object obj, out ClonePolicy p) => _policyMap.TryGetValue(obj, out p);

        private ClonePolicy GetClonePolicy(UnityEngine.Object obj)
               => TryGetClonePolicy(obj, out ClonePolicy p) ? p : DefaultPolicy;

        public Dictionary<UnityEngine.Object, ClonePolicy> GetAllClonePolicy() => new(_policyMap);

        public object[] CloneObjects(IEnumerable<object> objs)
        {
            return objs.Select(obj => CloneObject(obj)).ToArray();
        }

        public object CloneObject(object obj) => obj switch
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

        public bool TryCloneObject(object obj, out object clone)
        {
            object tempClone;
            if (obj == null || (tempClone = CloneObject(obj)) == null)
            {
                clone = null;
                return false;
            }

            clone = tempClone;
            return true;
        }


        public AnimatorController CloneAnimatorController(AnimatorController ac)
        {
            bool isCreated = TryGetOrCreateCloneInstance(ac, out AnimatorController cloneAC);
            if (!isCreated) return cloneAC;

            cloneAC.hideFlags = ac.hideFlags;

            cloneAC.parameters = CloneAnimatorControllerParameters(ac.parameters);
            cloneAC.layers = CloneAnimatorControllerLayers(ac.layers);

            return cloneAC;
        }

        public AnimatorControllerParameter[] CloneAnimatorControllerParameters(IEnumerable<AnimatorControllerParameter> acps)
        {
            return acps.Select(acp => CloneAnimatorControllerParameter(acp)).ToArray();
        }

        public AnimatorControllerParameter CloneAnimatorControllerParameter(AnimatorControllerParameter acp)
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

        public AnimatorControllerLayer[] CloneAnimatorControllerLayers(IEnumerable<AnimatorControllerLayer> acls)
        {
            return acls.Select(acl => CloneAnimatorControllerLayer(acl)).ToArray();
        }

        public AnimatorControllerLayer CloneAnimatorControllerLayer(AnimatorControllerLayer acl)
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

        public ChildAnimatorStateMachine[] CloneChildAnimatorStateMachines(IEnumerable<ChildAnimatorStateMachine> casms)
        {
            return casms.Select(casm => CloneChildAnimatorStateMachine(casm)).ToArray();
        }

        public ChildAnimatorStateMachine CloneChildAnimatorStateMachine(ChildAnimatorStateMachine casm)
        {
            ChildAnimatorStateMachine cloneCAS = new()
            {
                position = casm.position,
                stateMachine = CloneAnimatorStateMachine(casm.stateMachine)
            };

            return cloneCAS;
        }

        public AnimatorStateMachine CloneAnimatorStateMachine(AnimatorStateMachine asm)
        {
            bool isCreated = TryGetOrCreateCloneInstance(asm, out AnimatorStateMachine cloneASM);
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

        public ChildAnimatorState[] CloneChildAnimatorStates(IEnumerable<ChildAnimatorState> cass)
        {
            return cass.Select(cas => CloneChildAnimatorState(cas)).ToArray();
        }

        public ChildAnimatorState CloneChildAnimatorState(ChildAnimatorState cas)
        {
            ChildAnimatorState cloneCAS = new()
            {
                position = cas.position,
                state = CloneAnimatorState(cas.state)
            };

            return cloneCAS;
        }

        public AnimatorState CloneAnimatorState(AnimatorState aState)
        {
            bool isCreated = TryGetOrCreateCloneInstance(aState, out AnimatorState cloneAS);
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

        public AnimatorTransition[] CloneAnimatorTransitions(IEnumerable<AnimatorTransition> ats)
        {
            return ats.Select(at => CloneAnimatorTransition(at)).ToArray();
        }

        public AnimatorTransition CloneAnimatorTransition(AnimatorTransition at)
        {
            bool isCreated = TryGetOrCreateCloneInstance(at, out AnimatorTransition cloneAT);
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

        public AnimatorStateTransition[] CloneAnimatorStateTransitions(IEnumerable<AnimatorStateTransition> asts)
        {
            return asts.Select(ast => CloneAnimatorStateTransition(ast)).ToArray();
        }

        public AnimatorStateTransition CloneAnimatorStateTransition(AnimatorStateTransition ast)
        {
            bool isCreated = TryGetOrCreateCloneInstance(ast, out AnimatorStateTransition cloneAST);
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

        public AnimatorCondition[] CloneAnimatorConditions(IEnumerable<AnimatorCondition> acs)
        {
            return acs.Select(ac => CloneAnimatorCondition(ac)).ToArray();
        }

        public AnimatorCondition CloneAnimatorCondition(AnimatorCondition ac)
        {
            return ac;
        }

        public StateMachineBehaviour[] CloneStateMachineBehaviours(IEnumerable<StateMachineBehaviour> smbs)
        {
            return smbs.Select(smb => CloneStateMachineBehaviour(smb)).ToArray();
        }

        public StateMachineBehaviour CloneStateMachineBehaviour(StateMachineBehaviour smb)
        {
            StateMachineBehaviour cloneSMB = (StateMachineBehaviour)ScriptableObject.CreateInstance(smb.GetType());
            EditorUtility.CopySerialized(smb, cloneSMB);

            return cloneSMB;
        }

        private bool TryGetOrCreateCloneInstance<T>(T orig, out T clone) where T : UnityEngine.Object, new()
        {
            if (orig == null)
            {
                clone = default;
                return false;
            }
            if (_cloneMap.TryGetValue(orig, out UnityEngine.Object cached) && cached is T tCached)
            {
                clone = tCached;
                return false;
            }

            switch (GetClonePolicy(orig))
            {
                case ClonePolicy.Clone:
                    clone = new T();
                    _cloneMap[orig] = _cloneMap[clone] = clone;
                    return true;

                case ClonePolicy.KeepReference:
                    _cloneMap[orig] = clone = orig;
                    return false;

                case ClonePolicy.Detach:
                default:
                    _cloneMap[orig] = clone = default;
                    return false;

                case ClonePolicy.UnSetting:
                    throw new Exception("ClonePolicyが未設定のオブジェクトをクローンしようとしました");
            }
        }

        private string GetCloneObjName(string origName) => NameTransformer(origName);

        /// <summary>
        /// ClonePolicyの登録漏れを検出する
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public IReadOnlyCollection<UnregisteredEntry> ValidateRegistration(UnityEngine.Object target) => ValidateRegistrationInternal(target);


        /// <summary>
        /// ClonePolicyの登録漏れを検出する
        /// </summary>
        /// <param name="targets"></param>
        /// <returns></returns>
        public IReadOnlyCollection<UnregisteredEntry> ValidateRegistrations(IEnumerable<UnityEngine.Object> targets) => targets.SelectMany(t => ValidateRegistration(t)).ToHashSet();


        private IReadOnlyCollection<UnregisteredEntry> ValidateRegistrationInternal(object target)
        {
            if (target == null)
            {
                return new List<UnregisteredEntry>();
            }

            HashSet<UnityEngine.Object> visitedObjSet = new();

            IReadOnlyCollection<UnregisteredEntry> unregisteredList = target switch
            {
                AnimatorController castedObj => ValidateRegistrationAnimatorController(castedObj, null, "", ref visitedObjSet),
                AnimatorControllerLayer castedObj => ValidateRegistrationAnimatorControllerLayer(castedObj, null, "", ref visitedObjSet),
                ChildAnimatorStateMachine castedObj => ValidateRegistrationChildAnimatorStateMachine(castedObj, null, "", ref visitedObjSet),
                AnimatorStateMachine castedObj => ValidateRegistrationAnimatorStateMachine(castedObj, null, "", ref visitedObjSet),
                ChildAnimatorState castedObj => ValidateRegistrationChildAnimatorState(castedObj, null, "", ref visitedObjSet),
                AnimatorState castedObj => ValidateRegistrationAnimatorState(castedObj, null, "", ref visitedObjSet),
                AnimatorTransition castedObj => ValidateRegistrationAnimatorTransition(castedObj, null, "", ref visitedObjSet),
                AnimatorStateTransition castedObj => ValidateRegistrationAnimatorStateTransition(castedObj, null, "", ref visitedObjSet),
                StateMachineBehaviour castedObj => ValidateRegistrationStateMachineBehaviour(castedObj, null, "", ref visitedObjSet),
                _ => new UnregisteredEntry[0],
            };

            return unregisteredList;
        }

        private IReadOnlyCollection<UnregisteredEntry> ValidateRegistrationAnimatorController(AnimatorController target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (visitedObjSet.Contains(target)) return new UnregisteredEntry[0];
            visitedObjSet.Add(target);
            bool existPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out UnregisteredEntry entry, out ClonePolicy policy);
            if (!existPolicy) return new UnregisteredEntry[] { entry };
            if (policy != ClonePolicy.Clone) return new UnregisteredEntry[0];

            HashSet<UnregisteredEntry> entries = new();

            entries.UnionWith(ValidateRegistrationAnimatorControllerLayers(target.layers, target, nameof(target.layers), ref visitedObjSet));

            return entries;
        }

        public IReadOnlyCollection<UnregisteredEntry> ValidateRegistrationAnimatorControllerLayers(IEnumerable<AnimatorControllerLayer> target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<UnregisteredEntry> entries = new();
            foreach (AnimatorControllerLayer acl in target)
            {
                entries.UnionWith(ValidateRegistrationAnimatorControllerLayer(acl, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return entries;
        }

        private IReadOnlyCollection<UnregisteredEntry> ValidateRegistrationAnimatorControllerLayer(AnimatorControllerLayer target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            HashSet<UnregisteredEntry> entries = new();
            entries.UnionWith(ValidateRegistrationAnimatorStateMachine(target.stateMachine, parent, $"{memberName}.{nameof(target.stateMachine)}", ref visitedObjSet));

            AnimatorClipboardUtility.StateMotionPair[] overrideStateMotionPairs = AnimatorClipboardUtility.GetAllOverrideStateMotionPairs(target);
            foreach (AnimatorClipboardUtility.StateMotionPair pair in overrideStateMotionPairs)
            {
                entries.UnionWith(ValidateRegistrationAnimatorState(pair.State, parent, $"{memberName}.m_Motions.m_State", ref visitedObjSet));
            }
            AnimatorClipboardUtility.StateBehavioursPair[] overrideBehavioursPairs = AnimatorClipboardUtility.GetAllOverrideBehavioursPairs(target);
            foreach (AnimatorClipboardUtility.StateBehavioursPair pair in overrideBehavioursPairs)
            {
                entries.UnionWith(ValidateRegistrationAnimatorState(pair.State, parent, $"{memberName}.m_Behaviours.m_State", ref visitedObjSet));
                entries.UnionWith(ValidateRegistrationStateMachineBehaviours(pair.Behaviours, parent, $"{memberName}.m_Behaviours.m_Behaviours", ref visitedObjSet));
            }
            return entries;
        }

        public IReadOnlyCollection<UnregisteredEntry> ValidateRegistrationChildAnimatorStateMachines(IEnumerable<ChildAnimatorStateMachine> targets, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<UnregisteredEntry> entries = new();
            foreach (ChildAnimatorStateMachine target in targets)
            {
                entries.UnionWith(ValidateRegistrationChildAnimatorStateMachine(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return entries;
        }

        private IReadOnlyCollection<UnregisteredEntry> ValidateRegistrationChildAnimatorStateMachine(ChildAnimatorStateMachine target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            return ValidateRegistrationAnimatorStateMachine(target.stateMachine, parent, $"{memberName}.{nameof(target.stateMachine)}", ref visitedObjSet);
        }

        private IReadOnlyCollection<UnregisteredEntry> ValidateRegistrationAnimatorStateMachine(AnimatorStateMachine target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (visitedObjSet.Contains(target)) return new UnregisteredEntry[0];
            visitedObjSet.Add(target);
            bool existPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out UnregisteredEntry entry, out ClonePolicy policy);
            if (!existPolicy) return new UnregisteredEntry[] { entry };
            if (policy != ClonePolicy.Clone) return new UnregisteredEntry[0];

            HashSet<UnregisteredEntry> entries = new();

            entries.UnionWith(ValidateRegistrationChildAnimatorStates(target.states, target, nameof(target), ref visitedObjSet));
            entries.UnionWith(ValidateRegistrationChildAnimatorStateMachines(target.stateMachines, target, nameof(target), ref visitedObjSet));
            entries.UnionWith(ValidateRegistrationAnimatorState(target.defaultState, target, nameof(target), ref visitedObjSet));
            entries.UnionWith(ValidateRegistrationAnimatorTransitions(target.entryTransitions, target, nameof(target), ref visitedObjSet));
            entries.UnionWith(ValidateRegistrationAnimatorStateTransitions(target.anyStateTransitions, target, nameof(target.anyStateTransitions), ref visitedObjSet));
            int i = 0;
            foreach (ChildAnimatorStateMachine curCASM in target.stateMachines)
            {
                //TODO:ここのネイティブコードでのm_StateMachineTransitionsが見れるかデバッグモードで確認
                AnimatorTransition[] transitions = target.GetStateMachineTransitions(curCASM.stateMachine);
                entries.UnionWith(ValidateRegistrationAnimatorTransitions(transitions, target, $"StateMachineTransitions[{curCASM.stateMachine.name}]", ref visitedObjSet));
                i++;
            }
            entries.UnionWith(ValidateRegistrationStateMachineBehaviours(target.behaviours, target, nameof(target), ref visitedObjSet));

            return entries;

        }

        public IReadOnlyCollection<UnregisteredEntry> ValidateRegistrationChildAnimatorStates(IEnumerable<ChildAnimatorState> targets, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<UnregisteredEntry> entries = new();
            foreach (ChildAnimatorState target in targets)
            {
                entries.UnionWith(ValidateRegistrationChildAnimatorState(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return entries;
        }

        private IReadOnlyCollection<UnregisteredEntry> ValidateRegistrationChildAnimatorState(ChildAnimatorState target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            return ValidateRegistrationAnimatorState(target.state, parent, $"{memberName}.{nameof(target.state)}", ref visitedObjSet);
        }

        private IReadOnlyCollection<UnregisteredEntry> ValidateRegistrationAnimatorState(AnimatorState target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (visitedObjSet.Contains(target)) return new UnregisteredEntry[0];
            visitedObjSet.Add(target);
            bool existPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out UnregisteredEntry entry, out ClonePolicy policy);
            if (!existPolicy) return new UnregisteredEntry[] { entry };
            if (policy != ClonePolicy.Clone) return new UnregisteredEntry[0];

            HashSet<UnregisteredEntry> entries = new();

            entries.UnionWith(ValidateRegistrationAnimatorStateTransitions(target.transitions, target, nameof(target.transitions), ref visitedObjSet));
            entries.UnionWith(ValidateRegistrationStateMachineBehaviours(target.behaviours, target, nameof(target.behaviours), ref visitedObjSet));

            return entries;

        }

        public IReadOnlyCollection<UnregisteredEntry> ValidateRegistrationAnimatorTransitions(IEnumerable<AnimatorTransition> targets, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<UnregisteredEntry> entries = new();
            foreach (AnimatorTransition target in targets)
            {
                entries.UnionWith(ValidateRegistrationAnimatorTransition(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return entries;
        }

        private IReadOnlyCollection<UnregisteredEntry> ValidateRegistrationAnimatorTransition(AnimatorTransition target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (visitedObjSet.Contains(target)) return new UnregisteredEntry[0];
            visitedObjSet.Add(target);
            bool existPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out UnregisteredEntry entry, out ClonePolicy policy);
            if (!existPolicy) return new UnregisteredEntry[] { entry };
            if (policy != ClonePolicy.Clone) return new UnregisteredEntry[0];

            HashSet<UnregisteredEntry> entries = new();

            entries.UnionWith(ValidateRegistrationAnimatorState(target.destinationState, target, nameof(target.destinationState), ref visitedObjSet));
            entries.UnionWith(ValidateRegistrationAnimatorStateMachine(target.destinationStateMachine, target, nameof(target.destinationStateMachine), ref visitedObjSet));

            return entries;

        }

        public IReadOnlyCollection<UnregisteredEntry> ValidateRegistrationAnimatorStateTransitions(IEnumerable<AnimatorStateTransition> targets, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<UnregisteredEntry> entries = new();
            foreach (AnimatorStateTransition target in targets)
            {
                entries.UnionWith(ValidateRegistrationAnimatorStateTransition(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return entries;
        }

        private IReadOnlyCollection<UnregisteredEntry> ValidateRegistrationAnimatorStateTransition(AnimatorStateTransition target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (visitedObjSet.Contains(target)) return new UnregisteredEntry[0];
            visitedObjSet.Add(target);
            bool existPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out UnregisteredEntry entry, out ClonePolicy policy);
            if (!existPolicy) return new UnregisteredEntry[] { entry };
            if (policy != ClonePolicy.Clone) return new UnregisteredEntry[0];

            HashSet<UnregisteredEntry> entries = new();

            entries.UnionWith(ValidateRegistrationAnimatorState(target.destinationState, target, nameof(target.destinationState), ref visitedObjSet));
            entries.UnionWith(ValidateRegistrationAnimatorStateMachine(target.destinationStateMachine, target, nameof(target.destinationStateMachine), ref visitedObjSet));

            return entries;

        }

        public IReadOnlyCollection<UnregisteredEntry> ValidateRegistrationStateMachineBehaviours(IEnumerable<StateMachineBehaviour> targets, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<UnregisteredEntry> entries = new();
            foreach (StateMachineBehaviour target in targets)
            {
                entries.UnionWith(ValidateRegistrationStateMachineBehaviour(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return entries;
        }

        private IReadOnlyCollection<UnregisteredEntry> ValidateRegistrationStateMachineBehaviour(StateMachineBehaviour target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            // TODO:未実装 実装するかも未定
            return new UnregisteredEntry[0];
            //if (visitedObjSet.Contains(target)) return new UnregisteredEntry[0];
            //visitedObjSet.Add(target);
            //bool existPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out UnregisteredEntry entry, out ClonePolicy policy);
            //if (!existPolicy) return new UnregisteredEntry[] { entry };
            //if (policy != ClonePolicy.Clone) return new UnregisteredEntry[0];
            //
            //HashSet<UnregisteredEntry> entries = new();
            //
            ////entries.UnionWith(ValidateRegistrationAnimatorControllerLayers(target., target, nameof(target.), ref visitedObjSet));
            //
            //return entries;
        }

        private bool ValidateAndCreateUnregisteredEntry(UnityEngine.Object target, UnityEngine.Object parent, string memberName, out UnregisteredEntry entry, out ClonePolicy policy)
        {
            entry = null;
            if (_policyMap.TryGetValue(target, out policy) || target == null) return true;
            entry = new(target, parent, memberName);
            return false;
        }


        /// <summary>
        /// 値が大きいほど優先度が高い。
        /// SetPolicyIfAbsentは現在の設定より低い優先度のポリシーを無視する。
        /// 新しいポリシーを追加する際は優先度順に並べること。
        /// </summary>
        public enum ClonePolicy
        {
            /// 未設定（このポリシーのオブジェクトをクローンしようとした場合、例外を吐く）
            UnSetting,
            /// nullとして扱う（切り離す）
            Detach,
            /// 元のオブジェクトへの参照を保持する
            KeepReference,
            /// クローンを生成する
            Clone,
        }

        public record UnregisteredEntry
        {
            public UnityEngine.Object UnregisteredObject { get; }
            public UnityEngine.Object ReferencedFrom { get; }
            public string MemberName { get; }

            public UnregisteredEntry(UnityEngine.Object unregisteredObject, UnityEngine.Object referencedFrom, string memberName)
            {
                UnregisteredObject = unregisteredObject;
                ReferencedFrom = referencedFrom;
                MemberName = memberName;
            }
        }
    }
}