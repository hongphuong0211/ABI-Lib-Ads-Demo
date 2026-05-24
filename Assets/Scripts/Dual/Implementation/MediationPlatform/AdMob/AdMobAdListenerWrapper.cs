using System;
using System.Collections.Generic;
using Dual;
using GoogleMobileAds.Common;

class AdMobAdListenerWrapper : IAdListener
{
  private IAdListener adListener;
  private bool executeOnMainThread;

  public AdMobAdListenerWrapper(IAdListener adListener, bool executeOnMainThread)
  {
    this.adListener = adListener;
    this.executeOnMainThread = executeOnMainThread;
  }

  public override void OnAdLoaded(string adUnitId, AdFormat format, string mediationKey, Dictionary<string, object> extras)
  {
    Execute(() => { adListener.OnAdLoaded(adUnitId, format, mediationKey, extras); });
  }

  public override void OnAdFailedToLoad(string adUnitId, AdFormat format, string mediationKey, Dictionary<string, object> extras)
  {
    Execute(() => { adListener.OnAdFailedToLoad(adUnitId, format, mediationKey, extras); });
  }

  public override void OnAdDisplayed(string adUnitId, AdFormat format, string mediationKey, Dictionary<string, object> extras)
  {
    Execute(() => { adListener.OnAdDisplayed(adUnitId, format, mediationKey, extras); });
  }

  public override void OnAdClicked(string adUnitId, AdFormat format, string mediationKey, Dictionary<string, object> extras)
  {
    Execute(() => { adListener.OnAdClicked(adUnitId, format, mediationKey, extras); });
  }

  public override void OnAdHidden(string adUnitId, AdFormat format, string mediationKey, Dictionary<string, object> extras)
  {
    Execute(() => { adListener.OnAdHidden(adUnitId, format, mediationKey, extras); });
  }

  public override void OnAdDisplayFailed(string adUnitId, AdFormat format, string mediationKey, Dictionary<string, object> extras)
  {
    Execute(() => { adListener.OnAdDisplayFailed(adUnitId, format, mediationKey, extras); });
  }

  public override void OnAdReceivedReward(string adUnitId, AdFormat format, string mediationKey, Dictionary<string, object> extras)
  {
    Execute(() => { adListener.OnAdReceivedReward(adUnitId, format, mediationKey, extras); });
  }

  private void Execute(Action action)
  {
    if (executeOnMainThread)
    {
      MobileAdsEventExecutor.ExecuteInUpdate(() =>
      {
        action?.Invoke();
      });
    }
    else
    {
      action?.Invoke();
    }
  }
}
