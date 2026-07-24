using NUnit.Framework;
using UnityEngine;
using com.github.k_stand.ksanimatorclipboard.editor;

namespace com.github.k_stand.ksanimatorclipboard.editor.tests
{
    public class AnimatorClipboardBehaviourCopyTests
    {
        [Test]
        public void Copy_StateMachineBehaviour_ResolvesToBehaviours()
        {
            DummyStateMachineBehaviour behaviour = ScriptableObject.CreateInstance<DummyStateMachineBehaviour>();
            try
            {
                AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(behaviour);
                Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.Behaviours, clipSet.Type);
            }
            finally
            {
                Object.DestroyImmediate(behaviour);
            }
        }

        [Test]
        public void Copy_MultipleStateMachineBehaviours_ResolvesToBehaviours()
        {
            DummyStateMachineBehaviour behaviour1 = ScriptableObject.CreateInstance<DummyStateMachineBehaviour>();
            DummyStateMachineBehaviour behaviour2 = ScriptableObject.CreateInstance<DummyStateMachineBehaviour>();
            try
            {
                AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(new[] { behaviour1, behaviour2 });
                Assert.AreEqual(AnimatorCopyClipSet.AnimatorCopyClipSetType.Behaviours, clipSet.Type);
            }
            finally
            {
                Object.DestroyImmediate(behaviour1);
                Object.DestroyImmediate(behaviour2);
            }
        }
    }
}
