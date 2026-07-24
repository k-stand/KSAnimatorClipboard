using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using com.github.k_stand.ksanimatorclipboard.editor;

namespace com.github.k_stand.ksanimatorclipboard.editor.tests
{
    public class AnimatorClonerCloneMethodsTests : AnimatorClipboardTestFixtureBase
    {
        [Test]
        public void CloneAnimatorState_ReturnsDistinctClone_WithoutOutParam()
        {
            AnimatorState orig = Create<AnimatorState>();
            orig.name = "OrigState";
            AnimatorCloner cloner = new();
            cloner.SetClonePolicy(orig, AnimatorCloner.ClonePolicy.Clone);

            AnimatorState clone = cloner.CloneAnimatorState(orig);

            Assert.AreNotSame(orig, clone);
            Assert.AreEqual("OrigState", clone.name);
        }

        [Test]
        public void CloneAnimatorState_PopulatesClonedMap_WithOutParam()
        {
            AnimatorState orig = Create<AnimatorState>();
            AnimatorCloner cloner = new();
            cloner.SetClonePolicy(orig, AnimatorCloner.ClonePolicy.Clone);

            AnimatorState clone = cloner.CloneAnimatorState(orig, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap);

            Assert.IsTrue(clonedMap.ContainsKey(orig));
            Assert.AreEqual(clone, clonedMap[orig]);
        }

        [Test]
        public void CloneAnimatorControllerLayers_ReturnsClonesForAllElements_WithoutOutParam()
        {
            AnimatorStateMachine stateMachine1 = Create<AnimatorStateMachine>();
            AnimatorStateMachine stateMachine2 = Create<AnimatorStateMachine>();
            AnimatorControllerLayer layer1 = new() { name = "Layer1", stateMachine = stateMachine1 };
            AnimatorControllerLayer layer2 = new() { name = "Layer2", stateMachine = stateMachine2 };
            AnimatorControllerLayer[] origs = { layer1, layer2 };

            AnimatorCloner cloner = new();
            cloner.SetClonePolicy(stateMachine1, AnimatorCloner.ClonePolicy.Clone);
            cloner.SetClonePolicy(stateMachine2, AnimatorCloner.ClonePolicy.Clone);

            AnimatorControllerLayer[] clones = cloner.CloneAnimatorControllerLayers(origs);

            Assert.AreEqual(2, clones.Length);
            Assert.AreEqual("Layer1", clones[0].name);
            Assert.AreEqual("Layer2", clones[1].name);
        }

        [Test]
        public void CloneAnimatorControllerLayers_PopulatesClonedMap_WithOutParam()
        {
            AnimatorStateMachine stateMachine = Create<AnimatorStateMachine>();
            AnimatorControllerLayer layer = new() { name = "Layer1", stateMachine = stateMachine };
            AnimatorControllerLayer[] origs = { layer };

            AnimatorCloner cloner = new();
            cloner.SetClonePolicy(stateMachine, AnimatorCloner.ClonePolicy.Clone);

            AnimatorControllerLayer[] clones = cloner.CloneAnimatorControllerLayers(origs, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap);

            Assert.IsTrue(clonedMap.ContainsKey(stateMachine));
            Assert.AreEqual(clones[0].stateMachine, clonedMap[stateMachine]);
        }

        [Test]
        public void CloneObject_ReturnsCorrectlyTypedClone_ForAnimatorState()
        {
            AnimatorState orig = Create<AnimatorState>();
            AnimatorCloner cloner = new();
            cloner.SetClonePolicy(orig, AnimatorCloner.ClonePolicy.Clone);

            object clone = cloner.CloneObject(orig);

            Assert.IsInstanceOf<AnimatorState>(clone);
            Assert.AreNotSame(orig, clone);
        }

        [Test]
        public void ForEachCloned_InstanceOverload_InvokesCallbackForClonedPairsOfMatchingType()
        {
            AnimationClip origClip = Create<AnimationClip>();
            AnimatorState origState = Create<AnimatorState>();
            origState.motion = origClip;

            AnimatorCloner cloner = new();
            cloner.SetClonePolicy(origState, AnimatorCloner.ClonePolicy.Clone);
            cloner.SetClonePolicy(origClip, AnimatorCloner.ClonePolicy.Clone);

            AnimatorState cloneState = cloner.CloneAnimatorState(origState);

            List<(AnimatorState Orig, AnimatorState Clone)> calls = new();
            cloner.ForEachCloned<AnimatorState>((orig, clone) => calls.Add((orig, clone)));

            Assert.AreEqual(1, calls.Count);
            Assert.AreSame(origState, calls[0].Orig);
            Assert.AreSame(cloneState, calls[0].Clone);
        }

        [Test]
        public void ForEachCloned_StaticOverload_ExcludesSelfPairs()
        {
            AnimatorState realOrig = Create<AnimatorState>();
            AnimatorState realClone = Create<AnimatorState>();
            AnimatorState selfPaired = Create<AnimatorState>();

            Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap = new()
            {
                [realOrig] = realClone,
                [selfPaired] = selfPaired,
            };

            List<(AnimatorState Orig, AnimatorState Clone)> calls = new();
            AnimatorCloner.ForEachCloned<AnimatorState>(clonedMap, (orig, clone) => calls.Add((orig, clone)));

            Assert.AreEqual(1, calls.Count);
            Assert.AreSame(realOrig, calls[0].Orig);
            Assert.AreSame(realClone, calls[0].Clone);
        }

        [Test]
        public void CloneAnimatorState_Throws_WhenClonePolicyIsUnsetAndDefaultPolicyIsUnSetting()
        {
            AnimatorCloner cloner = new() { DefaultPolicy = AnimatorCloner.ClonePolicy.UnSetting };
            AnimatorState orig = Create<AnimatorState>();

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => cloner.CloneAnimatorState(orig));
            Assert.AreEqual("ClonePolicyが未設定のオブジェクトをクローンしようとしました", ex.Message);
        }

        [Test]
        public void CloneAnimatorState_WithUnregisteredAnimationClipMotion_KeepsOriginalReference()
        {
            AnimationClip motionClip = Create<AnimationClip>();
            AnimatorState orig = Create<AnimatorState>();
            orig.motion = motionClip;
            AnimatorCloner cloner = new();
            cloner.SetClonePolicy(orig, AnimatorCloner.ClonePolicy.Clone);

            AnimatorState clone = cloner.CloneAnimatorState(orig);

            Assert.AreSame(motionClip, clone.motion);
        }

        [Test]
        public void CloneAnimatorState_WithUnregisteredBlendTreeMotion_KeepsOriginalReference()
        {
            BlendTree motionTree = Create<BlendTree>();
            AnimatorState orig = Create<AnimatorState>();
            orig.motion = motionTree;
            AnimatorCloner cloner = new();
            cloner.SetClonePolicy(orig, AnimatorCloner.ClonePolicy.Clone);

            AnimatorState clone = cloner.CloneAnimatorState(orig);

            Assert.AreSame(motionTree, clone.motion);
        }

        [Test]
        public void CloneAnimatorState_WithExplicitlyClonedBlendTreeMotion_ReturnsDistinctClone()
        {
            BlendTree motionTree = Create<BlendTree>();
            motionTree.name = "OrigTree";
            AnimatorState orig = Create<AnimatorState>();
            orig.motion = motionTree;
            AnimatorCloner cloner = new();
            cloner.SetClonePolicy(orig, AnimatorCloner.ClonePolicy.Clone);
            cloner.SetClonePolicy(motionTree, AnimatorCloner.ClonePolicy.Clone);

            AnimatorState clone = cloner.CloneAnimatorState(orig);

            Assert.AreNotSame(motionTree, clone.motion);
            Assert.AreEqual("OrigTree", clone.motion.name);
        }
    }
}
