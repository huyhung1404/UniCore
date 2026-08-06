using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

namespace UniCore.Threading
{
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private struct DelayedTask
        {
            public float TimeExecute;
            public Action Action;
        }

        private struct DelayedTaskComparer : IComparer<DelayedTask>
        {
            public int Compare(DelayedTask x, DelayedTask y)
            {
                return y.TimeExecute.CompareTo(x.TimeExecute);
            }
        }

        private static readonly DelayedTaskComparer s_comparer = new DelayedTaskComparer();

        private static List<Action> s_pendingImmediate = new List<Action>(32);
        private static List<DelayedTask> s_pendingDelayed = new List<DelayedTask>(16);
        private static int s_spinLockIndicator;

        private static List<Action> s_executingImmediate = new List<Action>(32);
        private static List<DelayedTask> s_executingDelayedTemp = new List<DelayedTask>(16);
        private static readonly List<DelayedTask> s_mainDelayed = new List<DelayedTask>(32);

        private static UnityMainThreadDispatcher s_instance;

        private static void EnsureInitialized()
        {
            if (s_instance != null) return;
            try
            {
#if UNITY_2023_1_OR_NEWER
                s_instance = FindAnyObjectByType<UnityMainThreadDispatcher>();
#else
                s_instance = FindObjectOfType<UnityMainThreadDispatcher>();
#endif
                if (s_instance != null) return;
                var go = new GameObject("[MainThreadDispatcher]");
                s_instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
            catch (Exception)
            {
                //Ignore
            }
        }

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            DontDestroyOnLoad(this);
        }

        private static void EnterWriteLock(ref SpinWait spinWait)
        {
            while (Interlocked.CompareExchange(ref s_spinLockIndicator, 1, 0) != 0) spinWait.SpinOnce();
        }

        private static void ExitWriteLock() => Volatile.Write(ref s_spinLockIndicator, 0);

        public static void RunOnMainThread(Action action)
        {
            if (action == null) return;
            EnsureInitialized();

            var spinWait = new SpinWait();
            EnterWriteLock(ref spinWait);
            try
            {
                s_pendingImmediate.Add(action);
            }
            finally
            {
                ExitWriteLock();
            }

            DebugLog($"Schedule immediate - {action.Method.Name}.{action.Method.DeclaringType?.Name}");
        }

        public static float RunOnMainThread(Action action, float timeDelay)
        {
            if (action == null) return 0;
            if (timeDelay <= 0)
            {
                RunOnMainThread(action);
                return Time.realtimeSinceStartup;
            }

            EnsureInitialized();

            var timeExecute = Time.realtimeSinceStartup + timeDelay;
            var task = new DelayedTask { TimeExecute = timeExecute, Action = action };

            var spinWait = new SpinWait();
            EnterWriteLock(ref spinWait);
            try
            {
                s_pendingDelayed.Add(task);
            }
            finally
            {
                ExitWriteLock();
            }

            DebugLog($"Schedule at <color=green>{timeExecute:#.####}</color> - {action.Method.Name}.{action.Method.DeclaringType?.Name}");
            return timeExecute;
        }

        public static void RunOnMainThread(IEnumerator enumerator)
        {
            if (enumerator == null) return;
            RunOnMainThread(() =>
            {
                if (s_instance != null) s_instance.StartCoroutine(enumerator);
            });
        }

        private void Update()
        {
            var spinWait = new SpinWait();
            EnterWriteLock(ref spinWait);
            try
            {
                (s_executingImmediate, s_pendingImmediate) = (s_pendingImmediate, s_executingImmediate);
                (s_executingDelayedTemp, s_pendingDelayed) = (s_pendingDelayed, s_executingDelayedTemp);
            }
            finally
            {
                ExitWriteLock();
            }

            var immCount = s_executingImmediate.Count;
            if (immCount > 0)
            {
                for (var i = 0; i < immCount; i++) InvokeAction(s_executingImmediate[i]);
                s_executingImmediate.Clear();
            }

            var newDelCount = s_executingDelayedTemp.Count;
            if (newDelCount > 0)
            {
                s_mainDelayed.AddRange(s_executingDelayedTemp);
                s_mainDelayed.Sort(s_comparer);
                s_executingDelayedTemp.Clear();
            }

            var mainDelCount = s_mainDelayed.Count;
            if (mainDelCount > 0)
            {
                var currentTime = Time.realtimeSinceStartup;
                var processedCount = 0;

                for (var i = mainDelCount - 1; i >= 0; i--)
                {
                    var task = s_mainDelayed[i];
                    if (task.TimeExecute <= currentTime)
                    {
                        InvokeAction(task.Action);
                        processedCount++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (processedCount > 0)
                {
                    s_mainDelayed.RemoveRange(mainDelCount - processedCount, processedCount);
                }
            }
        }

        private static void InvokeAction(Action action)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogError($"[MainThreadDispatcher] Exception in {action.Method.Name}: {e.Message}\n{e.StackTrace}");
#endif
            }
        }

        private void OnDestroy()
        {
            s_instance = null;
            StopAllCoroutines();
        }

        [Conditional("ENABLE_MAIN_THREAD_DEBUG")]
        private static void DebugLog(string content)
        {
            UnityEngine.Debug.Log($"<color=red>MainThread:</color> {content}");
        }
    }
}