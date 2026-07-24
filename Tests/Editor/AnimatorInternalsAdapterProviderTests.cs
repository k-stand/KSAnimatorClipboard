using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using com.github.k_stand.ksanimatorclipboard.editor;

namespace com.github.k_stand.ksanimatorclipboard.editor.tests
{
    public class AnimatorInternalsAdapterProviderTests
    {
        [Test]
        public void Resolve_SelectsUnity2022Adapter_ForKnownMajorVersion()
        {
            IAnimatorInternalsAdapter adapter = AnimatorInternalsAdapterProvider.Resolve("2022.3.22f1");

            Assert.IsInstanceOf<Unity2022AnimatorInternalsAdapter>(adapter);
        }

        [Test]
        public void Resolve_FallsBackToUnity2022Adapter_ForUnknownMajorVersion()
        {
            LogAssert.Expect(LogType.Warning, "KSAnimatorClipboard: Unity 6000.0.10f1 は動作検証されていません。Unity2022AnimatorInternalsAdapter でのフォールバック動作を試みます。");

            IAnimatorInternalsAdapter adapter = AnimatorInternalsAdapterProvider.Resolve("6000.0.10f1");

            Assert.IsInstanceOf<Unity2022AnimatorInternalsAdapter>(adapter);
        }

        [Test]
        public void ValidateOrThrow_DoesNotThrow_WhenAdapterValidateSucceeds()
        {
            Unity2022AnimatorInternalsAdapter adapter = new();

            Assert.DoesNotThrow(() => AnimatorInternalsAdapterProvider.ValidateOrThrow(adapter, "2022.3.22f1"));
        }

        [Test]
        public void ValidateOrThrow_ThrowsInvalidOperationException_WhenAdapterValidateFails()
        {
            FailingAnimatorInternalsAdapter adapter = new();

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => AnimatorInternalsAdapterProvider.ValidateOrThrow(adapter, "2022.3.22f1"));

            Assert.IsInstanceOf<NotSupportedException>(ex.InnerException);
        }

        [Test]
        public void Current_ReturnsSameInstance_OnRepeatedAccess()
        {
            IAnimatorInternalsAdapter first = AnimatorInternalsAdapterProvider.Current;
            IAnimatorInternalsAdapter second = AnimatorInternalsAdapterProvider.Current;

            Assert.AreSame(first, second);
        }

        private sealed class FailingAnimatorInternalsAdapter : IAnimatorInternalsAdapter
        {
            public StateMotionPair[] GetAllOverrideStateMotionPairs(UnityEditor.Animations.AnimatorControllerLayer acl) => null;

            public StateBehavioursPair[] GetAllOverrideBehavioursPairs(UnityEditor.Animations.AnimatorControllerLayer acl) => null;

            public void InitOverrideStateMotionPairs(UnityEditor.Animations.AnimatorControllerLayer acl) { }

            public void InitOverrideStateBehavioursPairs(UnityEditor.Animations.AnimatorControllerLayer acl) { }

            public void Validate() => throw new NotSupportedException("intentional failure for test");
        }
    }
}
