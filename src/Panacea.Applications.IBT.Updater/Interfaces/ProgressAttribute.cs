using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBT.Updater.Interfaces
{
    internal class ActionAttribute : Attribute
    {
        public string Text { get; set; }

        public ActionAttribute(string text)
        {
            Text = text;
        }
    }

    internal class ExecutionPriorityAttribute : Attribute
    {
        public int Priority { get; set; }

        public ExecutionPriorityAttribute(int priority)
        {
            Priority = priority;
        }

    }
}
