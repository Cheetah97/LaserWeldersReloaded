using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;

namespace EemRdx.LaserWelders.Helpers
{
    public static class PlayerCollectionHelpers
    {
        public static IMyPlayer GetPlayer(this IMyPlayerCollection Players, Func<IMyPlayer, bool> filter)
        {
            List<IMyPlayer> list = new List<IMyPlayer>();
            Players.GetPlayers(list, filter);
            return list.FirstOrDefault();
        }
    }
}
