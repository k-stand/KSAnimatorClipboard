using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using com.github.k_stand.ksanimatorclipboard.editor.CrossController;
using com.github.k_stand.ksanimatorclipboard.editor.tests;

namespace com.github.k_stand.ksanimatorclipboard.editor.tests.CrossController
{
    public class ParameterReferenceResolverRegistryTests
    {
        private sealed class DerivedDummyStateMachineBehaviour : DummyStateMachineBehaviour { }

        private sealed class StubResolver : IParameterReferenceResolver
        {
            public Type BehaviourType => typeof(DummyStateMachineBehaviour);

            public IEnumerable<string> GetReferencedParameterNames(StateMachineBehaviour behaviour) => Array.Empty<string>();
        }

        [Test]
        public void Resolve_ReturnsNullForUnregisteredType()
        {
            ParameterReferenceResolverRegistry registry = new();
            Assert.IsNull(registry.Resolve(typeof(DummyStateMachineBehaviour)));
        }

        [Test]
        public void Register_ThenResolve_ReturnsRegisteredResolverForExactType()
        {
            ParameterReferenceResolverRegistry registry = new();
            StubResolver resolver = new();
            registry.Register(resolver);

            Assert.AreSame(resolver, registry.Resolve(typeof(DummyStateMachineBehaviour)));
        }

        [Test]
        public void Resolve_WalksBaseTypeForSubclass()
        {
            ParameterReferenceResolverRegistry registry = new();
            StubResolver resolver = new();
            registry.Register(resolver);

            Assert.AreSame(resolver, registry.Resolve(typeof(DerivedDummyStateMachineBehaviour)));
        }

        [Test]
        public void Register_ThrowsArgumentNullException_WhenResolverIsNull()
        {
            ParameterReferenceResolverRegistry registry = new();
            Assert.Throws<ArgumentNullException>(() => registry.Register(null));
        }

        [Test]
        public void Unregister_RemovesRegisteredResolver()
        {
            ParameterReferenceResolverRegistry registry = new();
            registry.Register(new StubResolver());
            registry.Unregister(typeof(DummyStateMachineBehaviour));

            Assert.IsNull(registry.Resolve(typeof(DummyStateMachineBehaviour)));
        }

        [Test]
        public void Shared_HasNoResolversRegisteredByDefault()
        {
            Assert.IsNull(ParameterReferenceResolverRegistry.Shared.Resolve(typeof(DummyStateMachineBehaviour)));
        }
    }
}
