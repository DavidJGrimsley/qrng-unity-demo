using System.Collections;
using System.Reflection;
using NUnit.Framework;
using QuantumApi.Unity;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Networking;

namespace QRNG.Tests
{
    public sealed class QuantumApiManagerTests
    {
        [Test]
        public void DirectAndProxyRequestsUseTheirOwnUrlsAndAuthentication()
        {
            var direct = new QuantumApiClient(new QuantumApiClientOptions { ApiKey = "test-key" });
            using (var request = BuildRequest(direct))
            {
                Assert.AreEqual("https://davidjgrimsley.com/public-facing/api/quantum/v1/random", request.url);
                Assert.AreEqual("test-key", request.GetRequestHeader("X-API-Key"));
            }

            var proxy = new QuantumApiClient(new QuantumApiClientOptions
            {
                BackendProxyMode = true,
                BackendProxyUrl = " https://proxy.example.test/quantum/ ",
                ApiKey = "must-not-send",
                BearerToken = "must-not-send",
            });
            using (var request = BuildRequest(proxy))
            {
                Assert.AreEqual("https://proxy.example.test/quantum/v1/random", request.url);
                Assert.IsNull(request.GetRequestHeader("X-API-Key"));
                Assert.IsNull(request.GetRequestHeader("Authorization"));
            }
            using (var request = BuildRequest(proxy, new QuantumApiRequestOptions
            {
                AuthMode = QuantumApiAuthMode.ApiKey,
                ApiKey = "must-not-send",
                Headers = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["X-API-Key"] = "must-not-send",
                    ["Authorization"] = "must-not-send",
                },
            }))
            {
                Assert.IsNull(request.GetRequestHeader("X-API-Key"));
                Assert.IsNull(request.GetRequestHeader("Authorization"));
            }
        }

        [Test]
        public void ProxyRequiresUrlAndIbmDefaultsAllowOverrides()
        {
            var emptyProxy = new QuantumApiClient(new QuantumApiClientOptions { BackendProxyMode = true });
            var args = new object[] { "/random", UnityWebRequest.kHttpVerbPOST, null, null, null };
            var build = typeof(QuantumApiClient).GetMethod("BuildRequest", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNull(build.Invoke(emptyProxy, args));
            Assert.AreEqual("missing_proxy_url", ((QuantumApiError)args[4]).ErrorCode);
            args[4] = null;
            var invalidProxy = new QuantumApiClient(new QuantumApiClientOptions
            {
                BackendProxyMode = true, BackendProxyUrl = "ftp://proxy.example.test",
            });
            Assert.IsNull(build.Invoke(invalidProxy, args));
            Assert.AreEqual("missing_proxy_url", ((QuantumApiError)args[4]).ErrorCode);

            var client = new QuantumApiClient(new QuantumApiClientOptions
            {
                DefaultIbmBackend = "ibm_fez",
                DefaultIbmProfile = "Demo Profile",
            });
            var json = typeof(QuantumApiClient).GetMethod("BuildRandomJobSubmitJson", BindingFlags.Instance | BindingFlags.NonPublic);
            var fallback = (string)json.Invoke(client, new object[] { new RandomJobSubmitRequest { min = 0, max = 3 } });
            StringAssert.Contains("\"backend_name\":\"ibm_fez\"", fallback);
            StringAssert.Contains("\"ibm_profile\":\"Demo Profile\"", fallback);
            var explicitValues = (string)json.Invoke(client, new object[] { new RandomJobSubmitRequest
            {
                min = 0, max = 3, backend_name = "ibm_torino", ibm_profile = "Other Profile",
            } });
            StringAssert.Contains("\"backend_name\":\"ibm_torino\"", explicitValues);
            StringAssert.Contains("\"ibm_profile\":\"Other Profile\"", explicitValues);
            var nonIbm = (string)json.Invoke(client, new object[] { new RandomJobSubmitRequest { provider = "local" } });
            Assert.IsFalse(nonIbm.Contains("Demo Profile"));
        }

        private static UnityWebRequest BuildRequest(QuantumApiClient client, QuantumApiRequestOptions options = null)
        {
            var method = typeof(QuantumApiClient).GetMethod("BuildRequest", BindingFlags.Instance | BindingFlags.NonPublic);
            var args = new object[] { "/random", UnityWebRequest.kHttpVerbPOST, null, options, null };
            return (UnityWebRequest)method.Invoke(client, args);
        }

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

            typeof(QuantumApiManager).GetMethod("ResetInstance", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, null);
            typeof(QuantumApiManager).GetMethod("RestoreInstanceAfterSceneLoad", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, null);
            Assert.AreSame(first, QuantumApiManager.Instance);

            Object.Destroy(first.gameObject);
            yield return null;
            Assert.IsNull(QuantumApiManager.Instance);
        }
    }
}
