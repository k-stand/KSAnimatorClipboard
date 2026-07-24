using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using com.github.k_stand.ksanimatorclipboard.editor;

namespace com.github.k_stand.ksanimatorclipboard.editor.tests
{
    public class StateMachineBehaviourCloneResultValidatorRegistryTests
    {
        private sealed class DerivedDummyStateMachineBehaviour : DummyStateMachineBehaviour { }

        private sealed class StubValidator : IStateMachineBehaviourCloneResultValidator
        {
            public Type BehaviourType => typeof(DummyStateMachineBehaviour);

            public IEnumerable<(string MemberName, object Child)> GetChildren(StateMachineBehaviour behaviour) => Array.Empty<(string, object)>();
        }

        [Test]
        public void Resolve_ReturnsNullForUnregisteredType()
        {
            StateMachineBehaviourCloneResultValidatorRegistry registry = new();
            Assert.IsNull(registry.Resolve(typeof(DummyStateMachineBehaviour)));
        }

        [Test]
        public void Register_ThenResolve_ReturnsRegisteredValidatorForExactType()
        {
            StateMachineBehaviourCloneResultValidatorRegistry registry = new();
            StubValidator validator = new();
            registry.Register(validator);

            Assert.AreSame(validator, registry.Resolve(typeof(DummyStateMachineBehaviour)));
        }

        [Test]
        public void Resolve_WalksBaseTypeForSubclass()
        {
            StateMachineBehaviourCloneResultValidatorRegistry registry = new();
            StubValidator validator = new();
            registry.Register(validator);

            Assert.AreSame(validator, registry.Resolve(typeof(DerivedDummyStateMachineBehaviour)));
        }

        [Test]
        public void Register_ThrowsArgumentNullException_WhenValidatorIsNull()
        {
            StateMachineBehaviourCloneResultValidatorRegistry registry = new();
            Assert.Throws<ArgumentNullException>(() => registry.Register(null));
        }

        [Test]
        public void Unregister_RemovesRegisteredValidator()
        {
            StateMachineBehaviourCloneResultValidatorRegistry registry = new();
            registry.Register(new StubValidator());
            registry.Unregister(typeof(DummyStateMachineBehaviour));

            Assert.IsNull(registry.Resolve(typeof(DummyStateMachineBehaviour)));
        }

        [Test]
        public void Shared_HasNoValidatorsRegisteredByDefault()
        {
            Assert.IsNull(StateMachineBehaviourCloneResultValidatorRegistry.Shared.Resolve(typeof(DummyStateMachineBehaviour)));
        }
    }
}
