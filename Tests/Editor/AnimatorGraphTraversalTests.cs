using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using com.github.k_stand.ksanimatorcopyengine.editor;

namespace com.github.k_stand.ksanimatorcopyengine.editor.tests
{
    public class AnimatorGraphTraversalTests : AnimatorCopyEngineTestFixtureBase
    {
        [Test]
        public void ListupObjectsInStateMachine_ReturnsEmptySet_WhenStateMachineIsNull()
        {
            HashSet<Object> result = AnimatorGraphTraversal.ListupObjectsInStateMachine(null);

            Assert.IsEmpty(result);
        }

        [Test]
        public void ListupObjectsInStateMachine_CollectsStatesTransitionsAndNestedStateMachines()
        {
            AnimatorStateMachine rootStateMachine = Create<AnimatorStateMachine>();
            AnimatorState state1 = Create<AnimatorState>();
            rootStateMachine.AddState(state1, Vector3.zero);
            AnimatorStateTransition transition = state1.AddTransition(state1);

            AnimatorStateMachine childStateMachine = Create<AnimatorStateMachine>();
            AnimatorState childState = Create<AnimatorState>();
            childStateMachine.AddState(childState, Vector3.zero);
            rootStateMachine.stateMachines = new[] { new ChildAnimatorStateMachine { stateMachine = childStateMachine } };

            HashSet<Object> result = AnimatorGraphTraversal.ListupObjectsInStateMachine(rootStateMachine);

            Assert.IsTrue(result.Contains(state1));
            Assert.IsTrue(result.Contains(transition));
            Assert.IsTrue(result.Contains(childStateMachine));
            Assert.IsTrue(result.Contains(childState));
        }

        [Test]
        public void ListupObjectsInLayer_CollectsStateMachineContents_WhenOverridesAreUninitialized()
        {
            AnimatorStateMachine stateMachine = Create<AnimatorStateMachine>();
            AnimatorState state = Create<AnimatorState>();
            stateMachine.AddState(state, Vector3.zero);
            AnimatorControllerLayer layer = new() { name = "Layer1", stateMachine = stateMachine };

            HashSet<Object> result = AnimatorGraphTraversal.ListupObjectsInLayer(layer);

            Assert.IsTrue(result.Contains(stateMachine));
            Assert.IsTrue(result.Contains(state));
        }
    }
}
