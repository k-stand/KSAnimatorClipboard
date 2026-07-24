using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.Animations;
using com.github.k_stand.ksanimatorclipboard.editor;

namespace com.github.k_stand.ksanimatorclipboard.editor.tests
{
    public class AnimatorClonerValidateRegistrationTests : AnimatorClipboardTestFixtureBase
    {
        [Test]
        public void ValidateRegistrationAnimatorControllerLayers_DoesNotThrow_ForLayerWithUninitializedOverrideArrays()
        {
            AnimatorStateMachine stateMachine = Create<AnimatorStateMachine>();
            AnimatorControllerLayer layer = new() { name = "Layer1", stateMachine = stateMachine };
            AnimatorCloner cloner = new();
            HashSet<UnityEngine.Object> visitedObjSet = new();

            Assert.DoesNotThrow(() => cloner.ValidateRegistrationAnimatorControllerLayers(new[] { layer }, null, "layers", ref visitedObjSet));
        }
    }
}
