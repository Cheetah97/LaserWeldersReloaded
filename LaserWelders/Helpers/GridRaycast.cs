using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRageMath;

namespace EemRdx.LaserWelders.Helpers
{
    public static class GridRayCast
    {
        public static void GetBlocksOnRay(this IMyCubeGrid Grid, ICollection<LineD> Rays, ICollection<IMySlimBlock> Blocks, Func<IMySlimBlock, bool> collect = null)
        {
            foreach (LineD Ray in Rays)
            {
                List<Vector3I> BlockPositions = new List<Vector3I>();
                Grid.RayCastCells(Ray.From, Ray.To, BlockPositions);
                foreach (Vector3I Position in BlockPositions)
                {
                    IMySlimBlock Block = Grid.GetCubeBlock(Position);
                    if (Block == null) continue;
                    if (Blocks.Contains(Block)) continue;
                    if (collect == null || collect(Block)) Blocks.Add(Block);
                }
            }
        }

        public static void GetBlocksOnRay(this IMyCubeGrid Grid, LineD Ray, ICollection<IMySlimBlock> Blocks, Func<IMySlimBlock, bool> collect = null)
        {
            GetBlocksOnRay(Grid, new LineD[] { Ray }, Blocks, collect);
        }
    }
}
