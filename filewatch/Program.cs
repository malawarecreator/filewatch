using System;
using System.ServiceProcess;

namespace filewatch
{
    internal static class Program
    {
        static void Main(string[] args)
        {
         
            ServiceBase.Run(new Service());
        }
    }
}
