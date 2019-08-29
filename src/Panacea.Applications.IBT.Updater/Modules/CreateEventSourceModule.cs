using IBT.Updater.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using System.Text;
using System.Threading.Tasks;

namespace IBT.Updater.Modules
{
    [Interfaces.Action("Logging")]

    class CreateEventSourceModule : Module
    {
        internal override Task<bool> OnPreUpdate(SafeDictionary<string, object> keys)
        {
            CreateEventSourceSample1();
            return base.OnPreUpdate(keys);
        }
        static void CreateEventSourceSample1()
        {
            string myLogName;
            string sourceName = "Panacea";

            // Create the event source if it does not exist.
            if (!EventLog.SourceExists(sourceName))
            {
                // Create a new event source for the custom event log
                // named "myNewLog."  

                myLogName = "Application";
                EventSourceCreationData mySourceData = new EventSourceCreationData(sourceName, myLogName);
                EventLog.CreateEventSource(mySourceData);
            }

        }
    }
}
