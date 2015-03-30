using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegendaryClient.Plugin
{
    public interface IPluginManager
    {
        #region Plugin loading
        
        /// <summary>
        /// Load all plugins
        /// </summary>
        public void LoadPlugins();

        /// <summary>
        /// Load plugin from path provided
        /// </summary>
        /// <param name="Path">Path to plugin binary</param>
        public void LoadPlugin(string Path);

        /// <summary>
        /// Get instance of a plugin
        /// </summary>
        /// <param name="Name">Plugin to get</param>
        public IPlugin GetPlugin(string Name);

        /// <summary>
        /// Get all plugins loaded as array
        /// </summary>
        /// <returns>array of plugins</returns>
        public IPlugin[] GetPlugins();

        /// <summary>
        /// Disable a plugin
        /// </summary>
        /// <param name="Name">Plugin to disable</param>
        public void DisablePlugin(string Name);

        /// <summary>
        /// Disable all plugins
        /// </summary>
        public void DisablePlugins();
        #endregion

        #region Events
        /// <summary>
        /// Call an event
        /// </summary>
        /// <param name="Event">Event to call</param>
        /// <returns>Event after manipulated by plugins</returns>
        public IEvent CallEvent(IEvent Event);
        #endregion
    }
}
