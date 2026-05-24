using System;
using UnityEngine;

public class FsnAd
{
  private AndroidJavaObject androidFsnAd;
  private AndroidOnFsnLoadListener onFsnLoadListener;
  private AndroidOnFsnCompletedListener onFsnCompletedListener;

  public FsnAd()
  {
    androidFsnAd = new AndroidJavaObject("com.ads.fsnforunity.fsnlib.FsnAd");
  }

  public void Init(string adUnitId, int countDownSec, bool loadNewAfterClose, bool startMuteVideo, bool autoDismissWhenAdClicked)
  {
    if (Application.platform == RuntimePlatform.Android)
    {

      object[] parameters = new object[5];
      parameters[0] = adUnitId;
      parameters[1] = countDownSec;
      parameters[2] = loadNewAfterClose;
      parameters[3] = startMuteVideo;
      parameters[4] = autoDismissWhenAdClicked;

      androidFsnAd?.Call("Init", parameters);
    }
    else
    {
      Debug.Log("Init() only supports Android for now ...");
    }
  }

  public void SetFsnListener(Action<int, string> onLoadingCompleted, Action onLoadingStarted, Action<string, string, double, string> onAdPaid)
  {
    if (Application.platform == RuntimePlatform.Android)
    {

      onFsnLoadListener = new AndroidOnFsnLoadListener(
        onLoadingCompleted, onLoadingStarted, onAdPaid
      );

      object[] parameters = new object[1];
      parameters[0] = onFsnLoadListener;

      androidFsnAd?.Call("SetFsnListener", parameters);
    }
    else
    {
      Debug.Log("SetFsnListener() only supports Android for now ...");
    }
  }

  public void LoadAd()
  {
    if (Application.platform == RuntimePlatform.Android)
    {
      Debug.Log("LoadAd()");

      AndroidJavaClass unityPlayerClass = new("com.unity3d.player.UnityPlayer");
      AndroidJavaObject unityActivity = unityPlayerClass
        .GetStatic<AndroidJavaObject>("currentActivity");

      object[] parameters = new object[1];
      parameters[0] = unityActivity;

      androidFsnAd?.Call("LoadAd", parameters);

    }
    else
    {
      Debug.Log("LoadAd() only supports Android for now ...");
    }
  }

  public void ShowAd(Action<string> onAdCompleted)
  {
    if (Application.platform == RuntimePlatform.Android)
    {
      Debug.Log("ShowAd()");

      AndroidJavaClass unityPlayerClass = new("com.unity3d.player.UnityPlayer");
      AndroidJavaObject unityActivity = unityPlayerClass
        .GetStatic<AndroidJavaObject>("currentActivity");

      onFsnCompletedListener = new AndroidOnFsnCompletedListener(onAdCompleted);

      object[] parameters = new object[2];
      parameters[0] = unityActivity;
      parameters[1] = onFsnCompletedListener;

      androidFsnAd?.Call("ShowAd", parameters);
    }
    else
    {
      Debug.Log("ShowAd() only supports Android for now ...");
      onAdCompleted?.Invoke("");
    }
  }

  public bool IsAdReady()
  {
    if (Application.platform == RuntimePlatform.Android)
    {

      return androidFsnAd?.Call<bool>("IsAdReady") ?? false;
    }
    else
    {
      Debug.Log("IsAdReady() only supports Android for now ...");
      return false;
    }
  }

  public bool IsAdLoading()
  {
    if (Application.platform == RuntimePlatform.Android)
    {
      return androidFsnAd?.Call<bool>("IsAdLoading") ?? false;
    }
    else
    {
      Debug.Log("IsAdLoading() only supports Android for now ...");
      return false;
    }
  }

  public void Release()
  {
    if (Application.platform == RuntimePlatform.Android)
    {
      androidFsnAd?.Dispose();
      androidFsnAd = null;

      onFsnLoadListener = null;
      onFsnCompletedListener = null;
    }
    else
    {
      Debug.Log("Release() only supports Android for now ...");
    }
  }

  class AndroidOnFsnLoadListener : AndroidJavaProxy
  {
    private readonly Action<int, string> onLoadingCompleted;
    private readonly Action onLoadingStarted;
    private readonly Action<string, string, double, string> onAdPaid;

    public AndroidOnFsnLoadListener(Action<int, string> onLoadingCompleted, Action onLoadingStarted, Action<string, string, double, string> onAdPaid) : base("com.ads.fsnforunity.fsnlib.OnFsnLoadListener")
    {
      this.onLoadingCompleted = onLoadingCompleted;
      this.onLoadingStarted = onLoadingStarted;
      this.onAdPaid = onAdPaid;
    }

    // Implement the Java interface methods
    public void OnLoadingCompleted(int errorCode, string errorMessage)
    {
      onLoadingCompleted?.Invoke(errorCode, errorMessage);
    }

    public void OnLoadingStarted()
    {
      onLoadingStarted?.Invoke();
    }

    public void OnAdPaid(string adSource, string adUnitId, double valueMicros, string currencyCode)
    {
      onAdPaid?.Invoke(adSource, adUnitId, valueMicros, currencyCode);
    }
  }

  class AndroidOnFsnCompletedListener : AndroidJavaProxy
  {
    private readonly Action<string> onAdCompleted;

    public AndroidOnFsnCompletedListener(Action<string> onAdCompleted) : base("com.ads.fsnforunity.fsnlib.OnFsnCompletedListener")
    {
      this.onAdCompleted = onAdCompleted;
    }

    // Implement the Java interface methods
    public void OnAdCompleted(string errorMessage)
    {
      onAdCompleted?.Invoke(errorMessage);
    }
  }
}
