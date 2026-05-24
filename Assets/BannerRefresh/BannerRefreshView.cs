//
//  Copyright 2025 Google LLC
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using GoogleMobileAds.Api;
using System.Linq;
using System.Text;

[assembly:System.Runtime.CompilerServices.InternalsVisibleTo("GoogleMobileAds.BannerRefresh.EditmodeTests")]
[assembly:System.Runtime.CompilerServices.InternalsVisibleTo("GoogleMobileAds.BannerRefresh.PlaymodeTests")]
namespace GoogleMobileAds.BannerRefresh
{
    /// <summary>
    /// A Google Mobile Ads Banner View with a
    /// configurable refresh rate which may be set per buyer.
    /// </summary>
    /// <remarks>
    /// Note: This component disables automatic refresh for your ad unit.
    /// </remarks>
    [Serializable]
    public class BannerRefreshView
    {
        internal const int BACKOFF_RATE_MIN = 1;
        internal const string VERSION = "1.3.0";

        // Telemetry Keys.
        internal const string EXTRA_ADS_BUFFER_SIZE = "brb";
        internal const string EXTRA_DEFAULT_REFRESH_RATE = "brrd";
        internal const string EXTRA_REFRESH_RATE_OVERRIDES = "brro";
        internal const string EXTRA_PREVIOUS_IDLE_TIME = "brpi";
        internal const string EXTRA_PREVIOUS_REFRESH_RATE = "brrp";
        internal const string EXTRA_PREVIOUS_AD_SOURCE = "brpas";

        /// <summary>
        /// A flag to enable or disable logging. Defaults to `true`.
        /// </summary>
        public static bool IsDebugLoggingEnabled { get; set; } = true;

        /// <summary>
        /// Weither this BannerRefreshView released its resources.
        /// </summary>
        public bool IsDestroyed { get; private set; }

        /// <summary>
        /// Raised when a banner loads.
        /// </summary>
        public event Action OnBannerAdLoaded = delegate { };

        /// <summary>
        /// Raised when a banner fails to load.
        /// </summary>
        public event Action<LoadAdError> OnBannerAdLoadFailed = delegate { };

        /// <summary>
        /// Raised when the ad registers an impression and attributes an ad value.
        /// </summary>
        public event Action<AdValue> OnAdPaid = delegate { };

        /// <summary>
        /// Raised when a user clicks on the ad.
        /// </summary>
        public event Action OnAdClicked = delegate { };

        /// <summary>
        /// Raised when an impression is recorded for the ad.
        /// </summary>
        public event Action OnAdImpressionRecorded = delegate { };

        /// <summary>
        /// Raised when a full-screen content is opened from the ad.
        /// </summary>
        public event Action OnAdFullScreenContentOpened = delegate { };

        /// <summary>
        /// Raised when a full-screen content is closed from the ad.
        /// </summary>
        public event Action OnAdFullScreenContentClosed = delegate { };

        private string _adUnitId;
        private AdSize _adSize;
        private AdPosition? _adPosition;
        private Vector2Int? _adPositionXY;
        private CancellationTokenSource _failedToken;
        private CancellationTokenSource _rotateToken;
        internal BannerRefreshConfiguration _configuration;
        internal List<BannerView> _bannersAll = new List<BannerView>();
        internal Queue<BannerView> _bannersLoaded = new Queue<BannerView>();
        internal Queue<BannerView> _bannersFailed = new Queue<BannerView>();
        internal BannerView _currentBanner;
        internal bool _currentBannerExpired;
        internal bool _isFailedBannerLoading;
        internal AdRequest _adRequest;
        internal int _backoffTime = BACKOFF_RATE_MIN;
        internal bool _isBannerVisible = true;
        internal bool _loadAdCalled = false;
        internal bool _isTimerRunning = false;

        // Telemetry.
        internal BannerRefreshTimer _bannerRefreshTimer = new BannerRefreshTimer();

        // Events for testing.
        internal event Action<BannerView, AdRequest> _onBannerTelemetryInitialLoad = delegate { };
        internal event Action<BannerView, AdRequest> _onBannerTelemetryRotationLoad = delegate { };
        internal event Action _onBannerRotated = delegate { };

        /// <summary>
        /// Initializes a new instance of the <see cref="BannerRefreshView"/>
        /// class with specified parameters.
        /// </summary>
        /// <param name="adUnitId">The AdMob ad unit ID.</param>
        /// <param name="adSize">The size of the banner ad.</param>
        /// <param name="position">The position of the banner ad on the screen.</param>
        /// <param name="configuration">Optional configuration for the refresh banner.</param>
        public BannerRefreshView(
            string adUnitId,
            AdSize adSize,
            AdPosition position,
            BannerRefreshConfiguration configuration = null)
        {
            _adSize = adSize;
            _adPosition = position;
            Init(adUnitId, configuration);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BannerRefreshView"/>
        /// class with specified parameters.
        /// </summary>
        /// <param name="adUnitId">The AdMob ad unit ID.</param>
        /// <param name="adSize">The size of the banner ad.</param>
        /// <param name="x">The x-coordinate of the banner ad's top-left corner.</param>
        /// <param name="y">The y-coordinate of the banner ad's top-left corner.</param>
        /// <param name="configuration">Optional configuration for the refresh banner.</param>
        public BannerRefreshView(
            string adUnitId,
            AdSize adSize,
            int x,
            int y,
            BannerRefreshConfiguration configuration = null)
        {
            _adSize = adSize;
            _adPositionXY = new Vector2Int(x, y);
            Init(adUnitId, configuration);
        }

        /// <summary>
        /// Destroys the banner view, releasing all resources.
        /// </summary>
        public void Destroy()
        {
            IsDestroyed = true;
            if (_failedToken != null)
            {
                _failedToken.Cancel();
            }
            if (_rotateToken != null)
            {
                _rotateToken.Cancel();
            }
            foreach (var banner in _bannersAll)
            {
                banner.Destroy();
            }
            _bannersAll.Clear();
            _bannersLoaded.Clear();
            _bannersFailed.Clear();
            _currentBanner = null;
        }

        /// <summary>
        /// Gets the AdMob ad unit ID associated with this banner view.
        /// </summary>
        /// <returns>The AdMob ad unit ID.</returns>
        public string GetAdUnitID()
        {
            return _currentBanner?.GetAdUnitID();
        }

        /// <summary>
        /// Gets the response information for the last ad request.
        /// </summary>
        /// <returns>The response information.</returns>
        public ResponseInfo GetResponseInfo()
        {
            return _currentBanner?.GetResponseInfo();
        }

        /// <summary>
        /// Gets the height of the banner view in pixels.
        /// </summary>
        /// <returns>The height in pixels.</returns>
        public float GetHeightInPixels()
        {
            return _currentBanner?.GetHeightInPixels() ?? 0f;
        }

        /// <summary>
        /// Gets the width of the banner view in pixels.
        /// </summary>
        /// <returns>The width in pixels.</returns>
        public float GetWidthInPixels()
        {
            return _currentBanner?.GetWidthInPixels() ?? 0f;
        }

        /// <summary>
        /// Loads an ad request into the banner view.
        /// </summary>
        /// <param name="request">The ad request to load.</param>
        public void LoadAd(AdRequest request)
        {
            Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                _adRequest = request == null ? null : new AdRequest(request);

                // Add Extras.
                if (_adRequest != null)
                {
                    _adRequest.Extras.Add("_noRefresh", "1");
                    _adRequest.Extras.Add("google-banner-refresh", VERSION);
                    _adRequest.CustomTargeting =
                         new Dictionary<string, string>(request.CustomTargeting);
                }

                // Check if we should initialize banner loading.
                if (!_loadAdCalled)
                {
                    _loadAdCalled = true;

                    // Since the buffer size grows when we show the first banner,
                    // we must ToList() to prevent crashes from modifing the list
                    // from within the list.
                    var telemetryExtras = GetTelemetryExtras(_configuration, 0, null);
                    var adRequest = GetTelemetryAdRequest(_adRequest, telemetryExtras);
                    foreach (var banner in _bannersAll.ToList())
                    {
                        LoadAd(banner, adRequest);
                    }
                }
            });
        }

        private void LoadAd(BannerView banner, AdRequest request)
        {
            DebugLog($"LoadAd");
            banner.LoadAd(request);
        }

        /// <summary>
        /// Shows the banner view.
        /// </summary>
        public void Show()
        {
            Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (_isBannerVisible)
                {
                    return;
                }
                _isBannerVisible = true;
                if (_currentBanner != null)
                {
                    _currentBanner.Show();
                    // Start the rotation timer if it is not already running.
                    TryStartRotateTimer();
                }
            });
        }

        /// <summary>
        /// Hides the banner view.
        /// </summary>
        public void Hide()
        {
            _isBannerVisible = false;
            Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (_currentBanner != null)
                {
                    _currentBanner.Hide();
                }
            });
        }

        /// <summary>
        /// Sets the position of the banner view using an <see cref="AdPosition"/> enum.
        /// </summary>
        /// <param name="adPosition">The new position of the banner view.</param>
        public void SetPosition(AdPosition adPosition)
        {
            foreach (var banner in _bannersAll)
            {
                banner.SetPosition(adPosition);
            }
            _adPosition = adPosition;
            _adPositionXY = null;
        }

        /// <summary>
        /// Sets the position of the banner view using explicit x and y coordinates.
        /// </summary>
        /// <param name="x">The x-coordinate of the banner view's top-left corner.</param>
        /// <param name="y">The y-coordinate of the banner view's top-left corner.</param>
        public void SetPosition(int x, int y)
        {
            foreach (var banner in _bannersAll)
            {
                banner.SetPosition(x, y);
            }
            _adPosition = null;
            _adPositionXY = new Vector2Int(x, y);
        }

        /// <summary>
        /// Helper for callback termination check.
        /// </summary>
        private bool IsDestroyedOrStopped()
        {
            return IsDestroyed || !Application.isPlaying;
        }

        /// <summary>
        /// Helper for code shared by the two constructors.
        /// </summary>
        private void Init(string adUnitId, BannerRefreshConfiguration configuration)
        {
            _adUnitId = adUnitId;
            _configuration = configuration == null
                ? new BannerRefreshConfiguration()
                : new BannerRefreshConfiguration(configuration);
            _backoffTime = BACKOFF_RATE_MIN;
            _rotateToken = new CancellationTokenSource();
            _failedToken = new CancellationTokenSource();

            // If ad sources includes ADMOB_NETWORK, add entry for ADMOB_NETWORK_WATERFALL.
            if (_configuration.AdSourceRefreshRatesInSeconds.ContainsKey(AdSource.ADMOB_NETWORK))
            {
                _configuration.AdSourceRefreshRatesInSeconds.Add(
                    AdSource.ADMOB_NETWORK_WATERFALL,
                    _configuration.AdSourceRefreshRatesInSeconds[AdSource.ADMOB_NETWORK]);
            }

            if (IsDebugLoggingEnabled)
            {
                var sb = new StringBuilder();
                sb.Append($"Start: Ads buffer size: {_configuration.AdsBufferSize}, ");
                sb.Append($"default refresh rate {_configuration.DefaultRefreshRateInSeconds}, ");
                var refreshOverridesLog = string.Join(", ",
                    _configuration.AdSourceRefreshRatesInSeconds.Select(c => $"{c.Key}={c.Value}")
                );
                sb.Append($"refresh overrides({refreshOverridesLog}).");
                DebugLog(sb.ToString());
            }

            Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                // Preload the buffer.
                for (int i = 0; i < _configuration.AdsBufferSize; i++)
                {
                    CreateBanner();
                }
            });
        }

        private BannerView CreateBanner()
        {
            var banner = _adPosition.HasValue
                ? new BannerView(_adUnitId, _adSize, _adPosition.Value)
                : new BannerView(_adUnitId, _adSize, _adPositionXY.Value.x, _adPositionXY.Value.y);

            // Make sure the default visual state of the banner is hidden.
            banner.Hide();

            // Enqueue the banner for future loading.
            _bannersAll.Add(banner);

            // Plugin callbacks.
            banner.OnBannerAdLoaded +=
                () => HandleBannerLoaded(banner);
            banner.OnBannerAdLoadFailed += 
                (LoadAdError error) => HandleBannerAdLoadFailed(banner, error);

            return banner;
        }

        private void HandleBannerLoaded(BannerView banner)
        {
            Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (IsDestroyedOrStopped())
                {
                    return;
                }

                if (IsDebugLoggingEnabled)
                {
                    var responseInfo = banner?.GetResponseInfo();
                    var adapterInfo = responseInfo?.GetLoadedAdapterResponseInfo();
                    var adSourceId = adapterInfo?.AdSourceId;
                    var adSourceName = adapterInfo?.AdSourceName;
                    DebugLog($"LoadAdSuccess: Ad source name {adSourceName}, " +
                             $"Ad source ID {adSourceId}, " +
                             $"Banners in cache: {_bannersLoaded.Count}.");
                }

                // Reset failed banner loading with a minimal backoff rate on success load.
                _backoffTime = BACKOFF_RATE_MIN;
                TryLoadFailedBanner();

                // Mark banner as ready to show.
                _bannersLoaded.Enqueue(banner);

                // If no banner or expired banner, show one.
                if (_currentBanner == null || _currentBannerExpired)
                {
                    TryRotateBanner();
                }

                OnBannerAdLoaded();
            });
        }

        private void HandleBannerAdLoadFailed(BannerView banner, LoadAdError error)
        {
            Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (IsDestroyedOrStopped())
                {
                    return;
                }

                _isFailedBannerLoading = false;

                // Mark banner as failed.
                _bannersFailed.Enqueue(banner);

                // Increment the backoff rate.
                _backoffTime = Mathf.Clamp(_backoffTime * 2,
                    BACKOFF_RATE_MIN,
                    _configuration.DefaultRefreshRateInSeconds);

                DebugLog($"LoadAdFailure: {_backoffTime}s until next retry."
                    + $" Error message: {error.GetMessage()}");
                TryLoadFailedBanner();

                // Forward AdLoadError to the API.
                OnBannerAdLoadFailed(error);
            });
        }

        private void TryRotateBanner()
        {
            if (_bannersLoaded.Count == 0)
            {
                _currentBannerExpired = true;

                // Start tracking idle time.
                _bannerRefreshTimer.StartIdleTime();
                DebugLog($"RotateFailure: Starting idle timer.");
                return;
            }

            // Finalize the idle time from the previous period.
            var idleTime = _bannerRefreshTimer.FinalizeIdleTime();

            // Switch banners.
            var nextBanner = _bannersLoaded.Dequeue();
            var lastBanner = _currentBanner;
            _currentBanner = nextBanner;
            _currentBannerExpired = false;

            // Reset last banner.
            if (lastBanner != null)
            {
                lastBanner.OnAdClicked -= OnAdClicked;
                lastBanner.OnAdFullScreenContentClosed -= OnAdFullScreenContentClosed;
                lastBanner.OnAdFullScreenContentOpened -= OnAdFullScreenContentOpened;
                lastBanner.OnAdImpressionRecorded -= HandleAdImpression;
                lastBanner.OnAdPaid -= OnAdPaid;

                lastBanner.Hide();

                // Record the ad request details;
                var telemetryExtras = GetTelemetryExtras(
                    _configuration,
                    idleTime,
                    lastBanner);

                // Update the ad request.
                var adRequest = GetTelemetryAdRequest(_adRequest, telemetryExtras);
                _onBannerTelemetryRotationLoad(lastBanner, adRequest);
                LoadAd(lastBanner, adRequest);
            }
            else
            {
                // Load 1 extra to fill buffer slot which is visible.
                var banner = CreateBanner();
                var telemetryExtras = GetTelemetryExtras(_configuration, 0, null);
                var adRequest = GetTelemetryAdRequest(_adRequest, telemetryExtras);
                _onBannerTelemetryInitialLoad(banner, adRequest);
                LoadAd(banner, adRequest);
            }

            // Prepare the next banner.
            nextBanner.OnAdClicked += OnAdClicked;
            nextBanner.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
            nextBanner.OnAdFullScreenContentOpened += OnAdFullScreenContentOpened;
            nextBanner.OnAdImpressionRecorded += HandleAdImpression;
            nextBanner.OnAdPaid += OnAdPaid;

            // Show the next banner if the refresh view is visible.
            if (_isBannerVisible)
            {
                nextBanner.Show();
                TryStartRotateTimer();
                _onBannerRotated();
            }
            DebugLog($"RotateSuccess: Idle time {idleTime}, Banners in cache {_bannersAll.Count}.");
        }

        private async void TryStartRotateTimer()
        {
            try
            {
                if (_currentBanner == null)
                {
                    return;
                }
                if (_isTimerRunning)
                {
                    return;
                }

                _isTimerRunning = true;
                string adSourceId = null;

                var responseInfo = _currentBanner.GetResponseInfo();
                if (responseInfo != null)
                {
                    var adapterResponseInfo = responseInfo.GetLoadedAdapterResponseInfo();
                    if (adapterResponseInfo != null)
                    {
                        try
                        {
                            adSourceId = adapterResponseInfo.AdSourceId;
                        }
                        catch (Exception ex)
                        {
                            DebugErrorLog($"Unable to get previous ad source ID: {ex}");
                        }
                    }
                }

                // Get the refresh rate for the ad source.
                var refreshRate = _configuration.GetRefreshRateInSecondsForAdSourceId(adSourceId);
                await Task.Delay(TimeSpan.FromSeconds(refreshRate), _rotateToken.Token);

                if (IsDestroyedOrStopped())
                {
                    return;
                }
                _isTimerRunning = false;
                TryRotateBanner();
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                DebugErrorLog($"TryStartRotateTimer failed: {ex}");
            }
        }

        private async void TryLoadFailedBanner()
        {
            try
            {
                // Only load previously-failed banners one at a time.
                if (_isFailedBannerLoading || _bannersFailed.Count == 0)
                {
                    return;
                }

                _isFailedBannerLoading = true;

                var banner = _bannersFailed.Dequeue();

                await Task.Delay(TimeSpan.FromSeconds(_backoffTime), _failedToken.Token);
                if (IsDestroyedOrStopped())
                {
                    return;
                }
                var telemetryExtras = GetTelemetryExtras(_configuration, 0, null);
                var adRequest = GetTelemetryAdRequest(_adRequest, telemetryExtras);
                LoadAd(banner, adRequest);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                DebugErrorLog($"TryLoadFailedBanner failed: {ex}");
            }
        }

        private void HandleAdImpression()
        {
            if (IsDebugLoggingEnabled)
            {
                var adSourceId = _currentBanner?.GetResponseInfo()?
                                     .GetLoadedAdapterResponseInfo()?.AdSourceId;
                var adSourceName = _currentBanner?.GetResponseInfo()?
                                       .GetLoadedAdapterResponseInfo()?.AdSourceName;
                var refreshRate =
                    _configuration.GetRefreshRateInSecondsForAdSourceId(adSourceId);
                var isOverride = _configuration.AdSourceRefreshRatesInSeconds.ContainsKey(adSourceId);
                var refreshRateSource = isOverride ? "Override" : "Default";
                DebugLog($"AdVisible: Ad source name {adSourceName}, Ad source ID {adSourceId}, " +
                         $"Banners in cache: {_bannersLoaded.Count}, Refresh rate {refreshRate}, " +
                         $"Refresh rate source {refreshRateSource}.");
            }

            // Relay the ad impression to the listeners of the plugin.
            OnAdImpressionRecorded();
        }

        private void DebugLog(string message)
        {
            if (!IsDebugLoggingEnabled)
            {
                return;
            }

            Debug.Log($"GoogleBannerRefresh [ID: {this.GetHashCode()}] {message}");
        }

        private void DebugErrorLog(string message)
        {
            Debug.LogError($"GoogleBannerRefresh Error [ID: {this.GetHashCode()}] {message}");
        }

        #region TELEMETRY

        internal static AdRequest GetTelemetryAdRequest(AdRequest adRequest,
                Dictionary<string, string> extras)
        {
            if (adRequest == null || extras == null)
            {
                return adRequest;
            }

            var adRequestForReload = new AdRequest(adRequest);
            if (adRequest.Extras == null)
            {
                adRequestForReload.Extras = new Dictionary<string, string>();
            }

            foreach (var extra in extras)
            {
                adRequestForReload.Extras[extra.Key] = extra.Value;
            }
            return adRequestForReload;
        }

        internal static void PrintTelemetryLog(Dictionary<string, string> telemetryExtras)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("BannerRefreshTelemetry Log:");
            foreach (var extra in telemetryExtras)
            {
                stringBuilder.AppendLine($" {extra.Key}: {extra.Value}");
            }
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null,
                stringBuilder.ToString());
        }

        internal static Dictionary<string, string> GetTelemetryExtras(
            BannerRefreshConfiguration configuration,
            int previousIdleTime,
            BannerView lastBanner)
        {
            var extras = new Dictionary<string, string>();
            extras[EXTRA_ADS_BUFFER_SIZE] = configuration.AdsBufferSize.ToString();
            extras[EXTRA_DEFAULT_REFRESH_RATE] = configuration.DefaultRefreshRateInSeconds.ToString();
            extras[EXTRA_REFRESH_RATE_OVERRIDES] = ToQueryString(configuration.AdSourceRefreshRatesInSeconds);
            if (lastBanner != null && lastBanner.GetResponseInfo() != null)
            {
                extras[EXTRA_PREVIOUS_IDLE_TIME] = previousIdleTime.ToString();
                try
                {
                    var lastResponseInfo = lastBanner.GetResponseInfo();
                    var lastAdSourceId =
                        lastResponseInfo?.GetLoadedAdapterResponseInfo()?.AdSourceId;
                    var lastRefreshRate =
                         configuration.GetRefreshRateInSecondsForAdSourceId(lastAdSourceId);
                    var lastRefreshRateInSeconds = Mathf.RoundToInt(lastRefreshRate);

                    extras[EXTRA_PREVIOUS_REFRESH_RATE] = lastRefreshRateInSeconds.ToString();
                    extras[EXTRA_PREVIOUS_AD_SOURCE] = lastAdSourceId;
                }
                catch (Exception ex)
                {
                    Debug.LogError(
                        $"GoogleBannerRefresh Exception: Unable to get previous ad source ID: {ex}");
                }
            }
            return extras;
        }

        internal static string ToQueryString(Dictionary<string, int> dict)
        {
            if (dict == null || dict.Count == 0)
            {
                return string.Empty;
            }
            var pairs = dict.Select(kv => $"{kv.Key}={kv.Value}");
            return string.Join("&", pairs);
        }
        #endregion
    }
}
