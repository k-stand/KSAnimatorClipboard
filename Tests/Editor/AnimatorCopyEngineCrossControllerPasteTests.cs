using NUnit.Framework;
using UnityEditor.Animations;
using com.github.k_stand.ksanimatorcopyengine.editor;

namespace com.github.k_stand.ksanimatorcopyengine.editor.tests
{
    public class AnimatorCopyEngineCrossControllerPasteTests : AnimatorCopyEngineTestFixtureBase
    {
        [Test]
        public void PasteLayers_ResetsSyncedLayerIndex_WhenPastingAcrossControllers()
        {
            AnimatorController sourceController = Create<AnimatorController>();
            AnimatorStateMachine baseStateMachine = Create<AnimatorStateMachine>();
            AnimatorStateMachine sourceStateMachine = Create<AnimatorStateMachine>();
            AnimatorControllerLayer baseLayer = new() { name = "BaseLayer", stateMachine = baseStateMachine };
            AnimatorControllerLayer sourceLayer = new() { name = "SourceLayer", stateMachine = sourceStateMachine, syncedLayerIndex = 0 };
            sourceController.layers = new[] { baseLayer, sourceLayer };

            AnimatorCopyClipSet clipSet = AnimatorCopyEngine.Copy(sourceLayer, sourceController);

            AnimatorController destController = Create<AnimatorController>();
            destController.layers = System.Array.Empty<AnimatorControllerLayer>();

            AnimatorControllerLayer[] pastedLayers = AnimatorCopyEngine.PasteLayers(clipSet, destController);

            Assert.AreEqual(-1, pastedLayers[0].syncedLayerIndex);
        }

        [Test]
        public void PasteLayers_KeepsSyncedLayerIndex_WhenPastingWithinSameController()
        {
            AnimatorController controller = Create<AnimatorController>();
            AnimatorStateMachine sourceStateMachine = Create<AnimatorStateMachine>();
            AnimatorStateMachine otherStateMachine = Create<AnimatorStateMachine>();
            AnimatorControllerLayer otherLayer = new() { name = "OtherLayer", stateMachine = otherStateMachine };
            AnimatorControllerLayer sourceLayer = new() { name = "SourceLayer", stateMachine = sourceStateMachine, syncedLayerIndex = 0 };
            controller.layers = new[] { otherLayer, sourceLayer };

            AnimatorCopyClipSet clipSet = AnimatorCopyEngine.Copy(sourceLayer, controller);

            AnimatorControllerLayer[] pastedLayers = AnimatorCopyEngine.PasteLayers(clipSet, controller);

            Assert.AreEqual(0, pastedLayers[0].syncedLayerIndex);
        }
    }
}
