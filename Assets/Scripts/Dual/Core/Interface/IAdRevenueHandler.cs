using System.Collections;
using System.Collections.Generic;
using Dual;
using UnityEngine;

public interface IAdRevenueHandler
{
  public void OnAdRevenuePaid(
    string mediationKey,
    string adSource,
    string adUnitId,
    string adFormat,
    double adValue,
    string currencyCode,
    Dictionary<string, object> extras
  )
  { }
}
