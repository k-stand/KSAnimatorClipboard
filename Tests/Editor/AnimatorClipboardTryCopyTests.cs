using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using com.github.k_stand.ksanimatorclipboard.editor;

namespace com.github.k_stand.ksanimatorclipboard.editor.tests
{
    public class AnimatorClipboardTryCopyTests : AnimatorClipboardTestFixtureBase
    {
        [Test]
        public void TryCopy_Layers_ReturnsFalse_WhenGivenEmptyCollection()
        {
            AnimatorController parentController = Create<AnimatorController>();

            bool success = AnimatorClipboard.TryCopy(System.Array.Empty<AnimatorControllerLayer>(), parentController, out AnimatorCopyClipSet result);

            Assert.IsFalse(success);
            Assert.IsNull(result);
        }

        [Test]
        public void TryCopy_StateMachineCategory_ReturnsFalse_WhenGivenEmptyCollection()
        {
            AnimatorStateMachine ancestorStateMachine = Create<AnimatorStateMachine>();

            bool success = AnimatorClipboard.TryCopy(System.Array.Empty<object>(), ancestorStateMachine, out AnimatorCopyClipSet result);

            Assert.IsFalse(success);
            Assert.IsNull(result);
        }

        [Test]
        public void TryCopy_Behaviours_ReturnsFalse_WhenGivenEmptyCollection()
        {
            bool success = AnimatorClipboard.TryCopy(System.Array.Empty<StateMachineBehaviour>(), out AnimatorCopyClipSet result);

            Assert.IsFalse(success);
            Assert.IsNull(result);
        }

        [Test]
        public void TryCopy_Layers_ReturnsTrueAndPopulatesResult_WhenValidLayerGiven()
        {
            AnimatorStateMachine stateMachine = Create<AnimatorStateMachine>();
            AnimatorControllerLayer layer = new() { name = "Layer1", stateMachine = stateMachine };
            AnimatorController parentController = Create<AnimatorController>();

            bool success = AnimatorClipboard.TryCopy(layer, parentController, out AnimatorCopyClipSet result);

            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.Layers, result.Type);
        }

        [Test]
        public void Copy_Layers_StillThrows_WhenGivenEmptyCollection()
        {
            AnimatorController parentController = Create<AnimatorController>();

            Assert.Throws<System.ArgumentException>(() => AnimatorClipboard.Copy(System.Array.Empty<AnimatorControllerLayer>(), parentController));
        }
    }
}
