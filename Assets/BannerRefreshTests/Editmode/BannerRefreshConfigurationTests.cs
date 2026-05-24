using NUnit.Framework;
using GoogleMobileAds.BannerRefresh;

public class BannerRefreshConfigurationTests
{
    [Test]
    public void RefreshRate()
    {
        var config = new BannerRefreshConfiguration();
        config.DefaultRefreshRateInSeconds = 6;
        Assert.AreEqual(6, config.DefaultRefreshRateInSeconds);
    }

    [Test]
    public void MinRefreshRate()
    {
        var config = new BannerRefreshConfiguration();
        config.DefaultRefreshRateInSeconds = -100;
        Assert.AreEqual(BannerRefreshConfiguration.REFRESH_RATE_MIN,
            config.DefaultRefreshRateInSeconds);
    }

    [Test]
    public void MaxRefreshRate()
    {
        var config = new BannerRefreshConfiguration();
        config.DefaultRefreshRateInSeconds = 999;
        Assert.AreEqual(BannerRefreshConfiguration.REFRESH_RATE_MAX,
            config.DefaultRefreshRateInSeconds);
    }

    [Test]
    public void BufferSize()
    {
        var config = new BannerRefreshConfiguration();
        config.AdsBufferSize = 2;
        Assert.AreEqual(2, config.AdsBufferSize);
    }

    [Test]
    public void MinBufferSize()
    {
        var config = new BannerRefreshConfiguration();
        config.AdsBufferSize = -100;
        Assert.AreEqual(BannerRefreshConfiguration.ADS_BUFFERSIZE_MIN,
            config.AdsBufferSize);
    }

    [Test]
    public void MaxBufferSize()
    {
        var config = new BannerRefreshConfiguration();
        config.AdsBufferSize = 999;
        Assert.AreEqual(BannerRefreshConfiguration.ADS_BUFFERSIZE_MAX,
            config.AdsBufferSize);
    }

    [Test]
    public void DefaultRefreshRatesInSeconds()
    {
        var config = new BannerRefreshConfiguration();
        var result = config.GetRefreshRateInSecondsForAdSourceId(string.Empty);
        Assert.AreEqual(config.DefaultRefreshRateInSeconds, result);
    }

    [Test]
    public void GetRefreshRateForAdSourceId()
    {
        var config = new BannerRefreshConfiguration();
        config.AdSourceRefreshRatesInSeconds.Add(AdSource.ADMOB_NETWORK,
            BannerRefreshConfiguration.REFRESH_RATE_MAX);
        var result = config.GetRefreshRateInSecondsForAdSourceId(AdSource.ADMOB_NETWORK);
        Assert.AreEqual(BannerRefreshConfiguration.REFRESH_RATE_MAX, result);
    }
}
