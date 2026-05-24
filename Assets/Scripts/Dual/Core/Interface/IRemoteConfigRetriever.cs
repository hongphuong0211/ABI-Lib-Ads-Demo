using System;

public interface IRemoteConfigRetriever
{
  public bool IsComponentReady();
  public bool IsFinishFetchingConfig();
  public string GetDualConfigJsonString();
  public string GetString(string key, string defaultVal);
  public bool GetBool(string key, bool defaultVal);
  public double GetDouble(string key, double defaultVal);
  public long GetLong(string key, long defaultVal);
}
