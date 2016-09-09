using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace BackGroundJob
{
    class Program
    {
        
        
        static void Main(string[] args)
        {

            /*Timer t = new Timer(TimeSpan.FromMinutes(1).TotalMilliseconds); // set the time (5 min in this case)
            t.AutoReset = true;
            t.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
            t.Start();*/
            /*
            Timer myTimer = new Timer();
            myTimer = new System.Timers.Timer();
            myTimer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            myTimer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
            myTimer.Enabled = true;*/
            Generator gen = new Generator();
            while (true)
            {
                
                Console.WriteLine(DateTime.Now.ToString() + ": Start executing");
                gen.verifyClientFlights();
                gen.verifyFlightsForStaff();
                Console.WriteLine(DateTime.Now.ToString() + ": Finished executing");
                Thread.Sleep((int)TimeSpan.FromMinutes(1).TotalMilliseconds);
            }

            
        }
        

    }
}
