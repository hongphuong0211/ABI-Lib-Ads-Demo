using UnityEngine;
using System.Collections;

namespace Dual.CoroutineRunner
{
  /// <summary>
  /// Allows for a coroutine to be run from objects that do not extend Monobehaviour
  /// </summary>
  /// <remarks>
  /// Only use a coroutine in a non-monobehaviour if aboslutely needed as it will place a Unity dependancy in your code
  /// </remarks>
  public class CoroutineRunner
  {
    private const string ROUNTINE_RUNNER_NAME = "DualSDKCoroutineRunner";

    public delegate void DelayedFunction();

    private static GameObject _handlerParent = null;
    private static DualCR _coroutineHandler = null;

    private static DelayedFunction _delayedFunction;

    /// <summary>
    /// Allows for non-monobehaviours to run coroutines
    /// </summary>
    /// <param name="method">Coroutine must be passed in as an IEnumerator</param>
    public static Coroutine RunCoroutine(IEnumerator method)
    {
      if (_handlerParent == null)
      {
        CreateRunner();
      }

      return _coroutineHandler.StartCoroutine(method);
    }

    /// <summary>
    /// Allows non-monobehaviours to stop coroutines that were started using CoroutineRunner
    /// </summary>
    /// <param name="method">Coroutine must be passed in as an IEnumerator</param>
    public static void StopCoroutine(IEnumerator method)
    {
      if (_handlerParent == null)
      {
        Debug.LogError("Coroutine Runner : There is no active coroutine object.");
        return;
      }

      _coroutineHandler.StopCoroutine(method);
    }

    /// <summary>
    /// Delays the calling of a function by a specified amount of time.
    /// </summary>
    /// <param name="delayedFunction">The function to call after time has passed</param>
    /// <param name="delay">The time to wait for</param>
    /// <remarks>You should probably avoid using this if you can. Invoking functions with a time delay is kind of icky.</remarks>
    public static void DelayedInvokation(DelayedFunction delayedFunction, float delay)
    {
      _delayedFunction = delayedFunction;

      RunCoroutine(InvokeFunction(delay));
    }

    /// <summary>
    /// Creates the actual Coroutine Runner object. This object will be what owns and executes the Coroutines.
    /// </summary>
    private static void CreateRunner()
    {
      _handlerParent = new GameObject();
      _coroutineHandler = _handlerParent.AddComponent<DualCR>();
      _coroutineHandler.name = ROUNTINE_RUNNER_NAME;
      Object.DontDestroyOnLoad(_handlerParent);
    }

    /// <summary>
    /// Coroutine function that calles the delayed function after the time has passed
    /// </summary>
    /// <param name="delay">The amount to delay by</param>
    private static IEnumerator InvokeFunction(float delay)
    {
      yield return new WaitForSeconds(delay);

      if (_delayedFunction != null)
      {
        _delayedFunction();
      }
    }
  }

  /// <summary>
  /// The MonoBehaviour that owns and executes the Coroutines that are owned by the Coroutine Runner.
  /// </summary>
  /// <remarks>By sharing a .cs file that does not share a name with this class we effectively hide the Monobehaviour from Unity. 
  /// This prevents someone from accidentally stumbling upon the component and trying to add it manually.</remarks>
  public class DualCR : MonoBehaviour
  {
    /// <summary>
    /// Stops all active coroutines when destroyed
    /// </summary>
    private void OnDestroy()
    {
      StopAllCoroutines();
    }
  }
}