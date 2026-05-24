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
using UnityEngine;

[assembly:System.Runtime.CompilerServices.InternalsVisibleTo("GoogleMobileAds.BannerRefresh.EditmodeTests")]
[assembly:System.Runtime.CompilerServices.InternalsVisibleTo("GoogleMobileAds.BannerRefresh.PlaymodeTests")]
namespace GoogleMobileAds.BannerRefresh
{
    /// <summary>
    /// Timer class for tracking idle time.
    /// </summary>
    [Serializable]
    internal class BannerRefreshTimer
    {
        internal float? _idleStartTime = null;

        /// <summary>
        /// Starts tracking idle time.
        /// </summary>
        internal void StartIdleTime()
        {
            if (!_idleStartTime.HasValue)
            {
                _idleStartTime = Time.time;
            }
        }

        /// <summary>
        /// Finalizes the idle time from the previous period.
        /// </summary>
        /// <returns>The idle time in seconds.</returns>
        internal int FinalizeIdleTime()
        {
            int seconds = 0;
            if (_idleStartTime.HasValue)
            {
                seconds = Mathf.RoundToInt(Time.time - _idleStartTime.Value);
                _idleStartTime = null;
            }
            return seconds;
        }
    }
}
