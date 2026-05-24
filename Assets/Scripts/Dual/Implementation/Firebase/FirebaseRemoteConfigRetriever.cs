using Dual;
using Firebase.Extensions;
using Firebase.RemoteConfig;

public class FirebaseRemoteConfigRetriever : IRemoteConfigRetriever
{
  private readonly string dualConfigRemoteKey;
  private bool hasCallInitFirebase = false;
  private bool isReady = false;
  private bool hasCallFetchRemoteConfig = false;
  private bool hasFinishFetchingRemoteConfig = false;

  public FirebaseRemoteConfigRetriever(string dualConfigRemoteKey)
  {
    this.dualConfigRemoteKey = dualConfigRemoteKey;
  }

  public bool GetBool(string key, bool defaultVal)
  {
    var value = FirebaseRemoteConfig.DefaultInstance.GetValue(key);

    if (value.Source == ValueSource.StaticValue) return defaultVal;
    else return value.BooleanValue;
  }

  public double GetDouble(string key, double defaultVal)
  {
    var value = FirebaseRemoteConfig.DefaultInstance.GetValue(key);

    if (value.Source == ValueSource.StaticValue) return defaultVal;
    else return value.DoubleValue;
  }

  public string GetDualConfigJsonString()
  {
    var value = FirebaseRemoteConfig.DefaultInstance.GetValue(dualConfigRemoteKey);

    if (value.Source == ValueSource.StaticValue) return "{}";
    else return value.StringValue;
  }

  public long GetLong(string key, long defaultVal)
  {
    var value = FirebaseRemoteConfig.DefaultInstance.GetValue(key);

    if (value.Source == ValueSource.StaticValue) return defaultVal;
    else return value.LongValue;
  }

  public string GetString(string key, string defaultVal)
  {
    var value = FirebaseRemoteConfig.DefaultInstance.GetValue(key);

    if (value.Source == ValueSource.StaticValue) return defaultVal;
    else return value.StringValue;
  }

  public bool IsComponentReady()
  {
    if (!hasCallInitFirebase)
    {
      hasCallInitFirebase = true;
      InitializeFirebaseAndRemoteConfig();
    }

    return isReady;
  }

  private void InitializeFirebaseAndRemoteConfig()
  {
    Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
    {
      var dependencyStatus = task.Result;
      if (dependencyStatus == Firebase.DependencyStatus.Available)
      {
        isReady = true;
      }
      else
      {
        DualHelper.Log($"Could not resolve all Firebase dependencies: {dependencyStatus}");
      }
    });
  }

  public bool IsFinishFetchingConfig()
  {
    if (isReady && !hasCallFetchRemoteConfig)
    {
      hasCallFetchRemoteConfig = true;
      FetchRemoteConfig();
    }
    return hasFinishFetchingRemoteConfig;
  }

  private void FetchRemoteConfig()
  {
    FirebaseRemoteConfig.DefaultInstance.FetchAndActivateAsync()
        .ContinueWithOnMainThread(task =>
        {
          hasFinishFetchingRemoteConfig = true;
        });
  }
}
