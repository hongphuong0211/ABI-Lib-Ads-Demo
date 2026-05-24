using System;
using System.Collections.Generic;
using Dual;

[Serializable]
class RCMediationConfig
{
  public string defaultMediation;
  public List<TrafficRule> trafficRules;
}

[Serializable]
class TrafficRule
{
  public string mediation;
  public Condition condition;
}

[Serializable]
class Condition
{
  public const string FORMAT_INTER = "interstitial";
  public const string FORMAT_REWARD = "reward";
  public const string FORMAT_APPOPEN = "appopen";
  public const string FORMAT_BANNER = "banner";
  public const string FORMAT_MREC = "mrec";

  public List<string> formats;
  public List<string> uaChannels;
  public int daysAfterFirstOpen = -1;

  public List<AdFormat> ConvertToAdFormats()
  {
    List<AdFormat> output = new();
    HashSet<string> formatSet = new(formats);

    foreach (string item in formatSet)
    {
      var normalizedItem = item.Replace("_", "");

      if (string.Equals(normalizedItem, FORMAT_INTER, StringComparison.OrdinalIgnoreCase))
      {
        output.Add(AdFormat.Interstitial);
      }
      else if (string.Equals(normalizedItem, FORMAT_REWARD, StringComparison.OrdinalIgnoreCase))
      {
        output.Add(AdFormat.Reward);
      }
      else if (string.Equals(normalizedItem, FORMAT_APPOPEN, StringComparison.OrdinalIgnoreCase))
      {
        output.Add(AdFormat.AppOpen);
      }
      else if (string.Equals(normalizedItem, FORMAT_BANNER, StringComparison.OrdinalIgnoreCase))
      {
        output.Add(AdFormat.Banner);
      }
      else if (string.Equals(normalizedItem, FORMAT_MREC, StringComparison.OrdinalIgnoreCase))
      {
        output.Add(AdFormat.Mrec);
      }
    }

    return output;
  }
}
