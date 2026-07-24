using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using com.github.k_stand.ksanimatorclipboard.editor;
using com.github.k_stand.ksanimatorclipboard.editor.Copying;
using com.github.k_stand.ksanimatorclipboard.editor.tests;

namespace com.github.k_stand.ksanimatorclipboard.editor.tests.Copying
{
    public class AnimatorCopyObjectKindRegistryTests
    {
        [Test]
        public void Resolve_ReturnsRegisteredKindForExactType()
        {
            IAnimatorCopyObjectKind kind = AnimatorCopyObjectKindRegistry.Shared.Resolve(typeof(AnimatorControllerLayer));
            Assert.IsNotNull(kind);
            Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.Layers, kind.SingleClipSetType);
        }

        [Test]
        public void Resolve_ReturnsNullForUnregisteredType()
        {
            IAnimatorCopyObjectKind kind = AnimatorCopyObjectKindRegistry.Shared.Resolve(typeof(string));
            Assert.IsNull(kind);
        }

        [Test]
        public void Resolve_WalksBaseTypeForStateMachineBehaviourSubclass()
        {
            IAnimatorCopyObjectKind kind = AnimatorCopyObjectKindRegistry.Shared.Resolve(typeof(DummyStateMachineBehaviour));
            Assert.IsNotNull(kind);
            Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.Behaviours, kind.SingleClipSetType);
        }

        [Test]
        public void Resolve_WalksBaseTypeToGenericUnityObjectFallbackForUnregisteredUnityType()
        {
            IAnimatorCopyObjectKind kind = AnimatorCopyObjectKindRegistry.Shared.Resolve(typeof(AnimationClip));
            Assert.IsNotNull(kind);
            Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.Other, kind.SingleClipSetType);
        }

        [Test]
        public void Normalize_ConvertsAnimatorStateToChildAnimatorState()
        {
            AnimatorState state = new();
            try
            {
                object normalized = AnimatorCopyObjectKindRegistry.Shared.Normalize(state);
                Assert.IsInstanceOf<ChildAnimatorState>(normalized);
                Assert.AreEqual(state, ((ChildAnimatorState)normalized).state);
            }
            finally
            {
                Object.DestroyImmediate(state);
            }
        }

        [Test]
        public void Normalize_ConvertsAnimatorStateMachineToChildAnimatorStateMachine()
        {
            AnimatorStateMachine stateMachine = new();
            try
            {
                object normalized = AnimatorCopyObjectKindRegistry.Shared.Normalize(stateMachine);
                Assert.IsInstanceOf<ChildAnimatorStateMachine>(normalized);
                Assert.AreEqual(stateMachine, ((ChildAnimatorStateMachine)normalized).stateMachine);
            }
            finally
            {
                Object.DestroyImmediate(stateMachine);
            }
        }

        [Test]
        public void Normalize_ReturnsSameObjectWhenNoNormalizerRegistered()
        {
            AnimatorControllerLayer layer = new() { name = "TestLayer" };
            object normalized = AnimatorCopyObjectKindRegistry.Shared.Normalize(layer);
            Assert.AreEqual(layer, normalized);
        }

        [Test]
        public void Normalize_ReturnsNullForNullInput()
        {
            Assert.IsNull(AnimatorCopyObjectKindRegistry.Shared.Normalize(null));
        }
    }
}
