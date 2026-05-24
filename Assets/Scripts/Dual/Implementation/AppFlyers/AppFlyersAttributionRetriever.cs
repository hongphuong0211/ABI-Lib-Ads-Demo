using System.Collections.Generic;
using AppsFlyerSDK;
using Dual;
using UnityEngine;

public class AppFlyersAttributionRetriever : IAttributionRetriever
{
  private bool hasCallFetchConversionData = false;

  private static DualAppFlyersConversionDataReceiver conversionDataReceiver = null;

  public AppFlyersAttributionRetriever()
  {
    var handlerParent = new GameObject();
    conversionDataReceiver = handlerParent.AddComponent<DualAppFlyersConversionDataReceiver>();
    conversionDataReceiver.name = "AppFlyerConversionDataReceiver";
    Object.DontDestroyOnLoad(handlerParent);
  }

  public string GetUASource()
  {
    return conversionDataReceiver.mediaSource;
  }

  public bool IsComponentReady()
  {
    return AppsFlyer.instance?.isInit == true;
  }

  public bool IsFinishFetchingUASource()
  {
    if (!hasCallFetchConversionData)
    {
      hasCallFetchConversionData = true;
      AppsFlyer.getConversionData(conversionDataReceiver.name);
    }
    return conversionDataReceiver.hasFinishFetchingConversionData;
  }

  public void LogAdImpression(string adPlatform, string adSource, string adUnitId, string adFormat, double adValue, string currencyCode)
  {
    MediationNetwork mediationNetwork;
    if (adPlatform.Contains("AdMob", System.StringComparison.OrdinalIgnoreCase))
    {
      mediationNetwork = MediationNetwork.GoogleAdMob;
    }
    else if (adPlatform.Contains("Iron", System.StringComparison.OrdinalIgnoreCase))
    {
      mediationNetwork = MediationNetwork.IronSource;
    }
    else if (adPlatform.Contains("max", System.StringComparison.OrdinalIgnoreCase)
              || adPlatform.Contains("applovin", System.StringComparison.OrdinalIgnoreCase))
    {
      mediationNetwork = MediationNetwork.ApplovinMax;
    }
    else
    {
      mediationNetwork = MediationNetwork.Custom;
    }
    var adRevenueData = new AFAdRevenueData(
      monetization: adSource,
      mediation: mediationNetwork,
      currency: currencyCode,
      revenue: adValue
    );

    Dictionary<string, string> additionalParameters = new();
    additionalParameters["adUnitId"] = adUnitId;
    additionalParameters["adFormat"] = adFormat;

    AppsFlyer.logAdRevenue(
      adRevenueData: adRevenueData,
      additionalParameters: additionalParameters
    );
  }

  public class DualAppFlyersConversionDataReceiver : MonoBehaviour, IAppsFlyerConversionData
  {
    public bool hasFinishFetchingConversionData = false;
    public string mediaSource = "";

    public void onAppOpenAttribution(string attributionData)
    {
    }

    public void onAppOpenAttributionFailure(string error)
    {
    }

    public void onConversionDataFail(string error)
    {
      hasFinishFetchingConversionData = true;
    }

    public void onConversionDataSuccess(string conversionData)
    {
      hasFinishFetchingConversionData = true;

      // Parse the JSON string into a dictionary for easier access
      Dictionary<string, object> conversionDataDictionary = AppsFlyer.CallbackStringToDictionary(conversionData);

      // Access specific data points, e.g., 'af_status'
      if (conversionDataDictionary.ContainsKey("af_status"))
      {
        string status = conversionDataDictionary["af_status"].ToString();
        DualHelper.Log("Conversion Status: " + status);

        // Customize content based on 'af_status' or other campaign data
        if (status == "Non-organic")
        {
          // Handle non-organic install (e.g., provide welcome bonus)
          if (conversionDataDictionary.ContainsKey("media_source"))
          {
            mediaSource = conversionDataDictionary["media_source"].ToString();
          }
        }
        else
        {
          mediaSource = "organic";
        }
      }
      DualHelper.Log("AppsFlyer Conversion Data: " + conversionData);
    }
  }
}
