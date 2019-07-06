using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Voxels;
using VRageMath;

namespace EemRdx.LaserWelders.Helpers
{
    public static class VoxelHelpers
    {
        public static bool HasVoxelAt(this IMyVoxelBase Voxel, Vector3D Point)
        {
            if (Voxel == null) throw new ArgumentNullException(nameof(Voxel));
            if (Voxel.Storage == null) throw new ArgumentException("Voxel.Storage is null");

            MyVoxelBase _Voxel = Voxel as MyVoxelBase;
            VoxelHit hit = new VoxelHit();

            //The following magic is taken from Sandbox.Game.Entities.VoxelBaseExtensions
            Vector3 value;
            MyVoxelCoordSystems.WorldPositionToLocalPosition(Point, _Voxel.PositionComp.WorldMatrix, _Voxel.PositionComp.WorldMatrixInvScaled, _Voxel.SizeInMetresHalf, out value);
            Vector3I voxelCoord = new Vector3I(value / 1f) + _Voxel.StorageMin;

            _Voxel.Storage.ExecuteOperationFast(ref hit, MyStorageDataTypeFlags.Content, ref voxelCoord, ref voxelCoord, false);
            return hit.HasHit;
        }

        public static bool HasVoxelAt(this IMyVoxelBase Voxel, Vector3I LocalPoint)
        {
            if (Voxel == null) throw new ArgumentNullException(nameof(Voxel));
            if (Voxel.Storage == null) throw new ArgumentException("Voxel.Storage is null");

            MyVoxelBase _Voxel = Voxel as MyVoxelBase;
            VoxelHit hit = new VoxelHit();

            _Voxel.Storage.ExecuteOperationFast(ref hit, MyStorageDataTypeFlags.Content, ref LocalPoint, ref LocalPoint, false);
            return hit.HasHit;
        }

        public static void RemoveVoxelAt(this IMyVoxelBase Voxel, Vector3I LocalPoint)
        {
            if (Voxel == null) throw new ArgumentNullException(nameof(Voxel));
            if (Voxel.Storage == null) throw new ArgumentException("Voxel.Storage is null");

            MyVoxelBase _Voxel = Voxel as MyVoxelBase;
            VoxelRemove remover = new VoxelRemove();

            _Voxel.Storage.ExecuteOperationFast(ref remover, MyStorageDataTypeFlags.Content, ref LocalPoint, ref LocalPoint, false);
        }

        public static Vector3D? GetClosestPointOnRay(this IMyVoxelBase Voxel, LineD Ray, float step = 0.25f)
        {
            if (Voxel == null) throw new ArgumentNullException(nameof(Voxel));
            if (Voxel.Storage == null) throw new ArgumentException("Voxel.Storage is null");

            float length = -step;
            bool hashit = false;
            Vector3D hit = Voxel.WorldMatrix.Translation;
            while (!hashit && length <= Ray.Length)
            {
                length += step;
                Vector3D point = Ray.From + (Ray.Direction * length);
                hashit = Voxel.HasVoxelAt(point);
                if (hashit) hit = point;
            }
            if (hashit) return hit; else return null;
        }

        public static List<Vector3I> GetVoxelPointsInSphere(this IMyVoxelBase Voxel, BoundingSphereD Sphere)
        {
            MyStorageData trash = null;
            return GetVoxelPointsInSphere(Voxel, Sphere, out trash);
        }

        public static List<Vector3I> GetVoxelPointsInSphere(this IMyVoxelBase Voxel, BoundingSphereD Sphere, out MyStorageData cache)
        {
            if (Voxel == null) throw new ArgumentNullException(nameof(Voxel));
            if (Voxel.Storage == null) throw new ArgumentException("Voxel.Storage is null");

            List<Vector3I> VoxelPoints = new List<Vector3I>();
            MyVoxelBase myVoxel = (Voxel as MyVoxelBase);

            BoundingBoxD SphereBound = VoxelHelpers.GetWorldBoundariesForSphere(Sphere);
            Vector3 CoordsSystemOutValue;
            MyVoxelCoordSystems.WorldPositionToLocalPosition(Sphere.Center, Voxel.PositionComp.WorldMatrix, Voxel.PositionComp.WorldMatrixInvScaled, myVoxel.SizeInMetresHalf, out CoordsSystemOutValue);
            Vector3I VoxelInSphereCenter = new Vector3I(CoordsSystemOutValue / 1f) + myVoxel.StorageMin;

            // --- I don't know what exactly this vector magic does, this was salvaged from the game's source ---
            // The only idea I have is that from the engine's perspective, every asteroid and planet consist of
            // voxel points (or just "voxels") which are 1 m^3 (2x2x2 small ship blocks) cubes aligned to
            // the world's XYZ axes, which may have different "filling" levels from 0 (empty) to 255.
            // The smooth surfaces we see on asteroids are a result of postprocessing of semi-filled voxel cubes
            // on the outer edges of voxel maps, though this post-processing also affects physics (e.g. collisions).
            // Voxels, like cube grids, have their local coordinate systems, which are built relative to the voxel map's
            // left bottom corner. If you want to find or work with a specific voxel point at a given world position,
            // you have to convert the world coordinates to the voxel map's local coordinates,
            // which can be done using the whitelisted static class
            /// <see cref="MyVoxelCoordSystems"/>
            Vector3I minCorner, maxCorner;
            Voxel.MyVoxelGenerator_ComputeShapeBounds(ref SphereBound, out minCorner, out maxCorner);
            Vector3I minCorner1 = minCorner - 1;
            Vector3I maxCorner1 = maxCorner + 1;
            Voxel.Storage_ClampVoxelCoord(ref minCorner1, 1);
            Voxel.Storage_ClampVoxelCoord(ref maxCorner1, 1);
            cache = new MyStorageData(MyStorageDataTypeFlags.ContentAndMaterial);
            cache.Resize(minCorner1, maxCorner1);
            MyVoxelRequestFlags myVoxelRequestFlags = MyVoxelRequestFlags.AdviseCache | MyVoxelRequestFlags.ConsiderContent;
            Voxel.Storage.ReadRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, 0, minCorner1, maxCorner1, ref myVoxelRequestFlags);
            // ---

            Vector3I voxelpoint;
            voxelpoint.X = minCorner.X;
            while (voxelpoint.X <= maxCorner.X)
            {
                voxelpoint.Y = minCorner.Y;
                while (voxelpoint.Y <= maxCorner.Y)
                {
                    voxelpoint.Z = minCorner.Z;
                    while (voxelpoint.Z <= maxCorner.Z)
                    {
                        if (Vector3D.DistanceSquared(VoxelInSphereCenter, voxelpoint) <= Sphere.Radius * Sphere.Radius && cache.Content(ref voxelpoint) > 0)
                        {
                            VoxelPoints.Add(voxelpoint);
                        }
                        voxelpoint.Z++;
                    }
                    voxelpoint.Y++;
                }
                voxelpoint.X++;
            }

            return VoxelPoints;
        }

        public static MyStorageData GetVoxelCacheInSphere(this IMyVoxelBase Voxel, BoundingSphereD Sphere, out Vector3I refCorner)
        {
            if (Voxel == null) throw new ArgumentNullException(nameof(Voxel));
            if (Voxel.Storage == null) throw new ArgumentException("Voxel.Storage is null");

            BoundingBoxD SphereBound = VoxelHelpers.GetWorldBoundariesForSphere(Sphere);
            Vector3I minCorner, maxCorner;
            Voxel.MyVoxelGenerator_ComputeShapeBounds(ref SphereBound, out minCorner, out maxCorner);
            Vector3I minCorner1 = minCorner - 1;
            Vector3I maxCorner1 = maxCorner + 1;
            Voxel.Storage_ClampVoxelCoord(ref minCorner1, 1);
            Voxel.Storage_ClampVoxelCoord(ref maxCorner1, 1);
            MyStorageData cache = new MyStorageData(MyStorageDataTypeFlags.ContentAndMaterial);
            cache.Resize(minCorner1, maxCorner1);
            MyVoxelRequestFlags myVoxelRequestFlags = MyVoxelRequestFlags.AdviseCache | MyVoxelRequestFlags.ConsiderContent;
            Voxel.Storage.ReadRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, 0, minCorner1, maxCorner1, ref myVoxelRequestFlags);
            refCorner = minCorner1;
            return cache;
        }

        public static void MyVoxelGenerator_ComputeShapeBounds(this IMyVoxelBase VoxelMap, ref BoundingBoxD shapeAabb, out Vector3I voxelMin, out Vector3I voxelMax)
        {
            if (VoxelMap == null) throw new ArgumentNullException(nameof(VoxelMap));
            if (VoxelMap.Storage == null) throw new ArgumentException("Voxel.Storage is null");

            MyVoxelBase voxelMap = VoxelMap as MyVoxelBase;
            Vector3I storageSize = voxelMap.Storage.Size;
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(voxelMap.PositionLeftBottomCorner, ref shapeAabb.Min, out voxelMin);
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(voxelMap.PositionLeftBottomCorner, ref shapeAabb.Max, out voxelMax);
            voxelMin += voxelMap.StorageMin;
            voxelMax += voxelMap.StorageMin;
            voxelMax += 1;
            storageSize -= 1;
            Vector3I.Clamp(ref voxelMin, ref Vector3I.Zero, ref storageSize, out voxelMin);
            Vector3I.Clamp(ref voxelMax, ref Vector3I.Zero, ref storageSize, out voxelMax);
        }

        public static BoundingBoxD GetWorldBoundariesForSphere(BoundingSphereD Sphere)
        {
            BoundingBoxD boundingBoxD = new BoundingBoxD(Sphere.Center - Sphere.Radius, Sphere.Center + Sphere.Radius);
            return boundingBoxD.TransformFast(MatrixD.Identity);
        }

        public static void Storage_ClampVoxelCoord(this IMyVoxelBase Voxel, ref Vector3I voxelCoord, int distance = 1)
        {
            if (Voxel == null) throw new ArgumentNullException(nameof(Voxel));
            if (Voxel.Storage == null) throw new ArgumentException("Voxel.Storage is null");

            Vector3I vector3I = Voxel.Storage.Size - distance;
            Vector3I.Clamp(ref voxelCoord, ref Vector3I.Zero, ref vector3I, out voxelCoord);
        }
    }

    public struct VoxelHit : IVoxelOperator
    {
        public VoxelOperatorFlags Flags { get { return VoxelOperatorFlags.Read; } }

        public bool HasHit { get; private set; }

        public void Op(ref Vector3I position, MyStorageDataTypeEnum dataType, ref byte inOutContent)
        {
            if (inOutContent != MyVoxelConstants.VOXEL_CONTENT_EMPTY) HasHit = true;
        }
    }

    public struct VoxelRemove : IVoxelOperator
    {
        public VoxelOperatorFlags Flags { get { return VoxelOperatorFlags.Write; } }

        public void Op(ref Vector3I position, MyStorageDataTypeEnum dataType, ref byte inOutContent)
        {
            if (inOutContent != MyVoxelConstants.VOXEL_CONTENT_EMPTY)
            {
                inOutContent = MyVoxelConstants.VOXEL_CONTENT_EMPTY;
            }
        }
    }

    public struct CutOutSphereCustom : IVoxelOperator
    {
        // Token: 0x1700135B RID: 4955
        // (get) Token: 0x0600814F RID: 33103 RVA: 0x00348D58 File Offset: 0x00346F58
        public VoxelOperatorFlags Flags
        {
            get
            {
                return VoxelOperatorFlags.ReadWrite;
            }
        }

        // Token: 0x06008150 RID: 33104 RVA: 0x00348D5C File Offset: 0x00346F5C
        public void Op(ref Vector3I pos, MyStorageDataTypeEnum dataType, ref byte content)
        {
            VoxelsProcessed++;
            if (content == 0)
            {
                return;
            }
            Vector3D value = pos;
            double dist = Vector3D.DistanceSquared(LocalCenter, value);
            if (dist < Radius * Radius)
            {
                VoxelsRemoved++;
                content = MyVoxelConstants.VOXEL_CONTENT_EMPTY;
            }
            else
            {
                Log.WriteToLog("CutOutSphereCustom.Op", $"Dist: {Math.Round(dist, 2)}");
            }
        }

        // Token: 0x04005892 RID: 22674
        public float Radius;

        // Token: 0x04005893 RID: 22675
        public Vector3I LocalCenter;

        // Token: 0x04005894 RID: 22676
        public int VoxelsRemoved;
        public int VoxelsProcessed;
        public Utilities.ILog Log;
    }
}
