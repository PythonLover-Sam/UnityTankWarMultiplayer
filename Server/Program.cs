using System;
using System.Threading;

namespace UnityMultiplayerServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Network server = new Network();
            while(true)
            {
                Thread.Sleep(100);
            }

        }
    }
}
