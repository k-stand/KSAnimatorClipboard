using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using com.github.k_stand.ksanimatorcopyengine.editor;

namespace com.github.k_stand.ksanimatorcopyengine.editor.tests
{
    public class AnimatorCopyEngineTryPasteTests : AnimatorCopyEngineTestFixtureBase
    {
        private const string TempControllerPath = "Assets/_TempTryPasteTest.controller";

        private string _tempControllerPath;

        [SetUp]
        public void SetUpTempController()
        {
            _tempControllerPath = null;

            // 前回のテストが異常終了した場合の残骸を除去する
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

        // 貼り付け先のStateMachineはAssetDatabase.GetAssetPathが有効なパスを返す必要があるため、
        // メモリ上のみのインスタンスではなく、実際にディスクへ保存されたAnimatorControllerアセットの
        // レイヤー0のStateMachineを使用する。
        private AnimatorStateMachine CreateAssetBackedDestStateMachine()
        {
            _tempControllerPath = TempControllerPath;
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(_tempControllerPath);
            return controller.layers[0].stateMachine;
        }

        [Test]
        public void TryPasteLayers_ReturnsFalse_WhenClipSetTypeMismatches()
        {
            AnimatorState state = Create<AnimatorState>();
            AnimatorCopyClipSet clipSet = AnimatorCopyEngine.Copy(state);
            AnimatorController destController = Create<AnimatorController>();

            bool success = AnimatorCopyEngine.TryPasteLayers(clipSet, destController, out AnimatorControllerLayer[] result);

            Assert.IsFalse(success);
            Assert.IsNull(result);
        }

        [Test]
        public void TryPasteIntoStateMachine_ReturnsFalse_WhenClipSetTypeMismatches()
        {
            AnimatorStateMachine stateMachine = Create<AnimatorStateMachine>();
            AnimatorControllerLayer layer = new() { name = "Layer1", stateMachine = stateMachine };
            AnimatorController parentController = Create<AnimatorController>();
            AnimatorCopyClipSet clipSet = AnimatorCopyEngine.Copy(layer, parentController);
            AnimatorStateMachine destStateMachine = Create<AnimatorStateMachine>();

            bool success = AnimatorCopyEngine.TryPasteIntoStateMachine(clipSet, destStateMachine, out UnityEngine.Object[] result);

            Assert.IsFalse(success);
            Assert.IsNull(result);
        }

        [Test]
        public void TryPasteBehaviours_ReturnsFalse_WhenClipSetTypeMismatches()
        {
            AnimatorState state = Create<AnimatorState>();
            AnimatorCopyClipSet clipSet = AnimatorCopyEngine.Copy(state);
            AnimatorStateMachine destStateMachine = Create<AnimatorStateMachine>();

            bool success = AnimatorCopyEngine.TryPasteBehaviours(clipSet, destStateMachine, out StateMachineBehaviour[] result);

            Assert.IsFalse(success);
            Assert.IsNull(result);
        }

        [Test]
        public void PasteLayers_StillThrows_WhenClipSetTypeMismatches()
        {
            AnimatorState state = Create<AnimatorState>();
            AnimatorCopyClipSet clipSet = AnimatorCopyEngine.Copy(state);
            AnimatorController destController = Create<AnimatorController>();

            Assert.Throws<AnimatorCopyClipSetTypeMismatchException>(() => AnimatorCopyEngine.PasteLayers(clipSet, destController));
        }

        [Test]
        public void TryPasteSettings_AnimatorState_ReturnsFalse_WhenClipSetTypeMismatches()
        {
            AnimatorTransition transition = Create<AnimatorTransition>();
            AnimatorCopyClipSet clipSet = AnimatorCopyEngine.Copy(transition);
            AnimatorState destState = Create<AnimatorState>();

            bool success = AnimatorCopyEngine.TryPasteSettings(clipSet, destState);

            Assert.IsFalse(success);
        }

        [Test]
        public void TryPasteSettings_AnimatorTransition_ReturnsFalse_WhenClipSetTypeMismatches()
        {
            AnimatorState state = Create<AnimatorState>();
            AnimatorCopyClipSet clipSet = AnimatorCopyEngine.Copy(state);
            AnimatorTransition destTransition = Create<AnimatorTransition>();

            bool success = AnimatorCopyEngine.TryPasteSettings(clipSet, destTransition);

            Assert.IsFalse(success);
        }

        [Test]
        public void TryPasteSettings_AnimatorState_ReturnsTrueAndAppliesSettings_WhenClipSetTypeMatches()
        {
            AnimatorState srcState = Create<AnimatorState>();
            srcState.speed = 2.5f;
            AnimatorCopyClipSet clipSet = AnimatorCopyEngine.Copy(srcState);
            AnimatorState destState = Create<AnimatorState>();
            destState.speed = 1f;

            bool success = AnimatorCopyEngine.TryPasteSettings(clipSet, destState);

            Assert.IsTrue(success);
            Assert.AreEqual(2.5f, destState.speed);
        }

        [Test]
        public void PasteSettings_AnimatorState_StillThrows_WhenClipSetTypeMismatches()
        {
            AnimatorTransition transition = Create<AnimatorTransition>();
            AnimatorCopyClipSet clipSet = AnimatorCopyEngine.Copy(transition);
            AnimatorState destState = Create<AnimatorState>();

            Assert.Throws<AnimatorCopyClipSetTypeMismatchException>(() => AnimatorCopyEngine.PasteSettings(clipSet, destState));
        }

        [Test]
        public void TryPasteBehaviours_ReturnsTrueAndAppliesBehaviours_WhenClipSetTypeMatches()
        {
            DummyStateMachineBehaviour behaviour = Track(ScriptableObject.CreateInstance<DummyStateMachineBehaviour>());
            AnimatorCopyClipSet clipSet = AnimatorCopyEngine.Copy(behaviour);
            AnimatorStateMachine destStateMachine = Create<AnimatorStateMachine>();

            bool success = AnimatorCopyEngine.TryPasteBehaviours(clipSet, destStateMachine, out StateMachineBehaviour[] result);

            Assert.IsTrue(success);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(1, destStateMachine.behaviours.Length);
            Assert.IsInstanceOf<DummyStateMachineBehaviour>(destStateMachine.behaviours[0]);
        }

        [Test]
        public void TryPasteIntoStateMachine_ReturnsTrueAndPastesObjects_WhenClipSetTypeMatches()
        {
            AnimatorState state = Create<AnimatorState>();
            ChildAnimatorState childState = new() { state = state };
            AnimatorTransition transition = Create<AnimatorTransition>();
            AnimatorCopyClipSet clipSet = AnimatorCopyEngine.Copy(new object[] { childState, transition });

            AnimatorStateMachine destStateMachine = CreateAssetBackedDestStateMachine();

            bool success = AnimatorCopyEngine.TryPasteIntoStateMachine(clipSet, destStateMachine, out UnityEngine.Object[] result);

            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
        }

        // 遷移を持つAnimatorStateを、自分自身が属するStateMachine(祖先の子孫として自分自身を含むスコープ)へ
        // 貼り付けるケース。かつてはChildAnimatorStateのClonePolicy登録がstate本体のみに留まっていたため、
        // 貼り付け先スコープ側の一括KeepReference登録がstateのtransitionsを先に捕捉してしまい、
        // クローン時に「親がCloneのオブジェクトの子にKeepReferenceが設定されている」例外が発生していた。
        [Test]
        public void TryPasteIntoStateMachine_PastesStateWithTransitions_WhenDestinationIsWithinSameAncestorScope()
        {
            AnimatorStateMachine ancestorStateMachine = CreateAssetBackedDestStateMachine();
            AnimatorState state = Create<AnimatorState>();
            ancestorStateMachine.AddState(state, Vector3.zero);
            state.AddTransition(state);

            AnimatorCopyClipSet clipSet = AnimatorCopyEngine.Copy(state, ancestorStateMachine);

            bool success = AnimatorCopyEngine.TryPasteIntoStateMachine(clipSet, ancestorStateMachine, out UnityEngine.Object[] result);

            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
        }

        [Test]
        public void TryPasteSettings_AnimatorStateTransition_ReturnsTrueAndAppliesSettings_WhenClipSetTypeMatches()
        {
            AnimatorStateTransition srcStateTransition = Create<AnimatorStateTransition>();
            srcStateTransition.duration = 2.5f;
            AnimatorCopyClipSet clipSet = AnimatorCopyEngine.Copy(srcStateTransition);
            AnimatorStateTransition destStateTransition = Create<AnimatorStateTransition>();
            destStateTransition.duration = 1f;

            bool success = AnimatorCopyEngine.TryPasteSettings(clipSet, destStateTransition);

            Assert.IsTrue(success);
            Assert.AreEqual(2.5f, destStateTransition.duration);
        }

        [Test]
        public void TryPasteConditions_AnimatorTransition_ReturnsTrueAndAppliesConditions_WhenClipSetTypeMatches()
        {
            AnimatorTransition srcTransition = Create<AnimatorTransition>();
            srcTransition.conditions = new AnimatorCondition[] { new() { parameter = "TestParam", mode = AnimatorConditionMode.If } };
            AnimatorCopyClipSet clipSet = AnimatorCopyEngine.Copy(srcTransition);
            AnimatorTransition destTransition = Create<AnimatorTransition>();

            bool success = AnimatorCopyEngine.TryPasteConditions(clipSet, destTransition);

            Assert.IsTrue(success);
            Assert.AreEqual(1, destTransition.conditions.Length);
            Assert.AreEqual("TestParam", destTransition.conditions[0].parameter);
        }

        [Test]
        public void TryPasteSettingsAndConditions_AnimatorStateTransition_ReturnsTrueAndAppliesBoth_WhenClipSetTypeMatches()
        {
            AnimatorStateTransition srcStateTransition = Create<AnimatorStateTransition>();
            srcStateTransition.duration = 3f;
            srcStateTransition.conditions = new AnimatorCondition[] { new() { parameter = "AnotherParam", mode = AnimatorConditionMode.If } };
            AnimatorCopyClipSet clipSet = AnimatorCopyEngine.Copy(srcStateTransition);
            AnimatorStateTransition destStateTransition = Create<AnimatorStateTransition>();

            bool success = AnimatorCopyEngine.TryPasteSettingsAndConditions(clipSet, destStateTransition);

            Assert.IsTrue(success);
            Assert.AreEqual(3f, destStateTransition.duration);
            Assert.AreEqual(1, destStateTransition.conditions.Length);
        }
    }
}
