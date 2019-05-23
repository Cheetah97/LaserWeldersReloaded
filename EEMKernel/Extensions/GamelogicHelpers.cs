using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace EemRdx.Extensions
{
    public static class GamelogicHelpers
    {
        public static bool IsToolWorking(this IMyShipToolBase Tool)
        {
            if (!Tool.IsFunctional) return false;
            return Tool.Enabled || (Tool as IMyGunObject<Sandbox.Game.Weapons.MyToolBase>).IsShooting;
        }

        public static bool IsWeldable(this IMySlimBlock Block)
        {
            if (Block.CubeGrid.Physics == null || Block.CubeGrid.Physics.Enabled == false) return false;
            if (Block.IsDestroyed || Block.IsFullyDismounted) return false;
            return !Block.IsFullIntegrity || Block.BuildLevelRatio < 1 || Block.CurrentDamage > 0.1f || Block.HasDeformation;
        }

        public static bool IsProjectable(this IMySlimBlock Block, bool CheckPlacement = true)
        {
            MyCubeGrid Grid = Block.CubeGrid as MyCubeGrid;
            if (Grid.Projector == null)
            {
                //SessionCore.DebugWrite($"IsProjectable()", "Grid doesn't have a Projector!", IsExcessive: true);
                return false;
            }

            var CheckResult = (Grid.Projector as IMyProjector).CanBuild(Block, true);
            if (CheckResult != BuildCheckResult.OK)
            {
                //SessionCore.DebugWrite($"IsProjectable()", $"Block cannot be built. Check result: {CheckResult.ToString()}", IsExcessive: false);
                return false;
            }
            return true;
        }

        public static bool IsGrindable(this IMySlimBlock Block)
        {
            MyCubeGrid Grid = Block.CubeGrid as MyCubeGrid;
            if (!Grid.Editable) return false;
            if (Grid.Physics?.Enabled != true) return false;
            return true;
        }

        public static long BuiltBy(this IMyCubeBlock Block)
        {
            return (Block as MyCubeBlock).BuiltBy;
        }

        public static long BuiltBy(this IMySlimBlock Block)
        {
            return Block.GetObjectBuilder().BuiltBy;
        }

        public static ComponentType GetComponent<ComponentType>(this IMyEntity Entity) where ComponentType : MyEntityComponentBase
        {
            if (Entity == null || Entity.Components == null) return null;
            return Entity.Components.Has<ComponentType>() ? Entity.Components.Get<ComponentType>() : Entity.GameLogic.GetAs<ComponentType>();
        }

        public static bool TryGetComponent<ComponentType>(this IMyEntity Entity, out ComponentType Component) where ComponentType : MyEntityComponentBase
        {
            Component = GetComponent<ComponentType>(Entity);
            return Component != null;
        }

        public static bool HasComponent<ComponentType>(this IMyEntity Entity) where ComponentType : MyEntityComponentBase
        {
            var Component = GetComponent<ComponentType>(Entity);
            return Component != null;
        }

        public static List<IMyInventory> GetInventories(this IMyEntity Entity)
        {
            List<IMyInventory> Inventories = new List<IMyInventory>();

            for (int i = 0; i < Entity.InventoryCount; ++i)
            {
                var blockInventory = Entity.GetInventory(i) as MyInventory;
                if (blockInventory != null) Inventories.Add(blockInventory);
            }

            return Inventories;
        }

        public static IMyModelDummy GetDummy(this IMyModel Model, string DummyName)
        {
            Dictionary<string, IMyModelDummy> Dummies = new Dictionary<string, IMyModelDummy>();
            Model.GetDummies(Dummies);
            return Dummies.ContainsKey(DummyName) ? Dummies[DummyName] : null;
        }

        /// <summary>
        /// (c) Phoera
        /// </summary>
        public static T EnsureComponent<T>(this IMyEntity entity) where T : MyEntityComponentBase, new()
        {
            return EnsureComponent(entity, () => new T());
        }
        /// <summary>
        /// (c) Phoera
        /// </summary>
        public static T EnsureComponent<T>(this IMyEntity entity, Func<T> factory) where T : MyEntityComponentBase
        {
            T res;
            if (entity.TryGetComponent(out res))
                return res;
            res = factory();
            if (res is MyGameLogicComponent)
            {
                if (entity.GameLogic?.GetAs<T>() == null)
                {
                    //"Added as game logic".ShowNotification();
                    entity.AddGameLogic(res as MyGameLogicComponent);
                    (res as MyGameLogicComponent).Init((MyObjectBuilder_EntityBase)null);
                }
            }
            else
            {
                //"Added as component".ShowNotification();
                entity.Components.Add(res);
                res.Init(null);
            }
            return res;
        }
        public static void AddGameLogic(this IMyEntity entity, MyGameLogicComponent logic)
        {
            var comp = entity.GameLogic as MyCompositeGameLogicComponent;
            if (comp != null)
            {
                entity.GameLogic = MyCompositeGameLogicComponent.Create(new List<MyGameLogicComponent>(2) { comp, logic }, entity as MyEntity);
            }
            else if (entity.GameLogic != null)
            {
                entity.GameLogic = MyCompositeGameLogicComponent.Create(new List<MyGameLogicComponent>(2) { entity.GameLogic as MyGameLogicComponent, logic }, entity as MyEntity);
            }
            else
            {
                entity.GameLogic = logic;
            }
        }
    }

}
