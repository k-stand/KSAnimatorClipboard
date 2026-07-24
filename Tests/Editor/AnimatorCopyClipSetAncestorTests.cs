using NUnit.Framework;
using UnityEditor.Animations;
using com.github.k_stand.ksanimatorclipboard.editor;

namespace com.github.k_stand.ksanimatorclipboard.editor.tests
{
    public class AnimatorCopyClipSetAncestorTests : AnimatorClipboardTestFixtureBase
    {
        [Test]
        public void Layer_ContainedInParentController_IsNotAncestorMismatched()
        {
            AnimatorController parentController = Create<AnimatorController>();
            AnimatorStateMachine stateMachine = Create<AnimatorStateMachine>();
            AnimatorControllerLayer layer = new() { name = "Layer1", stateMachine = stateMachine };
            parentController.layers = new[] { layer };

            AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(layer, parentController);

            Assert.IsFalse(clipSet.IsAncestorMismatched);
            Assert.AreSame(parentController, clipSet.ParentController);
        }

        [Test]
        public void Layer_NotContainedInParentController_SetsIsAncestorMismatched_ButCopyStillSucceeds()
        {
            AnimatorController parentController = Create<AnimatorController>();
            AnimatorStateMachine otherStateMachine = Create<AnimatorStateMachine>();
            parentController.layers = new[] { new AnimatorControllerLayer { name = "OtherLayer", stateMachine = otherStateMachine } };

            AnimatorStateMachine stateMachine = Create<AnimatorStateMachine>();
            AnimatorControllerLayer layer = new() { name = "Layer1", stateMachine = stateMachine };

            AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(layer, parentController);

            Assert.IsTrue(clipSet.IsAncestorMismatched);
            Assert.IsNull(clipSet.ParentController);
        }

        [Test]
        public void ChildState_DescendantOfAncestorStateMachine_IsNotAncestorMismatched()
        {
            AnimatorStateMachine ancestorStateMachine = Create<AnimatorStateMachine>();
            AnimatorState state = Create<AnimatorState>();
            ChildAnimatorState childState = new() { state = state };
            ancestorStateMachine.states = new[] { childState };

            AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(childState, ancestorStateMachine);

            Assert.IsFalse(clipSet.IsAncestorMismatched);
            Assert.AreSame(ancestorStateMachine, clipSet.AncestorStateMachine);
        }

        [Test]
        public void ChildState_NotDescendantOfAncestorStateMachine_SetsIsAncestorMismatched_ButCopyStillSucceeds()
        {
            AnimatorStateMachine ancestorStateMachine = Create<AnimatorStateMachine>();
            AnimatorState state = Create<AnimatorState>();
            ChildAnimatorState childState = new() { state = state };
            // ancestorStateMachineの子孫としては登録しない

            AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(childState, ancestorStateMachine);

            Assert.IsTrue(clipSet.IsAncestorMismatched);
            Assert.IsNull(clipSet.AncestorStateMachine);
        }
    }
}
