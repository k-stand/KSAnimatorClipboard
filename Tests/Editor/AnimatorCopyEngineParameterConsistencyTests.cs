using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using com.github.k_stand.ksanimatorcopyengine.editor;

namespace com.github.k_stand.ksanimatorcopyengine.editor.tests
{
    public class AnimatorCopyEngineParameterConsistencyTests : AnimatorCopyEngineTestFixtureBase
    {
        [Test]
        public void FindMissingParameters_ThrowsArgumentNullException_WhenClipSetIsNull()
        {
            AnimatorController controller = Create<AnimatorController>();
            Assert.Throws<ArgumentNullException>(() => AnimatorCopyEngineParameterConsistency.FindMissingParameters(null, controller));
        }

        [Test]
        public void FindMissingParameters_ThrowsArgumentNullException_WhenDestControllerIsNull()
        {
            AnimatorStateTransition transition = Create<AnimatorStateTransition>();
            AnimatorCopyClipSet clipSet = AnimatorCopyEngine.Copy(transition);
            Assert.Throws<ArgumentNullException>(() => AnimatorCopyEngineParameterConsistency.FindMissingParameters(clipSet, null));
        }

        [Test]
        public void FindMissingParameters_ReturnsEmpty_WhenAllParametersExist()
        {
            AnimatorController controller = Create<AnimatorController>();
            controller.parameters = new[] { new AnimatorControllerParameter { name = "Speed", type = AnimatorControllerParameterType.Float } };

            AnimatorStateTransition transition = Create<AnimatorStateTransition>();
            transition.AddCondition(AnimatorConditionMode.Greater, 0f, "Speed");
            AnimatorCopyClipSet clipSet = AnimatorCopyEngine.Copy(transition);

            IReadOnlyList<string> missing = AnimatorCopyEngineParameterConsistency.FindMissingParameters(clipSet, controller);

            Assert.IsEmpty(missing);
        }

        [Test]
        public void FindMissingParameters_DetectsMissingParameter_FromStateTransitionCondition()
        {
            AnimatorController controller = Create<AnimatorController>();

            AnimatorStateTransition transition = Create<AnimatorStateTransition>();
            transition.AddCondition(AnimatorConditionMode.Greater, 0f, "Speed");
            AnimatorCopyClipSet clipSet = AnimatorCopyEngine.Copy(transition);

            IReadOnlyList<string> missing = AnimatorCopyEngineParameterConsistency.FindMissingParameters(clipSet, controller);

            CollectionAssert.AreEquivalent(new[] { "Speed" }, missing);
        }

        [Test]
        public void FindMissingParameters_DetectsMissingParameter_FromNestedStateMachineEntryTransition()
        {
            AnimatorController controller = Create<AnimatorController>();

            AnimatorStateMachine childStateMachine = Create<AnimatorStateMachine>();
            AnimatorState innerState = Create<AnimatorState>();
            childStateMachine.AddState(innerState, Vector3.zero);
            AnimatorTransition entryTransition = childStateMachine.AddEntryTransition(innerState);
            entryTransition.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");

            ChildAnimatorStateMachine childAnimatorStateMachine = new() { stateMachine = childStateMachine };
            AnimatorCopyClipSet clipSet = AnimatorCopyEngine.Copy(childAnimatorStateMachine);

            IReadOnlyList<string> missing = AnimatorCopyEngineParameterConsistency.FindMissingParameters(clipSet, controller);

            CollectionAssert.AreEquivalent(new[] { "Grounded" }, missing);
        }

        [Test]
        public void FindMissingParameters_IgnoresStateMachineBehaviourReferences()
        {
            AnimatorController controller = Create<AnimatorController>();
            DummyStateMachineBehaviour behaviour = Track(ScriptableObject.CreateInstance<DummyStateMachineBehaviour>());
            AnimatorState state = Create<AnimatorState>();
            state.behaviours = new StateMachineBehaviour[] { behaviour };
            ChildAnimatorState childState = new() { state = state };
            AnimatorCopyClipSet clipSet = AnimatorCopyEngine.Copy(childState);

            IReadOnlyList<string> missing = AnimatorCopyEngineParameterConsistency.FindMissingParameters(clipSet, controller);

            Assert.IsEmpty(missing);
        }

        [Test]
        public void FindMissingParameters_DoesNotDuplicateParameterNamesReferencedMultipleTimes()
        {
            AnimatorController controller = Create<AnimatorController>();

            AnimatorStateTransition transition1 = Create<AnimatorStateTransition>();
            transition1.AddCondition(AnimatorConditionMode.Greater, 0f, "Speed");
            AnimatorStateTransition transition2 = Create<AnimatorStateTransition>();
            transition2.AddCondition(AnimatorConditionMode.Less, 1f, "Speed");
            AnimatorCopyClipSet clipSet = AnimatorCopyEngine.Copy(new object[] { transition1, transition2 });

            IReadOnlyList<string> missing = AnimatorCopyEngineParameterConsistency.FindMissingParameters(clipSet, controller);

            CollectionAssert.AreEquivalent(new[] { "Speed" }, missing);
        }
    }
}
