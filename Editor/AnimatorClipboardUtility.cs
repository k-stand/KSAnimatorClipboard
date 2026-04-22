
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor.Animations;
using UnityEngine;

namespace io.github.kiriumestand.animatorclipboard.editor
{
    internal class AnimatorClipboardUtility
    {
        private static readonly Func<AnimatorControllerLayer, Array> LayerMotionsGetter = CreateFieldGetter<AnimatorControllerLayer, Array>("m_Motions");
        private static readonly Func<AnimatorControllerLayer, Array> LayerBehavioursGetter = CreateFieldGetter<AnimatorControllerLayer, Array>("m_Behaviours");
        private static readonly Func<object, AnimatorState> m_StateGetter = CreateFieldGetter<object, AnimatorState>("m_State");
        private static readonly Func<object, Motion> m_MotionGetter = CreateFieldGetter<object, Motion>("m_Motion");
        private static readonly Func<object, StateMachineBehaviour[]> m_BehavioursGetter = CreateFieldGetter<object, StateMachineBehaviour[]>("m_Behaviours");

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
                pairs[i].Behaviours = m_BehavioursGetter(pair);
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
            List<object> containObjs = new() { stateMachine };

            List<AnimatorStateMachine> searchQueue = new() { stateMachine };
            List<AnimatorStateMachine> searchedList = new();
            while (searchQueue.Count == 0)
            {
                AnimatorStateMachine curASM = searchQueue.Last();

                containObjs.AddRange(curASM.entryTransitions);
                containObjs.AddRange(curASM.anyStateTransitions);

                IEnumerable<AnimatorState> states = curASM.states.Select(x => x.state);
                containObjs.AddRange(states);
                containObjs.AddRange(states.SelectMany(x => x.transitions));

                IEnumerable<AnimatorStateMachine> stateMachines = curASM.stateMachines.Select(x => x.stateMachine);
                containObjs.AddRange(stateMachines);

                searchQueue.Remove(curASM);
                searchedList.Add(curASM);

                searchQueue.AddRange(stateMachines.Where(x => !searchedList.Contains(x)));
            }

            return containObjs.ToHashSet();
        }
    }
}