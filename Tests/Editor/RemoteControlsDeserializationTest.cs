using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;
using Cognitive3D;

namespace Cognitive3D.Tests
{
    public class RemoteControlsDeserializationTest
    {
        private RemoteVariableCollection fetchedCollection;

        [OneTimeSetUp]
        public void FetchRemoteControls()
        {
            string url = CognitiveStatics.GetRemoteControlsURL("test_user");

            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-HTTP-Method-Override", "GET");
            request.SetRequestHeader("Authorization", "APIKEY:DATA " + Cognitive3D_Preferences.Instance.ApplicationKey);
            request.SendWebRequest();

            // Edit Mode tests can't use coroutines, so wait synchronously
            while (!request.isDone) { }

            Assert.AreEqual(200, request.responseCode, $"Remote controls request failed with code {request.responseCode}: {request.error}");

            string json = request.downloadHandler.text;
            Debug.Log($"Remote controls response: {json}");

            fetchedCollection = JsonUtility.FromJson<RemoteVariableCollection>(json);
            Assert.IsNotNull(fetchedCollection, "Failed to deserialize remote controls response");
            Assert.IsTrue(fetchedCollection.remoteConfigurations.Count > 0, "No remote configurations returned from the API");
        }

        [Test]
        public void Boolean_DeserializesCorrectly()
        {
            var boolItem = fetchedCollection.remoteConfigurations.Find(x => x.type == "boolean");
            Assert.IsNotNull(boolItem, "No boolean remote variable found in API response");
            // A boolean variable set to true on the dashboard should not deserialize as false
            Assert.IsTrue(boolItem.valueBoolean, $"Boolean variable '{boolItem.remoteVariableName}' should be true — check JSON field name matches C# field");
        }

        [Test]
        public void Int_DeserializesCorrectly()
        {
            var intItem = fetchedCollection.remoteConfigurations.Find(x => x.type == "int");
            Assert.IsNotNull(intItem, "No int remote variable found in API response");
            Assert.AreNotEqual(0, intItem.valueInt, $"Int variable '{intItem.remoteVariableName}' deserialized as 0 — check JSON field name matches C# field");
        }

        [Test]
        public void String_DeserializesCorrectly()
        {
            var stringItem = fetchedCollection.remoteConfigurations.Find(x => x.type == "string");
            Assert.IsNotNull(stringItem, "No string remote variable found in API response");
            Assert.IsNotEmpty(stringItem.valueString, $"String variable '{stringItem.remoteVariableName}' deserialized as empty — check JSON field name matches C# field");
        }
    }
}
