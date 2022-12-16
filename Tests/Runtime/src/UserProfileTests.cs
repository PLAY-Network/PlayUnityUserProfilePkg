using NUnit.Framework;
using RGN.Extensions;
using RGN.Impl.Firebase.Core;
using RGN.Model;
using RGN.Modules.UserProfile;
using System.Collections;
using UnityEngine.TestTools;

namespace RGN.UserProfile.Tests.Runtime
{
    [TestFixture]
    public class UserProfileTests
    {
        private static bool[] isAdminArray = new bool[] { false, true };
        private static int[] accessLevelArray = new int[] { 1, 4, 34, -1 };

        [OneTimeSetUp]
        public async void OneTimeSetup()
        {
            var applicationStore = ApplicationStore.I; //TODO: this will work only in editor.
            RGNCoreBuilder.AddModule(new UserProfileModule<UserProfileData>(applicationStore.RGNStorageURL));
            var appOptions = new AppOptions()
            {
                ApiKey = applicationStore.RGNMasterApiKey,
                AppId = applicationStore.RGNMasterAppID,
                ProjectId = applicationStore.RGNMasterProjectId
            };

            await RGNCoreBuilder.Build(
                new RGN.Impl.Firebase.Dependencies(
                    appOptions,
                    applicationStore.RGNStorageURL),
                appOptions,
               applicationStore.RGNStorageURL,
               applicationStore.RGNAppId);

            if (applicationStore.usingEmulator)
            {
                RGNCore rgnCore = (RGNCore)RGNCoreBuilder.I;
                var firestore = rgnCore.readyMasterFirestore;
                string firestoreHost = applicationStore.emulatorServerIp + applicationStore.firestorePort;
                bool firestoreSslEnabled = false;
                firestore.UserEmulator(firestoreHost, firestoreSslEnabled);
                rgnCore.readyMasterFunction.UseFunctionsEmulator(applicationStore.emulatorServerIp + applicationStore.functionsPort);
                //TODO: storage, auth, realtime db
            }
        }

        [UnityTest]
        public IEnumerator ChangeAdminStatusByUserId_CanBeCalledOnlyWithAdminRights(
            [ValueSource("isAdminArray")] bool isAdmin,
            [ValueSource("accessLevelArray")] int accessLevel)
        {
            var task = RGNCoreBuilder.I.GetModule<UserProfileModule<UserProfileData>>().ChangeAdminStatusByUserId(
                "00c377dca1054b64b6186d1c6eab96d4",
                isAdmin,
                accessLevel);
            yield return task.AsIEnumeratorReturnNull();
            var result = task.Result;

            Assert.NotNull(result, "The result is null");
            UnityEngine.Debug.Log(result);
        }
        [UnityTest]
        public IEnumerator ChangeAdminStatusByEmail_CanBeCalledOnlyWithAdminRights()
        {
            var task = RGNCoreBuilder.I.GetModule<UserProfileModule<UserProfileData>>().ChangeAdminStatusByEmail(
                "readyemailtest@gmail.com",
                true,
                1);
            yield return task.AsIEnumeratorReturnNull();
            var result = task.Result;

            Assert.NotNull(result, "The result is null");
            UnityEngine.Debug.Log(result);
        }
        [UnityTest]
        public IEnumerator GetUserCustomClaimsByUserId_CanBeCalledByAnyUser()
        {
            var task = RGNCoreBuilder.I.GetModule<UserProfileModule<UserProfileData>>().GetUserCustomClaimsByUserId(
                "88da8d6527b44c00b2ece69d9d561469");
            yield return task.AsIEnumeratorReturnNull();
            var result = task.Result;

            Assert.NotNull(result, "The result is null");
            UnityEngine.Debug.Log(result);
        }
        [UnityTest]
        public IEnumerator GetUserCustomClaimsByUserId_GivesErrorForNonExistingUser()
        {
            var task = RGNCoreBuilder.I.GetModule<UserProfileModule<UserProfileData>>().GetUserCustomClaimsByUserId(
                "user_id_that_does_not_exist");
            yield return task.AsIEnumeratorReturnNullDontThrow();

            Assert.IsTrue(task.IsFaulted);
        }
        [UnityTest]
        public IEnumerator GetUserCustomClaimsByEmail_CanBeCalledByAnyUser()
        {
            var task = RGNCoreBuilder.I.GetModule<UserProfileModule<UserProfileData>>().GetUserCustomClaimsByEmail(
                "readyemailtest@gmail.com");
            yield return task.AsIEnumeratorReturnNull();
            var result = task.Result;

            Assert.NotNull(result, "The result is null");
            UnityEngine.Debug.Log(result);
        }
        [UnityTest]
        public IEnumerator GetUserCustomClaimsByEmail_GivesErrorForNonExistingUser()
        {
            var task = RGNCoreBuilder.I.GetModule<UserProfileModule<UserProfileData>>().GetUserCustomClaimsByEmail(
                "not_existing_user@fake.me");
            yield return task.AsIEnumeratorReturnNullDontThrow();

            Assert.IsTrue(task.IsFaulted);
        }
        [UnityTest]
        public IEnumerator MeasureUserClaimsSearchTime_ReturnsHtmlPage()
        {
            var task = RGNCoreBuilder.I.GetModule<UserProfileModule<UserProfileData>>().MeasureUserClaimsSearchTime(
                "88da8d6527b44c00b2ece69d9d561469");
            yield return task.AsIEnumeratorReturnNull();

            Assert.IsNotNull(task.Result);
            UnityEngine.Debug.Log(task.Result);
        }
    }
}
