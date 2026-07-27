using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using com.github.k_stand.ksanimatorcopyengine.editor;

namespace com.github.k_stand.ksanimatorcopyengine.editor.tests
{
    public class ReferenceRemapperTests : AnimatorCopyEngineTestFixtureBase
    {
        [Test]
        public void RemappingRecursively_UnifiesDuplicateBlendTreeClones_FromIndependentCloneOperations()
        {
            BlendTree origBlendTree = Create<BlendTree>();
            origBlendTree.name = "Shared";

            AnimatorCloner cloner1 = new();
            cloner1.SetClonePolicy(origBlendTree, AnimatorCloner.ClonePolicy.Clone);
            BlendTree clone1 = cloner1.CloneBlendTree(origBlendTree, out Dictionary<UnityEngine.Object, UnityEngine.Object> map1);

            AnimatorCloner cloner2 = new();
            cloner2.SetClonePolicy(origBlendTree, AnimatorCloner.ClonePolicy.Clone);
            BlendTree clone2 = cloner2.CloneBlendTree(origBlendTree, out Dictionary<UnityEngine.Object, UnityEngine.Object> map2);

            Assert.AreNotSame(clone1, clone2);

            AnimatorState state1 = Create<AnimatorState>();
            state1.motion = clone1;
            AnimatorState state2 = Create<AnimatorState>();
            state2.motion = clone2;

            ReferenceRemapper remapper = new();
            remapper.AddClonedMap(map1);
            remapper.AddClonedMap(map2);
            remapper.RemappingRecursively(new UnityEngine.Object[] { state1, state2 });

            Assert.AreSame(state1.motion, state2.motion);
        }

        [Test]
        public void GetOrigRoot_ResolvesThroughMultiHopCloneChain()
        {
            BlendTree origA = Create<BlendTree>();

            AnimatorCloner cloner1 = new();
            cloner1.SetClonePolicy(origA, AnimatorCloner.ClonePolicy.Clone);
            BlendTree cloneB = cloner1.CloneBlendTree(origA, out Dictionary<UnityEngine.Object, UnityEngine.Object> map1);

            AnimatorCloner cloner2 = new();
            cloner2.SetClonePolicy(cloneB, AnimatorCloner.ClonePolicy.Clone);
            BlendTree cloneC = cloner2.CloneBlendTree(cloneB, out Dictionary<UnityEngine.Object, UnityEngine.Object> map2);

            ReferenceRemapper remapper = new();
            remapper.AddClonedMap(map1);
            remapper.AddClonedMap(map2);

            UnityEngine.Object origRoot = remapper.GetOrigRoot(cloneC);

            Assert.AreSame(origA, origRoot);
        }

        [Test]
        public void RemappingRecursively_UnifiesDuplicateAnimationClipClones_FromIndependentCloneOperations()
        {
            AnimationClip origClip = Create<AnimationClip>();
            origClip.name = "SharedClip";

            AnimatorCloner cloner1 = new();
            cloner1.SetClonePolicy(origClip, AnimatorCloner.ClonePolicy.Clone);
            AnimationClip clone1 = cloner1.CloneAnimationClip(origClip, out Dictionary<UnityEngine.Object, UnityEngine.Object> map1);

            AnimatorCloner cloner2 = new();
            cloner2.SetClonePolicy(origClip, AnimatorCloner.ClonePolicy.Clone);
            AnimationClip clone2 = cloner2.CloneAnimationClip(origClip, out Dictionary<UnityEngine.Object, UnityEngine.Object> map2);

            Assert.AreNotSame(clone1, clone2);

            AnimatorState state1 = Create<AnimatorState>();
            state1.motion = clone1;
            AnimatorState state2 = Create<AnimatorState>();
            state2.motion = clone2;

            ReferenceRemapper remapper = new();
            remapper.AddClonedMap(map1);
            remapper.AddClonedMap(map2);
            remapper.RemappingRecursively(new UnityEngine.Object[] { state1, state2 });

            Assert.AreSame(state1.motion, state2.motion);
        }

        [Test]
        public void RemappingRecursively_ReachesNestedMotionReferences_ThroughAnimatorStateMachineTraversal()
        {
            BlendTree origBlendTree = Create<BlendTree>();
            origBlendTree.name = "Shared";

            AnimatorCloner cloner1 = new();
            cloner1.SetClonePolicy(origBlendTree, AnimatorCloner.ClonePolicy.Clone);
            BlendTree clone1 = cloner1.CloneBlendTree(origBlendTree, out Dictionary<UnityEngine.Object, UnityEngine.Object> map1);

            AnimatorCloner cloner2 = new();
            cloner2.SetClonePolicy(origBlendTree, AnimatorCloner.ClonePolicy.Clone);
            BlendTree clone2 = cloner2.CloneBlendTree(origBlendTree, out Dictionary<UnityEngine.Object, UnityEngine.Object> map2);

            AnimatorState state1 = Create<AnimatorState>();
            state1.motion = clone1;
            AnimatorState state2 = Create<AnimatorState>();
            state2.motion = clone2;

            AnimatorStateMachine stateMachine = Create<AnimatorStateMachine>();
            stateMachine.states = new[]
            {
                new ChildAnimatorState { state = state1 },
                new ChildAnimatorState { state = state2 },
            };

            ReferenceRemapper remapper = new();
            remapper.AddClonedMap(map1);
            remapper.AddClonedMap(map2);
            remapper.RemappingRecursively(stateMachine);

            Assert.AreSame(state1.motion, state2.motion);
        }
    }
}
