using System.Collections.Generic;

public interface IAnalyticsHandler
{
  void LogEvent(string eventName, Dictionary<string, object> param);
  void SetUserProperty(string key, string value);
}
