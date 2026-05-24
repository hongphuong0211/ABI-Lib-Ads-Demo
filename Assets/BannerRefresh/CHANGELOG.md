# Google Mobile Ads Banner Refresh Plugin

## Version 1.3.0

- Fixed an issue where an incorrect InMobi Bidding ad source ID caused the refresh rate override to be ignored.
- Added debug logging for banner refresh view events. To disable logging, set
the `BannerRefreshView.IsDebugLoggingEnabled` property to a `false` value.

## Version 1.2.0

- Fixed an issue where `OnBannerAdLoaded` is never called if the `BannerRefreshView` is hidden.
- Changed `OnBannerAdLoaded` to raise on all banner ad loads instead of when the
  banner ad is rotated. It is now expected behavior to see multiple
  `OnBannerAdLoaded` events shortly after calling `LoadAd()` as the banner
  cache fills up.

## Version 1.1.1

- Fixed an issue where banner refresh configuration settings were sometimes not
  appended to ad request URLs.

## Version 1.1.0

- Added banner refresh configuration settings to ad request URLs.

- Fixed an issue where banner ads weren't refreshing at their custom ad source refresh rates.

## Version 1.0.1

- Removed requirement for MobileAds.RaiseAdEventsOnUnityMainThread to be set to
true.

- Added defensive code to execute plugin calls on the Unity Main thread and
invoke `OnBannerAdLoaded` and `OnBannerAdLoadFailed` on the Unity Main thread.

- Banner presentation events may be called on a background thread depending on
whether your app calls `MobileAds.RaiseAdEventsOnUnityMainThread`. For more
information, see [Raise Ad Events on the Unity Main thread](https://developers.google.com/admob/unity/global-settings#raise_ad_events_on_the_unity_main_thread).

## Version 1.0.0

- Initial release.