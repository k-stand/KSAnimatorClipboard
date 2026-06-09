
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace com.github.k_stand.ksanimatorclipboard.editor
{
    public static class AnimatorClipboardUtility
    {
        // Unity内部のinternalな型を取得
        private static readonly Type StateMotionPairType = typeof(AnimatorControllerLayer).Assembly.GetType("UnityEditor.Animations.StateMotionPair");
        private static readonly Type StateBehavioursPairType = typeof(AnimatorControllerLayer).Assembly.GetType("UnityEditor.Animations.StateBehavioursPair");

        private static readonly Func<AnimatorControllerLayer, Array> LayerMotionsGetter = CreateFieldGetter<AnimatorControllerLayer, Array>("m_Motions");
        private static readonly Func<AnimatorControllerLayer, Array> LayerBehavioursGetter = CreateFieldGetter<AnimatorControllerLayer, Array>("m_Behaviours");
        private static readonly Func<object, AnimatorState> m_StateGetter = CreateFieldGetterFromType<AnimatorState>(StateMotionPairType, "m_State");
        private static readonly Func<object, Motion> m_MotionGetter = CreateFieldGetterFromType<Motion>(StateMotionPairType, "m_Motion");
        private static readonly Func<object, ScriptableObject[]> m_BehavioursGetter = CreateFieldGetterFromType<ScriptableObject[]>(StateBehavioursPairType, "m_Behaviours");

        private static Func<TTarget, TValue> CreateFieldGetter<TTarget, TValue>(string fieldName)
        {
            ParameterExpression targetParam = Expression.Parameter(typeof(TTarget));
            FieldInfo fieldInfo = typeof(TTarget).GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);

            Expression<Func<TTarget, TValue>> lambda =
                Expression.Lambda<Func<TTarget, TValue>>(
                    // ラムダ式で処理する内容
                    Expression.Field(targetParam, fieldInfo)
                    // ラムダ式の引数
                    , targetParam
                );

            return lambda.Compile();
        }

        private static Func<object, TValue> CreateFieldGetterFromType<TValue>(Type targetType, string fieldName)
        {
            FieldInfo fieldInfo = targetType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance) ?? throw new InvalidOperationException($"Field '{fieldName}' not found in '{targetType.FullName}'");

            ParameterExpression targetParam = Expression.Parameter(typeof(object));

            Expression<Func<object, TValue>> lambda =
                Expression.Lambda<Func<object, TValue>>(
                    // ラムダ式で処理する内容
                    Expression.Field(
                        Expression.Convert(targetParam, targetType),
                        fieldInfo),
                    // ラムダ式の引数
                    targetParam
                );

            return lambda.Compile();
        }

        internal static StateMotionPair[] GetAllOverrideStateMotionPairs(AnimatorControllerLayer acl)
        {
            Array stateMotionPairs = LayerMotionsGetter(acl);

            StateMotionPair[] pairs = new StateMotionPair[stateMotionPairs.Length];
            for (int i = 0; i < stateMotionPairs.Length; i++)
            {
                object pair = stateMotionPairs.GetValue(i);
                pairs[i].State = m_StateGetter(pair);
                pairs[i].Motion = m_MotionGetter(pair);
            }

            return pairs;
        }

        internal static StateBehavioursPair[] GetAllOverrideBehavioursPairs(AnimatorControllerLayer acl)
        {
            Array stateBehavioursPairs = LayerBehavioursGetter(acl);

            StateBehavioursPair[] pairs = new StateBehavioursPair[stateBehavioursPairs.Length];
            for (int i = 0; i < stateBehavioursPairs.Length; i++)
            {
                object pair = stateBehavioursPairs.GetValue(i);
                pairs[i].State = m_StateGetter(pair);
                pairs[i].Behaviours = m_BehavioursGetter(pair).Select(x => (StateMachineBehaviour)x).ToArray();
            }

            return pairs;
        }

        internal struct StateMotionPair
        {
            internal AnimatorState State;

            internal Motion Motion;
        }

        internal struct StateBehavioursPair
        {
            internal AnimatorState State;

            internal StateMachineBehaviour[] Behaviours;
        }

        internal static HashSet<UnityEngine.Object> ListupObjectsInLayer(AnimatorControllerLayer layer)
        {
            List<UnityEngine.Object> containObjs = new() { layer.stateMachine };
            containObjs.AddRange(ListupObjectsInStateMachine(layer.stateMachine));
            containObjs.AddRange(GetAllOverrideStateMotionPairs(layer).Select(x => x.State));
            containObjs.AddRange(GetAllOverrideBehavioursPairs(layer).Select(x => x.State));

            return containObjs.ToHashSet();
        }

        internal static HashSet<UnityEngine.Object> ListupObjectsInStateMachine(AnimatorStateMachine stateMachine)
        {
            if (stateMachine == null) { return new(); }
            List<UnityEngine.Object> containObjs = new() { };

            Queue<AnimatorStateMachine> searchQueue = new() { };
            searchQueue.Enqueue(stateMachine);
            List<AnimatorStateMachine> searchedList = new();
            while (searchQueue.Count > 0)
            {
                AnimatorStateMachine curASM = searchQueue.Dequeue();

                containObjs.AddRange(curASM.entryTransitions);
                containObjs.AddRange(curASM.anyStateTransitions);

                IEnumerable<AnimatorState> states = curASM.states.Select(x => x.state);
                containObjs.AddRange(states);
                containObjs.AddRange(states.SelectMany(x => x.transitions));

                IEnumerable<AnimatorStateMachine> innerStateMachines = curASM.stateMachines.Select(x => x.stateMachine);
                containObjs.AddRange(innerStateMachines);
                foreach (AnimatorStateMachine innerStateMachine in innerStateMachines)
                {
                    containObjs.AddRange(curASM.GetStateMachineTransitions(innerStateMachine));
                }

                searchedList.Add(curASM);

                foreach (AnimatorStateMachine item in innerStateMachines.Where(x => !searchedList.Contains(x)))
                {
                    searchQueue.Enqueue(item);
                }
            }

            return containObjs.ToHashSet();
        }

        public static bool CheckAndAddObjectToAsset(UnityEngine.Object objectToAdd, AnimatorController controller) => CheckAndAddObjectToAsset(objectToAdd, AssetDatabase.GetAssetPath(controller));

        public static bool CheckAndAddObjectToAsset(UnityEngine.Object objectToAdd, string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("無効なパスが指定されました。");

            bool doAdd = objectToAdd != null && AssetDatabase.GetAssetPath(objectToAdd) == "";
            if (doAdd)
            {
                AssetDatabase.AddObjectToAsset(objectToAdd, path);
            }
            return doAdd;
        }

        public static HashSet<UnityEngine.Object> AddObjectToAssetRecursively(UnityEngine.Object objectToAdd, AnimatorController controller) => AddObjectToAssetRecursively(objectToAdd, AssetDatabase.GetAssetPath(controller));

        public static HashSet<UnityEngine.Object> AddObjectToAssetRecursively(UnityEngine.Object objectToAdd, string path)
        {
            if (objectToAdd == null) throw new ArgumentNullException("指定された UnityEngine.Object は null です。");
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("無効なパスが指定されました。");

            RecursiveSearchContext context = new();
            HashSet<UnityEngine.Object> addedObjects = AddObjectToAssetRecursivelyInternal(objectToAdd, path, context);

            return addedObjects;
        }

        private static HashSet<UnityEngine.Object> AddObjectToAssetRecursivelyInternal(UnityEngine.Object objectToAdd, string path, RecursiveSearchContext context)
        {
            if (objectToAdd == null || context.SearchedObjects.Contains(objectToAdd)) return new();
            context.SearchedObjects.Add(objectToAdd);

            HashSet<UnityEngine.Object> addedObjects = new();
            bool added = CheckAndAddObjectToAsset(objectToAdd, path);
            if (added) addedObjects.Add(objectToAdd);

            if (AssetDatabase.GetAssetPath(objectToAdd) != path) return addedObjects;

            using SerializedObject so = new(objectToAdd);
            SerializedProperty prop = so.GetIterator();
            while (prop.Next(true))
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    addedObjects.UnionWith(AddObjectToAssetRecursivelyInternal(prop.objectReferenceValue, path, context));
                }
            }

            return addedObjects;
        }

        public static void NormalizeAnimator(AnimatorController animator)
        {
            foreach (AnimatorControllerLayer layer in animator.layers)
            {
                AnimatorStateMachine[] innerStateMachines = GetAllStateMachineRecursively(layer.stateMachine);

                List<AnimatorStateTransition> anyStateTransitions = new();
                anyStateTransitions.AddRange(layer.stateMachine.anyStateTransitions);
                foreach (AnimatorStateMachine curStateMachine in innerStateMachines)
                {
                    anyStateTransitions.AddRange(curStateMachine.anyStateTransitions);
                    curStateMachine.anyStateTransitions = Array.Empty<AnimatorStateTransition>();
                }

                layer.stateMachine.anyStateTransitions = anyStateTransitions.ToArray();
            }

            string path = AssetDatabase.GetAssetPath(animator);
            if (!string.IsNullOrEmpty(path))
            {
                AddObjectToAssetRecursively(animator, path);
                RemoveUnusedSubAssets(animator);
            }
        }

        private static AnimatorStateMachine[] GetAllStateMachineRecursively(AnimatorStateMachine stateMachine)
        {
            List<AnimatorStateMachine> stateMachines = new();
            foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines)
            {
                stateMachines.Add(childStateMachine.stateMachine);
                stateMachines.AddRange(GetAllStateMachineRecursively(childStateMachine.stateMachine));
            }
            return stateMachines.ToArray();
        }


        private static bool RemoveUnusedSubAssets(UnityEngine.Object obj)
        {
            // TODO:循環参照があると使用してなくても削除されないので、ルートからの到達可能性で削除するべき
            // Based on lilEditorToolbox by lilxyzw (MIT License)
            // https://github.com/lilxyzw/lilEditorToolbox/blob/8a7d26ee90d67be02499d2f4b64e5ac788d942ce/Editor/Utils/SubAssetCleaner.cs

            string path = AssetDatabase.GetAssetPath(obj);
            HashSet<UnityEngine.Object> allAssets = AssetDatabase.LoadAllAssetsAtPath(path)
                                         .Where(a => a != null)
                                         .ToHashSet();

            // ルートから到達可能なノードをBFSでマーク
            HashSet<UnityEngine.Object> reachable = new();
            Queue<UnityEngine.Object> queue = new();
            queue.Enqueue(obj);

            while (queue.Count > 0)
            {
                UnityEngine.Object current = queue.Dequeue();
                if (!reachable.Add(current)) continue; // 訪問済みならスキップ

                // SerializedObjectで子参照を辿る
                using SerializedObject so = new(current);
                SerializedProperty prop = so.GetIterator();
                while (prop.Next(true))
                {
                    if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                    UnityEngine.Object referenced = prop.objectReferenceValue;
                    if (referenced != null && allAssets.Contains(referenced) && !reachable.Contains(referenced))
                    {
                        queue.Enqueue(referenced);
                    }
                }
            }

            // 到達不能なものを削除
            UnityEngine.Object[] unreachable = allAssets.Except(reachable).ToArray();
            foreach (UnityEngine.Object asset in unreachable)
            {
                AssetDatabase.RemoveObjectFromAsset(asset);
            }

            return unreachable.Length > 0;
        }

        public static IReadOnlyCollection<InvalidNullMember> ValidateCloneResult(UnityEngine.Object target) => ValidateCloneResultInternal(target);

        public static IReadOnlyCollection<InvalidNullMember> ValidateCloneResults(IEnumerable<UnityEngine.Object> targets) => targets.SelectMany(t => ValidateCloneResult(t)).ToHashSet();

        private static IReadOnlyCollection<InvalidNullMember> ValidateCloneResultInternal(object target)
        {
            if (target == null)
            {
                return new List<InvalidNullMember>();
            }

            HashSet<UnityEngine.Object> visitedObjSet = new();

            IReadOnlyCollection<InvalidNullMember> unregisteredList = target switch
            {
                AnimatorController castedObj => ValidateAnimatorControllerCloneResult(castedObj, null, "", ref visitedObjSet),
                AnimatorControllerLayer castedObj => ValidateAnimatorControllerLayerCloneResult(castedObj, null, "", ref visitedObjSet),
                ChildAnimatorStateMachine castedObj => ValidateChildAnimatorStateMachineCloneResult(castedObj, null, "", ref visitedObjSet),
                AnimatorStateMachine castedObj => ValidateAnimatorStateMachineCloneResult(castedObj, null, "", ref visitedObjSet),
                ChildAnimatorState castedObj => ValidateChildAnimatorStateCloneResult(castedObj, null, "", ref visitedObjSet),
                AnimatorState castedObj => ValidateAnimatorStateCloneResult(castedObj, null, "", ref visitedObjSet),
                AnimatorTransition castedObj => ValidateAnimatorTransitionCloneResult(castedObj, null, "", ref visitedObjSet),
                AnimatorStateTransition castedObj => ValidateAnimatorStateTransitionCloneResult(castedObj, null, "", ref visitedObjSet),
                StateMachineBehaviour castedObj => ValidateStateMachineBehaviourCloneResult(castedObj, null, "", ref visitedObjSet),
                _ => new InvalidNullMember[0],
            };

            return unregisteredList;
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateAnimatorControllerCloneResult(AnimatorController target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (target == null) return new InvalidNullMember[] { new(parent, memberName) };

            if (visitedObjSet.Contains(target)) return new InvalidNullMember[0];
            visitedObjSet.Add(target);

            HashSet<InvalidNullMember> invalidNullMembers = new();

            invalidNullMembers.UnionWith(ValidateAnimatorControllerLayersCloneResult(target.layers, target, nameof(target.layers), ref visitedObjSet));

            return invalidNullMembers;
        }

        public static IReadOnlyCollection<InvalidNullMember> ValidateAnimatorControllerLayersCloneResult(IEnumerable<AnimatorControllerLayer> target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach (AnimatorControllerLayer acl in target)
            {
                invalidNullMembers.UnionWith(ValidateAnimatorControllerLayerCloneResult(acl, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return invalidNullMembers;
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateAnimatorControllerLayerCloneResult(AnimatorControllerLayer target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (target == null) return new InvalidNullMember[] { new(parent, memberName) };

            HashSet<InvalidNullMember> invalidNullMembers = new();
            invalidNullMembers.UnionWith(ValidateAnimatorStateMachineCloneResult(target.stateMachine, parent, $"{memberName}.{nameof(target.stateMachine)}", ref visitedObjSet));

            StateMotionPair[] overrideStateMotionPairs = GetAllOverrideStateMotionPairs(target);
            foreach (StateMotionPair pair in overrideStateMotionPairs)
            {
                invalidNullMembers.UnionWith(ValidateAnimatorStateCloneResult(pair.State, parent, $"{memberName}.m_Motions.m_State", ref visitedObjSet));
            }
            StateBehavioursPair[] overrideBehavioursPairs = GetAllOverrideBehavioursPairs(target);
            foreach (StateBehavioursPair pair in overrideBehavioursPairs)
            {
                invalidNullMembers.UnionWith(ValidateAnimatorStateCloneResult(pair.State, parent, $"{memberName}.m_Behaviours.m_State", ref visitedObjSet));
                invalidNullMembers.UnionWith(ValidateStateMachineBehavioursCloneResult(pair.Behaviours, parent, $"{memberName}.m_Behaviours.m_Behaviours", ref visitedObjSet));
            }
            return invalidNullMembers;
        }

        public static IReadOnlyCollection<InvalidNullMember> ValidateChildAnimatorStateMachinesCloneResult(IEnumerable<ChildAnimatorStateMachine> targets, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach (ChildAnimatorStateMachine target in targets)
            {
                invalidNullMembers.UnionWith(ValidateChildAnimatorStateMachineCloneResult(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return invalidNullMembers;
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateChildAnimatorStateMachineCloneResult(ChildAnimatorStateMachine target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            return ValidateAnimatorStateMachineCloneResult(target.stateMachine, parent, $"{memberName}.{nameof(target.stateMachine)}", ref visitedObjSet);
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateAnimatorStateMachineCloneResult(AnimatorStateMachine target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (target == null) return new InvalidNullMember[] { new(parent, memberName) };

            if (visitedObjSet.Contains(target)) return new InvalidNullMember[0];
            visitedObjSet.Add(target);

            HashSet<InvalidNullMember> invalidNullMembers = new();

            invalidNullMembers.UnionWith(ValidateChildAnimatorStatesCloneResult(target.states, target, nameof(target), ref visitedObjSet));
            invalidNullMembers.UnionWith(ValidateChildAnimatorStateMachinesCloneResult(target.stateMachines, target, nameof(target), ref visitedObjSet));
            invalidNullMembers.UnionWith(ValidateAnimatorStateCloneResult(target.defaultState, target, nameof(target), ref visitedObjSet));
            invalidNullMembers.UnionWith(ValidateAnimatorTransitionsCloneResult(target.entryTransitions, target, nameof(target), ref visitedObjSet));
            invalidNullMembers.UnionWith(ValidateAnimatorStateTransitionsCloneResult(target.anyStateTransitions, target, nameof(target.anyStateTransitions), ref visitedObjSet));
            int i = 0;
            foreach (ChildAnimatorStateMachine curCASM in target.stateMachines)
            {
                //TODO:ここのネイティブコードでのm_StateMachineTransitionsが見れるかデバッグモードで確認
                AnimatorTransition[] transitions = target.GetStateMachineTransitions(curCASM.stateMachine);
                invalidNullMembers.UnionWith(ValidateAnimatorTransitionsCloneResult(transitions, target, $"StateMachineTransitions[{curCASM.stateMachine.name}]", ref visitedObjSet));
                i++;
            }
            invalidNullMembers.UnionWith(ValidateStateMachineBehavioursCloneResult(target.behaviours, target, nameof(target), ref visitedObjSet));

            return invalidNullMembers;

        }

        public static IReadOnlyCollection<InvalidNullMember> ValidateChildAnimatorStatesCloneResult(IEnumerable<ChildAnimatorState> targets, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach (ChildAnimatorState target in targets)
            {
                invalidNullMembers.UnionWith(ValidateChildAnimatorStateCloneResult(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return invalidNullMembers;
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateChildAnimatorStateCloneResult(ChildAnimatorState target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            return ValidateAnimatorStateCloneResult(target.state, parent, $"{memberName}.{nameof(target.state)}", ref visitedObjSet);
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateAnimatorStateCloneResult(AnimatorState target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (target == null) return new InvalidNullMember[] { new(parent, memberName) };

            if (visitedObjSet.Contains(target)) return new InvalidNullMember[0];
            visitedObjSet.Add(target);

            HashSet<InvalidNullMember> invalidNullMembers = new();

            invalidNullMembers.UnionWith(ValidateAnimatorStateTransitionsCloneResult(target.transitions, target, nameof(target.transitions), ref visitedObjSet));
            invalidNullMembers.UnionWith(ValidateStateMachineBehavioursCloneResult(target.behaviours, target, nameof(target.behaviours), ref visitedObjSet));

            return invalidNullMembers;

        }

        public static IReadOnlyCollection<InvalidNullMember> ValidateAnimatorTransitionsCloneResult(IEnumerable<AnimatorTransition> targets, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach (AnimatorTransition target in targets)
            {
                invalidNullMembers.UnionWith(ValidateAnimatorTransitionCloneResult(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return invalidNullMembers;
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateAnimatorTransitionCloneResult(AnimatorTransition target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (target == null) return new InvalidNullMember[] { new(parent, memberName) };

            if (visitedObjSet.Contains(target)) return new InvalidNullMember[0];
            visitedObjSet.Add(target);

            HashSet<InvalidNullMember> invalidNullMembers = new();

            invalidNullMembers.UnionWith(ValidateAnimatorStateCloneResult(target.destinationState, target, nameof(target.destinationState), ref visitedObjSet));
            invalidNullMembers.UnionWith(ValidateAnimatorStateMachineCloneResult(target.destinationStateMachine, target, nameof(target.destinationStateMachine), ref visitedObjSet));

            return invalidNullMembers;

        }

        public static IReadOnlyCollection<InvalidNullMember> ValidateAnimatorStateTransitionsCloneResult(IEnumerable<AnimatorStateTransition> targets, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach (AnimatorStateTransition target in targets)
            {
                invalidNullMembers.UnionWith(ValidateAnimatorStateTransitionCloneResult(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return invalidNullMembers;
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateAnimatorStateTransitionCloneResult(AnimatorStateTransition target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (target == null) return new InvalidNullMember[] { new(parent, memberName) };

            if (visitedObjSet.Contains(target)) return new InvalidNullMember[0];
            visitedObjSet.Add(target);

            HashSet<InvalidNullMember> invalidNullMembers = new();

            if (!target.isExit)
            {
                invalidNullMembers.UnionWith(ValidateAnimatorStateCloneResult(target.destinationState, target, nameof(target.destinationState), ref visitedObjSet));
                invalidNullMembers.UnionWith(ValidateAnimatorStateMachineCloneResult(target.destinationStateMachine, target, nameof(target.destinationStateMachine), ref visitedObjSet));
            }

            return invalidNullMembers;

        }

        public static IReadOnlyCollection<InvalidNullMember> ValidateStateMachineBehavioursCloneResult(IEnumerable<StateMachineBehaviour> targets, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach (StateMachineBehaviour target in targets)
            {
                invalidNullMembers.UnionWith(ValidateStateMachineBehaviourCloneResult(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return invalidNullMembers;
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateStateMachineBehaviourCloneResult(StateMachineBehaviour target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            // TODO:未実装 実装するかも未定
            return new InvalidNullMember[0];
            //if (visitedObjSet.Contains(target)) return new InvalidNullMember[0];
            //visitedObjSet.Add(target);
            //
            //HashSet<InvalidNullMember> invalidNullMembers = new();
            //
            ////invalidNullMembers.UnionWith(ValidateAnimatorControllerLayersCloneResult(target., target, nameof(target.), ref visitedObjSet));
            //
            //return invalidNullMembers;
        }

        private class RecursiveSearchContext
        {
            internal HashSet<object> SearchedObjects = new();
        }

        public record InvalidNullMember
        {
            UnityEngine.Object Parent { get; }
            string MemberName { get; }

            public InvalidNullMember(UnityEngine.Object parent, string memberName)
            {
                Parent = parent;
                MemberName = memberName;
            }
        }
    }
}