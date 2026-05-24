using System;
using System.Collections.Generic;
using Dual;

public abstract class BaseMediation : BaseAdFunction
{
  protected DualConfig dualConfig { get; private set; }
  protected readonly string mediationKey;

  protected BaseMediation(string mediationKey)
  {
    this.mediationKey = mediationKey;
  }

  public virtual void SetupBeforeInit(DualConfig dualConfig)
  {
    this.dualConfig = dualConfig;
  }
  public abstract void Init(Action onComplete);
  public abstract bool IsAdMobMediation();

  public virtual void HandleAdRevenuePaid(
    string adPlatform,
    string adSource,
    string adUnitId,
    string adFormat,
    double adValue,
    string currencyCode,
    Dictionary<string, object> extras,
    bool logBuiltInAdImpression
  )
  {
    if (dualConfig == null) return;

    if (logBuiltInAdImpression)
    {
      dualConfig.analyticsHandler?.LogEvent(
        eventName: "ad_impression",
        new Dictionary<string, object>
        {
          {"ad_platform", adPlatform},
          {"ad_source", adSource},
          {"ad_unit_id", adUnitId},
          {"ad_format", adFormat},
          {"value", adValue},
          {"currency", currencyCode}
        }
      );
    }

    if (!string.IsNullOrEmpty(dualConfig.customAdImpressionEventName))
    {
      dualConfig.analyticsHandler?.LogEvent(
        eventName: dualConfig.customAdImpressionEventName,
        param: new Dictionary<string, object>
        {
          {"ad_platform", adPlatform},
          {"ad_source", adSource},
          {"ad_unit_id", adUnitId},
          {"ad_format", adFormat},
          {"value", adValue},
          {"currency", currencyCode}
        }
      );
    }

    if (dualConfig.autoLogMMPAdImpression)
    {
      dualConfig.attributionRetriever.LogAdImpression(
        adPlatform: adPlatform,
        adSource: adSource,
        adUnitId: adUnitId,
        adFormat: adFormat,
        adValue: adValue,
        currencyCode: currencyCode
      );
    }

    dualConfig.adRevenueHandler?.OnAdRevenuePaid(
      mediationKey: mediationKey,
      adSource: adSource,
      adUnitId: adUnitId,
      adFormat: adFormat,
      adValue: adValue,
      currencyCode: currencyCode,
      extras: extras
    );

    // Log built-in events for tracking purpose
    // dual_ad_impression_d{X}_d{Y} -> Total ads by users who used the app for at least {X} (inclusive) days and no more than {Y} (exclusive) days
    // X = [3 7, 14, 21, 28, 49, 63, 77, 91, 105] - Up to 4 months

    var elapsedDaysFromFirstOpen = DualHelper.ConvertMillisecondsToDays(
      LocalStorageHelper.GetElapsedTimeFromFirstOpenMs()
    );
    var range = new int[] { 3, 7, 14, 21, 28, 49, 63, 77, 91, 105 };

    for (var i = 0; i < range.Length; i++)
    {
      var start = i == 0 ? 0 : range[i - 1];
      var end = range[i];

      if (elapsedDaysFromFirstOpen >= start && elapsedDaysFromFirstOpen < end)
      {
        dualConfig.analyticsHandler?.LogEvent(
          eventName: "dual_ad_impression_d" + start + "_d" + end,
          param: new Dictionary<string, object>
          {
            {"ad_platform", adPlatform},
            {"ad_source", adSource},
            {"ad_unit_id", adUnitId},
            {"ad_format", adFormat},
            {"value", adValue},
            {"currency", currencyCode}
          }
        );
      }
      else if (elapsedDaysFromFirstOpen >= end && i == range.Length - 1) // No matching any range and is the last item
      {
        dualConfig.analyticsHandler?.LogEvent(
          eventName: "dual_ad_impression_d" + end + "_onward",
          param: new Dictionary<string, object>
          {
            {"ad_platform", adPlatform},
            {"ad_source", adSource},
            {"ad_unit_id", adUnitId},
            {"ad_format", adFormat},
            {"value", adValue},
            {"currency", currencyCode}
          }
        );
      }
    }
  }
}

public class NoOpsMediationController : BaseMediation
{
  private bool isInit = false;

  public NoOpsMediationController() : base("no_ops")
  {
    DualHelper.Log("NoOpsMediationController inited !!! Please check your mediationKey");
  }

  public override void SetupBeforeInit(DualConfig dualConfig)
  {
  }

  public override bool IsAdMobMediation()
  {
    return false;
  }

  public override void Init(Action onComplete)
  {
    if (isInit) return;
    isInit = true;
    onComplete?.Invoke();
  }

  public override void LoadInterstitial(DualAdId adId) { }
  public override void ShowInterstitial(DualAdId adId, Action onComplete, Dictionary<string, object> placementData = null) { }
  public override bool IsInterstitialReady(DualAdId adId) { return false; }

  public override void LoadReward(DualAdId adId) { }
  public override void ShowReward(DualAdId adId, Action<bool> onComplete, Dictionary<string, object> placementData = null) { }
  public override bool IsRewardReady(DualAdId adId) { return false; }

  public override void LoadAppOpen(DualAdId adId) { }
  public override void ShowAppOpen(DualAdId adId, Action onComplete, Dictionary<string, object> placementData = null) { }
  public override bool IsAppOpenReady(DualAdId adId) { return false; }

  public override void InitBanner(DualAdId adId, DualBannerConfig config) { }
  public override void LoadBanner(DualAdId adId) { }
  public override void ShowBanner(DualAdId adId) { }
  public override void HideBanner(DualAdId adId) { }
  public override void DestroyBanner(DualAdId adId) { }
  public override void InitMrec(DualAdId adId, DualBannerConfig config) { }
  public override void LoadMrec(DualAdId adId) { }
  public override void ShowMrec(DualAdId adId) { }
  public override void HideMrec(DualAdId adId) { }
  public override void DestroyMrec(DualAdId adId) { }
}
