using System;
using System.Collections;
using System.Collections.Generic;
using Dual;
using Dual.CoroutineRunner;
using Unity.Collections;
using UnityEngine;

public class DualMediationController : BaseAdFunction
{
  private readonly DualConfig dualConfig;
  private readonly IRemoteConfigRetriever remoteConfigRetriever;
  private readonly IAnalyticsHandler analyticsHandler;
  private readonly IAttributionRetriever attributionRetriever;
  private readonly int resyncDataWaitTimeSec;
  private readonly int firstSyncDataWaitTimeSec;

  private bool uaSourceDone = false;
  private bool dualConfigDone = false;

  private bool hasCallInit = false;
  private bool initDone = false;

  private Dictionary<AdFormat, BaseMediation> mediations = new();

  public DualMediationController(DualConfig dualConfig)
  {
    this.dualConfig = dualConfig;
    remoteConfigRetriever = dualConfig.remoteConfigRetriever;
    analyticsHandler = dualConfig.analyticsHandler;
    attributionRetriever = dualConfig.attributionRetriever;
    resyncDataWaitTimeSec = dualConfig.resyncDataWaitTimeSec;
    firstSyncDataWaitTimeSec = dualConfig.firstSyncDataWaitTimeSec;
  }

  public void Init(Action onComplete)
  {
    if (hasCallInit) return;
    hasCallInit = true;

    // Start InitCoroutine() & WaitForDataCoroutine()
    CoroutineRunner.RunCoroutine(InitCoroutine(onComplete));
    CoroutineRunner.RunCoroutine(WaitForRemoteDataCoroutine());
  }

  private IEnumerator InitCoroutine(Action onComplete)
  {
    // Wait for maximum of maxRemoteDataWaitTimeSec
    // Or stop waiting until there is local data for UA & RemoteConfig
    var longCheckWait = new WaitForSecondsRealtime(0.5f);

    // If has cached, wait for resyncDataWaitTimeSec to attempt fetching new config
    // Otherwise wait for firstSyncDataWaitTimeSec to attempt fetching new config
    var waitTimeSec = (LocalStorageHelper.HasLocalRemoteConfigData()
      && LocalStorageHelper.HasLocalUASource()) ? resyncDataWaitTimeSec : firstSyncDataWaitTimeSec;
    var timeOut = Time.realtimeSinceStartup + waitTimeSec;

    while (!uaSourceDone || !dualConfigDone)
    {
      if (Time.realtimeSinceStartup > timeOut)
      {
        break;
      }
      yield return longCheckWait;
    }

    TrafficHandler trafficHandler = new(dualConfig: dualConfig);
    trafficHandler.SetupMediations();
    mediations = trafficHandler.GetMediationsByFormat();

    // Tagging user properties
    var uaSource = LocalStorageHelper.GetLocalUASource();
    if (!string.IsNullOrEmpty(uaSource))
    {
      analyticsHandler?.SetUserProperty(DualConst.PROPERTY_UA_SOURCE, uaSource);
    }

    var elapsedDaysFromFirstOpen = DualHelper.ConvertMillisecondsToDays(
      LocalStorageHelper.GetElapsedTimeFromFirstOpenMs()
    );
    analyticsHandler?.SetUserProperty(DualConst.PROPERTY_DAYS_SINCE_FIRST_OPEN, elapsedDaysFromFirstOpen.ToString());

    // Init mediation by order: AdMob always go first.
    Dictionary<string, BaseMediation> allMediations = trafficHandler.GetMediationByKey();
    List<BaseMediation> initQueue = new();
    foreach (string key in allMediations.Keys)
    {
      if (allMediations[key].IsAdMobMediation())
      {
        initQueue.Insert(0, allMediations[key]);
      }
      else
      {
        initQueue.Add(allMediations[key]);
      }
    }

    var quickCheckWait = new WaitForSecondsRealtime(0.1f);
    foreach (BaseMediation baseMediation in initQueue)
    {
      var isInitDone = false;
      baseMediation.Init(() =>
      {
        isInitDone = true;
      });

      while (!isInitDone)
      {
        yield return quickCheckWait;
      }
    }

    initDone = true;
    onComplete?.Invoke();
  }

  private IEnumerator WaitForRemoteDataCoroutine()
  {
    // Wait for UA & RemoteConfig data
    // Local data for UA and RemoteConfig will automatically overwriten after finish fetching
    var wait = new WaitForSecondsRealtime(1f);

    uaSourceDone = false;
    dualConfigDone = false;

    bool IsUAFetched()
    {
      return attributionRetriever.IsComponentReady() && attributionRetriever.IsFinishFetchingUASource();
    }

    bool IsDualConfigFetched()
    {
      return remoteConfigRetriever.IsComponentReady()
        && remoteConfigRetriever.IsFinishFetchingConfig();
    }

    while (!uaSourceDone || !dualConfigDone)
    {
      if (!uaSourceDone && IsUAFetched())
      {
        string source = attributionRetriever.GetUASource();

        if (!string.IsNullOrEmpty(source))
        {
          LocalStorageHelper.SetLocalUASource(source);
        }

        uaSourceDone = true;
      }
      if (!dualConfigDone && IsDualConfigFetched())
      {
        LocalStorageHelper.SetLocalDualConfigJsonString(remoteConfigRetriever.GetDualConfigJsonString());
        dualConfigDone = true;
      }
      yield return wait;
    }

    yield return null;
  }

  public override void LoadInterstitial(DualAdId adId)
  {
    if (!initDone) return;
    mediations[AdFormat.Interstitial].LoadInterstitial(adId);
  }
  public override void ShowInterstitial(
    DualAdId adId,
    Action onComplete,
    Dictionary<string, object> placementData = null
  )
  {
    if (!initDone)
    {
      onComplete?.Invoke();
      return;
    }

    mediations[AdFormat.Interstitial].ShowInterstitial(adId, onComplete, placementData);
  }
  public override bool IsInterstitialReady(DualAdId adId)
  {
    if (!initDone) return false;
    return mediations[AdFormat.Interstitial].IsInterstitialReady(adId);
  }

  public override void LoadReward(DualAdId adId)
  {
    if (!initDone) return;
    mediations[AdFormat.Reward].LoadReward(adId);
  }
  public override void ShowReward(
    DualAdId adId,
    Action<bool> onComplete,
    Dictionary<string, object> placementData = null
  )
  {
    if (!initDone)
    {
      onComplete?.Invoke(false);
      return;
    }
    mediations[AdFormat.Reward].ShowReward(adId, onComplete, placementData);
  }
  public override bool IsRewardReady(DualAdId adId)
  {
    if (!initDone) return false;
    return mediations[AdFormat.Reward].IsRewardReady(adId);
  }

  public override void LoadAppOpen(DualAdId adId)
  {
    if (!initDone) return;
    mediations[AdFormat.AppOpen].LoadAppOpen(adId);
  }
  public override void ShowAppOpen(
    DualAdId adId,
    Action onComplete,
    Dictionary<string, object> placementData = null
  )
  {
    if (!initDone)
    {
      onComplete?.Invoke();
      return;
    }
    mediations[AdFormat.AppOpen].ShowAppOpen(adId, onComplete, placementData);
  }
  public override bool IsAppOpenReady(DualAdId adId)
  {
    if (!initDone) return false;
    return mediations[AdFormat.AppOpen].IsAppOpenReady(adId);
  }

  public override void InitBanner(DualAdId adId, DualBannerConfig config)
  {
    if (!initDone) return;
    mediations[AdFormat.Banner].InitBanner(adId, config);
  }

  public override void LoadBanner(DualAdId adId)
  {
    if (!initDone) return;
    mediations[AdFormat.Banner].LoadBanner(adId);
  }
  public override void ShowBanner(DualAdId adId)
  {
    if (!initDone) return;
    mediations[AdFormat.Banner].ShowBanner(adId);
  }
  public override void HideBanner(DualAdId adId)
  {
    if (!initDone) return;
    mediations[AdFormat.Banner].HideBanner(adId);
  }
  public override void DestroyBanner(DualAdId adId)
  {
    if (!initDone) return;
    mediations[AdFormat.Banner].DestroyBanner(adId);
  }
  public override void InitMrec(DualAdId adId, DualBannerConfig config)
  {
    if (!initDone) return;
    mediations[AdFormat.Mrec].InitMrec(adId, config);
  }
  public override void LoadMrec(DualAdId adId)
  {
    if (!initDone) return;
    mediations[AdFormat.Mrec].LoadMrec(adId);
  }
  public override void ShowMrec(DualAdId adId)
  {
    if (!initDone) return;
    mediations[AdFormat.Mrec].ShowMrec(adId);
  }
  public override void HideMrec(DualAdId adId)
  {
    if (!initDone) return;
    mediations[AdFormat.Mrec].HideMrec(adId);
  }
  public override void DestroyMrec(DualAdId adId)
  {
    if (!initDone) return;
    mediations[AdFormat.Mrec].DestroyMrec(adId);
  }
}
