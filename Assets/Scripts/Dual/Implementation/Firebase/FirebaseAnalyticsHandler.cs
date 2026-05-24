using System.Collections.Generic;
using Firebase.Analytics;

public class FirebaseAnalyticsHandler : IAnalyticsHandler
{
  public void LogEvent(string eventName, Dictionary<string, object> param)
  {
    if (param == null)
    {
      FirebaseAnalytics.LogEvent(eventName);
    }
    else
    {
      List<Parameter> firebaseParam = new();
      foreach (string key in param.Keys)
      {
        var value = param[key];
        if (value is long || value is int)
        {
          firebaseParam.Add(new Parameter(key, (long)value));
        }
        else if (value is double || value is float)
        {
          firebaseParam.Add(new Parameter(key, (double)value));
        }
        else
        {
          firebaseParam.Add(new Parameter(key, value.ToString()));
        }
      }

      FirebaseAnalytics.LogEvent(eventName, firebaseParam.ToArray());
    }
  }

  public void SetUserProperty(string key, string value)
  {
    FirebaseAnalytics.SetUserProperty(key, value);
  }
}
