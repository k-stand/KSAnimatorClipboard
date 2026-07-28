using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using com.github.k_stand.ksanimatorcopyengine.editor;

namespace com.github.k_stand.ksanimatorcopyengine.editor.tests
{
    public class AnimatorAssetPersistenceTests : AnimatorCopyEngineTestFixtureBase
    {
        private const string TempControllerPath = "Assets/_TempAssetPersistenceTest.controller";

        private string _tempControllerPath;

        [SetUp]
        public void SetUpTempController()
        {
            _tempControllerPath = null;
            AssetDatabase.DeleteAsset(TempControllerPath);
        }

        [TearDown]
        public void TearDownTempController()
        {
            if (!string.IsNullOrEmpty(_tempControllerPath))
            {
                AssetDatabase.DeleteAsset(_tempControllerPath);
                _tempControllerPath = null;
            }
        }

        [Test]
        public void AddObjectToAssetRecursively_AddsStateToAssetPath()
        {
            _tempControllerPath = TempControllerPath;
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(_tempControllerPath);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState state = stateMachine.AddState("State1", Vector3.zero);

            AnimatorAssetPersistence.AddObjectToAssetRecursively(state, _tempControllerPath);

            Assert.AreEqual(_tempControllerPath, AssetDatabase.GetAssetPath(state));
        }

        [Test]
        public void CheckAndAddObjectToAsset_ReturnsTrue_ForObjectNotYetPartOfAnyAsset()
        {
            _tempControllerPath = TempControllerPath;
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(_tempControllerPath);
            AnimatorState looseState = Create<AnimatorState>();

            bool added = AnimatorAssetPersistence.CheckAndAddObjectToAsset(looseState, controller);

            Assert.IsTrue(added);
        }

        [Test]
        public void CheckAndAddObjectToAsset_ReturnsFalse_ForObjectAlreadyPartOfAsset()
        {
            _tempControllerPath = TempControllerPath;
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(_tempControllerPath);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

            bool added = AnimatorAssetPersistence.CheckAndAddObjectToAsset(stateMachine, controller);

            Assert.IsFalse(added);
        }

        [Test]
        public void RemoveUnusedSubAssets_RemovesUnreachableSubAsset()
        {
            _tempControllerPath = TempControllerPath;
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(_tempControllerPath);
            AnimatorState orphanState = Create<AnimatorState>();
            AssetDatabase.AddObjectToAsset(orphanState, _tempControllerPath);
            AssetDatabase.SaveAssets();

            bool removedAny = AnimatorAssetPersistence.RemoveUnusedSubAssets(controller, muteLogs: true);

            Assert.IsTrue(removedAny);
        }
    }
}
