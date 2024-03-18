using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using SharpDX;

namespace Fatality.Utils.Oktw
{
    public class Cache
    {
        public static List<AIBaseClient> AllMinionsObj = new List<AIBaseClient>();
        public static List<AIBaseClient> MinionsListEnemy = new List<AIBaseClient>();
        public static List<AIBaseClient> MinionsListAlly = new List<AIBaseClient>();
        public static List<AIBaseClient> MinionsListNeutral = new List<AIBaseClient>();
        public static List<AITurretClient> TurretList = ObjectManager.Get<AITurretClient>().ToList();
        public static List<HQClient> NexusList = ObjectManager.Get<HQClient>().ToList();
        public static List<BarracksDampenerClient> InhiList = ObjectManager.Get<BarracksDampenerClient>().ToList();

        static Cache()
        {
            foreach (var minion in ObjectManager.Get<AIMinionClient>().Where(minion => minion.IsValid))
            {
                AddMinionObject(minion);
                if (!minion.IsAlly)
                    AllMinionsObj.Add(minion);
            }

            GameObject.OnCreate += Obj_AI_Base_OnCreate;
            Game.OnUpdate += Game_OnUpdate;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            MinionsListEnemy.RemoveAll(minion => !IsValidMinion(minion));
            MinionsListNeutral.RemoveAll(minion => !IsValidMinion(minion));
            MinionsListAlly.RemoveAll(minion => !IsValidMinion(minion));
            AllMinionsObj.RemoveAll(minion => !IsValidMinion(minion));
        }

        private static void Obj_AI_Base_OnCreate(GameObject sender, EventArgs args)
        {
            var minion = sender as AIMinionClient;
            if (minion != null)
            {
                AddMinionObject(minion);
                if (!minion.IsAlly )
                    AllMinionsObj.Add(minion);
            }
        }

        private static void AddMinionObject(AIMinionClient minion)
        {
            if (minion.MaxHealth >= 225)
            {
                if (minion.Team == GameObjectTeam.Neutral)
                {
                    MinionsListNeutral.Add(minion);
                }
                else if (minion.MaxMana == 0 && minion.MaxHealth >= 300)
                {
                    if (minion.Team == GameObjectTeam.Unknown)
                        return;
                    else if (minion.Team != ObjectManager.Player.Team)
                        MinionsListEnemy.Add(minion);
                    else if (minion.Team == ObjectManager.Player.Team)
                        MinionsListAlly.Add(minion);
                }
            }
        }

        public static List<AIBaseClient> GetMinions(Vector3 from, float range = float.MaxValue, MinionManager.MinionTeam team = MinionManager.MinionTeam.Enemy)
        {
            if (team == MinionManager.MinionTeam.Neutral)
            {
                
                return MinionsListEnemy.FindAll(minion => CanReturn(minion, from, range));
            }
            else if (team == MinionManager.MinionTeam.Ally)
            {
                
                return MinionsListAlly.FindAll(minion => CanReturn(minion, from, range));
            }
            else if(team == MinionManager.MinionTeam.Neutral)
            {
                
                return MinionsListNeutral.Where(minion => CanReturn(minion, from, range)).OrderByDescending(minion => minion.MaxHealth).ToList();
            }
            else
            {
                return AllMinionsObj.FindAll(minion => CanReturn(minion, from, range));
            }
        }

        private static bool IsValidMinion(AIBaseClient minion)
        {
            if (minion == null || !minion.IsValid || minion.IsDead)
                return false;
            else
                return true;
        }

        private static bool CanReturn(AIBaseClient minion, Vector3 from, float range)
        {
            
            if (minion != null && minion.IsValid && !minion.IsDead && minion.IsVisible && minion.IsTargetable && minion.IsHPBarRendered)
            {
                if (range == float.MaxValue)
                    return true;
                else if (range == 0)
                {
                    if (ObjectManager.Player.InAutoAttackRange(minion))
                        return true;
                    else
                        return false;
                }
                else if (Vector2.DistanceSquared((@from).ToVector2(), minion.Position.ToVector2()) < range * range)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }
    }
}