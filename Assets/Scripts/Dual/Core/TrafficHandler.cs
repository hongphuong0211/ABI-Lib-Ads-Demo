using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dual;
using Unity.VisualScripting;
using UnityEngine;

public class TrafficHandler
{
  private readonly DualConfig dualConfig;
  private readonly string defaultMediationKey;
  private readonly IMediationRetriever mediationRetriever;
  private Dictionary<string, BaseMediation> mediationByKey;
  private Dictionary<AdFormat, BaseMediation> mediationsByFormat;
  public string UaSourceData { get; private set; } = "";
  public string RcJsonString { get; private set; } = "";

  public TrafficHandler(DualConfig dualConfig)
  {
    this.dualConfig = dualConfig;
    defaultMediationKey = dualConfig.defaultMediation;
    mediationRetriever = dualConfig.mediationRetriever;
  }

  public void SetupMediations()
  {
    UaSourceData = LocalStorageHelper.GetLocalUASource();
    RcJsonString = LocalStorageHelper.GetLocalRemoteConfigJsonString();

    DualHelper.Log("RCJsonString = " + RcJsonString);

    RCMediationConfig remoteMediationConfig;
    try
    {
      remoteMediationConfig = JsonUtility.FromJson<RCMediationConfig>(RcJsonString);
    }
    catch (Exception e)
    {
      DualHelper.Log("Exception when parsing config, message: " + e.Message);
      remoteMediationConfig = JsonUtility.FromJson<RCMediationConfig>("{}");
    }

    mediationsByFormat = new();
    mediationByKey = new();

    var elapsedDaysFromFirstOpen = DualHelper.ConvertMillisecondsToDays(
      LocalStorageHelper.GetElapsedTimeFromFirstOpenMs()
    );

    List<TrafficRule> rules = remoteMediationConfig.trafficRules ?? new List<TrafficRule>();
    string defaultKey = string.IsNullOrEmpty(remoteMediationConfig.defaultMediation) ? defaultMediationKey : remoteMediationConfig.defaultMediation;

    DualHelper.Log("elapsedDaysFromFirstOpen = " + elapsedDaysFromFirstOpen);
    DualHelper.Log("uaSourceData = " + UaSourceData);

    foreach (TrafficRule rule in rules)
    {
      if (HasFoundAllMediationForFormat())
      {
        break;
      }

      if (rule == null) continue;
      if (string.IsNullOrEmpty(rule.mediation)) continue;

      // Check if existing rule can be matched
      var matched = false;
      var formats = Enum.GetValues(typeof(AdFormat));

      if (rule.condition == null) // No condition assume always matched
      {
        matched = true;
      }
      else
      {
        var condition = rule.condition;
        var uaChannels = condition.uaChannels;
        var formatRules = condition.formats;
        var daysAfterFirstOpen = condition.daysAfterFirstOpen;

        var uaChannelMatched = uaChannels?.Any() != true
                            || uaChannels?.Contains("*") == true
                            || uaChannels?.Contains(UaSourceData) == true;
        var timeMatched = elapsedDaysFromFirstOpen > daysAfterFirstOpen;

        if (uaChannelMatched && timeMatched)
        {
          if (formatRules == null || formatRules.Count == 0 || formatRules.Contains("*"))
          {
            // Apply to all formats, do nothing here
          }
          else
          {
            formats = condition.ConvertToAdFormats().ToArray();
          }
          matched = true;
        }
      }

      if (matched)
      {
        // Populate unassigned format with mediation
        var mediationKey = rule.mediation;
        BaseMediation mediation = GetOrPutFromCacheMediation(mediationKey);
        if (mediation == null) continue;

        foreach (AdFormat format in formats)
        {
          if (!mediationsByFormat.ContainsKey(format))
          {
            mediationsByFormat[format] = mediation;
          }
        }
      }
    }

    if (HasFoundAllMediationForFormat()) return;

    // Populate the remaining unassigned formats slot with defaultMediation
    BaseMediation defaultMediation = GetOrPutFromCacheMediation(defaultKey);
    if (defaultMediation == null) return;

    var adFormats = Enum.GetValues(typeof(AdFormat));
    foreach (AdFormat adFormat in adFormats)
    {
      if (!mediationsByFormat.ContainsKey(adFormat))
      {
        mediationsByFormat[adFormat] = defaultMediation;
      }
    }
  }

  private bool HasFoundAllMediationForFormat()
  {
    return mediationsByFormat.Count >= Enum.GetValues(typeof(AdFormat)).Length;
  }

  private BaseMediation GetOrPutFromCacheMediation(string key)
  {
    if (!mediationByKey.ContainsKey(key))
    {
      var newMediation = mediationRetriever.GetMediationByKey(key)
        ?? new NoOpsMediationController();
      mediationByKey[key] = newMediation;
      mediationByKey[key].SetupBeforeInit(dualConfig: dualConfig);
    }

    return mediationByKey[key];
  }

  public Dictionary<AdFormat, BaseMediation> GetMediationsByFormat()
  {
    return mediationsByFormat;
  }

  public Dictionary<string, BaseMediation> GetMediationByKey()
  {
    return mediationByKey;
  }
}
