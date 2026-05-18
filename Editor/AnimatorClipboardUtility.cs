
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


        internal static HashSet<object> ListupInLayer(AnimatorControllerLayer layer)
        {
            List<object> containObjs = new();

            containObjs.AddRange(ListupInStateMachineObjectsAndSelf(layer.stateMachine));
            containObjs.AddRange(GetAllOverrideStateMotionPairs(layer).Select(x => x.State));
            containObjs.AddRange(GetAllOverrideBehavioursPairs(layer).Select(x => x.State));

            return containObjs.ToHashSet();
        }

        internal static HashSet<object> ListupInStateMachineObjectsAndSelf(AnimatorStateMachine stateMachine)
        {
            if (stateMachine == null) { return new(); }
            List<object> containObjs = new() { stateMachine };

            List<AnimatorStateMachine> searchQueue = new() { stateMachine };
            List<AnimatorStateMachine> searchedList = new();
            while (searchQueue.Count > 0)
            {
                AnimatorStateMachine curASM = searchQueue.Last();

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

                searchQueue.Remove(curASM);
                searchedList.Add(curASM);

                searchQueue.AddRange(innerStateMachines.Where(x => !searchedList.Contains(x)));
            }

            return containObjs.ToHashSet();
        }

        internal static bool CheckAndAddObjectToAsset(UnityEngine.Object objectToAdd, string path)
        {
            bool doAdd = AssetDatabase.GetAssetPath(objectToAdd) == "" && !string.IsNullOrEmpty(path);
            if (doAdd)
            {
                AssetDatabase.AddObjectToAsset(objectToAdd, path);
            }
            return doAdd;
        }

        public static UnityEngine.Object[] AddObjectToAssetRecursively(UnityEngine.Object objectToAdd, string path)
        {
            if (objectToAdd == null) throw new ArgumentNullException("The specified UnityEngine.Object is null.");
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("An invalid path was specified.");

            string propPath = AssetDatabase.GetAssetPath(objectToAdd);
            List<UnityEngine.Object> addedObjects = new();

            if (propPath == "")
            {
                AssetDatabase.AddObjectToAsset(objectToAdd, path);
                addedObjects.Add(objectToAdd);
                propPath = path;
            }

            if (propPath == path)
            {
                RecursiveSearchContext context = new();
                context.SearchedObjects.Add(objectToAdd);
                addedObjects.AddRange(AddObjectToAssetRecursively(objectToAdd, path, context));
            }

            return addedObjects.ToArray();
        }

        private static UnityEngine.Object[] AddObjectToAssetRecursively(UnityEngine.Object objectToAdd, string path, RecursiveSearchContext context)
        {
            if (objectToAdd == null || context.SearchedObjects.Contains(objectToAdd)) return Array.Empty<UnityEngine.Object>();
            context.SearchedObjects.Add(objectToAdd);

            List<UnityEngine.Object> addedObjects = new();

            SerializedObject so = new(objectToAdd);
            SerializedProperty prop = so.GetIterator();
            while (prop.Next(true))
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference && prop.objectReferenceValue != null)
                {
                    string propPath = AssetDatabase.GetAssetPath(prop.objectReferenceValue);
                    if (propPath == "")
                    {
                        AssetDatabase.AddObjectToAsset(prop.objectReferenceValue, path);
                        addedObjects.Add(prop.objectReferenceValue);
                        propPath = path;
                    }

                    if (propPath == path)
                    {
                        addedObjects.AddRange(AddObjectToAssetRecursively(prop.objectReferenceValue, path, context));
                    }
                }
            }

            return addedObjects.ToArray();
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
            // Based on lilEditorToolbox by lilxyzw (MIT License)
            // https://github.com/lilxyzw/lilEditorToolbox/blob/8a7d26ee90d67be02499d2f4b64e5ac788d942ce/Editor/Utils/SubAssetCleaner.cs

            bool isCleaned = false;
            var path = AssetDatabase.GetAssetPath(obj);
            while (true)
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath(path).Where(asset => asset);
                var usedAssetsTemp = new HashSet<UnityEngine.Object>();
                foreach (var asset in assets)
                {
                    var so = new SerializedObject(asset);
                    var prop = so.GetIterator();
                    while (prop.Next(true))
                    {
                        if (prop.propertyType == SerializedPropertyType.ObjectReference && prop.objectReferenceValue)
                        {
                            usedAssetsTemp.Add(prop.objectReferenceValue);
                        }
                    }
                }
                bool shouldContinue = false;
                foreach (var asset in assets.Where(asset => !usedAssetsTemp.Contains(asset)))
                {
                    // Debug.Log($"[AnimatorClipboard] Remove from {obj.name}: {asset.name}");
                    AssetDatabase.RemoveObjectFromAsset(asset);
                    shouldContinue = true;
                }
                if (!shouldContinue) break;
                isCleaned = true;
            }
            return isCleaned;
        }

        private class RecursiveSearchContext
        {
            internal HashSet<object> SearchedObjects = new();
        }
    }
}