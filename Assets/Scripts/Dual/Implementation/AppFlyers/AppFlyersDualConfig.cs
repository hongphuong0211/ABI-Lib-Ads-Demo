using Dual;

public class AppFlyersDualConfig
{
  public class Builder : DualConfig.Builder
  {
    public Builder()
    {
      base.SetMediationRetriever(new DefaultMediationRetriever());
      base.SetRemoteConfigRetriever(new FirebaseRemoteConfigRetriever("dual_mediation_config"));
      base.SetAttributionRetriever(new AppFlyersAttributionRetriever());
      base.SetAnalyticsHandler(new FirebaseAnalyticsHandler());
    }

    public override DualConfig.Builder SetMediationRetriever(IMediationRetriever mediationRetriever)
    {
      DualHelper.Log("SetMediationRetriever is not support for this Builder");
      return this;
    }

    public override DualConfig.Builder SetRemoteConfigRetriever(IRemoteConfigRetriever remoteConfigRetriever)
    {
      DualHelper.Log("SetRemoteConfigRetriever is not support for this Builder");
      return this;
    }

    public override DualConfig.Builder SetAttributionRetriever(IAttributionRetriever attributionRetriever)
    {
      DualHelper.Log("SetAttributionRetriever is not support for this Builder");
      return this;
    }

    public override DualConfig.Builder SetAnalyticsHandler(IAnalyticsHandler analyticsHandler)
    {
      DualHelper.Log("SetAnalyticsHandler is not support for this Builder");
      return this;
    }
  }
}
