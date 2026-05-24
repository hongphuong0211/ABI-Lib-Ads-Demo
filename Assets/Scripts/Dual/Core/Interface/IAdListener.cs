using System.Collections.Generic;
using Dual;

public abstract class IAdListener
{
  public virtual void OnAdLoaded(string adUnitId, AdFormat format, string mediationKey, Dictionary<string, object> extras) { }

  public virtual void OnAdFailedToLoad(string adUnitId, AdFormat format, string mediationKey, Dictionary<string, object> extras) { }

  public virtual void OnAdDisplayed(string adUnitId, AdFormat format, string mediationKey, Dictionary<string, object> extras) { }

  public virtual void OnAdClicked(string adUnitId, AdFormat format, string mediationKey, Dictionary<string, object> extras) { }

  public virtual void OnAdHidden(string adUnitId, AdFormat format, string mediationKey, Dictionary<string, object> extras) { }

  public virtual void OnAdDisplayFailed(string adUnitId, AdFormat format, string mediationKey, Dictionary<string, object> extras) { }

  public virtual void OnAdReceivedReward(string adUnitId, AdFormat format, string mediationKey, Dictionary<string, object> extras) { }
}
