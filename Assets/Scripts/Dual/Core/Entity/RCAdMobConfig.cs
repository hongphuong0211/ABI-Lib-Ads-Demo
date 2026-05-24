using System;
using System.Collections.Generic;

[Serializable]
class RCAdMobConfig
{
  public int baseBannerRefreshRate;
  public List<AdMobRefreshRateByAdSource> bannerRefreshRateByAdSource;

  public int baseMrecRefreshRate;
  public List<AdMobRefreshRateByAdSource> mrecRefreshRateByAdSource;

  public int interBufferSize;
  public int rewardBufferSize;
}

[Serializable]
class AdMobRefreshRateByAdSource
{
  public string sourceId;
  public int refreshRate;
}
