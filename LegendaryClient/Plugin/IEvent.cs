using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegendaryClient.Plugin
{
    public abstract class IEvent
    {
        private string Name;

        public string GetName()
        {
            if(Name == null)
            {
                Name = this.GetType().Name;
            }
            return Name;
        }

        [Flags]
        public enum Result
        {
            DEFAULT = 0,
            ALLOW = 1,
            DENY = 2
        }
    }
}
