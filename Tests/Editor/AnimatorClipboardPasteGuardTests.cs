using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using com.github.k_stand.ksanimatorclipboard.editor;

namespace com.github.k_stand.ksanimatorclipboard.editor.tests
{
    public class AnimatorClipboardPasteGuardTests : AnimatorClipboardTestFixtureBase
    {
        private const string TempControllerPath = "Assets/_TempPasteGuardTest.controller";

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
        public void PasteIntoStateMachine_AcceptsChildState()
        {
            AnimatorState state = Create<AnimatorState>();
            ChildAnimatorState childState = new() { state = state };
            AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(childState);

            AnimatorStateMachine destStateMachine = CreateAssetBackedDestStateMachine();

            Assert.DoesNotThrow(() => AnimatorClipboard.PasteIntoStateMachine(clipSet, destStateMachine));
        }

        [Test]
        public void PasteIntoStateMachine_AcceptsInStateMachineObjects()
        {
            AnimatorState state = Create<AnimatorState>();
            ChildAnimatorState childState = new() { state = state };
            AnimatorTransition transition = Create<AnimatorTransition>();
            AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(new object[] { childState, transition });

            AnimatorStateMachine destStateMachine = CreateAssetBackedDestStateMachine();

            Assert.DoesNotThrow(() => AnimatorClipboard.PasteIntoStateMachine(clipSet, destStateMachine));
        }

        [Test]
        public void PasteIntoStateMachine_RejectsLayers()
        {
            AnimatorController controller = Create<AnimatorController>();
            AnimatorStateMachine sourceStateMachine = Create<AnimatorStateMachine>();
            AnimatorControllerLayer layer = new() { name = "Layer1", stateMachine = sourceStateMachine };
            AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(layer, controller);

            AnimatorStateMachine destStateMachine = CreateAssetBackedDestStateMachine();

            Assert.Throws<AnimatorCopyClipSetTypeMismatchException>(() => AnimatorClipboard.PasteIntoStateMachine(clipSet, destStateMachine));
        }

        [Test]
        public void PasteIntoStateMachine_RejectsBehaviours()
        {
            DummyStateMachineBehaviour behaviour = Track(ScriptableObject.CreateInstance<DummyStateMachineBehaviour>());
            AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(behaviour);

            AnimatorStateMachine destStateMachine = CreateAssetBackedDestStateMachine();

            Assert.Throws<AnimatorCopyClipSetTypeMismatchException>(() => AnimatorClipboard.PasteIntoStateMachine(clipSet, destStateMachine));
        }
    }
}
