using System.Linq;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using com.github.k_stand.ksanimatorcopyengine.editor;
using com.github.k_stand.ksanimatorcopyengine.editor.Copying;
using com.github.k_stand.ksanimatorcopyengine.editor.tests;

namespace com.github.k_stand.ksanimatorcopyengine.editor.tests.Copying
{
    public class AnimatorCopyObjectKindTests : AnimatorCopyEngineTestFixtureBase
    {
        [Test]
        public void LayerCopyObjectKind_HasExpectedProperties()
        {
            IAnimatorCopyObjectKind kind = new LayerCopyObjectKind();
            Assert.AreEqual(typeof(AnimatorControllerLayer), kind.ObjectType);
            Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.Layers, kind.SingleClipSetType);
            Assert.IsFalse(kind.IsInStateMachineObject);
        }

        [Test]
        public void ChildStateCopyObjectKind_HasExpectedProperties()
        {
            IAnimatorCopyObjectKind kind = new ChildStateCopyObjectKind();
            Assert.AreEqual(typeof(ChildAnimatorState), kind.ObjectType);
            Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.ChildState, kind.SingleClipSetType);
            Assert.IsTrue(kind.IsInStateMachineObject);
        }

        [Test]
        public void ChildStateMachineCopyObjectKind_HasExpectedProperties()
        {
            IAnimatorCopyObjectKind kind = new ChildStateMachineCopyObjectKind();
            Assert.AreEqual(typeof(ChildAnimatorStateMachine), kind.ObjectType);
            Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.ChildStateMachine, kind.SingleClipSetType);
            Assert.IsTrue(kind.IsInStateMachineObject);
        }

        [Test]
        public void TransitionCopyObjectKind_HasExpectedProperties()
        {
            IAnimatorCopyObjectKind kind = new TransitionCopyObjectKind();
            Assert.AreEqual(typeof(AnimatorTransition), kind.ObjectType);
            Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.Transition, kind.SingleClipSetType);
            Assert.IsTrue(kind.IsInStateMachineObject);
        }

        [Test]
        public void StateTransitionCopyObjectKind_HasExpectedProperties()
        {
            IAnimatorCopyObjectKind kind = new StateTransitionCopyObjectKind();
            Assert.AreEqual(typeof(AnimatorStateTransition), kind.ObjectType);
            Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.StateTransition, kind.SingleClipSetType);
            Assert.IsTrue(kind.IsInStateMachineObject);
        }

        [Test]
        public void StateMachineBehaviourCopyObjectKind_HasExpectedProperties()
        {
            IAnimatorCopyObjectKind kind = new StateMachineBehaviourCopyObjectKind();
            Assert.AreEqual(typeof(StateMachineBehaviour), kind.ObjectType);
            Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.Behaviours, kind.SingleClipSetType);
            Assert.IsFalse(kind.IsInStateMachineObject);
        }

        [Test]
        public void GenericUnityObjectCopyObjectKind_HasExpectedProperties()
        {
            IAnimatorCopyObjectKind kind = new GenericUnityObjectCopyObjectKind();
            Assert.AreEqual(typeof(Object), kind.ObjectType);
            Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.Other, kind.SingleClipSetType);
            Assert.IsFalse(kind.IsInStateMachineObject);
        }

        [Test]
        public void LayerCopyObjectKind_GetCloneScope_MatchesListupObjectsInLayer()
        {
            AnimatorStateMachine stateMachine = Create<AnimatorStateMachine>();
            AnimatorState state = Create<AnimatorState>();
            stateMachine.AddState(state, Vector3.zero);
            AnimatorControllerLayer layer = new() { stateMachine = stateMachine };

            IAnimatorCopyObjectKind kind = new LayerCopyObjectKind();

            CollectionAssert.AreEquivalent(AnimatorGraphTraversal.ListupObjectsInLayer(layer), kind.GetCloneScope(layer).ToArray());
        }

        [Test]
        public void LayerCopyObjectKind_GetCloneScope_ReturnsEmpty_WhenStateMachineIsNull()
        {
            IAnimatorCopyObjectKind kind = new LayerCopyObjectKind();

            Assert.IsEmpty(kind.GetCloneScope(new AnimatorControllerLayer { stateMachine = null }));
        }

        [Test]
        public void ChildStateCopyObjectKind_GetCloneScope_ReturnsStateTransitionsAndBehaviours()
        {
            AnimatorState state = Create<AnimatorState>();
            AnimatorStateTransition transition = state.AddTransition(state);
            StateMachineBehaviour behaviour = Track(ScriptableObject.CreateInstance<DummyStateMachineBehaviour>());
            state.behaviours = new[] { behaviour };
            ChildAnimatorState childState = new() { state = state };

            IAnimatorCopyObjectKind kind = new ChildStateCopyObjectKind();

            CollectionAssert.AreEquivalent(new Object[] { state, transition, behaviour }, kind.GetCloneScope(childState).ToArray());
        }

        [Test]
        public void ChildStateCopyObjectKind_GetCloneScope_ReturnsEmpty_WhenStateIsNull()
        {
            IAnimatorCopyObjectKind kind = new ChildStateCopyObjectKind();

            Assert.IsEmpty(kind.GetCloneScope(new ChildAnimatorState { state = null }));
        }

        [Test]
        public void ChildStateMachineCopyObjectKind_GetCloneScope_MatchesStateMachineAndListupObjectsInStateMachine()
        {
            AnimatorStateMachine stateMachine = Create<AnimatorStateMachine>();
            AnimatorState state = Create<AnimatorState>();
            stateMachine.AddState(state, Vector3.zero);
            ChildAnimatorStateMachine childStateMachine = new() { stateMachine = stateMachine };

            IAnimatorCopyObjectKind kind = new ChildStateMachineCopyObjectKind();

            Object[] expected = new Object[] { stateMachine }.Concat(AnimatorGraphTraversal.ListupObjectsInStateMachine(stateMachine)).ToArray();
            CollectionAssert.AreEquivalent(expected, kind.GetCloneScope(childStateMachine).ToArray());
        }

        [Test]
        public void ChildStateMachineCopyObjectKind_GetCloneScope_ReturnsEmpty_WhenStateMachineIsNull()
        {
            IAnimatorCopyObjectKind kind = new ChildStateMachineCopyObjectKind();

            Assert.IsEmpty(kind.GetCloneScope(new ChildAnimatorStateMachine { stateMachine = null }));
        }

        [Test]
        public void TransitionCopyObjectKind_GetCloneScope_ReturnsTransitionOnly()
        {
            AnimatorStateMachine stateMachine = Create<AnimatorStateMachine>();
            AnimatorState state = Create<AnimatorState>();
            stateMachine.AddState(state, Vector3.zero);
            AnimatorTransition transition = stateMachine.AddEntryTransition(state);

            IAnimatorCopyObjectKind kind = new TransitionCopyObjectKind();

            CollectionAssert.AreEquivalent(new Object[] { transition }, kind.GetCloneScope(transition).ToArray());
        }

        [Test]
        public void StateTransitionCopyObjectKind_GetCloneScope_ReturnsStateTransitionOnly()
        {
            AnimatorState state = Create<AnimatorState>();
            AnimatorStateTransition transition = state.AddTransition(state);

            IAnimatorCopyObjectKind kind = new StateTransitionCopyObjectKind();

            CollectionAssert.AreEquivalent(new Object[] { transition }, kind.GetCloneScope(transition).ToArray());
        }

        [Test]
        public void StateMachineBehaviourCopyObjectKind_GetCloneScope_ReturnsBehaviourOnly()
        {
            StateMachineBehaviour behaviour = Track(ScriptableObject.CreateInstance<DummyStateMachineBehaviour>());

            IAnimatorCopyObjectKind kind = new StateMachineBehaviourCopyObjectKind();

            CollectionAssert.AreEquivalent(new Object[] { behaviour }, kind.GetCloneScope(behaviour).ToArray());
        }

        [Test]
        public void GenericUnityObjectCopyObjectKind_GetCloneScope_ReturnsObjectItself()
        {
            AnimationClip clip = Track(new AnimationClip());

            IAnimatorCopyObjectKind kind = new GenericUnityObjectCopyObjectKind();

            CollectionAssert.AreEquivalent(new Object[] { clip }, kind.GetCloneScope(clip).ToArray());
        }

        [Test]
        public void GenericUnityObjectCopyObjectKind_GetCloneScope_ReturnsEmpty_WhenObjectIsNotUnityObject()
        {
            IAnimatorCopyObjectKind kind = new GenericUnityObjectCopyObjectKind();

            Assert.IsEmpty(kind.GetCloneScope(new object()));
        }
    }
}
