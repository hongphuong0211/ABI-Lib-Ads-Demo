using System;
using UnityEngine;

class LocalStorageHelper
{
  public static readonly string KEY_LOCAL_DUAL_CONFIG = "dual_local_dual_config";
  public static readonly string KEY_LOCAL_UA_SOURCE = "dual_local_ua_source";
  public static readonly string KEY_FIRST_OPEN_TIME_MS = "first_open_time_ms";

  public static bool HasLocalRemoteConfigData()
  {
    return PlayerPrefs.HasKey(KEY_LOCAL_DUAL_CONFIG);
  }

  public static string GetLocalRemoteConfigJsonString()
  {
    return PlayerPrefs.GetString(KEY_LOCAL_DUAL_CONFIG, "{}");
  }

  public static void SetLocalDualConfigJsonString(string data)
  {
    var toSaveJson = data;
    if (data == null || data == "") toSaveJson = "{}";
    PlayerPrefs.SetString(KEY_LOCAL_DUAL_CONFIG, toSaveJson);
  }

  public static bool HasLocalUASource()
  {
    return PlayerPrefs.HasKey(KEY_LOCAL_UA_SOURCE);
  }

  public static string GetLocalUASource()
  {
    return PlayerPrefs.GetString(KEY_LOCAL_UA_SOURCE, "");
  }

  public static void SetLocalUASource(string data)
  {
    PlayerPrefs.SetString(KEY_LOCAL_UA_SOURCE, data);
  }

  public static void SetFirstOpenTimeMsIfNeed()
  {
    if (PlayerPrefs.HasKey(KEY_FIRST_OPEN_TIME_MS)) return;

    long timeSinceEpochMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    PlayerPrefs.SetString(KEY_FIRST_OPEN_TIME_MS, timeSinceEpochMs.ToString());
  }

  public static long GetElapsedTimeFromFirstOpenMs()
  {
    if (!PlayerPrefs.HasKey(KEY_FIRST_OPEN_TIME_MS))
    {
      SetFirstOpenTimeMsIfNeed();
      return 0L;
    }

    long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    long firstOpentimeSinceEpochMs = long.Parse(
      PlayerPrefs.GetString(KEY_FIRST_OPEN_TIME_MS, now.ToString())
    );

    return now - firstOpentimeSinceEpochMs;
  }
}
