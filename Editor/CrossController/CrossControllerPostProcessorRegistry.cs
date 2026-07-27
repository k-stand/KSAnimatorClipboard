using System;
using System.Collections.Generic;

namespace com.github.k_stand.ksanimatorcopyengine.editor.CrossController
{
    internal sealed class CrossControllerPostProcessorRegistry
    {
        internal static CrossControllerPostProcessorRegistry Shared { get; } = CreateDefault();

        private readonly Dictionary<Type, List<ICrossControllerPostProcessor>> _processors = new();

        internal void Register(ICrossControllerPostProcessor processor)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));

            if (!_processors.TryGetValue(processor.ObjectType, out List<ICrossControllerPostProcessor> list))
            {
                list = new List<ICrossControllerPostProcessor>();
                _processors[processor.ObjectType] = list;
            }

            list.Add(processor);
        }

        internal IEnumerable<ICrossControllerPostProcessor> ResolveAll(Type type)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                if (_processors.TryGetValue(current, out List<ICrossControllerPostProcessor> list))
                {
                    foreach (ICrossControllerPostProcessor processor in list)
                    {
                        yield return processor;
                    }
                }
            }
        }

        private static CrossControllerPostProcessorRegistry CreateDefault()
        {
            CrossControllerPostProcessorRegistry registry = new();
            registry.Register(new LayerSyncedIndexPostProcessor());
            return registry;
        }
    }
}
