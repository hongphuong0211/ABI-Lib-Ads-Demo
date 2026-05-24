using System;
using System.Collections;
using System.Collections.Generic;
using Dual;
using UnityEngine;

public class DualAdManager : BaseAdFunction
{
  public static readonly bool LOG_ENABLE = true;
  public static readonly string LOG_TAG = "DUAL";
  private static DualAdManager instance = null;
  private static readonly object padlock = new();

  public static DualAdManager Instance
  {
    get
    {
      lock (padlock)
      {
        if (instance == null)
        {
          instance = new DualAdManager();
        }
        return instance;
      }
    }
  }

  private bool hasCallInit = false;
  private DualMediationController mediationController;

  public void Init(DualConfig config, Action callback)
  {
    if (hasCallInit)
    {
      DualHelper.Log("DualAdManager Init already called");
      return;
    }

    hasCallInit = true;

    LocalStorageHelper.SetFirstOpenTimeMsIfNeed();

    mediationController = new DualMediationController(dualConfig: config);
    mediationController.Init(callback);
  }

  public override void LoadInterstitial(DualAdId adId)
  {
    mediationController?.LoadInterstitial(adId);
  }
  public override void ShowInterstitial(
    DualAdId adId,
    Action onComplete = null,
    Dictionary<string, object> placementData = null
  )
  {
    mediationController?.ShowInterstitial(adId, onComplete, placementData);
  }
  public override bool IsInterstitialReady(DualAdId adId)
  {
    return mediationController?.IsInterstitialReady(adId) ?? false;
  }

  public override void LoadReward(DualAdId adId)
  {
    mediationController?.LoadReward(adId);
  }
  public override void ShowReward(
    DualAdId adId,
    Action<bool> onComplete = null,
    Dictionary<string, object> placementData = null
  )
  {
    mediationController?.ShowReward(adId, onComplete, placementData);
  }
  public override bool IsRewardReady(DualAdId adId)
  {
    return mediationController?.IsRewardReady(adId) ?? false;
  }

  public override void LoadAppOpen(DualAdId adId)
  {
    mediationController?.LoadAppOpen(adId);
  }
  public override void ShowAppOpen(
    DualAdId adId,
    Action onComplete = null,
    Dictionary<string, object> placementData = null
  )
  {
    mediationController?.ShowAppOpen(adId, onComplete, placementData);
  }
  public override bool IsAppOpenReady(DualAdId adId)
  {
    return mediationController?.IsAppOpenReady(adId) ?? false;
  }
  public override void InitBanner(DualAdId adId, DualBannerConfig config)
  {
    mediationController?.InitBanner(adId, config);
  }
  public override void LoadBanner(DualAdId adId)
  {
    mediationController?.LoadBanner(adId);
  }
  public override void ShowBanner(DualAdId adId)
  {
    mediationController?.ShowBanner(adId);
  }
  public override void HideBanner(DualAdId adId)
  {
    mediationController?.HideBanner(adId);
  }
  public override void DestroyBanner(DualAdId adId)
  {
    mediationController?.DestroyBanner(adId);
  }
  public override void InitMrec(DualAdId adId, DualBannerConfig config)
  {
    mediationController?.InitMrec(adId, config);
  }
  public override void LoadMrec(DualAdId adId)
  {
    mediationController?.LoadMrec(adId);
  }
  public override void ShowMrec(DualAdId adId)
  {
    mediationController?.ShowMrec(adId);
  }
  public override void HideMrec(DualAdId adId)
  {
    mediationController?.HideMrec(adId);
  }
  public override void DestroyMrec(DualAdId adId)
  {
    mediationController?.DestroyMrec(adId);
  }
}
