using NUnit.Framework;
using com.github.k_stand.ksanimatorclipboard.editor;
using com.github.k_stand.ksanimatorclipboard.editor.Copying;

namespace com.github.k_stand.ksanimatorclipboard.editor.tests.Copying
{
    public class AnimatorCopyClipSetTypeExtensionsTests
    {
        [TestCase(AnimatorCopyClipSet.AnimatorCopyClipSetType.ChildState, true)]
        [TestCase(AnimatorCopyClipSet.AnimatorCopyClipSetType.ChildStateMachine, true)]
        [TestCase(AnimatorCopyClipSet.AnimatorCopyClipSetType.Transition, true)]
        [TestCase(AnimatorCopyClipSet.AnimatorCopyClipSetType.StateTransition, true)]
        [TestCase(AnimatorCopyClipSet.AnimatorCopyClipSetType.InStateMachineObjects, true)]
        [TestCase(AnimatorCopyClipSet.AnimatorCopyClipSetType.Layers, false)]
        [TestCase(AnimatorCopyClipSet.AnimatorCopyClipSetType.Behaviours, false)]
        [TestCase(AnimatorCopyClipSet.AnimatorCopyClipSetType.Other, false)]
        [TestCase(AnimatorCopyClipSet.AnimatorCopyClipSetType.None, false)]
        public void IsInStateMachineCategory_ReturnsExpectedValue(AnimatorCopyClipSet.AnimatorCopyClipSetType setType, bool expected)
        {
            Assert.AreEqual(expected, setType.IsInStateMachineCategory());
        }
    }
}
