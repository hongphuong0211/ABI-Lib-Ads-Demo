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

using System.Collections;
using GoogleMobileAds.BannerRefresh;
using NUnit.Framework;
using UnityEngine;
using GoogleMobileAds.Api;
using UnityEngine.TestTools;
using System.Collections.Generic;

public class BannerRefreshViewTests
{
#if UNITY_ANDROID
    private const string _adUnitId = "ca-app-pub-3212738706492790/6862512005";
#elif UNITY_IPHONE
    private const string _adUnitId = "ca-app-pub-3212738706492790/7336853402";
#else
    private const string _adUnitId = "unused";
#endif

    private BannerRefreshView _refreshView;
    private BannerView _bannerView;
    private BannerRefreshConfiguration _refreshConfig;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        GoogleMobileAds.Common.MobileAdsEventExecutor.Initialize();

        _refreshConfig = new BannerRefreshConfiguration
        {
            DefaultRefreshRateInSeconds = BannerRefreshConfiguration.REFRESH_RATE_MIN,
            AdsBufferSize = 1,
        };
        _refreshView = new BannerRefreshView(_adUnitId,
            AdSize.Banner,
            AdPosition.Center,
            _refreshConfig);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (_bannerView != null)
        {
            _bannerView.Destroy();
            _bannerView = null;
        }
        if (_refreshView != null)
        {
            _refreshView.Destroy();
            _refreshView = null;
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator NoRefresh()
    {
        var view = new BannerRefreshView("TEST", AdSize.Banner, AdPosition.Center);
        view.LoadAd(new AdRequest());
        yield return null;
        Assert.IsTrue(view._adRequest.Extras.ContainsKey("_noRefresh"));
        Assert.IsTrue(view._adRequest.Extras["_noRefresh"] == "1");
    }

    [UnityTest]
    public IEnumerator BannerRefreshVersion()
    {
        var view = new BannerRefreshView("TEST", AdSize.Banner, AdPosition.Center);
        view.LoadAd(new AdRequest());
        yield return null;
        Assert.IsTrue(view._adRequest.Extras.ContainsKey("google-banner-refresh"));
    }

    [UnityTest]
    public IEnumerator InitialPreloadBuffer()
    {
        // Let the initial banners attempt to load.
        // The actual load success depends on the mock/real SDK.
        yield return null;
        Assert.AreEqual(_refreshView._bannersAll.Count, _refreshConfig.AdsBufferSize);
    }

    [UnityTest]
    public IEnumerator InitialShow()
    {
        // Let the initial banners attempt to load.
        // The actual load success depends on the mock/real SDK.
        yield return null;
        _refreshView.LoadAd(new AdRequest());
        yield return null;
        yield return null;

        // Assert that a banner is shown.
        Assert.IsNotNull(_refreshView._currentBanner);
        // Assert that another banner is loaded into the buffer.
        Assert.AreEqual(_refreshView._bannersAll.Count, _refreshConfig.AdsBufferSize + 1);
    }

    [UnityTest]
    public IEnumerator TestAdSourceRefreshRateIsApplied()
    {
        // Destroy the default banner.
        yield return TearDown();

        // Tests execute in the Unity Editor, which return "Placeholder AdSourceId" for
        // the banner's ad source ID. For more information, see
        // https://github.com/googleads/googleads-mobile-unity/blob/main/source/plugin/Assets/GoogleMobileAds/Platforms/Unity/AdapterResponseInfoClient.cs#L29
        _refreshConfig = new BannerRefreshConfiguration
        {
            DefaultRefreshRateInSeconds = 30,
            AdSourceRefreshRatesInSeconds = new System.Collections.Generic.Dictionary<string, int>
            {
                { "Placeholder AdSourceId", 5 }
            },
            AdsBufferSize = 1,
        };
        _refreshView = new BannerRefreshView(_adUnitId,
            AdSize.Banner,
            AdPosition.Center,
            _refreshConfig);
        var rotations = 0;
        _refreshView._onBannerRotated += () =>
        {
            rotations++;
        };
        yield return null;
        _refreshView.LoadAd(new AdRequest());
        yield return null;
        yield return null;

        var firstBanner = _refreshView._currentBanner;
        Assert.IsNotNull(firstBanner);
        Assert.AreEqual(rotations, 1);
        // Wait for a second refresh cycle.
        yield return new WaitForSeconds(5 + 1);
        var secondBanner = _refreshView._currentBanner;
        Assert.IsNotNull(secondBanner);
        Assert.IsTrue(secondBanner != firstBanner);
        Assert.AreEqual(rotations, 2);
    }

    [UnityTest]
    public IEnumerator BannerRotate()
    {
        var rotations = 0;
        _refreshView._onBannerRotated += () =>
        {
            rotations++;
        };

        // Let the initial banners attempt to load.
        // The actual load success depends on the mock/real SDK.
        yield return null;
        _refreshView.LoadAd(new AdRequest());
        yield return null;
        yield return null;

        // Assert that a banner is shown.
        var firstBanner = _refreshView._currentBanner;
        Assert.IsNotNull(firstBanner);
        Assert.IsTrue(rotations == 1);

        yield return new WaitForSeconds(_refreshConfig.DefaultRefreshRateInSeconds + 1);
        yield return null;
        yield return null;

        // Assert that a new banner is shown.
        var secondBanner = _refreshView._currentBanner;
        Assert.IsNotNull(secondBanner);
        Assert.IsTrue(secondBanner != firstBanner);
        Assert.IsTrue(rotations == 2);

        yield return new WaitForSeconds(_refreshConfig.DefaultRefreshRateInSeconds + 1);
        yield return null;
        yield return null;

        // Assert that a new banner is shown.
        var thirdBanner = _refreshView._currentBanner;
        Assert.IsNotNull(thirdBanner);
        Assert.IsTrue(thirdBanner != secondBanner);
        Assert.IsTrue(rotations == 3);
    }

    [UnityTest]
    public IEnumerator HideNoRotate()
    {
        // Hide the banner.
        _refreshView.Hide();
        yield return null;
        yield return null;

        // Load a banner while hidden.
        _refreshView.LoadAd(new AdRequest());
        yield return null;
        yield return null;

        // Assert that a banner is loaded.
        Assert.IsNotNull(_refreshView._currentBanner);
        var firstBanner = _refreshView._currentBanner;
        yield return null;
        yield return null;

        // Wait for a refresh cycle.
        yield return new WaitForSeconds(_refreshConfig.DefaultRefreshRateInSeconds + 1);
        yield return null;

        // Assert that a banner does not rotate.
        Assert.IsNotNull(_refreshView._currentBanner);
        Assert.IsTrue(_refreshView._currentBanner == firstBanner);

    }

    [UnityTest]
    public IEnumerator HideCallsOnAdLoad()
    {
        _refreshView.Destroy();
        yield return null;

        _refreshConfig = new BannerRefreshConfiguration
        {
            DefaultRefreshRateInSeconds = BannerRefreshConfiguration.REFRESH_RATE_MIN,
            AdsBufferSize = 2,
        };
        _refreshView = new BannerRefreshView(_adUnitId,
            AdSize.Banner,
            AdPosition.Center,
            _refreshConfig);

        // Hide the banner.
        _refreshView.Hide();

        // Load a banner while hidden.
        int loadCounter = 0;
        _refreshView.OnBannerAdLoaded += () => { loadCounter++; };

        // Load a banner while hidden.
        _refreshView.LoadAd(new AdRequest());
        yield return null;
        yield return null;
        // Assert that cache banners are loaded.
        Assert.AreEqual(2, loadCounter);
        yield return null;
        yield return null;
        // Assert that cache + 1 banners are loaded after first load.
        Assert.AreEqual(3, loadCounter);
    }

    [UnityTest]
    public IEnumerator FailedToLoadBackoff()
    {
        var expectedBackOffTime = new int[] { 1, 2, 4, 5, 5 };

        int failedToLoadCounter = 0;
        _refreshView.OnBannerAdLoadFailed += (error) =>
        {
            failedToLoadCounter++;
        };

        // LoadAd with a invalid AdRequest.
        _refreshView.LoadAd(null);
        yield return null;
        yield return null;

        // Assert that no banner is shown.
        Assert.IsNull(_refreshView._currentBanner);

        // Assert that we attempt to reload the banner 3 times,
        // each time with an increasing backoff time.
        Assert.AreEqual(failedToLoadCounter, 1);
        Assert.IsTrue(_refreshView._backoffTime == expectedBackOffTime[1]);
        Assert.IsTrue(_refreshView._isFailedBannerLoading);
        yield return new WaitForSeconds(_refreshView._backoffTime);
        yield return null;
        yield return null;

        Assert.AreEqual(failedToLoadCounter, 2);
        Assert.IsTrue(_refreshView._backoffTime == expectedBackOffTime[2]);
        Assert.IsTrue(_refreshView._isFailedBannerLoading);
        yield return new WaitForSeconds(_refreshView._backoffTime);
        yield return null;
        yield return null;

        Assert.AreEqual(failedToLoadCounter, 3);
        Assert.IsTrue(_refreshView._backoffTime == expectedBackOffTime[3]);
        Assert.IsTrue(_refreshView._isFailedBannerLoading);
        yield return new WaitForSeconds(_refreshView._backoffTime);
        yield return null;
        yield return null;

        Assert.AreEqual(failedToLoadCounter, 4);
        Assert.IsTrue(_refreshView._backoffTime == expectedBackOffTime[4]);
        Assert.IsTrue(_refreshView._isFailedBannerLoading);
    }

    [UnityTest]
    public IEnumerator FailedToLoadBackoffReset()
    {
        // LoadAd with a invalid AdRequest.
        _refreshView.LoadAd(null);
        yield return null;
        yield return null;

        // Assert that no banner is shown.
        Assert.IsNull(_refreshView._currentBanner);
        Assert.IsFalse(_refreshView._backoffTime == BannerRefreshView.BACKOFF_RATE_MIN);

        // Apply a valid ad request and wait for the refresh to complete.
        _refreshView.LoadAd(new AdRequest());

        yield return new WaitForSeconds(_refreshView._backoffTime);
        yield return null;
        yield return null;

        // Assert that the banner is loaded.
        Assert.IsNotNull(_refreshView._currentBanner);

        // Assert that backoff time is reset.
        Assert.IsTrue(_refreshView._backoffTime == BannerRefreshView.BACKOFF_RATE_MIN);
    }

    [UnityTest]
    public IEnumerator TestCustomAddSourceRefreshRate()
    {
        // Destroy the default banner.
        yield return TearDown();

        _refreshConfig = new BannerRefreshConfiguration
        {
            DefaultRefreshRateInSeconds = 30,
            AdSourceRefreshRatesInSeconds = new Dictionary<string, int>
            {
                { "Placeholder AdSourceId", 5 }
            },
            AdsBufferSize = 1,
        };
        _refreshView = new BannerRefreshView(_adUnitId,
            AdSize.Banner,
            AdPosition.Center,
            _refreshConfig);
        var rotations = 0;
        _refreshView._onBannerRotated += () =>
        {
            rotations++;
        };
        yield return null;
        _refreshView.LoadAd(new AdRequest());
        yield return null;
        yield return null;
        Assert.AreEqual(rotations, 1);

        // Wait for the refresh cycle.
        yield return new WaitForSeconds(5 + 1);
        var firstBanner = _refreshView._currentBanner;
        Assert.IsNotNull(firstBanner);
        Assert.AreEqual(rotations, 2);

        // Wait for a second refresh cycle.
        yield return new WaitForSeconds(5 + 1);
        var secondBanner = _refreshView._currentBanner;
        Assert.IsNotNull(secondBanner);
        Assert.IsTrue(secondBanner != firstBanner);
        Assert.AreEqual(rotations, 3);
    }

    [UnityTest]
    public IEnumerator TestAdRequestTelemetry()
    {
        var refreshConfig = new BannerRefreshConfiguration
        {
            DefaultRefreshRateInSeconds = 10,
            AdsBufferSize = 2,
            AdSourceRefreshRatesInSeconds = new Dictionary<string, int> {
                 { "adapter1", 1 }, { "adapter2", 2 }
            }
        };
        _refreshView = new BannerRefreshView(
            "Test",
            AdSize.Banner,
            AdPosition.Center,
            refreshConfig);

        bool hasInitialLoad = false;
        bool hasRotationLoad = false;
        _refreshView._onBannerTelemetryInitialLoad += (bannerView, adRequest) =>
        {
            Assert.AreEqual("adapter1=1&adapter2=2", adRequest.Extras["brro"]);
            Assert.AreEqual("10", adRequest.Extras["brrd"]);
            Assert.AreEqual("2", adRequest.Extras["brb"]);
            Assert.IsFalse(adRequest.Extras.ContainsKey("brpi"));
            Assert.IsFalse(adRequest.Extras.ContainsKey("brrp"));
            Assert.IsFalse(adRequest.Extras.ContainsKey("brpas"));
            hasInitialLoad = true;
        };
        _refreshView._onBannerTelemetryRotationLoad += (bannerView, adRequest) =>
        {
            Assert.AreEqual("adapter1=1&adapter2=2", adRequest.Extras["brro"]);
            Assert.AreEqual("10", adRequest.Extras["brrd"]);
            Assert.AreEqual("2", adRequest.Extras["brb"]);
            Assert.AreEqual("10", adRequest.Extras["brrp"]);
            Assert.AreEqual("0", adRequest.Extras["brpi"]);
            Assert.AreEqual("Placeholder AdSourceId", adRequest.Extras["brpas"]);
            hasRotationLoad = true;
        };

        // Let the initial banner.
        yield return null;
        _refreshView.LoadAd(new AdRequest());
        yield return new WaitForSeconds(refreshConfig.DefaultRefreshRateInSeconds + 1);
        yield return null;
        yield return new WaitForSeconds(refreshConfig.DefaultRefreshRateInSeconds + 1);
        yield return null;

        // Confirm idle time was sent.
        Assert.IsTrue(hasInitialLoad);
        Assert.IsTrue(hasRotationLoad);
    }

    [UnityTest]
    public IEnumerator TestGetTelemetryExtras()
    {
        _bannerView = new BannerView(_adUnitId, AdSize.Banner, AdPosition.Top);
        _bannerView.LoadAd(new AdRequest());
        yield return null;

        var extras = BannerRefreshView.GetTelemetryExtras(_refreshConfig, 0, null);
        Assert.IsTrue(extras.ContainsKey("brb"));
        Assert.IsTrue(extras.ContainsKey("brrd"));
        Assert.IsTrue(extras.ContainsKey("brro"));
        Assert.IsFalse(extras.ContainsKey("brrp"));
        Assert.IsFalse(extras.ContainsKey("brpi"));
        Assert.IsFalse(extras.ContainsKey("brpas"));;

        var extrasWithBanner = BannerRefreshView.GetTelemetryExtras(_refreshConfig, 8, _bannerView);
        Assert.IsTrue(extrasWithBanner.ContainsKey("brb"));
        Assert.IsTrue(extrasWithBanner.ContainsKey("brrd"));
        Assert.IsTrue(extrasWithBanner.ContainsKey("brro"));
        Assert.IsTrue(extrasWithBanner.ContainsKey("brrp"));
        Assert.IsTrue(extrasWithBanner.ContainsKey("brpi"));
        Assert.IsTrue(extrasWithBanner.ContainsKey("brpas"));
    }

    [UnityTest]
    public IEnumerator TestAdSourceID()
    {
        _bannerView = new BannerView(_adUnitId, AdSize.Banner, AdPosition.Top);
        _bannerView.LoadAd(new AdRequest());
        yield return null;
        _refreshConfig.AdSourceRefreshRatesInSeconds.Add("Placeholder AdSourceId", 88);
        var extras = BannerRefreshView.GetTelemetryExtras(_refreshConfig, 0, _bannerView);
        Assert.AreEqual(extras["brrp"], "88");
        Assert.AreEqual(extras["brpas"], "Placeholder AdSourceId");
    }

    [UnityTest]
    public IEnumerator TestToQueryString()
    {
        yield return null;
        _refreshConfig.AdSourceRefreshRatesInSeconds.Add(AdSource.ADMOB_NETWORK, 30);
        var refreshConfig = BannerRefreshView.ToQueryString(_refreshConfig.AdSourceRefreshRatesInSeconds);
        Assert.AreEqual(refreshConfig, $"{AdSource.ADMOB_NETWORK}=30");

        _refreshConfig.AdSourceRefreshRatesInSeconds.Add(AdSource.AD_COLONY, 30);
        var refreshConfig2 = BannerRefreshView.ToQueryString(_refreshConfig.AdSourceRefreshRatesInSeconds);
        Assert.AreEqual(refreshConfig2, $"{AdSource.ADMOB_NETWORK}=30&{AdSource.AD_COLONY}=30");
    }
}
