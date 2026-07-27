using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using com.github.k_stand.ksanimatorcopyengine.editor;

namespace com.github.k_stand.ksanimatorcopyengine.editor.tests
{
    public class AnimatorCloneResultValidatorTests : AnimatorCopyEngineTestFixtureBase
    {
        private sealed class NullChildStubValidator : IStateMachineBehaviourCloneResultValidator
        {
            public Type BehaviourType => typeof(DummyStateMachineBehaviour);

            public IEnumerable<(string MemberName, object Child)> GetChildren(StateMachineBehaviour behaviour) => new (string, object)[] { ("StubChild", null) };
        }

        [Test]
        public void ValidateCloneResult_ReturnsEmpty_ForFullyValidStateMachine()
        {
            AnimatorStateMachine stateMachine = Create<AnimatorStateMachine>();
            AnimatorState state = Create<AnimatorState>();
            stateMachine.AddState(state, Vector3.zero);

            IReadOnlyCollection<AnimatorCloneResultValidator.InvalidNullMember> result = AnimatorCloneResultValidator.ValidateCloneResult(stateMachine);

            Assert.IsEmpty(result);
        }

        [Test]
        public void ValidateCloneResult_DetectsNullDestinationState_OnStateTransition()
        {
            AnimatorState state = Create<AnimatorState>();
            AnimatorStateTransition transition = state.AddTransition(state);
            transition.isExit = false;
            transition.destinationState = null;
            transition.destinationStateMachine = null;

            IReadOnlyCollection<AnimatorCloneResultValidator.InvalidNullMember> result = AnimatorCloneResultValidator.ValidateCloneResult(state);

            Assert.IsNotEmpty(result);
        }

        [Test]
        public void ValidateCloneResult_InvalidNullMember_ExposesParentAndMemberName()
        {
            AnimatorState state = Create<AnimatorState>();
            AnimatorStateTransition transition = state.AddTransition(state);
            transition.isExit = false;
            transition.destinationState = null;
            transition.destinationStateMachine = null;

            IReadOnlyCollection<AnimatorCloneResultValidator.InvalidNullMember> result = AnimatorCloneResultValidator.ValidateCloneResult(state);

            Assert.IsTrue(result.Any(m => m.Parent == transition && m.MemberName == "destinationState"));
        }

        [Test]
        public void ValidateCloneResult_ReturnsEmpty_ForNullTarget()
        {
            IReadOnlyCollection<AnimatorCloneResultValidator.InvalidNullMember> result = AnimatorCloneResultValidator.ValidateCloneResult(null);

            Assert.IsEmpty(result);
        }

        [Test]
        public void ValidateCloneResult_DoesNotRevalidateSharedStateMachine_ReachedViaMultiplePaths()
        {
            AnimatorStateMachine shared = Create<AnimatorStateMachine>();
            AnimatorStateMachine branchA = Create<AnimatorStateMachine>();
            AnimatorStateMachine branchB = Create<AnimatorStateMachine>();
            branchA.stateMachines = new[] { new ChildAnimatorStateMachine { stateMachine = shared } };
            branchB.stateMachines = new[] { new ChildAnimatorStateMachine { stateMachine = shared } };

            AnimatorStateMachine root = Create<AnimatorStateMachine>();
            root.stateMachines = new[]
            {
                new ChildAnimatorStateMachine { stateMachine = branchA },
                new ChildAnimatorStateMachine { stateMachine = branchB },
            };

            Assert.DoesNotThrow(() => AnimatorCloneResultValidator.ValidateCloneResult(root));
        }

        [Test]
        public void ValidateCloneResult_SkipsStateMachineBehaviour_WhenNoValidatorRegistered()
        {
            DummyStateMachineBehaviour behaviour = Track(ScriptableObject.CreateInstance<DummyStateMachineBehaviour>());

            IReadOnlyCollection<AnimatorCloneResultValidator.InvalidNullMember> result = AnimatorCloneResultValidator.ValidateCloneResult(behaviour);

            Assert.IsEmpty(result);
        }

        [Test]
        public void ValidateCloneResult_DetectsNullChild_WhenValidatorRegistered()
        {
            DummyStateMachineBehaviour behaviour = Track(ScriptableObject.CreateInstance<DummyStateMachineBehaviour>());
            StateMachineBehaviourCloneResultValidatorRegistry.Shared.Register(new NullChildStubValidator());
            try
            {
                IReadOnlyCollection<AnimatorCloneResultValidator.InvalidNullMember> result = AnimatorCloneResultValidator.ValidateCloneResult(behaviour);

                Assert.IsNotEmpty(result);
            }
            finally
            {
                StateMachineBehaviourCloneResultValidatorRegistry.Shared.Unregister(typeof(DummyStateMachineBehaviour));
            }
        }
    }
}
