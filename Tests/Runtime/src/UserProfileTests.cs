using System.Collections;
using NUnit.Framework;
using RGN.Extensions;
using RGN.Modules.UserProfile;
using RGN.Tests;
using UnityEngine.TestTools;

namespace RGN.UserProfile.Tests.Runtime
{
    [TestFixture]
    public class UserProfileTests : BaseTests
    {
        private static bool[] isAdminArray = new bool[] { false, true };
        private static int[] accessLevelArray = new int[] { 1, 4, 34, -1 };

        [UnityTest]
        public IEnumerator ChangeAdminStatusByUserId_CanBeCalledOnlyWithAdminRights(
            [ValueSource("isAdminArray")] bool isAdmin,
            [ValueSource("accessLevelArray")] int accessLevel)
        {
            yield return LoginAsAdminTester();

            var task = UserProfileModule<UserProfileData>.I.ChangeAdminStatusByUserIdAsync(
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
            yield return LoginAsAdminTester();

            var task = UserProfileModule<UserProfileData>.I.ChangeAdminStatusByEmailAsync(
                "readyemailtest@gmail.com",
                true,
                1);
            yield return task.AsIEnumeratorReturnNull();
            var result = task.Result;

            Assert.NotNull(result, "The result is null");
            foreach (var item in result)
            {
                UnityEngine.Debug.Log(item.Key + ": " + item.Value);
            }
        }
        [UnityTest]
        public IEnumerator GetUserCustomClaimsByUserId_CanBeCalledByAnyUser()
        {
            yield return LoginAsNormalTester();

            var task = UserProfileModule<UserProfileData>.I.GetUserCustomClaimsByUserIdAsync(
                "88da8d6527b44c00b2ece69d9d561469");
            yield return task.AsIEnumeratorReturnNull();
            var result = task.Result;

            Assert.NotNull(result, "The result is null");
            foreach (var item in result)
            {
                UnityEngine.Debug.Log(item.Key + ": " + item.Value);
            }
        }
        [UnityTest]
        public IEnumerator GetUserCustomClaimsByUserId_GivesErrorForNonExistingUser()
        {
            yield return LoginAsNormalTester();

            var task = UserProfileModule<UserProfileData>.I.GetUserCustomClaimsByUserIdAsync(
                "user_id_that_does_not_exist");
            yield return task.AsIEnumeratorReturnNullDontThrow();

            Assert.IsTrue(task.IsFaulted);
        }
        [UnityTest]
        public IEnumerator GetUserCustomClaimsByEmail_CanBeCalledByAnyUser()
        {
            yield return LoginAsNormalTester();

            var task = UserProfileModule<UserProfileData>.I.GetUserCustomClaimsByEmailAsync(
                "readyemailtest@gmail.com");
            yield return task.AsIEnumeratorReturnNull();
            var result = task.Result;

            Assert.NotNull(result, "The result is null");
            foreach (var item in result)
            {
                UnityEngine.Debug.Log(item.Key + ": " + item.Value);
            }
        }
        [UnityTest]
        public IEnumerator GetUserCustomClaimsByEmail_GivesErrorForNonExistingUser()
        {
            yield return LoginAsNormalTester();

            var task = UserProfileModule<UserProfileData>.I.GetUserCustomClaimsByEmailAsync(
                "not_existing_user@fake.me");
            yield return task.AsIEnumeratorReturnNullDontThrow();

            Assert.IsTrue(task.IsFaulted);
        }
        [UnityTest]
        public IEnumerator MeasureUserClaimsSearchTime_ReturnsHtmlPage()
        {
            yield return LoginAsNormalTester();

            var task = UserProfileModule<UserProfileData>.I.MeasureUserClaimsSearchTimeAsync(
                "88da8d6527b44c00b2ece69d9d561469");
            yield return task.AsIEnumeratorReturnNull();

            Assert.IsNotNull(task.Result);
            UnityEngine.Debug.Log(task.Result);
        }
    }
}
