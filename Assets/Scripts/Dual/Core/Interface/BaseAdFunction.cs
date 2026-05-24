using System;
using System.Collections.Generic;

public abstract class BaseAdFunction
{
  public abstract void LoadInterstitial(DualAdId adId);
  public abstract void ShowInterstitial(
    DualAdId adId,
    Action onComplete = null,
    Dictionary<string, object> placementData = null
  );
  public abstract bool IsInterstitialReady(DualAdId adId);

  public abstract void LoadReward(DualAdId adId);
  public abstract void ShowReward(
    DualAdId adId,
    Action<bool> onComplete = null,
    Dictionary<string, object> placementData = null
  );
  public abstract bool IsRewardReady(DualAdId adId);

  public abstract void LoadAppOpen(DualAdId adId);
  public abstract void ShowAppOpen(
    DualAdId adId,
    Action onComplete = null,
    Dictionary<string, object> placementData = null
  );
  public abstract bool IsAppOpenReady(DualAdId adId);

  public abstract void InitBanner(DualAdId adId, DualBannerConfig config);
  public abstract void LoadBanner(DualAdId adId);
  public abstract void ShowBanner(DualAdId adId);
  public abstract void HideBanner(DualAdId adId);
  public abstract void DestroyBanner(DualAdId adId);

  public abstract void InitMrec(DualAdId adId, DualBannerConfig config);
  public abstract void LoadMrec(DualAdId adId);
  public abstract void ShowMrec(DualAdId adId);
  public abstract void HideMrec(DualAdId adId);
  public abstract void DestroyMrec(DualAdId adId);
}
