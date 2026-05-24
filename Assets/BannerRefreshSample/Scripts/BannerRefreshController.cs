using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.BannerRefresh;

namespace GoogleMobileAds.Samples
{
    /// <summary>
    /// Demonstrates how to use the BannerRefreshView.
    /// </summary>
    [AddComponentMenu("GoogleMobileAds/Samples/BanerRefreshController")]
    public class BannerRefreshController : MonoBehaviour
    {
        // These ad units are configured to always serve test ads.
#if UNITY_ANDROID
        private const string _adUnitId = "ca-app-pub-3940256099942544/9214589741";
#elif UNITY_IPHONE
        private const string _adUnitId = "ca-app-pub-3940256099942544/2934735716";
#else
        private const string _adUnitId = "unused";
#endif

        private BannerRefreshView _bannerRefreshView;

        private void Start()
        {
            // Add your own test device ids.
            // https://developers.google.com/admob/unity/test-ads
            var requestConfiguration = new RequestConfiguration();
            requestConfiguration.TestDeviceIds.Add(AdRequest.TestDeviceSimulator);
#if UNITY_IPHONE
            requestConfiguration.TestDeviceIds.Add("96e23e80653bb28980d3f40beb58915c");
#elif UNITY_ANDROID
            requestConfiguration.TestDeviceIds.Add("702815ACFC14FF222DA1DC767672A573");
#endif
            MobileAds.SetRequestConfiguration(requestConfiguration);

            // Initialize Google Mobile Ads.
            MobileAds.Initialize(initStatus => StartBannerRefresh());
        }

        // [START load_banner]
        private void StartBannerRefresh()
        {
            // [Optional] Define banner refresh configuration.
            var config = new BannerRefreshConfiguration
            {
                DefaultRefreshRateInSeconds = 60,
                AdsBufferSize = 4,
                AdSourceRefreshRatesInSeconds = new ()
                {
                    {AdSource.ADMOB_NETWORK, 30},
                }
            };

            // Define the ad size and position for our banner.
            _bannerRefreshView = new BannerRefreshView(_adUnitId, AdSize.Banner, AdPosition.Center, config);

            // [Optional] listen to ad events.
            _bannerRefreshView.OnBannerAdLoaded += () => {
                Debug.Log($"Banner loaded.");
            };
            _bannerRefreshView.OnBannerAdLoadFailed += (error) =>
            {
                Debug.Log($"Banner failed to load : {error}.");
            };
            _bannerRefreshView.OnAdClicked += () =>
            {
                Debug.Log($"Banner was clicked.");
            };
            _bannerRefreshView.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log($"Banner full screen content was closed.");
            };
            _bannerRefreshView.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log($"Banner full screen content was opened.");
            };
            _bannerRefreshView.OnAdImpressionRecorded += () =>
            {
                Debug.Log($"Banner had an impression.");
            };
            _bannerRefreshView.OnAdPaid += (value) =>
            {
                Debug.Log($"Banner was paid : {value.CurrencyCode} {value.Value}.");
            };

            // Create the request used to load the ad.
            var adRequest = new AdRequest();

            // Load the ads. This will preload several ads in the background,
            // enabling a quick and seamless banner refresh view experience.
            _bannerRefreshView.LoadAd(adRequest);
        }
        // [END load_banner]

        // [START destroy_banner]
        protected void OnDestroy()
        {
            // [Important] Release the banner once you are done using it.
            _bannerRefreshView.Destroy();
        }
        // [END destroy_banner]

        [ContextMenu("Show")]
        public void Show()
        {
            _bannerRefreshView.Show();
        }

        [ContextMenu("Hide")]
        public void Hide()
        {
            _bannerRefreshView.Hide();
        }

        [ContextMenu("LoadError")]
        public void LoadError()
        {
            _bannerRefreshView.LoadAd(null);
        }

        [ContextMenu("Load")]
        public void Load()
        {
            _bannerRefreshView.LoadAd(new AdRequest());
        }
    }
}
