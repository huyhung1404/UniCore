using UnityEngine;

namespace UniConsole
{
    public class ConsoleMemory
    {
        private readonly ConsoleLogMessage[] _logs;
        private readonly int _maxLogs;
        
        public int LogHead { get; private set; }
        public int LogCount { get; private set; }
        
        public int CommandCount { get; private set; }
        public int InfoCount { get; private set; }
        public int WarnCount { get; private set; }
        public int ErrorCount { get; private set; }

        public ConsoleMemory(int maxLogs)
        {
            _maxLogs = maxLogs;
            _logs = new ConsoleLogMessage[maxLogs];
            for (var i = 0; i < maxLogs; i++)
            {
                _logs[i] = new ConsoleLogMessage();
            }
        }

        public ConsoleLogMessage GetLog(int index) => _logs[index];
        public int MaxLogs => _maxLogs;

        public void Clear()
        {
            LogCount = 0;
            LogHead = 0;
            CommandCount = 0;
            InfoCount = 0;
            WarnCount = 0;
            ErrorCount = 0;
        }

        private void AdjustLogCount(LogType type, bool isEcho, int amount)
        {
            if (isEcho) CommandCount += amount;
            else if (type == LogType.Log) InfoCount += amount;
            else if (type == LogType.Warning) WarnCount += amount;
            else if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) ErrorCount += amount;
        }

        public void AddLog(string condition, string stackTrace, LogType type, bool isEcho, bool captureTrace, bool isCollapsed)
        {
            var newHash = 0;
            unchecked
            {
                newHash = (condition != null ? condition.GetHashCode() : 0);
                newHash = (newHash * 397) ^ (stackTrace != null ? stackTrace.GetHashCode() : 0);
            }

            if (isCollapsed && LogCount > 0)
            {
                var prevIndex = (LogHead - 1 + _maxLogs) % _maxLogs;
                if (_logs[prevIndex].HashCode == newHash)
                {
                    _logs[prevIndex].CollapseCount++;
                    AdjustLogCount(type, isEcho, 1);
                    return; 
                }
            }

            if (LogCount == _maxLogs)
            {
                var logToOverwrite = _logs[LogHead];
                if (logToOverwrite.CollapseCount > 0) 
                {
                    AdjustLogCount(logToOverwrite.Type, logToOverwrite.IsCommandEcho, -logToOverwrite.CollapseCount);
                }
            }

            var finalStackTrace = captureTrace ? stackTrace : string.Empty;
            var logToUpdate = _logs[LogHead];
            
            logToUpdate.UpdateData(condition, finalStackTrace, type, isEcho, newHash);
            AdjustLogCount(type, isEcho, 1);

            LogHead = (LogHead + 1) % _maxLogs;
            if (LogCount < _maxLogs) LogCount++;
        }
    }
}