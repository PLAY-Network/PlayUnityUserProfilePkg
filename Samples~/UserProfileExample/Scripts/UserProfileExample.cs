using System.IO;
using System.Threading.Tasks;
using RGN.Impl.Firebase;
using RGN.Modules.UserProfile;
using RGN.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RGN.Samples
{
    public interface IUserProfileClient
    {
        Task<string> GetPrimaryWalletAddressAsync();
        Task OpenWalletsScreenAsync();
    }

    public sealed class UserProfileExample : IUIScreen
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _uploadNewProfilePictureButton;
        [SerializeField] private Button _openWalletsScreenButton;
        [SerializeField] private LoadingIndicator _profilePictureLoadingIndicator;
        [SerializeField] private LoadingIndicator _fullScreenLoadingIndicator;
        [SerializeField] private LoadingIndicator _primaryWalletLoadingIndicator;
        [SerializeField] private TextMeshProUGUI _displayNameText;
        [SerializeField] private TextMeshProUGUI _emailText;
        [SerializeField] private TextMeshProUGUI _primaryWalletAddressText;
        [SerializeField] private RawImage _userProfileIconRawImage;

        private IUserProfileClient _userProfileClient;

        public override void PreInit(IRGNFrame rgnFrame)
        {
            base.PreInit(rgnFrame);
            _uploadNewProfilePictureButton.onClick.AddListener(OnUploadNewProfilePictureButtonClickAsync);
            _openWalletsScreenButton.onClick.AddListener(OnOpenWalletsScreenButtonClickAsync);
            RGNCore.I.AuthenticationChanged += OnAuthStateChangedAsync;
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _uploadNewProfilePictureButton.onClick.RemoveListener(OnUploadNewProfilePictureButtonClickAsync);
            _openWalletsScreenButton.onClick.RemoveListener(OnOpenWalletsScreenButtonClickAsync);
            RGNCore.I.AuthenticationChanged -= OnAuthStateChangedAsync;
        }
        public void SetUserProfileClient(IUserProfileClient userProfileClient)
        {
            _userProfileClient = userProfileClient;
        }

        private async void OnAuthStateChangedAsync(EnumLoginState state, EnumLoginError error)
        {
            switch (state)
            {
                case EnumLoginState.NotLoggedIn:
                    break;
                case EnumLoginState.Success:
                    SetEmailAndDisplayName("Loading email...", "Loading display name...");
                    string displayName = string.Empty;
                    string email = string.Empty;
                    if (RGNCore.I.AuthorizedProviders == EnumAuthProvider.Guest)
                    {
                        displayName = " Guest Account";
                        email = "guest@getready.io";
                    }
                    else if (RGNCore.I.AuthorizedProviders == EnumAuthProvider.Email)
                    {
                        var userProfile = await UserProfileModule.I.GetProfileAsync(RGNCore.I.MasterAppUser.UserId);
                        displayName = userProfile.displayName;
                        email = RGNCore.I.MasterAppUser.Email;
                    }
                    SetEmailAndDisplayName(email, displayName);
                    await LoadProfilePictureAsync(true);
                    await LoadPrimaryWalletAddressAsync();
                    _canvasGroup.interactable = true;
                    _fullScreenLoadingIndicator.SetEnabled(false);
                    break;
                case EnumLoginState.Error:
                    break;
            };
        }
        private void SetEmailAndDisplayName(string email, string displayName)
        {
            _emailText.text = email;
            _displayNameText.text = displayName;
        }
        private async Task LoadProfilePictureAsync(bool tryToloadFromCache)
        {
            _canvasGroup.interactable = false;
            _profilePictureLoadingIndicator.SetEnabled(true);
            string userId = RGNCore.I.MasterAppUser.UserId;
            string userProfileImageLocalPath = Path.Combine(Application.persistentDataPath, "user_profile", userId + ".png");
            Texture2D userProfilePicture = null;
            if (tryToloadFromCache)
            {
                if (File.Exists(userProfileImageLocalPath))
                {
                    byte[] bytes = File.ReadAllBytes(userProfileImageLocalPath);
                    userProfilePicture = new Texture2D(2, 2);
                    userProfilePicture.LoadImage(bytes);
                }
            }
            if (userProfilePicture == null)
            {
                userProfilePicture = await UserProfileModule.I.DownloadAvatarImageAsync<Texture2D>(userId);
                if (userProfilePicture != null)
                {
                    byte[] bytes = userProfilePicture.EncodeToPNG();
                    Directory.CreateDirectory(Path.GetDirectoryName(userProfileImageLocalPath));
                    File.WriteAllBytes(userProfileImageLocalPath, bytes);
                }
            }
            if (userProfilePicture != null)
            {
                _userProfileIconRawImage.texture = userProfilePicture;
            }
            _canvasGroup.interactable = true;
            _profilePictureLoadingIndicator.SetEnabled(false);
        }
        private async void OnUploadNewProfilePictureButtonClickAsync()
        {
            _canvasGroup.interactable = false;
            _fullScreenLoadingIndicator.SetEnabled(true);
            var tcs = new TaskCompletionSource<bool>();
            NativeGallery.GetImageFromGallery(async path => {
                try
                {
                    if (path == null)
                    {
                        Debug.Log("User cancelled the image upload, or no permission granted");
                        tcs.TrySetResult(false);
                        return;
                    }
                    if (!File.Exists(path))
                    {
                        Debug.LogError("File does not exist at path: " + path);
                        tcs.TrySetResult(false);
                        return;
                    }
                    byte[] textureBytes = File.ReadAllBytes(path);
                    var unityTexture = new Texture2D(2, 2);
                    unityTexture.LoadImage(textureBytes);
                    var rgnTexture = new Impl.Firebase.Engine.Texture2D(unityTexture);
                    await UserProfileModule.I.UploadAvatarImageAsync(rgnTexture);
                    await LoadProfilePictureAsync(false);
                    tcs.TrySetResult(true);
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                    tcs.TrySetException(ex);
                }
            },
            "Select User Image");
            await tcs.Task;
            _fullScreenLoadingIndicator.SetEnabled(false);
            _canvasGroup.interactable = true;
        }
        private async void OnOpenWalletsScreenButtonClickAsync()
        {
            if (_userProfileClient == null)
            {
                ToastMessage.I.ShowError("Please open UIRoot Example from Firebase Impl package to open wallets screen");
                return;
            }
            _canvasGroup.interactable = false;
            await _userProfileClient.OpenWalletsScreenAsync();
            _canvasGroup.interactable = true;
        }
        private async Task LoadPrimaryWalletAddressAsync()
        {
            if (_userProfileClient == null)
            {
                _primaryWalletAddressText.text = "Use UIRoot Example from Firebase Impl package";
                return;
            }
            _canvasGroup.interactable = false;
            _primaryWalletLoadingIndicator.SetEnabled(true);
            string primaryWalletAddress = await _userProfileClient.GetPrimaryWalletAddressAsync();
            _primaryWalletAddressText.text = primaryWalletAddress;
            _canvasGroup.interactable = true;
            _primaryWalletLoadingIndicator.SetEnabled(false);
        }
    }
}