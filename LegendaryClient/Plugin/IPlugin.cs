using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegendaryClient.Plugin
{
    public interface IPlugin
    {
        /// <summary>
        /// Human-readable name of plugin
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Plugin version
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Plugin's homepage
        /// </summary>
        public string Site { get; }

        /// <summary>
        /// Array of author names
        /// </summary>
        public string[] Authors { get; }

        /// <summary>
        /// Called when plugin gets enabled
        /// </summary>
        public void Enable();

        /// <summary>
        /// Called when plugin gets disabled
        /// </summary>
        public void Disable();
    }
}
