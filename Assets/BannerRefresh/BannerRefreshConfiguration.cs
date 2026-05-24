//
//  Copyright 2025 Google LLC
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoogleMobileAds.BannerRefresh
{
    /// <summary>
    /// Configuration for BannerRefreshView.
    /// </summary>
    public class BannerRefreshConfiguration
    {
        internal const int ADS_BUFFERSIZE_MIN = 1;
        internal const int ADS_BUFFERSIZE_MAX = 10;
        internal const int REFRESH_RATE_MIN = 5;
        internal const int REFRESH_RATE_MAX = 150;

        /// <summary>
        /// The network specific refresh time for a banner keyed by AdSource, in seconds.
        /// </summary>
        public Dictionary<string, int> AdSourceRefreshRatesInSeconds = new Dictionary<string, int>();

        /// <summary>
        /// The number of ads to preload within BannerRefresh's internal cache.
        /// </summary>
        /// <remarks>
        /// Increase buffer size if you find ads are unavailable to show.
        /// Decrease buffer size if you find this component uses too much memory.
        /// </remarks>
        public int AdsBufferSize
        {
            get => _adsBufferSize;
            set
            {
                var result = Mathf.Clamp(value, ADS_BUFFERSIZE_MIN, ADS_BUFFERSIZE_MAX);
                if (result != value)
                {
                    Debug.LogWarning(
                        $"Attempted to set buffer size to {value}, " +
                        $"which is outside the valid range of {ADS_BUFFERSIZE_MIN}-" +
                        $"{ADS_BUFFERSIZE_MAX}. Setting buffer size to {result}.");
                }
                _adsBufferSize = result;
           }
        }

        /// <summary>
        /// The refresh time used if no network-specific configuration is included, in seconds.
        /// </summary>
        public int DefaultRefreshRateInSeconds
        {
            get => _defaultRefreshRateInSeconds;
            set
            {
                var result = Mathf.Clamp(value, REFRESH_RATE_MIN, REFRESH_RATE_MAX);
                if (result != value)
                {
                    Debug.LogWarning(
                        $"Attempted to set default refresh rate to {value}, " +
                        $"which is outside the valid range of {REFRESH_RATE_MIN}-" +
                        $"{REFRESH_RATE_MAX}. Setting refresh rate to {result} seconds.");
                }
                _defaultRefreshRateInSeconds = result;
            }
        }

        private int _adsBufferSize = 3;
        private int _defaultRefreshRateInSeconds = 120;

        /// <summary>
        /// Initializes a new instance of the <see cref="BannerRefreshConfiguration"/>
        /// class with default values.
        /// </summary>
        public BannerRefreshConfiguration() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BannerRefreshConfiguration"/>
        //  class by copying values from another configuration.
        /// </summary>
        /// <param name="configuration">The configuration to copy values from.</param>
        public BannerRefreshConfiguration(BannerRefreshConfiguration configuration)
        {
            DefaultRefreshRateInSeconds = configuration.DefaultRefreshRateInSeconds;
            AdSourceRefreshRatesInSeconds = new Dictionary<string, int>(configuration.AdSourceRefreshRatesInSeconds);
            AdsBufferSize = configuration.AdsBufferSize;
        }

        /// <summary>
        /// Returns the refresh rate for a given ad source Id.
        /// </summary>
        /// <returns>refresh rate, in seconds.</returns>
        public float GetRefreshRateInSecondsForAdSourceId(string adSourceId)
        {
            if (AdSourceRefreshRatesInSeconds == null
                || string.IsNullOrEmpty(adSourceId)
                || !AdSourceRefreshRatesInSeconds.ContainsKey(adSourceId))
            {
                return DefaultRefreshRateInSeconds;
            }

            var value = AdSourceRefreshRatesInSeconds[adSourceId];
            var result = Mathf.Clamp(value, REFRESH_RATE_MIN, REFRESH_RATE_MAX);
            if (result != value)
            {
                Debug.LogWarning(
                    $"Attempted to set refresh rate for ad source ID {adSourceId} to {value}, " +
                    $"which is outside the valid range of {REFRESH_RATE_MIN}-{REFRESH_RATE_MAX}. " +
                    $"Setting refresh rate to {result} seconds.");            }
            return result;
        }
    }
}
