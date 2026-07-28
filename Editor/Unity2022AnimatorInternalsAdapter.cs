using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor.Animations;
using UnityEngine;

namespace com.github.k_stand.ksanimatorcopyengine.editor
{
    internal sealed class Unity2022AnimatorInternalsAdapter : IAnimatorInternalsAdapter
    {
        internal static readonly IReadOnlyCollection<int> SupportedMajorVersions = new[] { 2022 };

        // Unity内部のinternalな型を取得
        private readonly Type _stateMotionPairType = typeof(AnimatorControllerLayer).Assembly.GetType("UnityEditor.Animations.StateMotionPair");
        private readonly Type _stateBehavioursPairType = typeof(AnimatorControllerLayer).Assembly.GetType("UnityEditor.Animations.StateBehavioursPair");

        private readonly Func<AnimatorControllerLayer, Array> _layerMotionsGetter;
        private readonly Func<AnimatorControllerLayer, Array> _layerBehavioursGetter;
        private readonly Func<object, AnimatorState> _stateGetter;
        private readonly Func<object, Motion> _motionGetter;
        private readonly Func<object, ScriptableObject[]> _behavioursGetter;

        private readonly Action<AnimatorControllerLayer, Array> _layerMotionsSetter;
        private readonly Action<AnimatorControllerLayer, Array> _layerBehavioursSetter;
        private readonly Action<object, AnimatorState> _stateSetter;
        private readonly Action<object, Motion> _motionSetter;
        private readonly Action<object, ScriptableObject[]> _behavioursSetter;

        internal Unity2022AnimatorInternalsAdapter()
        {
            _layerMotionsGetter = CreateFieldGetter<AnimatorControllerLayer, Array>("m_Motions");
            _layerBehavioursGetter = CreateFieldGetter<AnimatorControllerLayer, Array>("m_Behaviours");
            _stateGetter = CreateFieldGetterFromType<AnimatorState>(_stateMotionPairType, "m_State");
            _motionGetter = CreateFieldGetterFromType<Motion>(_stateMotionPairType, "m_Motion");
            _behavioursGetter = CreateFieldGetterFromType<ScriptableObject[]>(_stateBehavioursPairType, "m_Behaviours");

            _layerMotionsSetter = CreateFieldSetter<AnimatorControllerLayer, Array>("m_Motions");
            _layerBehavioursSetter = CreateFieldSetter<AnimatorControllerLayer, Array>("m_Behaviours");
            _stateSetter = CreateFieldSetterFromType<AnimatorState>(_stateMotionPairType, "m_State");
            _motionSetter = CreateFieldSetterFromType<Motion>(_stateMotionPairType, "m_Motion");
            _behavioursSetter = CreateFieldSetterFromType<ScriptableObject[]>(_stateBehavioursPairType, "m_Behaviours");
        }

        private Func<TTarget, TValue> CreateFieldGetter<TTarget, TValue>(string fieldName)
        {
            Func<object, TValue> getterFromType = CreateFieldGetterFromType<TValue>(typeof(TTarget), fieldName);
            return target => getterFromType(target);
        }

        private Func<object, TValue> CreateFieldGetterFromType<TValue>(Type targetType, string fieldName)
        {
            FieldInfo fieldInfo = targetType.GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance) ?? throw new InvalidOperationException($"Field '{fieldName}' not found in '{targetType.FullName}'");

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

        private Action<TTarget, TValue> CreateFieldSetter<TTarget, TValue>(string fieldName)
        {
            Action<object, TValue> setterFromType = CreateFieldSetterFromType<TValue>(typeof(TTarget), fieldName);
            return (target, value) => setterFromType(target, value);
        }

        private Action<object, TValue> CreateFieldSetterFromType<TValue>(Type targetType, string fieldName)
        {
            // フィールドが見つからない場合、getter側と同じくInvalidOperationExceptionで即座に失敗させる
            // (修正前はここがnullのままExpression.Fieldに渡り、素のArgumentNullExceptionになっていた)
            FieldInfo fieldInfo = targetType.GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance) ?? throw new InvalidOperationException($"Field '{fieldName}' not found in '{targetType.FullName}'");

            ParameterExpression targetParam = Expression.Parameter(typeof(object));
            ParameterExpression valueParam = Expression.Parameter(typeof(TValue));

            Expression<Action<object, TValue>> lambda =
                Expression.Lambda<Action<object, TValue>>(
                    // ラムダ式で処理する内容
                    Expression.Assign(
                        Expression.Field(
                            Expression.Convert(targetParam, targetType),
                            fieldInfo),
                        Expression.Convert(valueParam, fieldInfo.FieldType)
                    ),
                    targetParam, valueParam
                );

            return lambda.Compile();
        }

        /// <summary>
        /// AnimatorControllerLayer.m_Motionsの内容を取得し、型変換した上で返します。
        /// </summary>
        /// <param name="acl"></param>
        /// <returns>型変換されたAnimatorControllerLayer.m_Motionsのデータ。m_Motionsがnullならnullを返します。</returns>
        public StateMotionPair[] GetAllOverrideStateMotionPairs(AnimatorControllerLayer acl)
        {
            Array stateMotionPairs = _layerMotionsGetter(acl);

            StateMotionPair[] pairs = null;
            if (stateMotionPairs != null)
            {
                pairs = new StateMotionPair[stateMotionPairs.Length];
                for (int i = 0; i < stateMotionPairs.Length; i++)
                {
                    object pair = stateMotionPairs.GetValue(i);
                    pairs[i].State = _stateGetter(pair);
                    pairs[i].Motion = _motionGetter(pair);
                }
            }

            return pairs;
        }

        /// <summary>
        /// AnimatorControllerLayer.m_Behavioursの内容を取得し、型変換した上で返します。
        /// </summary>
        /// <param name="acl"></param>
        /// <returns>型変換されたAnimatorControllerLayer.m_Behavioursのデータ。m_Behavioursがnullならnullを返します。</returns>
        public StateBehavioursPair[] GetAllOverrideBehavioursPairs(AnimatorControllerLayer acl)
        {
            Array stateBehavioursPairs = _layerBehavioursGetter(acl);

            StateBehavioursPair[] pairs = null;
            if (stateBehavioursPairs != null)
            {
                pairs = new StateBehavioursPair[stateBehavioursPairs.Length];
                for (int i = 0; i < stateBehavioursPairs.Length; i++)
                {
                    object pair = stateBehavioursPairs.GetValue(i);
                    pairs[i].State = _stateGetter(pair);
                    pairs[i].Behaviours = _behavioursGetter(pair).Select(x => (StateMachineBehaviour)x).ToArray();
                }
            }

            return pairs;
        }

        public void InitOverrideStateMotionPairs(AnimatorControllerLayer acl)
        {
            Array stateMotionPairs = _layerMotionsGetter(acl);
            if (stateMotionPairs == null)
            {
                // Unity内部の StateMotionPair[] と同じ型の配列を生成
                Array array = Array.CreateInstance(_stateMotionPairType, 0);
                _layerMotionsSetter(acl, array);
            }
        }

        public void InitOverrideStateBehavioursPairs(AnimatorControllerLayer acl)
        {
            Array stateBehavioursPairs = _layerBehavioursGetter(acl);
            if (stateBehavioursPairs == null)
            {
                // Unity内部の stateBehavioursPairs[] と同じ型の配列を生成
                Array array = Array.CreateInstance(_stateBehavioursPairType, 0);
                _layerBehavioursSetter(acl, array);
            }
        }

        public void Validate()
        {
            AnimatorControllerLayer probe = new() { name = "KSAnimatorCopyEngine_ValidationProbe" };

            InitOverrideStateMotionPairs(probe);
            StateMotionPair[] motionPairs = GetAllOverrideStateMotionPairs(probe)
                ?? throw new InvalidOperationException($"{nameof(InitOverrideStateMotionPairs)} completed without error, but {nameof(GetAllOverrideStateMotionPairs)} still returned null.");
            if (motionPairs.Length != 0)
            {
                throw new InvalidOperationException($"{nameof(InitOverrideStateMotionPairs)} did not produce an empty array as expected.");
            }

            InitOverrideStateBehavioursPairs(probe);
            StateBehavioursPair[] behavioursPairs = GetAllOverrideBehavioursPairs(probe)
                ?? throw new InvalidOperationException($"{nameof(InitOverrideStateBehavioursPairs)} completed without error, but {nameof(GetAllOverrideBehavioursPairs)} still returned null.");
            if (behavioursPairs.Length != 0)
            {
                throw new InvalidOperationException($"{nameof(InitOverrideStateBehavioursPairs)} did not produce an empty array as expected.");
            }
        }
    }
}
