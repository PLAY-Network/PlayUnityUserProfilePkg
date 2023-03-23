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
        void OpenWalletsScreen();
    }

    public sealed class UserProfileExample : IUIScreen
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _openWalletsScreenButton;
        [SerializeField] private LoadingIndicator _fullScreenLoadingIndicator;
        [SerializeField] private LoadingIndicator _primaryWalletLoadingIndicator;
        [SerializeField] private TextMeshProUGUI _displayNameText;
        [SerializeField] private TextMeshProUGUI _emailText;
        [SerializeField] private TextMeshProUGUI _primaryWalletAddressText;
        [SerializeField] private IconImage _profileIconImage;
        [SerializeField] private CoinInfoItem _rgnCoinInfo;
        [SerializeField] private CoinInfoItem _customCoinInfo;
        [SerializeField] private PullToRefresh _pullToRefresh;

        private IUserProfileClient _userProfileClient;

        public override void PreInit(IRGNFrame rgnFrame)
        {
            base.PreInit(rgnFrame);
            _profileIconImage.OnClick.AddListener(OnUploadNewProfilePictureButtonClickAsync);
            _openWalletsScreenButton.onClick.AddListener(OnOpenWalletsScreenButtonClickAsync);
            RGNCore.I.AuthenticationChanged += OnAuthStateChangedAsync;
            _pullToRefresh.RefreshRequested += ReloadUserProfileAsync;
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _profileIconImage.OnClick.RemoveListener(OnUploadNewProfilePictureButtonClickAsync);
            _openWalletsScreenButton.onClick.RemoveListener(OnOpenWalletsScreenButtonClickAsync);
            RGNCore.I.AuthenticationChanged -= OnAuthStateChangedAsync;
            _pullToRefresh.RefreshRequested -= ReloadUserProfileAsync;
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
                    await ReloadUserProfileAsync();
                    break;
                case EnumLoginState.Error:
                    break;
            };
        }
        private async Task ReloadUserProfileAsync()
        {
            SetEmailAndDisplayName("Loading email...", "Loading display name...");
            SetUserCoinInfoIsLoading();
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
            await LoadUserCoinInfoAsync();
            _canvasGroup.interactable = true;
            _fullScreenLoadingIndicator.SetEnabled(false);
        }
        private void SetEmailAndDisplayName(string email, string displayName)
        {
            _emailText.text = email;
            _displayNameText.text = displayName;
        }
        private async Task LoadProfilePictureAsync(bool tryToloadFromCache)
        {
            _canvasGroup.interactable = false;
            _profileIconImage.SetLoading(true);
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
                    userProfilePicture.Apply();
                }
            }
            if (userProfilePicture == null)
            {
                byte[] bytes = await UserProfileModule.I.DownloadAvatarImageAsync(userId);

                if (bytes != null)
                {
                    userProfilePicture = new Texture2D(1, 1);
                    userProfilePicture.LoadImage(bytes);
                    userProfilePicture.Apply();
                    Directory.CreateDirectory(Path.GetDirectoryName(userProfileImageLocalPath));
                    File.WriteAllBytes(userProfileImageLocalPath, bytes);
                }
            }
            _profileIconImage.SetProfileTexture(userProfilePicture);
            _canvasGroup.interactable = true;
            _profileIconImage.SetLoading(false);
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
                    await UserProfileModule.I.UploadAvatarImageAsync(textureBytes);
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
            _userProfileClient.OpenWalletsScreen();
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
        private async Task LoadUserCoinInfoAsync()
        {
            var result = await UserProfileModule.I.GetUserCurrenciesAsync();
            for (int i = 0; i < result.Count; ++i)
            {
                var currency = result[i];
                _rgnCoinInfo.Init(currency);
                _customCoinInfo.Init(currency);
            }
        }
        private void SetUserCoinInfoIsLoading()
        {
            _rgnCoinInfo.SetIsLoading();
            _customCoinInfo.SetIsLoading();
        }
    }
}
