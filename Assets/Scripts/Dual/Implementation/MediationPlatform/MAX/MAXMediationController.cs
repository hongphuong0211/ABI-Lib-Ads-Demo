using System;
using System.Collections;
using System.Collections.Generic;
using Dual;
using Dual.CoroutineRunner;
using UnityEngine;

public class MAXMediationController : BaseMediation
{
  private IAdRevenueHandler adRevenueHandler;
  private IAdListener adListener;
  private bool isInit = false;

  private Action currentInterstitialOnComplete;
  private Action<bool> currentRewardOnComplete;
  private Action currentAppOpenOnComplete;
  private bool hasEarnedReward = false;

  private Dictionary<string, object> interPlacementData;
  private Dictionary<string, object> rewardPlacementData;
  private Dictionary<string, object> appOpenPlacementData;

  private readonly Dictionary<string, bool> hasBannerInit = new();
  private readonly Dictionary<string, bool> hasMrecInit = new();

  private readonly Dictionary<string, int> interRetryAttempt = new();
  private readonly Dictionary<string, int> rewardRetryAttempt = new();
  private readonly Dictionary<string, int> appOpenRetryAttempt = new();

  private readonly HashSet<string> initedAdUnitIds = new();

  public MAXMediationController(string mediationKey) : base(mediationKey)
  {
  }

  public override void SetupBeforeInit(DualConfig dualConfig)
  {
    base.SetupBeforeInit(dualConfig);
    adRevenueHandler = dualConfig.adRevenueHandler;
    adListener = dualConfig.adListener;
  }

  public override bool IsAdMobMediation()
  {
    return false;
  }

  public override void Init(Action onComplete)
  {
    if (isInit) return;
    isInit = true;

    DualHelper.Log("MAXMediationController.Init()");

    InitializeMaxSdk(onComplete);
  }

  private void InitializeMaxSdk(Action onComplete)
  {
    MaxSdkCallbacks.OnSdkInitializedEvent += (MaxSdk.SdkConfiguration sdkConfiguration) =>
    {
      RegisterInterstitialCallbacks();
      RegisterRewardedCallbacks();
      RegisterAppOpenCallbacks();
      RegisterBannerCallbacks();
      RegisterMrecCallbacks();

      DualHelper.Log("MAXMediationController init finished");
      onComplete?.Invoke();
    };

    MaxSdk.InitializeSdk();
  }

  public override void LoadInterstitial(DualAdId adId)
  {
    if (!string.IsNullOrEmpty(adId.maxId))
    {
      initedAdUnitIds.Add(adId.maxId);
      MaxSdk.LoadInterstitial(adId.maxId);
    }

    DualHelper.Log("MAXMediationController.LoadInterstitial()");
  }

  private void OnInterstitialAdLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
  {
    interRetryAttempt[adUnitId] = 0;

    adListener?.OnAdLoaded(
      adUnitId: adUnitId,
      format: AdFormat.Interstitial,
      mediationKey: mediationKey,
      extras: new Dictionary<string, object>
      {
        { DualConst.KEY_AD_INFO, adInfo}
      }
    );
  }

  private void OnInterstitialAdLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
  {
    interRetryAttempt.TryGetValue(adUnitId, out int retryAttempt);
    interRetryAttempt[adUnitId] = retryAttempt + 1;

    adListener?.OnAdFailedToLoad(
      adUnitId: adUnitId,
      format: AdFormat.Interstitial,
      mediationKey: mediationKey,
      extras: new Dictionary<string, object>
      {
        { DualConst.KEY_ERROR_INFO, errorInfo }
      }
    );

    int retryDelay = (int)Math.Pow(2, Math.Min(6, retryAttempt));

    CoroutineRunner.RunCoroutine(WaitAndReloadInterstitial(adUnitId, retryDelay));
  }

  IEnumerator WaitAndReloadInterstitial(string adUnitId, int delay)
  {
    DualHelper.Log("MAXMediationController"
      + ".WaitAndReloadInterstitial(" + adUnitId + ", " + delay + ")");

    yield return new WaitForSeconds(delay);
    LoadInterstitial(new DualAdId.Builder().SetMAXId(adUnitId).Build());
  }

  public override void ShowInterstitial(DualAdId adId, Action onComplete, Dictionary<string, object> placementData = null)
  {
    if (MaxSdk.IsInterstitialReady(adId.maxId))
    {
      currentInterstitialOnComplete = onComplete;
      interPlacementData = placementData;
      MaxSdk.ShowInterstitial(adId.maxId);
      DualHelper.Log("MAXMediationController.ShowInterstitial()");
    }
    else
    {
      DualHelper.Log("[MAX] Interstitial is not ready");
      onComplete?.Invoke();
    }
  }

  private void OnInterstitialAdDisplayed(string adUnitId, MaxSdkBase.AdInfo adInfo)
  {
    Dictionary<string, object> extras = new(
      interPlacementData ?? new Dictionary<string, object>()
    );
    extras.TryAdd(DualConst.KEY_AD_INFO, adInfo);

    adListener?.OnAdDisplayed(
      adUnitId: adUnitId,
      format: AdFormat.Interstitial,
      mediationKey: mediationKey,
      extras: extras
    );
  }

  private void OnInterstitialAdHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
  {
    Dictionary<string, object> extras = new(
      interPlacementData ?? new Dictionary<string, object>()
    );
    extras.TryAdd(DualConst.KEY_AD_INFO, adInfo);

    adListener?.OnAdHidden(
      adUnitId: adUnitId,
      format: AdFormat.Interstitial,
      mediationKey: mediationKey,
      extras: extras
    );

    interPlacementData = null;

    currentInterstitialOnComplete?.Invoke();
    currentInterstitialOnComplete = null;

    LoadInterstitial(new DualAdId.Builder().SetMAXId(adUnitId).Build());
  }

  private void OnInterstitialAdClicked(string adUnitId, MaxSdkBase.AdInfo adInfo)
  {
    Dictionary<string, object> extras = new(
      interPlacementData ?? new Dictionary<string, object>()
    );
    extras.TryAdd(DualConst.KEY_AD_INFO, adInfo);

    adListener?.OnAdClicked(
      adUnitId: adUnitId,
      format: AdFormat.Interstitial,
      mediationKey: mediationKey,
      extras: extras
    );
  }

  private void OnInterstitialAdDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
  {
    Dictionary<string, object> extras = new(
      interPlacementData ?? new Dictionary<string, object>()
    );
    extras.TryAdd(DualConst.KEY_ERROR_INFO, errorInfo);
    extras.TryAdd(DualConst.KEY_AD_INFO, adInfo);

    adListener?.OnAdDisplayFailed(
      adUnitId: adUnitId,
      format: AdFormat.Interstitial,
      mediationKey: mediationKey,
      extras: extras
    );

    interPlacementData = null;

    currentInterstitialOnComplete?.Invoke();
    currentInterstitialOnComplete = null;

    LoadInterstitial(new DualAdId.Builder().SetMAXId(adUnitId).Build());
  }

  private void OnInterstitialAdRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
  {
    OnAdRevenueReceived(adInfo, interPlacementData);
  }

  public override bool IsInterstitialReady(DualAdId adId)
  {
    return MaxSdk.IsInterstitialReady(adId.maxId);
  }

  public override void LoadReward(DualAdId adId)
  {
    if (!string.IsNullOrEmpty(adId.maxId))
    {
      initedAdUnitIds.Add(adId.maxId);
      MaxSdk.LoadRewardedAd(adId.maxId);
    }

    DualHelper.Log("MAXMediationController.LoadReward()");
  }

  private void OnRewardedAdLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
  {
    rewardRetryAttempt[adUnitId] = 0;

    adListener?.OnAdLoaded(
      adUnitId: adUnitId,
      format: AdFormat.Reward,
      mediationKey: mediationKey,
      extras: new Dictionary<string, object>
      {
        { DualConst.KEY_AD_INFO, adInfo}
      });
  }

  private void OnRewardedAdLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
  {
    rewardRetryAttempt.TryGetValue(adUnitId, out int retryAttempt);
    rewardRetryAttempt[adUnitId] = retryAttempt + 1;

    adListener?.OnAdFailedToLoad(
      adUnitId: adUnitId,
      format: AdFormat.Reward,
      mediationKey: mediationKey,
      extras: new Dictionary<string, object>
      {
        { DualConst.KEY_ERROR_INFO, errorInfo}
      }
    );

    int retryDelay = (int)Math.Pow(2, Math.Min(6, retryAttempt));

    CoroutineRunner.RunCoroutine(WaitAndReloadReward(adUnitId, retryDelay));
  }

  IEnumerator WaitAndReloadReward(string adUnitId, int delay)
  {
    DualHelper.Log("MAXMediationController"
      + ".WaitAndReloadReward(" + adUnitId + ", " + delay + ")");

    yield return new WaitForSeconds(delay);
    LoadReward(new DualAdId.Builder().SetMAXId(adUnitId).Build());
  }

  public override void ShowReward(DualAdId adId, Action<bool> onComplete, Dictionary<string, object> placementData = null)
  {
    if (MaxSdk.IsRewardedAdReady(adId.maxId))
    {
      hasEarnedReward = false;
      currentRewardOnComplete = onComplete;

      rewardPlacementData = placementData;

      MaxSdk.ShowRewardedAd(adId.maxId);
      DualHelper.Log("MAXMediationController.ShowReward()");
    }
    else
    {
      DualHelper.Log("[MAX] Rewarded Ad is not ready");
      onComplete?.Invoke(false);
    }
  }

  private void OnRewardedAdDisplayed(string adUnitId, MaxSdkBase.AdInfo adInfo)
  {
    Dictionary<string, object> extras = new(
      rewardPlacementData ?? new Dictionary<string, object>()
    );
    extras.TryAdd(DualConst.KEY_AD_INFO, adInfo);

    adListener?.OnAdDisplayed(
      adUnitId: adUnitId,
      format: AdFormat.Reward,
      mediationKey: mediationKey,
      extras: extras
    );
  }

  private void OnRewardedAdHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
  {
    Dictionary<string, object> extras = new(
      rewardPlacementData ?? new Dictionary<string, object>()
    );
    extras.TryAdd(DualConst.KEY_AD_INFO, adInfo);

    adListener?.OnAdHidden(
      adUnitId: adUnitId,
      format: AdFormat.Reward,
      mediationKey: mediationKey,
      extras: extras
    );

    rewardPlacementData = null;

    currentRewardOnComplete?.Invoke(hasEarnedReward);
    currentRewardOnComplete = null;

    LoadReward(new DualAdId.Builder().SetMAXId(adUnitId).Build());
  }

  private void OnRewardedAdClicked(string adUnitId, MaxSdkBase.AdInfo adInfo)
  {
    Dictionary<string, object> extras = new(
      rewardPlacementData ?? new Dictionary<string, object>()
    );
    extras.TryAdd(DualConst.KEY_AD_INFO, adInfo);

    adListener?.OnAdClicked(
      adUnitId: adUnitId,
      format: AdFormat.Reward,
      mediationKey: mediationKey,
      extras: extras
    );
  }

  private void OnRewardedAdDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
  {
    Dictionary<string, object> extras = new(
      rewardPlacementData ?? new Dictionary<string, object>()
    );
    extras.TryAdd(DualConst.KEY_ERROR_INFO, errorInfo);
    extras.TryAdd(DualConst.KEY_AD_INFO, adInfo);

    adListener?.OnAdDisplayFailed(
      adUnitId: adUnitId,
      format: AdFormat.Reward,
      mediationKey: mediationKey,
      extras: extras
    );

    rewardPlacementData = null;

    currentRewardOnComplete?.Invoke(false);
    currentRewardOnComplete = null;

    LoadReward(new DualAdId.Builder().SetMAXId(adUnitId).Build());
  }

  private void OnRewardedAdReceivedReward(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
  {
    hasEarnedReward = true;

    Dictionary<string, object> extras = new(
      rewardPlacementData ?? new Dictionary<string, object>()
    );
    extras.TryAdd(DualConst.KEY_REWARD_INFO, reward);
    extras.TryAdd(DualConst.KEY_AD_INFO, adInfo);

    adListener?.OnAdReceivedReward(adUnitId, AdFormat.Reward, mediationKey, extras);
  }

  private void OnRewardedAdRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
  {
    OnAdRevenueReceived(adInfo, rewardPlacementData);
  }

  public override bool IsRewardReady(DualAdId adId)
  {
    return MaxSdk.IsRewardedAdReady(adId.maxId);
  }

  public override void LoadAppOpen(DualAdId adId)
  {
    if (!string.IsNullOrEmpty(adId.maxId))
    {
      initedAdUnitIds.Add(adId.maxId);
      MaxSdk.LoadAppOpenAd(adId.maxId);
    }
    DualHelper.Log("MAXMediationController.LoadAppOpen()");
  }

  private void OnAppOpenAdLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
  {
    appOpenRetryAttempt[adUnitId] = 0;

    adListener?.OnAdLoaded(
      adUnitId: adUnitId,
      format: AdFormat.AppOpen,
      mediationKey: mediationKey,
      extras: new Dictionary<string, object>
      {
        { DualConst.KEY_AD_INFO, adInfo }
      });
  }

  private void OnAppOpenAdLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
  {
    appOpenRetryAttempt.TryGetValue(adUnitId, out int retryAttempt);
    appOpenRetryAttempt[adUnitId] = retryAttempt + 1;

    adListener?.OnAdFailedToLoad(
      adUnitId: adUnitId,
      format: AdFormat.AppOpen,
      mediationKey: mediationKey,
      extras: new Dictionary<string, object> {
        { DualConst.KEY_ERROR_INFO, errorInfo }
      }
    );

    int retryDelay = (int)Math.Pow(2, Math.Min(6, retryAttempt));

    CoroutineRunner.RunCoroutine(WaitAndReloadAppOpen(adUnitId, retryDelay));
  }

  IEnumerator WaitAndReloadAppOpen(string adUnitId, int delay)
  {
    DualHelper.Log("MAXMediationController"
      + ".WaitAndReloadAppOpen(" + adUnitId + ", " + delay + ")");

    yield return new WaitForSeconds(delay);
    LoadAppOpen(new DualAdId.Builder().SetMAXId(adUnitId).Build());
  }

  public override void ShowAppOpen(DualAdId adId, Action onComplete, Dictionary<string, object> placementData = null)
  {
    if (MaxSdk.IsAppOpenAdReady(adId.maxId))
    {
      currentAppOpenOnComplete = onComplete;
      appOpenPlacementData = placementData;

      MaxSdk.ShowAppOpenAd(adId.maxId);
      DualHelper.Log("MAXMediationController.ShowAppOpen()");
    }
    else
    {
      DualHelper.Log("[MAX] App Open Ad is not ready");
      onComplete?.Invoke();
    }
  }

  private void OnAppOpenAdDisplayed(string adUnitId, MaxSdkBase.AdInfo adInfo)
  {
    Dictionary<string, object> extras = new(
      appOpenPlacementData ?? new Dictionary<string, object>()
    );
    extras.TryAdd(DualConst.KEY_AD_INFO, adInfo);

    adListener?.OnAdDisplayed(
      adUnitId: adUnitId,
      format: AdFormat.AppOpen,
      mediationKey: mediationKey,
      extras: extras
    );
  }

  private void OnAppOpenAdHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
  {
    Dictionary<string, object> extras = new(
      appOpenPlacementData ?? new Dictionary<string, object>()
    );
    extras.TryAdd(DualConst.KEY_AD_INFO, adInfo);

    adListener?.OnAdHidden(
      adUnitId: adUnitId,
      format: AdFormat.AppOpen,
      mediationKey: mediationKey,
      extras: extras
    );

    appOpenPlacementData = null;

    currentAppOpenOnComplete?.Invoke();
    currentAppOpenOnComplete = null;

    LoadAppOpen(new DualAdId.Builder().SetMAXId(adUnitId).Build());
  }

  private void OnAppOpenAdClicked(string adUnitId, MaxSdkBase.AdInfo adInfo)
  {
    Dictionary<string, object> extras = new(
      appOpenPlacementData ?? new Dictionary<string, object>()
    );
    extras.TryAdd(DualConst.KEY_AD_INFO, adInfo);

    adListener?.OnAdClicked(
      adUnitId: adUnitId,
      format: AdFormat.AppOpen,
      mediationKey: mediationKey,
      extras: extras
    );
  }

  private void OnAppOpenAdDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
  {
    Dictionary<string, object> extras = new(
      appOpenPlacementData ?? new Dictionary<string, object>()
    );
    extras.TryAdd(DualConst.KEY_ERROR_INFO, errorInfo);
    extras.TryAdd(DualConst.KEY_AD_INFO, adInfo);

    adListener?.OnAdDisplayFailed(
      adUnitId: adUnitId,
      format: AdFormat.AppOpen,
      mediationKey: mediationKey,
      extras: extras
    );

    appOpenPlacementData = null;

    currentAppOpenOnComplete?.Invoke();
    currentAppOpenOnComplete = null;

    LoadAppOpen(new DualAdId.Builder().SetMAXId(adUnitId).Build());
  }

  private void OnAppOpenAdRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
  {
    OnAdRevenueReceived(adInfo, appOpenPlacementData);
  }

  public override bool IsAppOpenReady(DualAdId adId)
  {
    return MaxSdk.IsAppOpenAdReady(adId.maxId);
  }

  public override void InitBanner(DualAdId adId, DualBannerConfig config)
  {
    hasBannerInit.TryGetValue(adId.maxId, out bool hasInit);

    if (!string.IsNullOrEmpty(adId.maxId) && !hasInit)
    {
      initedAdUnitIds.Add(adId.maxId);
      MaxSdk.AdViewPosition maxPosition = ConvertToMaxPosition(config.position);
      var adViewConfiguration = new MaxSdk.AdViewConfiguration(maxPosition);
      if (config.xPos != 0 || config.yPos != 0)
      {
        adViewConfiguration = new MaxSdk.AdViewConfiguration(config.xPos, config.yPos);
      }
      MaxSdk.CreateBanner(adId.maxId, adViewConfiguration);
      MaxSdk.SetBannerBackgroundColor(adId.maxId, new Color(0f, 0f, 0f, 0.2f));

      hasBannerInit[adId.maxId] = true;

      DualHelper.Log("MAXMediationController create & load new banner");
    }
    else
    {
      DualHelper.Log("MAXMediationController banner has been inited");
    }
  }

  public override void LoadBanner(DualAdId adId)
  {
    if (!string.IsNullOrEmpty(adId.maxId))
    {
      MaxSdk.LoadBanner(adId.maxId);
    }

    DualHelper.Log("MAXMediationController.LoadBanner()");
  }

  private void OnBannerAdLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
  {
    adListener?.OnAdLoaded(
      adUnitId: adUnitId,
      format: AdFormat.Banner,
      mediationKey: mediationKey,
      extras: new Dictionary<string, object>
      {
        { DualConst.KEY_AD_INFO, adInfo }
      });
  }

  private void OnBannerAdLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
  {
    adListener?.OnAdFailedToLoad(
      adUnitId: adUnitId,
      format: AdFormat.Banner,
      mediationKey: mediationKey,
      extras: new Dictionary<string, object> {
        { DualConst.KEY_ERROR_INFO, errorInfo }
      }
    );
  }

  public override void ShowBanner(DualAdId adId)
  {
    if (!string.IsNullOrEmpty(adId.maxId))
    {
      MaxSdk.ShowBanner(adId.maxId);
    }

    DualHelper.Log("MAXMediationController.ShowBanner()");
  }

  private void OnBannerAdClicked(string adUnitId, MaxSdkBase.AdInfo adInfo)
  {
    adListener?.OnAdClicked(
      adUnitId: adUnitId,
      format: AdFormat.Banner,
      mediationKey: mediationKey,
      extras: new Dictionary<string, object>
      {
        { DualConst.KEY_AD_INFO, adInfo }
      }
    );
  }

  private void OnBannerAdRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
  {
    OnAdRevenueReceived(adInfo, new Dictionary<string, object>());
  }

  public override void HideBanner(DualAdId adId)
  {
    if (!string.IsNullOrEmpty(adId.maxId))
    {
      MaxSdk.HideBanner(adId.maxId);
    }

    DualHelper.Log("MAXMediationController.HideBanner()");
  }
  public override void DestroyBanner(DualAdId adId)
  {
    if (!string.IsNullOrEmpty(adId.maxId))
    {
      MaxSdk.DestroyBanner(adId.maxId);
      hasBannerInit[adId.maxId] = false;
    }

    DualHelper.Log("MAXMediationController.DestroyBanner()");
  }

  public override void InitMrec(DualAdId adId, DualBannerConfig config)
  {
    hasMrecInit.TryGetValue(adId.maxId, out bool hasInit);

    if (!string.IsNullOrEmpty(adId.maxId) && !hasInit)
    {
      initedAdUnitIds.Add(adId.maxId);
      MaxSdk.AdViewPosition maxPosition = ConvertToMaxPosition(config.position);
      var adViewConfiguration = new MaxSdk.AdViewConfiguration(maxPosition);
      if (config.xPos != 0 || config.yPos != 0)
      {
        adViewConfiguration = new MaxSdk.AdViewConfiguration(config.xPos, config.yPos);
      }
      MaxSdk.CreateMRec(adId.maxId, adViewConfiguration);

      hasMrecInit[adId.maxId] = true;

      DualHelper.Log("MAXMediationController create & load new MREC");
    }
    else
    {
      DualHelper.Log("MAXMediationController MREC has been inited");
    }
  }

  public override void LoadMrec(DualAdId adId)
  {
    if (!string.IsNullOrEmpty(adId.maxId))
    {
      MaxSdk.LoadMRec(adId.maxId);
    }

    DualHelper.Log("MAXMediationController.LoadMrec()");
  }

  private void OnMrecAdLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
  {
    adListener?.OnAdLoaded(
      adUnitId: adUnitId,
      format: AdFormat.Mrec,
      mediationKey: mediationKey,
      extras: new Dictionary<string, object>
      {
        { DualConst.KEY_AD_INFO, adInfo }
      }
    );
  }

  private void OnMrecAdLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
  {
    adListener?.OnAdFailedToLoad(
      adUnitId: adUnitId,
      format: AdFormat.Mrec,
      mediationKey: mediationKey,
      extras: new Dictionary<string, object> {
        { DualConst.KEY_ERROR_INFO, errorInfo }
      }
    );
  }

  public override void ShowMrec(DualAdId adId)
  {
    if (!string.IsNullOrEmpty(adId.maxId))
    {
      MaxSdk.ShowMRec(adId.maxId);
    }

    DualHelper.Log("MAXMediationController.ShowMrec()");
  }

  private void OnMrecAdClicked(string adUnitId, MaxSdkBase.AdInfo adInfo)
  {
    adListener?.OnAdClicked(
      adUnitId: adUnitId,
      format: AdFormat.Mrec,
      mediationKey: mediationKey,
      extras: new Dictionary<string, object>
      {
        { DualConst.KEY_AD_INFO, adInfo }
      });
  }

  private void OnMrecAdRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
  {
    OnAdRevenueReceived(adInfo, new Dictionary<string, object>());
  }

  public override void HideMrec(DualAdId adId)
  {
    if (!string.IsNullOrEmpty(adId.maxId))
    {
      MaxSdk.HideMRec(adId.maxId);
    }

    DualHelper.Log("MAXMediationController.HideMrec()");
  }
  public override void DestroyMrec(DualAdId adId)
  {
    if (!string.IsNullOrEmpty(adId.maxId))
    {
      MaxSdk.DestroyMRec(adId.maxId);
      hasMrecInit[adId.maxId] = false;
    }

    DualHelper.Log("MAXMediationController.DestroyMrec()");
  }

  private void RegisterInterstitialCallbacks()
  {
    MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialAdLoaded;
    MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialAdLoadFailed;
    MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialAdDisplayed;
    MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialAdHidden;
    MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialAdClicked;
    MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialAdDisplayFailed;
    MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialAdRevenuePaid;
  }

  private void UnregisterInterstitialCallbacks()
  {
    MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnInterstitialAdLoaded;
    MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnInterstitialAdLoadFailed;
    MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= OnInterstitialAdDisplayed;
    MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnInterstitialAdHidden;
    MaxSdkCallbacks.Interstitial.OnAdClickedEvent -= OnInterstitialAdClicked;
    MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnInterstitialAdDisplayFailed;
    MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent -= OnInterstitialAdRevenuePaid;
  }

  private void RegisterRewardedCallbacks()
  {
    MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoaded;
    MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdLoadFailed;
    MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayed;
    MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdHidden;
    MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClicked;
    MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdDisplayFailed;
    MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedReward;
    MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaid;
  }

  private void UnregisterRewardedCallbacks()
  {
    MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= OnRewardedAdLoaded;
    MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= OnRewardedAdLoadFailed;
    MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent -= OnRewardedAdDisplayed;
    MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnRewardedAdHidden;
    MaxSdkCallbacks.Rewarded.OnAdClickedEvent -= OnRewardedAdClicked;
    MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnRewardedAdDisplayFailed;
    MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnRewardedAdReceivedReward;
    MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= OnRewardedAdRevenuePaid;
  }

  private void RegisterAppOpenCallbacks()
  {
    MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnAppOpenAdLoaded;
    MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnAppOpenAdLoadFailed;
    MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnAppOpenAdDisplayed;
    MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenAdHidden;
    MaxSdkCallbacks.AppOpen.OnAdClickedEvent += OnAppOpenAdClicked;
    MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAppOpenAdDisplayFailed;
    MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAppOpenAdRevenuePaid;
  }

  private void UnregisterAppOpenCallbacks()
  {
    MaxSdkCallbacks.AppOpen.OnAdLoadedEvent -= OnAppOpenAdLoaded;
    MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent -= OnAppOpenAdLoadFailed;
    MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent -= OnAppOpenAdDisplayed;
    MaxSdkCallbacks.AppOpen.OnAdHiddenEvent -= OnAppOpenAdHidden;
    MaxSdkCallbacks.AppOpen.OnAdClickedEvent -= OnAppOpenAdClicked;
    MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent -= OnAppOpenAdDisplayFailed;
    MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent -= OnAppOpenAdRevenuePaid;
  }

  private void RegisterBannerCallbacks()
  {
    MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoaded;
    MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdLoadFailed;
    MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClicked;
    MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerAdRevenuePaid;
  }

  private void UnregisterBannerCallbacks()
  {
    MaxSdkCallbacks.Banner.OnAdLoadedEvent -= OnBannerAdLoaded;
    MaxSdkCallbacks.Banner.OnAdLoadFailedEvent -= OnBannerAdLoadFailed;
    MaxSdkCallbacks.Banner.OnAdClickedEvent -= OnBannerAdClicked;
    MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent -= OnBannerAdRevenuePaid;
  }

  private void RegisterMrecCallbacks()
  {
    MaxSdkCallbacks.MRec.OnAdLoadedEvent += OnMrecAdLoaded;
    MaxSdkCallbacks.MRec.OnAdLoadFailedEvent += OnMrecAdLoadFailed;
    MaxSdkCallbacks.MRec.OnAdClickedEvent += OnMrecAdClicked;
    MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnMrecAdRevenuePaid;
  }

  private void UnregisterMrecCallbacks()
  {
    MaxSdkCallbacks.MRec.OnAdLoadedEvent -= OnMrecAdLoaded;
    MaxSdkCallbacks.MRec.OnAdLoadFailedEvent -= OnMrecAdLoadFailed;
    MaxSdkCallbacks.MRec.OnAdClickedEvent -= OnMrecAdClicked;
    MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent -= OnMrecAdRevenuePaid;
  }

  private MaxSdkBase.AdViewPosition ConvertToMaxPosition(DualBannerConfig.Position position)
  {
    MaxSdk.AdViewPosition maxPosition = MaxSdkBase.AdViewPosition.BottomCenter;
    switch (position)
    {
      case DualBannerConfig.Position.TopLeft:
        maxPosition = MaxSdkBase.AdViewPosition.TopLeft;
        break;
      case DualBannerConfig.Position.TopCenter:
        maxPosition = MaxSdkBase.AdViewPosition.TopCenter;
        break;
      case DualBannerConfig.Position.TopRight:
        maxPosition = MaxSdkBase.AdViewPosition.TopRight;
        break;
      case DualBannerConfig.Position.CenterLeft:
        maxPosition = MaxSdkBase.AdViewPosition.CenterLeft;
        break;
      case DualBannerConfig.Position.Centered:
        maxPosition = MaxSdkBase.AdViewPosition.Centered;
        break;
      case DualBannerConfig.Position.CenterRight:
        maxPosition = MaxSdkBase.AdViewPosition.CenterRight;
        break;
      case DualBannerConfig.Position.BottomLeft:
        maxPosition = MaxSdkBase.AdViewPosition.BottomLeft;
        break;
      case DualBannerConfig.Position.BottomCenter:
        maxPosition = MaxSdkBase.AdViewPosition.BottomCenter;
        break;
      case DualBannerConfig.Position.BottomRight:
        maxPosition = MaxSdkBase.AdViewPosition.BottomRight;
        break;
    }

    return maxPosition;
  }

  private void OnAdRevenueReceived(
    MaxSdkBase.AdInfo adInfo,
    Dictionary<string, object> placementData
  )
  {
    if (adInfo == null) return;
    if (!initedAdUnitIds.Contains(adInfo.AdUnitIdentifier)) return;

    string refinedAdSource = adInfo.NetworkName ?? "";
    if (refinedAdSource.Contains("AdMob", StringComparison.OrdinalIgnoreCase))
    {
      refinedAdSource = "AdMob"; // "Admob", "admob", "ADMOB", "AdMoB", … are all ok
    }
    Dictionary<string, object> extras = new(placementData ?? new Dictionary<string, object>());

    HandleAdRevenuePaid(
      adPlatform: "MAX",
      adSource: refinedAdSource,
      adUnitId: adInfo.AdUnitIdentifier,
      adFormat: adInfo.AdFormat,
      adValue: adInfo.Revenue,
      currencyCode: "USD",
      extras: extras,
      logBuiltInAdImpression: true
    );
  }
}
