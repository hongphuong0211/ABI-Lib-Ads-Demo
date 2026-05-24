using System;
using System.Collections.Generic;
using Dual;

public interface IAttributionRetriever
{
  public bool IsComponentReady();
  public bool IsFinishFetchingUASource();
  public string GetUASource();
  public void LogAdImpression(
    string adPlatform,
    string adSource,
    string adUnitId,
    string adFormat,
    double adValue,
    string currencyCode
  );
}
