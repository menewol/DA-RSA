using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DA_RSA
{
    class Log
    {
        static List<string> LoggedEvents;
        public static void Add(string message)
        {
        var sd = DateTime.Now.ToShortDateString();
        var st = DateTime.Now.ToShortTimeString();
        LoggedEvents.Add(sd + ": " + st + ": " + message);
        }
        public static void Write()
        {
            string path = Directory.GetCurrentDirectory();
            StreamWriter srw = new StreamWriter(path);
            srw.Write(LoggedEvents);
        }
    }
}
