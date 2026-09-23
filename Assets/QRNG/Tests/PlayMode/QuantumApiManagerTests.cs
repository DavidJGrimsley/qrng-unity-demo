using System.Collections;
using NUnit.Framework;
using QuantumApi.Unity;
using UnityEngine;
using UnityEngine.TestTools;

namespace QRNG.Tests
{
    public sealed class QuantumApiManagerTests
    {
        [UnityTest]
        public IEnumerator ManagerPersistsAndDestroysDuplicates()
        {
            if (QuantumApiManager.Instance != null)
            {
                Object.Destroy(QuantumApiManager.Instance.gameObject);
                yield return null;
            }

            var first = new GameObject("First Manager").AddComponent<QuantumApiManager>();
            yield return null;
            Assert.AreSame(first, QuantumApiManager.Instance);
            Assert.AreEqual("DontDestroyOnLoad", first.gameObject.scene.name);

            new GameObject("Duplicate Manager").AddComponent<QuantumApiManager>();
            yield return null;
            Assert.AreSame(first, QuantumApiManager.Instance);
            Assert.AreEqual(1, Object.FindObjectsByType<QuantumApiManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length);

            Object.Destroy(first.gameObject);
            yield return null;
            Assert.IsNull(QuantumApiManager.Instance);
        }
    }
}
