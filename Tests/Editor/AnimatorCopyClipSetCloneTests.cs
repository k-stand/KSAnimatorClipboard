using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using com.github.k_stand.ksanimatorclipboard.editor;

namespace com.github.k_stand.ksanimatorclipboard.editor.tests
{
    public class AnimatorCopyClipSetCloneTests : AnimatorClipboardTestFixtureBase
    {
        [Test]
        public void Clone_ChildState_ProducesIndependentStateAndTransitionAndPopulatesClonedMap()
        {
            AnimatorState state = Create<AnimatorState>();
            AnimatorStateTransition transition = state.AddTransition(state);
            ChildAnimatorState childState = new() { state = state };
            AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(childState);

            AnimatorCopyClipSet cloneClipSet = clipSet.Clone(out var clonedMap);

            Assert.IsTrue(clonedMap.TryGetValue(state, out Object clonedStateObj));
            AnimatorState cloneState = (AnimatorState)clonedStateObj;
            Assert.AreNotSame(state, cloneState);
            Assert.AreEqual(1, cloneState.transitions.Length);
            Assert.AreNotSame(transition, cloneState.transitions[0]);
            Assert.AreSame(cloneState, ((ChildAnimatorState)cloneClipSet.Clips[0].Object).state);
        }
    }
}
