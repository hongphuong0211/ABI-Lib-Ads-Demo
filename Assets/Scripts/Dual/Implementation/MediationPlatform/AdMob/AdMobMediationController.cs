using System;
using System.Collections;
using System.Collections.Generic;
using Dual;
using Dual.CoroutineRunner;
using GoogleMobileAds.Api;
using GoogleMobileAds.BannerRefresh;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

public class AdMobMediationController : BaseMediation
{
  public const string ADMOB_RC_ENABLE_LOG_AD_IMPRESSION = "admob_enable_log_ad_impression";
  public const string ADMOB_RC_INTER_PRELOAD_SIZE = "admob_inter_preload_size";
  public const string ADMOB_RC_REWARD_PRELOAD_SIZE = "admob_reward_preload_size";
  public const string ADMOB_RC_APPOPEN_PRELOAD_SIZE = "admob_appopen_preload_size";
  public const string ADMOB_RC_BANNER_REFRESH_RATE = "admob_banner_rate";
  public const string ADMOB_RC_BANNER_BUFFER_SIZE = "admob_banner_buffer_size";
  public const string ADMOB_RC_BANNER_RATE_BY_AD_SOURCE = "admob_banner_rate_by_ad_source";
  public const string ADMOB_RC_MREC_REFRESH_RATE = "admob_mrec_refresh_rate";
  public const string ADMOB_RC_MREC_BUFFER_SIZE = "admob_mrec_buffer_size";
  public const string ADMOB_RC_MREC_RATE_BY_AD_SOURCE = "admob_mrec_rate_by_ad_source";

  private IAdListener adListener;
  private bool isInit = false;

  private HashSet<string> preloadedInterAdUnits = new();
  private HashSet<string> preloadedRewardAdUnits = new();
  private HashSet<string> preloadAppOpenAdUnits = new();

  private bool earnedReward = false;

  private Dictionary<string, BannerRefreshView> bannerRefreshViews = new();
  private Dictionary<string, BannerRefreshView> mrecRefreshViews = new();

  public AdMobMediationController(string mediationKey) : base(mediationKey)
  {
  }

  public override void SetupBeforeInit(DualConfig dualConfig)
  {
    base.SetupBeforeInit(dualConfig);
    adListener = new AdMobAdListenerWrapper(dualConfig.adListener, true);
  }

  public override bool IsAdMobMediation()
  {
    return true;
  }

  public override void Init(Action onComplete)
  {
    if (isInit) return;
    isInit = true;

    DualHelper.Log("AdMobMediationController.Init()");

    MobileAdsEventExecutor.Initialize();

    InitUMPSDK(() =>
    {
      InitializeMobileAds(() =>
      {
        MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
          onComplete?.Invoke();
        });
      });
    });
  }

  private void InitializeMobileAds(Action onComplete)
  {
    MobileAds.Initialize(initStatus =>
    {
      DualHelper.Log("AdMobMediationController init finished");
      onComplete?.Invoke();
    });
  }

  public override void LoadInterstitial(DualAdId adId)
  {
    if (string.IsNullOrEmpty(adId.admobId) || preloadedInterAdUnits.Contains(adId.admobId)) return;
    preloadedInterAdUnits.Add(adId.admobId);

    var interstitialConfig = new PreloadConfiguration
    {
      AdUnitId = adId.admobId,
      Request = new AdRequest(),
      BufferSize = (uint)(dualConfig.remoteConfigRetriever?.GetLong(
        ADMOB_RC_INTER_PRELOAD_SIZE, 2) ?? 2)
    };

    InterstitialAdPreloader.Preload(adId.admobId, interstitialConfig,
        onAdPreloaded: (id, adInfo) =>
        {
          adListener?.OnAdLoaded(
            adUnitId: id,
            format: Dual.AdFormat.Interstitial,
            mediationKey: mediationKey,
            extras: new Dictionary<string, object> {
              { DualConst.KEY_AD_INFO, adInfo}
            }
          );
        },
        onAdFailedToPreload: (id, errorInfo) =>
        {
          adListener?.OnAdFailedToLoad(
            adUnitId: id,
            format: Dual.AdFormat.Interstitial,
            mediationKey: mediationKey,
            extras: new Dictionary<string, object> {
              { DualConst.KEY_ERROR_INFO, errorInfo }
            }
          );
        },
        onAdsExhausted: (id) =>
        {
          DualHelper.Log($"Interstitial Ad exhausted for {id}");
        }
    );

    DualHelper.Log("AdMobMediationController.LoadInterstitial()");
  }
  public override void ShowInterstitial(DualAdId adId, Action onComplete, Dictionary<string, object> placementData = null)
  {
    var ad = InterstitialAdPreloader.DequeueAd(adId.admobId);
    if (ad != null)
    {
      ad.OnAdPaid += (adValue) =>
      {
        OnAdRevenueReceived(
          adUnitId: adId.admobId,
          adFormat: Dual.AdFormat.Interstitial,
          adValue: adValue,
          adInfo: ad.GetResponseInfo(),
          placementData: placementData
        );
      };
      ad.OnAdFullScreenContentClosed += () =>
      {
        Dictionary<string, object> extras = new(placementData ?? new Dictionary<string, object>());
        extras.TryAdd(DualConst.KEY_AD_INFO, ad.GetResponseInfo());

        adListener?.OnAdHidden(
          adUnitId: adId.admobId,
          format: Dual.AdFormat.Interstitial,
          mediationKey: mediationKey,
          extras: extras
        );

        MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
          onComplete?.Invoke();
        });
      };
      ad.OnAdFullScreenContentOpened += () =>
      {
        Dictionary<string, object> extras = new(placementData ?? new Dictionary<string, object>());
        extras.TryAdd(DualConst.KEY_AD_INFO, ad.GetResponseInfo());

        adListener?.OnAdDisplayed(
          adUnitId: adId.admobId,
          format: Dual.AdFormat.Interstitial,
          mediationKey: mediationKey,
          extras: extras
        );
      };
      ad.OnAdClicked += () =>
      {
        Dictionary<string, object> extras = new(placementData ?? new Dictionary<string, object>());
        extras.TryAdd(DualConst.KEY_AD_INFO, ad.GetResponseInfo());

        adListener?.OnAdClicked(
          adUnitId: adId.admobId,
          format: Dual.AdFormat.Interstitial,
          mediationKey: mediationKey,
          extras: extras
        );
      };
      ad.OnAdFullScreenContentFailed += (errorInfo) =>
      {
        Dictionary<string, object> extras = new(placementData ?? new Dictionary<string, object>());
        extras.TryAdd(DualConst.KEY_AD_INFO, ad.GetResponseInfo());
        extras.TryAdd(DualConst.KEY_ERROR_INFO, errorInfo);

        adListener?.OnAdDisplayFailed(
          adUnitId: adId.admobId,
          format: Dual.AdFormat.Interstitial,
          mediationKey: mediationKey,
          extras: extras
        );

        MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
          onComplete?.Invoke();
        });
      };

      ad.Show();

      DualHelper.Log("AdMobMediationController.ShowInterstitial()");
    }
    else
    {
      DualHelper.Log("AdMobMediationController InterstitialAd dequeued empty");
      onComplete?.Invoke();
    }
  }
  public override bool IsInterstitialReady(DualAdId adId)
  {
    return InterstitialAdPreloader.IsAdAvailable(adId.admobId);
  }

  public override void LoadReward(DualAdId adId)
  {
    if (string.IsNullOrEmpty(adId.admobId) || preloadedRewardAdUnits.Contains(adId.admobId)) return;
    preloadedRewardAdUnits.Add(adId.admobId);

    var rewardedConfig = new PreloadConfiguration
    {
      AdUnitId = adId.admobId,
      Request = new AdRequest(),
      BufferSize = (uint)(dualConfig.remoteConfigRetriever?.GetLong(
        ADMOB_RC_REWARD_PRELOAD_SIZE, 2) ?? 2)
    };

    RewardedAdPreloader.Preload(adId.admobId, rewardedConfig,
        onAdPreloaded: (id, adInfo) =>
        {
          adListener?.OnAdLoaded(
            adUnitId: id,
            format: Dual.AdFormat.Reward,
            mediationKey: mediationKey,
            extras: new Dictionary<string, object> {
              { DualConst.KEY_AD_INFO, adInfo}
            }
          );
        },
        onAdFailedToPreload: (id, errorInfo) =>
        {
          adListener?.OnAdFailedToLoad(
            adUnitId: id,
            format: Dual.AdFormat.Reward,
            mediationKey: mediationKey,
            extras: new Dictionary<string, object> {
              { DualConst.KEY_ERROR_INFO, errorInfo }
            }
          );
        },
        onAdsExhausted: (id) =>
        {
          DualHelper.Log($"Reward Ads exhausted for {id}");
        }
    );

    DualHelper.Log("AdMobMediationController.LoadReward()");
  }
  public override void ShowReward(DualAdId adId, Action<bool> onComplete, Dictionary<string, object> placementData = null)
  {
    var ad = RewardedAdPreloader.DequeueAd(adId.admobId);
    if (ad != null)
    {
      ad.OnAdPaid += (adValue) =>
      {
        OnAdRevenueReceived(
          adUnitId: adId.admobId,
          adFormat: Dual.AdFormat.Reward,
          adValue: adValue,
          adInfo: ad.GetResponseInfo(),
          placementData: placementData
        );
      };
      ad.OnAdFullScreenContentClosed += () =>
      {
        Dictionary<string, object> extras = new(placementData ?? new Dictionary<string, object>());
        extras.TryAdd(DualConst.KEY_AD_INFO, ad.GetResponseInfo());

        adListener?.OnAdHidden(
          adUnitId: adId.admobId,
          format: Dual.AdFormat.Reward,
          mediationKey: mediationKey,
          extras: extras
        );

        MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
          CoroutineRunner.RunCoroutine(DelayRewardGrant(0.2f, onComplete));
        });
      };
      ad.OnAdFullScreenContentOpened += () =>
      {
        Dictionary<string, object> extras = new(placementData ?? new Dictionary<string, object>());
        extras.TryAdd(DualConst.KEY_AD_INFO, ad.GetResponseInfo());

        adListener?.OnAdDisplayed(
          adUnitId: adId.admobId,
          format: Dual.AdFormat.Reward,
          mediationKey: mediationKey,
          extras: extras
        );
      };
      ad.OnAdClicked += () =>
      {
        Dictionary<string, object> extras = new(placementData ?? new Dictionary<string, object>());
        extras.TryAdd(DualConst.KEY_AD_INFO, ad.GetResponseInfo());

        adListener?.OnAdClicked(
          adUnitId: adId.admobId,
          format: Dual.AdFormat.Reward,
          mediationKey: mediationKey,
          extras: extras
        );
      };
      ad.OnAdFullScreenContentFailed += (errorInfo) =>
      {
        Dictionary<string, object> extras = new(placementData ?? new Dictionary<string, object>());
        extras.TryAdd(DualConst.KEY_AD_INFO, ad.GetResponseInfo());
        extras.TryAdd(DualConst.KEY_ERROR_INFO, errorInfo);

        adListener?.OnAdDisplayFailed(
          adUnitId: adId.admobId,
          format: Dual.AdFormat.Reward,
          mediationKey: mediationKey,
          extras: extras
        );

        MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
          onComplete?.Invoke(false);
        });
      };
      ad.Show((reward) =>
      {
        Dictionary<string, object> extras = new(placementData ?? new Dictionary<string, object>());
        extras.TryAdd(DualConst.KEY_AD_INFO, ad.GetResponseInfo());
        extras.TryAdd(DualConst.KEY_REWARD_INFO, ad.GetResponseInfo());

        earnedReward = true;

        adListener?.OnAdReceivedReward(
          adUnitId: adId.admobId,
          format: Dual.AdFormat.Reward,
          mediationKey: mediationKey,
          extras: extras
        );
      });

      DualHelper.Log("AdMobMediationController.ShowReward()");
    }
    else
    {
      DualHelper.Log("AdMobMediationController RewardedAd dequeued empty");
      onComplete?.Invoke(false);
    }
  }

  private IEnumerator DelayRewardGrant(float delaySec, Action<bool> onComplete)
  {
    yield return new WaitForSeconds(delaySec);
    onComplete?.Invoke(earnedReward);
  }

  public override bool IsRewardReady(DualAdId adId)
  {
    return RewardedAdPreloader.IsAdAvailable(adId.admobId);
  }

  public override void LoadAppOpen(DualAdId adId)
  {
    if (string.IsNullOrEmpty(adId.admobId) || preloadAppOpenAdUnits.Contains(adId.admobId)) return;
    preloadAppOpenAdUnits.Add(adId.admobId);

    var appOpenConfig = new PreloadConfiguration
    {
      AdUnitId = adId.admobId,
      Request = new AdRequest(),
      BufferSize = (uint)(dualConfig.remoteConfigRetriever?.GetLong(
        ADMOB_RC_APPOPEN_PRELOAD_SIZE, 2) ?? 2)
    };

    AppOpenAdPreloader.Preload(adId.admobId, appOpenConfig,
        onAdPreloaded: (id, adInfo) =>
        {
          adListener?.OnAdLoaded(
            adUnitId: id,
            format: Dual.AdFormat.AppOpen,
            mediationKey: mediationKey,
            extras: new Dictionary<string, object> {
              { DualConst.KEY_AD_INFO, adInfo}
            }
          );
        },
        onAdFailedToPreload: (id, errorInfo) =>
        {
          adListener?.OnAdFailedToLoad(
            adUnitId: id,
            format: Dual.AdFormat.AppOpen,
            mediationKey: mediationKey,
            extras: new Dictionary<string, object> {
              { DualConst.KEY_ERROR_INFO, errorInfo }
            }
          );
        },
        onAdsExhausted: (id) =>
        {
          DualHelper.Log($"AdMobMediationController Ads exhausted for {id}");
        }
    );

    DualHelper.Log("AdMobMediationController.LoadAppOpen()");
  }
  public override void ShowAppOpen(DualAdId adId, Action onComplete, Dictionary<string, object> placementData = null)
  {
    var ad = AppOpenAdPreloader.DequeueAd(adId.admobId);
    if (ad != null)
    {
      ad.OnAdPaid += (adValue) =>
      {
        OnAdRevenueReceived(
          adUnitId: adId.admobId,
          adFormat: Dual.AdFormat.AppOpen,
          adValue: adValue,
          adInfo: ad.GetResponseInfo(),
          placementData: placementData
        );
      };
      ad.OnAdFullScreenContentClosed += () =>
      {
        Dictionary<string, object> extras = new(placementData ?? new Dictionary<string, object>());
        extras.TryAdd(DualConst.KEY_AD_INFO, ad.GetResponseInfo());

        adListener?.OnAdHidden(
          adUnitId: adId.admobId,
          format: Dual.AdFormat.AppOpen,
          mediationKey: mediationKey,
          extras: extras
        );

        MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
          onComplete?.Invoke();
        });
      };
      ad.OnAdFullScreenContentOpened += () =>
      {
        Dictionary<string, object> extras = new(placementData ?? new Dictionary<string, object>());
        extras.TryAdd(DualConst.KEY_AD_INFO, ad.GetResponseInfo());

        adListener?.OnAdDisplayed(
          adUnitId: adId.admobId,
          format: Dual.AdFormat.AppOpen,
          mediationKey: mediationKey,
          extras: extras
        );
      };
      ad.OnAdClicked += () =>
      {
        Dictionary<string, object> extras = new(placementData ?? new Dictionary<string, object>());
        extras.TryAdd(DualConst.KEY_AD_INFO, ad.GetResponseInfo());

        adListener?.OnAdClicked(
          adUnitId: adId.admobId,
          format: Dual.AdFormat.AppOpen,
          mediationKey: mediationKey,
          extras: extras
        );
      };
      ad.OnAdFullScreenContentFailed += (errorInfo) =>
      {
        Dictionary<string, object> extras = new(placementData ?? new Dictionary<string, object>());
        extras.TryAdd(DualConst.KEY_AD_INFO, ad.GetResponseInfo());
        extras.TryAdd(DualConst.KEY_ERROR_INFO, errorInfo);

        adListener?.OnAdDisplayFailed(
          adUnitId: adId.admobId,
          format: Dual.AdFormat.AppOpen,
          mediationKey: mediationKey,
          extras: extras
        );

        MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
          onComplete?.Invoke();
        });
      };

      ad.Show();
      DualHelper.Log("AdMobMediationController.ShowAppOpen()");
    }
    else
    {
      DualHelper.Log("AdMobMediationController AppOpenAd dequeued empty");
      onComplete?.Invoke();
    }
  }
  public override bool IsAppOpenReady(DualAdId adId)
  {
    return AppOpenAdPreloader.IsAdAvailable(adId.admobId);
  }

  public override void InitBanner(DualAdId adId, DualBannerConfig config)
  {
    if (string.IsNullOrEmpty(adId.admobId)) return;
    if (bannerRefreshViews.ContainsKey(adId.admobId))
    {
      DualHelper.Log("AdMobMediationController.InitBanner() - Banner is existed");
      return;
    }

    Dictionary<string, int> adSourceRefreshRate = new();
    try
    {
      var adSourceRateJsonString = dualConfig.remoteConfigRetriever?.GetString(
        ADMOB_RC_BANNER_RATE_BY_AD_SOURCE, "{}"
      ) ?? "{}";
      List<RefreshRateByAdSourceConfig.Item> adSourceRate = JsonUtility
        .FromJson<RefreshRateByAdSourceConfig>(adSourceRateJsonString).refreshRateConfig;

      if (adSourceRate != null)
      {
        foreach (RefreshRateByAdSourceConfig.Item item in adSourceRate)
        {
          adSourceRefreshRate[item.adSourceId] = item.refreshRate;
        }
      }
    }
    catch (Exception)
    {
    }

    int deviceWidth = MobileAds.Utils.GetDeviceSafeWidth();
    AdSize adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(deviceWidth);

    var configuration = new BannerRefreshConfiguration
    {
      AdsBufferSize = (int)(dualConfig.remoteConfigRetriever?.GetLong(
        ADMOB_RC_BANNER_BUFFER_SIZE, 2
      ) ?? 2),
      DefaultRefreshRateInSeconds = (int)(dualConfig.remoteConfigRetriever?.GetLong(
        ADMOB_RC_BANNER_REFRESH_RATE, 60
      ) ?? 60),
      AdSourceRefreshRatesInSeconds = adSourceRefreshRate
    };

    BannerRefreshView bannerRefreshView;

    if (config.xPos != 0 || config.yPos != 0)
    {
      bannerRefreshView = new BannerRefreshView(
            adUnitId: adId.admobId,
            adSize: adaptiveSize,
            x: config.xPos,
            y: config.yPos,
            configuration: configuration
          );
    }
    else
    {
      bannerRefreshView = new BannerRefreshView(
            adUnitId: adId.admobId,
            adSize: adaptiveSize,
            position: ConvertToAdMobPosition(config),
            configuration: configuration
          );
    }

    bannerRefreshView.OnBannerAdLoaded += () =>
    {
      adListener?.OnAdLoaded(
        adUnitId: adId.admobId,
        format: Dual.AdFormat.Banner,
        mediationKey: mediationKey,
        extras: new Dictionary<string, object>
        {
          { DualConst.KEY_AD_INFO, bannerRefreshView.GetResponseInfo() }
        }
      );
    };
    bannerRefreshView.OnBannerAdLoadFailed += (errorInfo) =>
    {
      adListener?.OnAdFailedToLoad(
        adUnitId: adId.admobId,
        format: Dual.AdFormat.Banner,
        mediationKey: mediationKey,
        extras: new Dictionary<string, object>
        {
          { DualConst.KEY_AD_INFO, bannerRefreshView.GetResponseInfo() },
          { DualConst.KEY_ERROR_INFO, errorInfo }
        }
      );
    };
    bannerRefreshView.OnAdPaid += (adValue) =>
    {
      OnAdRevenueReceived(
          adUnitId: adId.admobId,
          adFormat: Dual.AdFormat.Banner,
          adValue: adValue,
          adInfo: bannerRefreshView.GetResponseInfo(),
          placementData: new Dictionary<string, object>()
        );
    };
    bannerRefreshView.OnAdClicked += () =>
    {
      Dictionary<string, object> extras = new();
      extras.TryAdd(DualConst.KEY_AD_INFO, bannerRefreshView.GetResponseInfo());

      adListener?.OnAdClicked(
        adUnitId: adId.admobId,
        format: Dual.AdFormat.Banner,
        mediationKey: mediationKey,
        extras: extras
      );
    };

    bannerRefreshView.Hide();

    bannerRefreshViews[adId.admobId] = bannerRefreshView;

    DualHelper.Log("AdMobMediationController.itBanner()");

    LoadBanner(adId);
  }
  public override void LoadBanner(DualAdId adId)
  {
    if (string.IsNullOrEmpty(adId.admobId)) return;
    if (!bannerRefreshViews.TryGetValue(adId.admobId, out var bannerRefreshView)) return;

    bannerRefreshView.LoadAd(new AdRequest());
    DualHelper.Log("AdMobMediationController.LoadBanner()");
  }
  public override void ShowBanner(DualAdId adId)
  {
    if (string.IsNullOrEmpty(adId.admobId)) return;
    if (!bannerRefreshViews.TryGetValue(adId.admobId, out var bannerRefreshView)) return;

    bannerRefreshView.Show();
    DualHelper.Log("AdMobMediationController.ShowBanner()");
  }
  public override void HideBanner(DualAdId adId)
  {
    if (string.IsNullOrEmpty(adId.admobId)) return;
    if (!bannerRefreshViews.TryGetValue(adId.admobId, out var bannerRefreshView)) return;

    bannerRefreshView.Hide();
    DualHelper.Log("AdMobMediationController.HideBanner()");
  }
  public override void DestroyBanner(DualAdId adId)
  {
    if (string.IsNullOrEmpty(adId.admobId)) return;
    if (!bannerRefreshViews.TryGetValue(adId.admobId, out var bannerRefreshView)) return;

    bannerRefreshViews.Remove(adId.admobId);

    if (!bannerRefreshView.IsDestroyed)
    {
      bannerRefreshView.Destroy();
    }
    DualHelper.Log("AdMobMediationController.DestroyBanner()");
  }
  public override void InitMrec(DualAdId adId, DualBannerConfig config)
  {
    if (string.IsNullOrEmpty(adId.admobId)) return;
    if (mrecRefreshViews.ContainsKey(adId.admobId))
    {
      DualHelper.Log("AdMobMediationController.InitMrec() - Mrec is existed");
      return;
    }

    Dictionary<string, int> adSourceRefreshRate = new();
    try
    {
      var adSourceRateJsonString = dualConfig.remoteConfigRetriever?.GetString(
        ADMOB_RC_MREC_RATE_BY_AD_SOURCE, "{}"
      ) ?? "{}";
      List<RefreshRateByAdSourceConfig.Item> adSourceRate = JsonUtility
        .FromJson<RefreshRateByAdSourceConfig>(adSourceRateJsonString).refreshRateConfig;

      if (adSourceRate != null)
      {
        foreach (RefreshRateByAdSourceConfig.Item item in adSourceRate)
        {
          adSourceRefreshRate[item.adSourceId] = item.refreshRate;
        }
      }
    }
    catch (Exception)
    {
    }

    var configuration = new BannerRefreshConfiguration
    {
      AdsBufferSize = (int)(dualConfig.remoteConfigRetriever?.GetLong(
        ADMOB_RC_MREC_BUFFER_SIZE, 2
      ) ?? 2),
      DefaultRefreshRateInSeconds = (int)(dualConfig.remoteConfigRetriever?.GetLong(
        ADMOB_RC_MREC_REFRESH_RATE, 60
      ) ?? 60),
      AdSourceRefreshRatesInSeconds = adSourceRefreshRate
    };

    BannerRefreshView mrecRefreshView;

    if (config.xPos != 0 || config.yPos != 0)
    {
      mrecRefreshView = new BannerRefreshView(
           adUnitId: adId.admobId,
           adSize: AdSize.MediumRectangle,
           x: config.xPos,
           y: config.yPos,
           configuration: configuration
         );
    }
    else
    {
      mrecRefreshView = new BannerRefreshView(
           adUnitId: adId.admobId,
           adSize: AdSize.MediumRectangle,
           position: ConvertToAdMobPosition(config),
           configuration: configuration
         );
    }

    mrecRefreshView.OnBannerAdLoaded += () =>
    {
      adListener?.OnAdLoaded(
        adUnitId: adId.admobId,
        format: Dual.AdFormat.Mrec,
        mediationKey: mediationKey,
        extras: new Dictionary<string, object>
        {
          { DualConst.KEY_AD_INFO, mrecRefreshView.GetResponseInfo() }
        }
      );
    };
    mrecRefreshView.OnBannerAdLoadFailed += (errorInfo) =>
    {
      adListener?.OnAdFailedToLoad(
        adUnitId: adId.admobId,
        format: Dual.AdFormat.Mrec,
        mediationKey: mediationKey,
        extras: new Dictionary<string, object>
        {
          { DualConst.KEY_AD_INFO, mrecRefreshView.GetResponseInfo() },
          { DualConst.KEY_ERROR_INFO, errorInfo }
        }
      );
    };
    mrecRefreshView.OnAdPaid += (adValue) =>
    {
      OnAdRevenueReceived(
          adUnitId: adId.admobId,
          adFormat: Dual.AdFormat.Mrec,
          adValue: adValue,
          adInfo: mrecRefreshView.GetResponseInfo(),
          placementData: new Dictionary<string, object>()
        );
    };
    mrecRefreshView.OnAdClicked += () =>
    {
      Dictionary<string, object> extras = new();
      extras.TryAdd(DualConst.KEY_AD_INFO, mrecRefreshView.GetResponseInfo());

      adListener?.OnAdClicked(
        adUnitId: adId.admobId,
        format: Dual.AdFormat.Mrec,
        mediationKey: mediationKey,
        extras: extras
      );
    };

    mrecRefreshViews[adId.admobId] = mrecRefreshView;

    mrecRefreshView.Hide();

    DualHelper.Log("AdMobMediationController.InitMrec()");

    LoadMrec(adId);
  }
  public override void LoadMrec(DualAdId adId)
  {
    if (string.IsNullOrEmpty(adId.admobId)) return;
    if (!mrecRefreshViews.TryGetValue(adId.admobId, out var mrecRefreshView)) return;

    mrecRefreshView.LoadAd(new AdRequest());

    DualHelper.Log("AdMobMediationController.LoadMrec()");
  }
  public override void ShowMrec(DualAdId adId)
  {
    if (string.IsNullOrEmpty(adId.admobId)) return;
    if (!mrecRefreshViews.TryGetValue(adId.admobId, out var mrecRefreshView)) return;

    mrecRefreshView.Show();

    DualHelper.Log("AdMobMediationController.ShowMrec()");
  }
  public override void HideMrec(DualAdId adId)
  {
    if (string.IsNullOrEmpty(adId.admobId)) return;
    if (!mrecRefreshViews.TryGetValue(adId.admobId, out var mrecRefreshView)) return;

    mrecRefreshView.Hide();

    DualHelper.Log("AdMobMediationController.HideMrec()");
  }
  public override void DestroyMrec(DualAdId adId)
  {
    if (string.IsNullOrEmpty(adId.admobId)) return;
    if (!mrecRefreshViews.TryGetValue(adId.admobId, out var mrecRefreshView)) return;

    mrecRefreshViews.Remove(adId.admobId);

    if (!mrecRefreshView.IsDestroyed)
    {
      mrecRefreshView.Destroy();
    }

    DualHelper.Log("AdMobMediationController.DestroyMrec()");
  }

  private void InitUMPSDK(Action onComplete)
  {
    var request = new ConsentRequestParameters
    {
      TagForUnderAgeOfConsent = false,
      // ConsentDebugSettings = new ConsentDebugSettings
      // {
      //   // For debugging consent settings by geography.
      //   DebugGeography = DebugGeography.Disabled,
      //   // https://developers.google.com/admob/unity/test-ads
      //   TestDeviceHashedIds = new List<string>()
      // }
    };

    ConsentInformation.Update(request, error =>
    {
      if (error != null)
      {
        DualHelper.Log("AdMobMediationController.InitUMP get form error: " + error.Message);
        onComplete?.Invoke();
        return;
      }

      if (ConsentInformation.CanRequestAds())
      {
        // Consent has already been gathered or not required.
        // Return control back to the user.
        onComplete?.Invoke();
        return;
      }

      ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
      {
        if (formError != null)
        {
          DualHelper.Log("AdMobMediationController LoadAndShowConsentFormIfRequired error: " + formError.Message);
          onComplete?.Invoke();
          return;
        }

        DualHelper.Log("AdMobMediationController LoadAndShowConsentFormIfRequired success: ");
        onComplete?.Invoke();
      });
    });
  }

  private AdPosition ConvertToAdMobPosition(DualBannerConfig config)
  {
    DualBannerConfig.Position position = config.position;

    AdPosition admobPosition = AdPosition.Bottom;
    switch (position)
    {
      case DualBannerConfig.Position.TopLeft:
        admobPosition = AdPosition.TopLeft;
        break;
      case DualBannerConfig.Position.TopCenter:
        admobPosition = AdPosition.Top;
        break;
      case DualBannerConfig.Position.TopRight:
        admobPosition = AdPosition.TopRight;
        break;
      case DualBannerConfig.Position.Centered:
        admobPosition = AdPosition.Center;
        break;
      case DualBannerConfig.Position.BottomLeft:
        admobPosition = AdPosition.BottomLeft;
        break;
      case DualBannerConfig.Position.BottomCenter:
        admobPosition = AdPosition.Bottom;
        break;
      case DualBannerConfig.Position.BottomRight:
        admobPosition = AdPosition.BottomRight;
        break;
    }

    return admobPosition;
  }

  private void OnAdRevenueReceived(
    string adUnitId,
    Dual.AdFormat adFormat,
    AdValue adValue,
    ResponseInfo adInfo,
    Dictionary<string, object> placementData
  )
  {
    if (adValue == null) return;

    bool logBuiltInAdImpression = dualConfig.remoteConfigRetriever?.GetBool(ADMOB_RC_ENABLE_LOG_AD_IMPRESSION, false) ?? false;
    string adSource = adInfo?.GetLoadedAdapterResponseInfo()?.AdSourceName ?? "";
    Dictionary<string, object> extras = new(placementData ?? new Dictionary<string, object>());

    HandleAdRevenuePaid(
      adPlatform: "AdMob",
      adSource: adSource,
      adUnitId: adUnitId,
      adFormat: adFormat.ToString(),
      adValue: adValue.Value / 1000000f,
      currencyCode: adValue.CurrencyCode,
      extras: extras,
      logBuiltInAdImpression: logBuiltInAdImpression
    );
  }


  class RefreshRateByAdSourceConfig
  {
    public List<Item> refreshRateConfig;

    public class Item
    {
      public string adSourceId;
      public int refreshRate;
    }
  }
}
