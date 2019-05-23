using System;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace EemRdx.Models
{
    public class DamageInfo
    {
        public IMyEntity Attacker;
        public MyDamageInformation Damage;
        public IMySlimBlock DamagedBlock;
        public DateTime Time;
    }
}
