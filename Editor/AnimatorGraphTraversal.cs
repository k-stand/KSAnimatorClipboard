using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

namespace com.github.k_stand.ksanimatorclipboard.editor
{
    // コピー範囲(IAnimatorCopyObjectKind.GetCloneScope)を求めるための、StateMachine配下オブジェクトの列挙。
    // 同じくグラフを辿るAnimatorGraphSchemaとは用途が異なるため列挙範囲も異なる: behaviours(AnimatorStateMachine/
    // AnimatorState双方)はここでは含めない。behavioursのClonePolicyはGetCloneScopeではなく、
    // AnimatorCloner.RegisterChildrenRecursivelyによる親子関係の登録(_parentMap経由の継承)で解決されるため。
    internal static class AnimatorGraphTraversal
    {
        internal static HashSet<UnityEngine.Object> ListupObjectsInLayer(AnimatorControllerLayer layer)
        {
            List<UnityEngine.Object> containObjs = new() { layer.stateMachine };
            containObjs.AddRange(ListupObjectsInStateMachine(layer.stateMachine));
            containObjs.AddRange((AnimatorInternalsAdapterProvider.Current.GetAllOverrideStateMotionPairs(layer) ?? Array.Empty<StateMotionPair>()).Select(x => x.State));
            containObjs.AddRange((AnimatorInternalsAdapterProvider.Current.GetAllOverrideBehavioursPairs(layer) ?? Array.Empty<StateBehavioursPair>()).Select(x => x.State));

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
    }
}
