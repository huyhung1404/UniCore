using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnityEngine
{
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialization()
        {
            new GameObject("MainThreadDispatcher").AddComponent<UnityMainThreadDispatcher>();
        }

        private static readonly List<Action> _callback = new List<Action>();
        private static readonly List<float> _timeExecute = new List<float>();

        private static UnityMainThreadDispatcher _instance;
        private static bool _initialization;

        private void Awake()
        {
            DontDestroyOnLoad(this);
            _instance = this;
            _initialization = true;
        }
        
        public static float RunOnMainThread(Action action, float timeDelay)
        {
            if (action == null) return 0;
            if (!_initialization)
            {
                action.Invoke();
                return 0;
            }

            var timeExecute = Time.realtimeSinceStartup + timeDelay;

            lock (_timeExecute)
            {
                var currentLength = _timeExecute.Count;
                for (var i = 0; i < currentLength; i++)
                {
                    if (timeExecute >= _timeExecute[i]) continue;
                    _timeExecute.Insert(i, timeExecute);
                    _callback.Insert(i, action);
                    Debug(
                        $"Schedule at <color=green>{timeExecute:#.####}</color> with index <color=green>{i}</color> - {action.Method.Name}.{action.Method.DeclaringType?.Name}");
                    return timeExecute;
                }

                _timeExecute.Add(timeExecute);
                _callback.Add(action);
                Debug(
                    $"Schedule at <color=green>{timeExecute:#.####}</color> with index <color=green>{_timeExecute.Count - 1}</color> - {action.Method.Name}.{action.Method.DeclaringType?.Name}");
                return timeExecute;
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public static void RunOnMainThread(Action action)
        {
            if (action == null) return;
            if (!_initialization)
            {
                action.Invoke();
                return;
            }

            lock (_timeExecute)
            {
                var currentLength = _timeExecute.Count;
                for (var i = 0; i < currentLength; i++)
                {
                    if (0 >= _timeExecute[i]) continue;
                    _timeExecute.Insert(i, 0);
                    _callback.Insert(i, action);
                    Debug($"Schedule at index <color=green>{i}</color> - {action.Method.Name}.{action.Method.DeclaringType?.Name}");
                    return;
                }

                _timeExecute.Add(0);
                _callback.Add(action);
                Debug($"Schedule at index <color=green>{_timeExecute.Count - 1}</color> - {action.Method.Name}.{action.Method.DeclaringType?.Name}");
            }
        }

        public static void RunOnMainThread(IEnumerator enumerator)
        {
            RunOnMainThread(() => _instance.StartCoroutine(enumerator));
        }

        private void Update()
        {
            lock (_timeExecute)
            {
                while (_timeExecute.Count > 0 && _timeExecute[0] <= Time.realtimeSinceStartup)
                {
                    Debug($"Execute at <color=green>{Time.realtimeSinceStartup:#.####}</color> - {_callback[0].Method.Name}.{_callback[0].Method.DeclaringType?.Name}  " +
                          (_timeExecute[0] == 0 ? string.Empty : $"with time schedule: <color=yellow>{_timeExecute[0]:#.####}</color>"));
                    try
                    {
                        _callback[0].Invoke();
                    }
                    catch (Exception e)
                    {
#if UNITY_EDITOR
                        UnityEngine.Debug.LogError($"IAA: {_callback[0]?.Method.Name}  Exception: {e.Message})");
#endif
                    }

                    _timeExecute.RemoveAt(0);
                    _callback.RemoveAt(0);
                }
            }
        }

        private void OnDestroy()
        {
            _instance = null;
            _initialization = false;
            StopAllCoroutines();
        }

        [Conditional("ENABLE_MAIN_THREAD_DEBUG")]
        private static void Debug(string content)
        {
            UnityEngine.Debug.Log($"<color=red>MainThread:</color> {content}");
        }
    }
}