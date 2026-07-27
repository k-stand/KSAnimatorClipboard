using NUnit.Framework;
using UnityEditor.Animations;
using com.github.k_stand.ksanimatorcopyengine.editor;

namespace com.github.k_stand.ksanimatorcopyengine.editor.tests
{
    public class Unity2022AnimatorInternalsAdapterTests : AnimatorCopyEngineTestFixtureBase
    {
        [Test]
        public void GetAllOverrideStateMotionPairs_ReturnsNull_WhenUninitialized()
        {
            AnimatorControllerLayer layer = new() { name = "Layer1" };
            Unity2022AnimatorInternalsAdapter adapter = new();

            StateMotionPair[] pairs = adapter.GetAllOverrideStateMotionPairs(layer);

            Assert.IsNull(pairs);
        }

        [Test]
        public void GetAllOverrideBehavioursPairs_ReturnsNull_WhenUninitialized()
        {
            AnimatorControllerLayer layer = new() { name = "Layer1" };
            Unity2022AnimatorInternalsAdapter adapter = new();

            StateBehavioursPair[] pairs = adapter.GetAllOverrideBehavioursPairs(layer);

            Assert.IsNull(pairs);
        }

        [Test]
        public void InitOverrideStateMotionPairs_MakesGetAllOverrideStateMotionPairsReturnEmptyArray()
        {
            AnimatorControllerLayer layer = new() { name = "Layer1" };
            Unity2022AnimatorInternalsAdapter adapter = new();

            adapter.InitOverrideStateMotionPairs(layer);
            StateMotionPair[] pairs = adapter.GetAllOverrideStateMotionPairs(layer);

            Assert.IsNotNull(pairs);
            Assert.IsEmpty(pairs);
        }

        [Test]
        public void InitOverrideStateBehavioursPairs_MakesGetAllOverrideBehavioursPairsReturnEmptyArray()
        {
            AnimatorControllerLayer layer = new() { name = "Layer1" };
            Unity2022AnimatorInternalsAdapter adapter = new();

            adapter.InitOverrideStateBehavioursPairs(layer);
            StateBehavioursPair[] pairs = adapter.GetAllOverrideBehavioursPairs(layer);

            Assert.IsNotNull(pairs);
            Assert.IsEmpty(pairs);
        }

        [Test]
        public void Validate_DoesNotThrow_OnCurrentUnityVersion()
        {
            Unity2022AnimatorInternalsAdapter adapter = new();

            Assert.DoesNotThrow(() => adapter.Validate());
        }

        [Test]
        public void SupportedMajorVersions_ContainsUnity2022()
        {
            CollectionAssert.Contains(Unity2022AnimatorInternalsAdapter.SupportedMajorVersions, 2022);
        }
    }
}
