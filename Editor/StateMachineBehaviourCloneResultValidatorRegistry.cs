using System;
using System.Collections.Generic;

namespace com.github.k_stand.ksanimatorcopyengine.editor
{
    internal sealed class StateMachineBehaviourCloneResultValidatorRegistry
    {
        internal static StateMachineBehaviourCloneResultValidatorRegistry Shared { get; } = new();

        private readonly Dictionary<Type, IStateMachineBehaviourCloneResultValidator> _validators = new();

        internal void Register(IStateMachineBehaviourCloneResultValidator validator)
        {
            if (validator == null) throw new ArgumentNullException(nameof(validator));
            _validators[validator.BehaviourType] = validator;
        }

        internal void Unregister(Type behaviourType) => _validators.Remove(behaviourType);

        internal IStateMachineBehaviourCloneResultValidator Resolve(Type type)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                if (_validators.TryGetValue(current, out IStateMachineBehaviourCloneResultValidator validator))
                {
                    return validator;
                }
            }

            return null;
        }
    }
}
