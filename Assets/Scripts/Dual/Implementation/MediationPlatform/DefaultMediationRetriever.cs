public class DefaultMediationRetriever : IMediationRetriever
{
  public BaseMediation GetMediationByKey(string key)
  {
    if (key == "max")
    {
      return new MAXMediationController(key);
    }
    else if (key == "admob")
    {
      return new AdMobMediationController(key);
    }
    else return null;
  }
}
