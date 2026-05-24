using System;
using UnityEngine;

namespace Dual
{
  class DualHelper
  {
    public static double ConvertMillisecondsToDays(double milliseconds)
    {
      // Create a TimeSpan from the given milliseconds
      TimeSpan timeSpan = TimeSpan.FromMilliseconds(milliseconds);

      // Return the total days as a double
      return timeSpan.TotalDays;
    }

    public static void Log(string message)
    {
      if (DualAdManager.LOG_ENABLE)
      {
        Debug.Log(DualAdManager.LOG_TAG + " - " + message);
      }
    }
  }
}
