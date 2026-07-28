using NUnit.Framework;
using UnityEditor.Animations;
using com.github.k_stand.ksanimatorcopyengine.editor.CrossController;

namespace com.github.k_stand.ksanimatorcopyengine.editor.tests.CrossController
{
    public class CrossControllerPostProcessorTests
    {
        [Test]
        public void LayerSyncedIndexPostProcessor_ResetsSyncedLayerIndexToMinusOne()
        {
            AnimatorControllerLayer layer = new() { name = "Layer1", syncedLayerIndex = 2 };
            LayerSyncedIndexPostProcessor processor = new();

            processor.PostProcess(layer);

            Assert.AreEqual(-1, layer.syncedLayerIndex);
        }

        [Test]
        public void LayerSyncedIndexPostProcessor_ObjectTypeIsAnimatorControllerLayer()
        {
            LayerSyncedIndexPostProcessor processor = new();
            Assert.AreEqual(typeof(AnimatorControllerLayer), processor.ObjectType);
        }
    }
}
