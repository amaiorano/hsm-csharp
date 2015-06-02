using System;
using UnityEngine;

namespace Hsm
{
    public static partial class Client
    {
        public static void Log(StateMachine aStateMachine, string aMessage)
        {
            //Util.Log(aMessage);
            Console.Out.Write(aMessage); //@NOTE: We don't use WriteLine because Unity's version of Write outputs a newline
        }

        public static void LogError(StateMachine aStateMachine, string aMessage)
        {
            Log(aStateMachine, aMessage);
#if UNITY_EDITOR
				UnityEngine.Debug.Break();
#endif
        }
    }
}
