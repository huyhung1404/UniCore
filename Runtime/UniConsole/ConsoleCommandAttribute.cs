using System;
using System.Reflection;

namespace UniCore.Console
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ConsoleCommandAttribute : System.Attribute
    {
        public string Command { get; }
        public string Description { get; }

        public ConsoleCommandAttribute(string command, string description = "")
        {
            Command = command.ToLower();
            Description = description;
        }
    }
    
    public class PendingCommandState
    {
        public readonly ConsoleCommandInfo CommandInfo;
        public readonly object[] CollectedArgs;
        public int CurrentArgIndex;

        public PendingCommandState(ConsoleCommandInfo info)
        {
            CommandInfo = info;
            CollectedArgs = new object[info.Parameters.Length];
            CurrentArgIndex = 0;
        }
    }
    
    public class ConsoleCommandInfo
    {
        public MethodInfo Method;
        public string Command;
        public string Description;
        public ParameterInfo[] Parameters;
        public string TemplateUsage;
    }
}