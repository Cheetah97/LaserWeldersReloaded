using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRageMath;

namespace EemRdx.LaserWelders.Helpers
{
    public static class VectorHelpers
    {
        public static List<LineD> BuildLineGrid(int HalfHeight, int HalfWidth, Vector3D CenterStart, Vector3D CenterEnd, Vector3D UpOffset, Vector3D RightOffset)
        {
            List<LineD> Grid;
            if (HalfHeight <= 0 && HalfWidth <= 0)
            {
                Grid = new List<LineD>(2);
                Grid.Add(new LineD(CenterStart, CenterEnd));
                return Grid;
            }

            Grid = new List<LineD>(((HalfHeight * 2) + 1) * ((HalfWidth * 2) + 1) + 1);

            Vector3D LeftBottomCornerStart = CenterStart + (RightOffset * -1 * HalfWidth) + (UpOffset * -1 * HalfHeight);
            Vector3D LeftBottomCornerEnd = CenterEnd + (RightOffset * -1 * HalfWidth) + (UpOffset * -1 * HalfHeight);

            for (int width = 0; width <= HalfWidth * 2; width++)
            {
                for (int height = 0; height <= HalfHeight * 2; height++)
                {
                    Vector3D Point1 = LeftBottomCornerStart + (UpOffset * height) + (RightOffset * width);
                    Vector3D Point2 = LeftBottomCornerEnd + (UpOffset * height) + (RightOffset * width);
                    Grid.Add(new LineD(Point1, Point2));
                }
            }

            return Grid;
        }

    }
}
