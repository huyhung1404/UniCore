using System;
using UniCore.Utilities;
using UnityEngine;

namespace UniCore.Console
{
    public enum TriggerMode
    {
        None,
        MultiTaps,
        LongPress,
        DrawCircle
    }

    [Serializable]
    public class ConsoleSettings
    {
        [SerializeField] internal bool m_isSystemEnabled;

        [SerializeField] internal string m_password = "admin";
        [SerializeField] internal TriggerMode m_loginTriggerMode = TriggerMode.MultiTaps;
        [SerializeField] internal int m_loginTapCount = 5;
        [SerializeField] internal float m_loginTapTimeout = 0.5f;
        [SerializeField] internal float m_loginLongPressDuration = 3.0f;

        [SerializeField] internal TriggerMode m_openTriggerMode = TriggerMode.DrawCircle;
        [SerializeField] internal int m_openTapCount = 3;
        [SerializeField] internal float m_openTapTimeout = 0.5f;
        [SerializeField] internal float m_openLongPressDuration = 2.0f;
        [SerializeField] internal KeyCode m_openConsoleKey = KeyCode.BackQuote;

        [SerializeField, Range(0.5f, 3.0f)] internal float m_guiScaleMultiplier = 1f;
        [SerializeField] internal float m_referenceMinDimension = 1080f;
        [SerializeField, Range(0.1f, 1f)] internal float m_guiOpacity = 0.95f;

        [SerializeField] internal bool m_enableMiniProfiler = true;
        [SerializeField, Range(0f, 1f)] internal float m_profilerOpacity = 0.85f;

        [SerializeField] internal int m_maxLogs = 200;

        [SerializeField] internal bool m_captureLogStackTrace;
        [SerializeField] internal bool m_captureWarningStackTrace = true;
        [SerializeField] internal bool m_captureErrorStackTrace = true;
    }

    public class ConsoleRuntimeSettings : UniSettingsBase<ConsoleRuntimeSettings, ConsoleSettings, ConsoleEditorSettings>
    {
        internal const string k_FileName = "UniCore_Runtime_ConsoleSettings";
        public ConsoleSettings CurrentData => Data;
    }
}