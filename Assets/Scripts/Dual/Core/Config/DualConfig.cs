using System;

public class DualConfig
{
  public readonly IMediationRetriever mediationRetriever;
  public readonly IRemoteConfigRetriever remoteConfigRetriever;
  public readonly IAttributionRetriever attributionRetriever;

  public readonly IAnalyticsHandler analyticsHandler;
  public readonly IAdRevenueHandler adRevenueHandler;
  public readonly IAdListener adListener;
  public readonly int firstSyncDataWaitTimeSec;
  public readonly int resyncDataWaitTimeSec;
  public readonly string defaultMediation;

  public readonly bool autoLogMMPAdImpression;
  public readonly string customAdImpressionEventName;

  public DualConfig(
    IMediationRetriever mediationRetriever,
    IRemoteConfigRetriever remoteConfigRetriever,
    IAttributionRetriever attributionRetriever,
    IAnalyticsHandler analyticsHandler,
    IAdRevenueHandler adRevenueHandler,
    IAdListener adListener,
    int firstSyncDataWaitTimeSec,
    int resyncDataWaitTimeSec,
    string defaultMediation,
    bool autoLogMMPAdImpression,
    string customAdImpressionEventName
  )
  {
    this.mediationRetriever = mediationRetriever ?? throw new System.Exception("MediationRetriever must not be null");
    this.remoteConfigRetriever = remoteConfigRetriever ?? throw new System.Exception("RemoteConfigRetriever must not be null");
    this.attributionRetriever = attributionRetriever ?? throw new System.Exception("attributionRetriever must not be null");
    this.defaultMediation = defaultMediation ?? throw new System.Exception("defaultMediation must not be null");

    this.adRevenueHandler = adRevenueHandler;
    this.adListener = adListener;
    this.analyticsHandler = analyticsHandler;
    this.resyncDataWaitTimeSec = firstSyncDataWaitTimeSec;
    this.firstSyncDataWaitTimeSec = Math.Max(firstSyncDataWaitTimeSec, resyncDataWaitTimeSec);
    this.autoLogMMPAdImpression = autoLogMMPAdImpression;
    this.customAdImpressionEventName = customAdImpressionEventName;
  }

  public class Builder
  {
    private IMediationRetriever mediationRetriever;
    private IRemoteConfigRetriever remoteConfigRetriever;
    private IAttributionRetriever attributionRetriever;

    private IAnalyticsHandler analyticsHandler;

    private IAdRevenueHandler adRevenueHandler;
    private IAdListener adListener;
    private int firstSyncDataWaitTimeSec = 20;
    private int resyncDataWaitTimeSec = 3;
    private string defaultMediation;
    private bool autoLogMMPAdImpression = true;
    private string customAdImpressionEventName = "paid_ad_impression";

    public virtual Builder SetMediationRetriever(IMediationRetriever mediationRetriever)
    {
      this.mediationRetriever = mediationRetriever;
      return this;
    }

    public virtual Builder SetRemoteConfigRetriever(IRemoteConfigRetriever remoteConfigRetriever)
    {
      this.remoteConfigRetriever = remoteConfigRetriever;
      return this;
    }

    public virtual Builder SetAttributionRetriever(IAttributionRetriever attributionRetriever)
    {
      this.attributionRetriever = attributionRetriever;
      return this;
    }

    public virtual Builder SetAnalyticsHandler(IAnalyticsHandler analyticsHandler)
    {
      this.analyticsHandler = analyticsHandler;
      return this;
    }

    public virtual Builder SetAdRevenueHandler(IAdRevenueHandler adRevenueHandler)
    {
      this.adRevenueHandler = adRevenueHandler;
      return this;
    }

    public virtual Builder SetAdListener(IAdListener adListener)
    {
      this.adListener = adListener;
      return this;
    }

    public virtual Builder SetFirstSyncDataWaitTimeSec(int firstSyncDataWaitTimeSec)
    {
      this.firstSyncDataWaitTimeSec = firstSyncDataWaitTimeSec;
      return this;
    }

    public virtual Builder SetResyncDataWaitTimeSec(int resyncDataWaitTimeSec)
    {
      this.resyncDataWaitTimeSec = resyncDataWaitTimeSec;
      return this;
    }

    public virtual Builder SetDefaultMediation(string defaultMediation)
    {
      this.defaultMediation = defaultMediation;
      return this;
    }

    public virtual Builder SetAutoLogMMPAdImpression(bool autoLogMMPAdImpression)
    {
      this.autoLogMMPAdImpression = autoLogMMPAdImpression;
      return this;
    }

    public virtual Builder SetCustomAdImpressionEventName(string customAdImpressionEventName)
    {
      this.customAdImpressionEventName = customAdImpressionEventName;
      return this;
    }

    public DualConfig Build()
    {
      return new DualConfig(
        defaultMediation: defaultMediation,
        mediationRetriever: mediationRetriever,
        remoteConfigRetriever: remoteConfigRetriever,
        attributionRetriever: attributionRetriever,
        analyticsHandler: analyticsHandler,
        adRevenueHandler: adRevenueHandler,
        adListener: adListener,
        firstSyncDataWaitTimeSec: firstSyncDataWaitTimeSec,
        resyncDataWaitTimeSec: resyncDataWaitTimeSec,
        autoLogMMPAdImpression: autoLogMMPAdImpression,
        customAdImpressionEventName: customAdImpressionEventName
      );
    }
  }
}
