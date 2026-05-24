using System.Collections;
using GoogleMobileAds.BannerRefresh;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BannerRefreshTimerTests
{
    [UnityTest]
    public IEnumerator TestIdleTime()
    {
        // Asserting that idle time counts correctly.
        var timer = new BannerRefreshTimer();
        timer.StartIdleTime();
        yield return new WaitForSeconds(2f);
        var idleTime = timer.FinalizeIdleTime();
        Assert.AreEqual(idleTime, 2);
        var idleTime2 = timer.FinalizeIdleTime();
        Assert.AreEqual(idleTime2, 0);
    }

    [UnityTest]
    public IEnumerator TestMultipleIdleTime()
    {
        var timer = new BannerRefreshTimer();
        // Asserting that rotation does not reset idle time.
        timer.StartIdleTime();
        yield return new WaitForSeconds(2f);
        timer.StartIdleTime();
        yield return new WaitForSeconds(2f);
        var idleTime = timer.FinalizeIdleTime();
        Assert.AreEqual(idleTime, 4);
        var idleTime2 = timer.FinalizeIdleTime();
        Assert.AreEqual(idleTime2, 0);
    }
}
