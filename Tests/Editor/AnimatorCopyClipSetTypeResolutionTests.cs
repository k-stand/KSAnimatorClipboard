using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using com.github.k_stand.ksanimatorclipboard.editor;

namespace com.github.k_stand.ksanimatorclipboard.editor.tests
{
    public class AnimatorCopyClipSetTypeResolutionTests : AnimatorClipboardTestFixtureBase
    {
        [Test]
        public void SingleLayer_ResolvesToLayers()
        {
            AnimatorController controller = Create<AnimatorController>();
            AnimatorStateMachine stateMachine = Create<AnimatorStateMachine>();
            AnimatorControllerLayer layer = new() { name = "Layer1", stateMachine = stateMachine };

            AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(layer, controller);

            Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.Layers, clipSet.Type);
        }

        [Test]
        public void MultipleLayers_ResolvesToLayers()
        {
            AnimatorController controller = Create<AnimatorController>();
            AnimatorStateMachine sm1 = Create<AnimatorStateMachine>();
            AnimatorStateMachine sm2 = Create<AnimatorStateMachine>();
            AnimatorControllerLayer layer1 = new() { name = "Layer1", stateMachine = sm1 };
            AnimatorControllerLayer layer2 = new() { name = "Layer2", stateMachine = sm2 };

            AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(new[] { layer1, layer2 }, controller);

            Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.Layers, clipSet.Type);
        }

        [Test]
        public void SingleChildAnimatorState_ResolvesToChildState()
        {
            AnimatorState state = Create<AnimatorState>();
            ChildAnimatorState childState = new() { state = state };

            AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(childState);

            Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.ChildState, clipSet.Type);
        }

        [Test]
        public void PlainAnimatorState_IsNormalizedAndResolvesToChildState()
        {
            AnimatorState state = Create<AnimatorState>();

            AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(state);

            Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.ChildState, clipSet.Type);
        }

        [Test]
        public void PlainAnimatorStateMachine_IsNormalizedAndResolvesToChildStateMachine()
        {
            AnimatorStateMachine stateMachine = Create<AnimatorStateMachine>();

            AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(stateMachine);

            Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.ChildStateMachine, clipSet.Type);
        }

        [Test]
        public void TwoChildAnimatorStates_ResolvesToInStateMachineObjects()
        {
            AnimatorState state1 = Create<AnimatorState>();
            AnimatorState state2 = Create<AnimatorState>();
            ChildAnimatorState childState1 = new() { state = state1 };
            ChildAnimatorState childState2 = new() { state = state2 };

            AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(new object[] { childState1, childState2 });

            Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.InStateMachineObjects, clipSet.Type);
        }

        [Test]
        public void MixedChildStateAndTransition_ResolvesToInStateMachineObjects()
        {
            AnimatorState state = Create<AnimatorState>();
            ChildAnimatorState childState = new() { state = state };
            AnimatorTransition transition = Create<AnimatorTransition>();

            AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(new object[] { childState, transition });

            Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.InStateMachineObjects, clipSet.Type);
        }

        [Test]
        public void SingleTransition_ResolvesToTransition()
        {
            AnimatorTransition transition = Create<AnimatorTransition>();

            AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(transition);

            Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.Transition, clipSet.Type);
        }

        [Test]
        public void SingleStateTransition_ResolvesToStateTransition()
        {
            AnimatorStateTransition stateTransition = Create<AnimatorStateTransition>();

            AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(stateTransition);

            Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.StateTransition, clipSet.Type);
        }

        [Test]
        public void EmptyClips_ResolvesToOther()
        {
            AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(System.Array.Empty<object>());

            Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.Other, clipSet.Type);
        }

        [Test]
        public void EmptyClips_WithAncestorStateMachine_ThrowsArgumentException()
        {
            AnimatorStateMachine ancestorStateMachine = Create<AnimatorStateMachine>();

            Assert.Throws<System.ArgumentException>(() => AnimatorClipboard.Copy(System.Array.Empty<object>(), ancestorStateMachine));
        }

        [Test]
        public void UnsupportedType_ThrowsArgumentException()
        {
            Assert.Throws<System.ArgumentException>(() => AnimatorClipboard.Copy(new object()));
        }

        [Test]
        public void NullObject_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() => AnimatorClipboard.Copy((object)null));
        }

        [Test]
        public void UnregisteredUnityObjectType_ResolvesToOther()
        {
            AnimationClip clip = new();
            try
            {
                AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(clip);
                Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.Other, clipSet.Type);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }
    }
}
