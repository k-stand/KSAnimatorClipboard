using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Animations;
using com.github.k_stand.ksanimatorclipboard.editor.CrossController;

namespace com.github.k_stand.ksanimatorclipboard.editor.tests.CrossController
{
    public class CrossControllerPostProcessorRegistryTests
    {
        private sealed class RecordingPostProcessor : ICrossControllerPostProcessor
        {
            public List<object> ProcessedObjects { get; } = new();

            public Type ObjectType => typeof(AnimatorControllerLayer);

            public void PostProcess(object clonedObject) => ProcessedObjects.Add(clonedObject);
        }

        [Test]
        public void ResolveAll_ReturnsEmptyForUnregisteredType()
        {
            CrossControllerPostProcessorRegistry registry = new();
            Assert.IsEmpty(registry.ResolveAll(typeof(AnimatorControllerLayer)));
        }

        [Test]
        public void Register_ThenResolveAll_ReturnsRegisteredProcessorForExactType()
        {
            CrossControllerPostProcessorRegistry registry = new();
            RecordingPostProcessor processor = new();
            registry.Register(processor);

            CollectionAssert.Contains(registry.ResolveAll(typeof(AnimatorControllerLayer)).ToList(), processor);
        }

        [Test]
        public void Register_AllowsMultipleProcessorsForSameType()
        {
            CrossControllerPostProcessorRegistry registry = new();
            RecordingPostProcessor first = new();
            RecordingPostProcessor second = new();
            registry.Register(first);
            registry.Register(second);

            List<ICrossControllerPostProcessor> resolved = registry.ResolveAll(typeof(AnimatorControllerLayer)).ToList();

            Assert.AreEqual(2, resolved.Count);
        }

        [Test]
        public void Register_ThrowsArgumentNullException_WhenProcessorIsNull()
        {
            CrossControllerPostProcessorRegistry registry = new();
            Assert.Throws<ArgumentNullException>(() => registry.Register(null));
        }

        [Test]
        public void Shared_HasLayerSyncedIndexPostProcessorRegisteredForAnimatorControllerLayer()
        {
            List<ICrossControllerPostProcessor> processors = CrossControllerPostProcessorRegistry.Shared.ResolveAll(typeof(AnimatorControllerLayer)).ToList();
            Assert.IsTrue(processors.Any(p => p is LayerSyncedIndexPostProcessor));
        }
    }
}
