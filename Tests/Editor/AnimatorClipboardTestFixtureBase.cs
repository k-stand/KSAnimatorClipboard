using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace com.github.k_stand.ksanimatorclipboard.editor.tests
{
    public abstract class AnimatorClipboardTestFixtureBase
    {
        private List<Object> _createdObjects;

        [SetUp]
        public void BaseSetUp()
        {
            _createdObjects = new List<Object>();
        }

        [TearDown]
        public void BaseTearDown()
        {
            foreach (Object obj in _createdObjects)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
        }

        protected T Create<T>() where T : Object, new()
        {
            T obj = new();
            _createdObjects.Add(obj);
            return obj;
        }

        protected T Track<T>(T obj) where T : Object
        {
            _createdObjects.Add(obj);
            return obj;
        }
    }
}
