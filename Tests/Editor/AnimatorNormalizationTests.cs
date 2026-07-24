using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using com.github.k_stand.ksanimatorclipboard.editor;

namespace com.github.k_stand.ksanimatorclipboard.editor.tests
{
    public class AnimatorNormalizationTests : AnimatorClipboardTestFixtureBase
    {
        [Test]
        public void NormalizeAnyStateTransitions_HoistsNestedAnyStateTransitionsToTopLevel()
        {
            AnimatorStateMachine rootStateMachine = Create<AnimatorStateMachine>();
            AnimatorStateMachine childStateMachine = Create<AnimatorStateMachine>();
            rootStateMachine.stateMachines = new[] { new ChildAnimatorStateMachine { stateMachine = childStateMachine } };

            AnimatorState targetState = Create<AnimatorState>();
            childStateMachine.AddState(targetState, Vector3.zero);
            AnimatorStateTransition nestedAnyStateTransition = childStateMachine.AddAnyStateTransition(targetState);

            AnimatorControllerLayer layer = new() { name = "Layer1", stateMachine = rootStateMachine };

            AnimatorNormalization.NormalizeAnyStateTransitions(layer);

            CollectionAssert.Contains(rootStateMachine.anyStateTransitions, nestedAnyStateTransition);
            Assert.IsEmpty(childStateMachine.anyStateTransitions);
        }

        [Test]
        public void NormalizeAnimator_DoesNotThrow_ForInMemoryController()
        {
            AnimatorController controller = Create<AnimatorController>();
            AnimatorStateMachine stateMachine = Create<AnimatorStateMachine>();
            AnimatorControllerLayer layer = new() { name = "Layer1", stateMachine = stateMachine };
            controller.layers = new[] { layer };

            Assert.DoesNotThrow(() => AnimatorNormalization.NormalizeAnimator(controller));
        }
    }
}
