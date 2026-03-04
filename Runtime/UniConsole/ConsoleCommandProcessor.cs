using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UniCore.Console
{
    public class ConsoleCommandProcessor
    {
        private Dictionary<string, ConsoleCommandInfo> _commandCache;
        public PendingCommandState PendingCommand { get; private set; }
        
        public IEnumerable<ConsoleCommandInfo> GetAllCommands() => _commandCache?.Values ?? Enumerable.Empty<ConsoleCommandInfo>();

        public void Initialize()
        {
            if (_commandCache != null) return; 

            _commandCache = new Dictionary<string, ConsoleCommandInfo>(StringComparer.OrdinalIgnoreCase);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (var assembly in assemblies)
            {
                if (assembly.FullName.StartsWith("Unity") || assembly.FullName.StartsWith("System") || assembly.FullName.StartsWith("Mono")) continue;

                var methods = assembly.GetTypes()
                    .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    .Where(m => m.GetCustomAttribute<ConsoleCommandAttribute>() != null);

                foreach (var method in methods)
                {
                    var attr = method.GetCustomAttribute<ConsoleCommandAttribute>();
                    var parameters = method.GetParameters();
                    var paramStrings = parameters.Select(p => $"<{p.ParameterType.Name} {p.Name}>");
                    
                    _commandCache[attr.Command] = new ConsoleCommandInfo
                    {
                        Method = method,
                        Command = attr.Command,
                        Description = attr.Description,
                        Parameters = parameters,
                        TemplateUsage = $"{attr.Command} {string.Join(" ", paramStrings)}".Trim()
                    };
                }
            }
        }

        // Trả về string để UI tự AddLog nhằm tránh coupling ngược
        public string ProcessInput(string inputRaw, out UnityEngine.LogType logType, Action<ConsoleCommandInfo, string[]> onImmediateExecute)
        {
            logType = UnityEngine.LogType.Error;
            if (string.IsNullOrWhiteSpace(inputRaw)) return string.Empty;

            var parts = inputRaw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var cmdName = parts[0];

            if (PendingCommand != null)
            {
                return HandlePendingCommand(cmdName, out logType);
            }

            if (!_commandCache.TryGetValue(cmdName, out var commandInfo))
            {
                return $"Command not found: '{cmdName}'. Type 'help' to see list.";
            }

            var methodParams = commandInfo.Parameters;
            var argsProvided = parts.Length - 1;

            if (argsProvided == methodParams.Length)
            {
                onImmediateExecute?.Invoke(commandInfo, parts.Skip(1).ToArray());
                logType = UnityEngine.LogType.Log;
                return $"Executed: {commandInfo.Command} successfully.";
            }
            
            if (argsProvided == 0 && methodParams.Length > 0)
            {
                PendingCommand = new PendingCommandState(commandInfo);
                var nextParam = methodParams[0];
                logType = UnityEngine.LogType.Warning;
                return $"[Step 1/{methodParams.Length}] Please enter value for: <{nextParam.ParameterType.Name} {nextParam.Name}>";
            }

            return $"Syntax error. Usage: {commandInfo.TemplateUsage}";
        }

        private string HandlePendingCommand(string inputArg, out UnityEngine.LogType logType)
        {
            var pInfo = PendingCommand.CommandInfo.Parameters[PendingCommand.CurrentArgIndex];
            try
            {
                PendingCommand.CollectedArgs[PendingCommand.CurrentArgIndex] = Convert.ChangeType(inputArg, pInfo.ParameterType);
                PendingCommand.CurrentArgIndex++;

                if (PendingCommand.CurrentArgIndex >= PendingCommand.CommandInfo.Parameters.Length)
                {
                    var cmdToRun = PendingCommand.CommandInfo;
                    var argsToRun = PendingCommand.CollectedArgs;
                    PendingCommand = null; 
                    
                    cmdToRun.Method.Invoke(null, argsToRun);
                    logType = UnityEngine.LogType.Log;
                    return $"Executed: {cmdToRun.Command} successfully.";
                }
                
                var nextParam = PendingCommand.CommandInfo.Parameters[PendingCommand.CurrentArgIndex];
                logType = UnityEngine.LogType.Warning;
                return $"[Step {PendingCommand.CurrentArgIndex + 1}/{PendingCommand.CommandInfo.Parameters.Length}] Please enter value for: <{nextParam.ParameterType.Name} {nextParam.Name}>";
            }
            catch (Exception e)
            {
                logType = UnityEngine.LogType.Error;
                var errorMsg = $"Invalid input for type {pInfo.ParameterType.Name}: {e.Message}. Command aborted.";
                PendingCommand = null;
                return errorMsg;
            }
        }

        public string CancelPendingCommand()
        {
            if (PendingCommand == null) return string.Empty;
            var msg = $"Command '{PendingCommand.CommandInfo.Command}' was cancelled by user.";
            PendingCommand = null;
            return msg;
        }
    }
}