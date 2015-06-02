using System.Diagnostics;
using System;

namespace Hsm
{
    public static partial class Client
    {
        public static void Log(StateMachine aStateMachine, string aMessage)
        {
            Console.Out.WriteLine(aMessage);
        }

        public static void LogError(StateMachine aStateMachine, string aMessage)
        {
            Log(aStateMachine, aMessage);
            Trace.Assert(false, aMessage);
        }
    }
}
