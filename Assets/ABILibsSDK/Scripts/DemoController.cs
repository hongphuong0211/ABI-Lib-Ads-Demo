using UnityEngine;
using UnityEngine.UI;
using ABI.Ads.UnityBridge;

namespace ABILibsSDK
{
    public class DemoController : MonoBehaviour
    {
        [Header("ABI Ads Configuration")]
        [SerializeField] private TextAsset globalConfig;
        [SerializeField] private TextAsset placementConfig;
        [Header("ABI Ads Placements")]
        [SerializeField] private string bannerPlacement = "main_banner";
        [SerializeField] private string interstitialPlacement = "main_interstitial";
        [SerializeField] private string rewardedPlacement = "main_reward";
        [SerializeField] private string appOpenPlacement = "main_app_open";
        [SerializeField] private string nativePlacement = "main_native";
        [SerializeField] private string nativeFullscreenPlacement = "main_native_fullscreen";
        [SerializeField] private InputField nativeTemplateName;
        [SerializeField] private InputField nativeFullscreenTemplateName;

        [Header("UI References")]
        [SerializeField] private Button btnLoadBanner;
        [SerializeField] private Button btnShowBanner;
        [SerializeField] private Button btnHideBanner;
        [SerializeField] private Button btnLoadInterstitial;
        [SerializeField] private Button btnShowInterstitial;
        [SerializeField] private Button btnLoadRewarded;
        [SerializeField] private Button btnShowRewarded;
        [SerializeField] private Button btnLoadAppOpen;
        [SerializeField] private Button btnShowAppOpen;
        [SerializeField] private Button btnLoadNative;
        [SerializeField] private Button btnShowNative;
        [SerializeField] private Button btnLoadNativeFullscreen;
        [SerializeField] private Button btnShowNativeFullscreen;
        [SerializeField] private Button btnFetchRemoteConfig;
        [SerializeField] private Button btnLogAppsFlyerEvent;
        [SerializeField] private Text txtStatus;

        private void Start()
        {
            SetupButtons();
            SetupABIAdsCallbacks();
            ABIAds.Initialize(globalConfig != null ? globalConfig.text : Resources.Load<TextAsset>("Configs/global_config.json").text, 
            placementConfig != null ? placementConfig.text : Resources.Load<TextAsset>("Configs/placements.json").text);

            if (SDKInitializer.Instance != null)
            {
                SDKInitializer.Instance.OnAllSDKsInitialized += OnSDKsReady;
            }

            Log("ABI Ads bridge initializing...");
        }

        private void OnDestroy()
        {
            ABIAds.EventReceived -= OnABIAdsEvent;
            ABIAds.OnInitialized -= OnABIAdsInitialized;
            UnregisterPlacementCallbacks();
        }

        private void SetupButtons()
        {
            if (btnLoadBanner != null)
                btnLoadBanner.onClick.AddListener(OnLoadBanner);
            if (btnShowBanner != null)
                btnShowBanner.onClick.AddListener(OnShowBanner);
            if (btnHideBanner != null)
                btnHideBanner.onClick.AddListener(OnHideBanner);
            if (btnLoadInterstitial != null)
                btnLoadInterstitial.onClick.AddListener(OnLoadInterstitial);
            if (btnShowInterstitial != null)
                btnShowInterstitial.onClick.AddListener(OnShowInterstitial);
            if (btnLoadRewarded != null)
                btnLoadRewarded.onClick.AddListener(OnLoadRewarded);
            if (btnShowRewarded != null)
                btnShowRewarded.onClick.AddListener(OnShowRewarded);
            if (btnLoadAppOpen != null)
                btnLoadAppOpen.onClick.AddListener(OnLoadAppOpen);
            if (btnShowAppOpen != null)
                btnShowAppOpen.onClick.AddListener(OnShowAppOpen);
            if (btnLoadNative != null)
                btnLoadNative.onClick.AddListener(OnLoadNative);
            if (btnShowNative != null)
                btnShowNative.onClick.AddListener(OnShowNative);
            if (btnLoadNativeFullscreen != null)
                btnLoadNativeFullscreen.onClick.AddListener(OnLoadNativeFullscreen);
            if (btnShowNativeFullscreen != null)
                btnShowNativeFullscreen.onClick.AddListener(OnShowNativeFullscreen);
            if (btnFetchRemoteConfig != null)
                btnFetchRemoteConfig.onClick.AddListener(OnFetchRemoteConfig);
            if (btnLogAppsFlyerEvent != null)
                btnLogAppsFlyerEvent.onClick.AddListener(OnLogAppsFlyerEvent);
        }

        private void SetupABIAdsCallbacks()
        {
            ABIAds.EventReceived += OnABIAdsEvent;
            ABIAds.OnInitialized += OnABIAdsInitialized;
            RegisterPlacementCallbacks(bannerPlacement);
            RegisterPlacementCallbacks(interstitialPlacement);
            RegisterPlacementCallbacks(rewardedPlacement);
            RegisterPlacementCallbacks(appOpenPlacement);
            RegisterPlacementCallbacks(nativePlacement);
            RegisterPlacementCallbacks(nativeFullscreenPlacement);
        }

        private void RegisterPlacementCallbacks(string placement)
        {
            if (string.IsNullOrWhiteSpace(placement))
            {
                return;
            }

            var callbacks = ABIAds.RegisterPlacement(placement);
            callbacks.OnLoaded = evt => Log($"{evt.placement} loaded");
            callbacks.OnFailed = evt => Log($"{evt.placement} failed: {evt.error}");
            callbacks.OnDisplayFailed = evt => Log($"{evt.placement} display failed: {evt.error}");
            callbacks.OnImpression = evt => Log($"{evt.placement} impression");
            callbacks.OnClicked = evt => Log($"{evt.placement} clicked");
            callbacks.OnClosed = evt => Log($"{evt.placement} closed");
            callbacks.OnRewardGranted = evt => Log($"{evt.placement} reward granted");
            callbacks.OnRewardCompleted = evt => Log($"{evt.placement} reward completed");
            callbacks.OnRevenue = evt => Log($"{evt.placement} revenue={evt.revenue} {evt.currency}");
        }

        private void UnregisterPlacementCallbacks()
        {
            ABIAds.UnregisterPlacement(bannerPlacement);
            ABIAds.UnregisterPlacement(interstitialPlacement);
            ABIAds.UnregisterPlacement(rewardedPlacement);
            ABIAds.UnregisterPlacement(appOpenPlacement);
            ABIAds.UnregisterPlacement(nativePlacement);
            ABIAds.UnregisterPlacement(nativeFullscreenPlacement);
        }

        private void OnSDKsReady()
        {
            Log("All SDKs initialized!");

            if (AppsFlyerManager.Instance != null)
            {
                AppsFlyerManager.Instance.OnConversionDataReceived += (data) =>
                {
                    string source = AppsFlyerManager.Instance.IsOrganic ? "Organic" : AppsFlyerManager.Instance.MediaSource;
                    Log($"Attribution: {source}, Campaign: {AppsFlyerManager.Instance.Campaign}");
                };
            }
        }

        private void OnLoadBanner()
        {
            ABIAds.Load(bannerPlacement);
            Log($"Load banner: {bannerPlacement}");
        }

        private void OnShowBanner()
        {
            ABIAds.ShowBanner(bannerPlacement, "bottom");
            Log($"Show banner: {bannerPlacement}");
        }

        private void OnHideBanner()
        {
            ABIAds.HideBanner();
            Log("Banner hidden");
        }

        private void OnLoadInterstitial()
        {
            ABIAds.Load(interstitialPlacement);
            Log($"Load interstitial: {interstitialPlacement}");
        }

        private void OnShowInterstitial()
        {
            ABIAds.Show(interstitialPlacement);
            Log($"Show interstitial: {interstitialPlacement}");
        }

        private void OnLoadRewarded()
        {
            ABIAds.LoadRewarded(rewardedPlacement);
            Log($"Load rewarded: {rewardedPlacement}");
        }

        private void OnShowRewarded()
        {
            ABIAds.ShowRewarded(rewardedPlacement);
            Log($"Show rewarded: {rewardedPlacement}");
        }

        private void OnLoadAppOpen()
        {
            ABIAds.Load(appOpenPlacement);
            Log($"Load app open: {appOpenPlacement}");
        }

        private void OnShowAppOpen()
        {
            ABIAds.Show(appOpenPlacement);
            Log($"Show app open: {appOpenPlacement}");
        }

        private void OnLoadNative()
        {
            ABIAds.Load(nativePlacement);
            Log($"Load native: {nativePlacement}");
        }

        private void OnShowNative()
        {
            ABIAds.ShowNative(
                nativePlacement,
                nativeTemplateName == null || string.IsNullOrWhiteSpace(nativeTemplateName.text) ? null : nativeTemplateName.text,
                NativeSize.Medium,
                NativePosition.Bottom);
            Log($"Show native: {nativePlacement}");
        }

        private void OnLoadNativeFullscreen()
        {
            ABIAds.Load(nativeFullscreenPlacement);
            Log($"Load native fullscreen: {nativeFullscreenPlacement}");
        }

        private void OnShowNativeFullscreen()
        {
            ABIAds.ShowNativeFullScreen(nativeFullscreenPlacement, 5, nativeFullscreenTemplateName == null || string.IsNullOrWhiteSpace(nativeFullscreenTemplateName.text) ? null : nativeFullscreenTemplateName.text);
            Log($"Show native fullscreen: {nativeFullscreenPlacement}");
        }

        private void OnFetchRemoteConfig()
        {
            if (FirebaseManager.Instance == null) return;

            FirebaseManager.Instance.FetchRemoteConfig((success) =>
            {
                if (success)
                {
                    long interval = FirebaseManager.Instance.GetRemoteConfigLong("inter_ad_interval");
                    bool showBanner = FirebaseManager.Instance.GetRemoteConfigBool("show_banner");
                    Log($"Remote Config: interval={interval}, banner={showBanner}");
                }
                else
                {
                    Log("Remote Config fetch failed");
                }
            });
        }

        private void OnLogAppsFlyerEvent()
        {
            if (AppsFlyerManager.Instance == null) return;

            AppsFlyerManager.Instance.LogLevelComplete("demo_level_1", "100");
            Log("AppsFlyer event logged: level_complete");
        }

        private void Log(string message)
        {
            ABILibsSDKConfig.DebugLog($"[Demo] {message}");
            if (txtStatus != null)
                txtStatus.text = message;
        }

        private void OnABIAdsEvent(ABIAdsEvent adsEvent)
        {
            if (adsEvent == null)
            {
                return;
            }

            Log($"ABI Ads event: {adsEvent.eventName}");
        }

        private void OnABIAdsInitialized(ABIAdsEvent adsEvent)
        {
            Log("ABI Ads initialized");
        }
    }
}
