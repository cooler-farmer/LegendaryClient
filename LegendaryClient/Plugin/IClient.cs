using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryClient.Logic;

namespace LegendaryClient.Plugin
{
    /// <summary>
    /// Abstraction layer of LegendaryClient.Logic.Client
    /// </summary>
    public interface IClient
    {
        // TODO : Add appropriate fields from Client.cs
        public List<Group> FriendGroups;
        public string CallMessage;
        public bool ChatFilter;
        public bool Garena;
        public bool InstaCall;
        public bool Patching;
    }
}
