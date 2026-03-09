using System;
using UnityEngine;

namespace UniConsole
{
    public class ConsoleLogMessage
    {
        public string Condition;
        public string StackTrace;
        public string ShortStackTrace;
        public string TimeString;
        public LogType Type;
        public bool IsCommandEcho;
        public int CollapseCount;
        public int HashCode;

        public void UpdateData(string condition, string stackTrace, LogType type, bool isEcho, int hash)
        {
            Condition = condition;
            StackTrace = stackTrace;
            Type = type;
            IsCommandEcho = isEcho;
            CollapseCount = 1;
            HashCode = hash;
            TimeString = $"[{DateTime.Now:HH:mm:ss}]";

            if (!string.IsNullOrEmpty(stackTrace))
            {
                var newlineIndex = stackTrace.IndexOf('\n');
                ShortStackTrace = newlineIndex > 0 ? stackTrace[..newlineIndex] : stackTrace;
            }
            else
            {
                ShortStackTrace = string.Empty;
            }
        }
    }
}