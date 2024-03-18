using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Utility;
using SharpDX;

namespace Fatality.Utils.Oktw
{
    public class OktwCommon
    {
        private static AIHeroClient Player { get { return ObjectManager.Player; } }
        private static List<UnitIncomingDamage> IncomingDamageList = new List<UnitIncomingDamage>();
        private static List<AIHeroClient> ChampionList = new List<AIHeroClient>();
        public static bool YasuoInGame = false;
        private static YasuoWall yasuoWall = new YasuoWall();

        static OktwCommon()
        {
            foreach (var hero in ObjectManager.Get<AIHeroClient>())
            {
                ChampionList.Add(hero);
                if (hero.IsEnemy && hero.CharacterName == "Yasuo")
                    YasuoInGame = true;
            }
            
            AIBaseClient.OnProcessSpellCast += AIBaseClient_OnDoCast;
            AIBaseClient.OnDoCast += AIBaseClient_OnProcessSpellCast;
        }
        
        public static void debug(string msg)
        {
            if (true)
            {
                Console.WriteLine(msg);
            }
            if (false)
            {
            }
        }
        
        public static int GetBuffCount(AIBaseClient target, string buffName)
        {
            if (buffName.Equals("TwitchDeadlyVenom")) // ty finn
            {
                var twitchECount = 0;

                for (var i = 1; i < 7; i++)
                {
                    if (ObjectManager.Get<EffectEmitter>()
                        .Any(e => e.Position.Distance(target.ServerPosition) <= 55 &&
                                  e.Name == "twitch_poison_counter_0" + i + ".troy"))
                    {
                        twitchECount = i;
                    }
                }
                return twitchECount;
            }

            int stack = 0;
            foreach (var targetA in ObjectManager.Get<AIHeroClient>().Where(i => i.IsEnemy && i.IsValidTarget() && i.IsVisibleOnScreen))
            {
                foreach (var buff in target.Buffs)
                {
                    if(buff.Name.ToLower().Contains(buffName.ToLower()))
                    {
                        stack = target.GetBuffCount(buff.Name);
                    }
                }
            }
            return stack;
        }
        
        public static int CountEnemyMinions(AIBaseClient target, float range)
        {
            var allMinions = Cache.GetMinions(target.Position, range);
            if (allMinions != null)
                return allMinions.Count;
            else
                return 0;
        }
        
        

        private static void AIBaseClient_OnDoCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (args.SData == null || sender.Type != GameObjectType.AIHeroClient)
            {
                return;
            }

            var target = args.Target as AIBaseClient;

            if (target != null)
            {
                if (target.Type == GameObjectType.AIHeroClient && target.Team != sender.Team && (sender.IsMelee || !args.SData.Name.IsAutoAttack()))
                {
                    IncomingDamageList.Add(new UnitIncomingDamage
                    {
                        Damage = (sender as AIHeroClient).GetSpellDamage(target, args.Slot),
                        Skillshot = false,
                        TargetNetworkId = args.Target.NetworkId,
                        Time = Game.Time
                    });
                }
            }
            else
            {
                foreach (var hero in GameObjects.Heroes.Where(e => !e.IsDead && e.IsVisible && e.Team != sender.Team && e.Distance(sender) < 2000))
                {
                    if (hero.HasBuffOfType(BuffType.Slow) || hero.IsWindingUp || !CanMove(hero))
                    {
                        if (CanHitSkillShot(hero, args.Start, args.To, args.SData))
                        {
                            IncomingDamageList.Add(new UnitIncomingDamage
                            {
                                Damage = (sender as AIHeroClient).GetSpellDamage(target, args.Slot),
                                Skillshot = true,
                                TargetNetworkId = hero.NetworkId,
                                Time = Game.Time
                            });
                        }
                    }
                }
            }
        }

        private static void AIBaseClient_OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (args.Target != null && args.SData != null && sender.Type == GameObjectType.AIHeroClient)
            {
                if (args.Target.Type == GameObjectType.AIHeroClient && !sender.IsMelee && args.Target.Team != sender.Team)
                {
                    IncomingDamageList.Add(new UnitIncomingDamage
                    {
                        Damage = (sender as AIHeroClient).GetSpellDamage(args.Target as AIBaseClient, args.Slot),
                        Skillshot = false,
                        TargetNetworkId = args.Target.NetworkId,
                        Time = Game.Time
                    });
                }
            }
        }

        public static bool CanMove(AIHeroClient target)
        {
            return !((!target.IsWindingUp && !target.CanMove)
                || target.MoveSpeed < 50
                || target.HaveImmovableBuff());
        }

        public static bool CanHarass()
        {
            return !Player.IsWindingUp && !Player.IsUnderEnemyTurret() && Orbwalker.CanMove(50, false);
        }

        public static bool CanHitSkillShot(AIBaseClient target, Vector3 start, Vector3 end, SpellData sdata)
        {
            if (target.IsValidTarget(float.MaxValue, false))
            {
                var pred = Prediction.GetPrediction(target, 0.25f)?.CastPosition;

                if (pred == null)
                {
                    return false;
                }

                if (sdata.LineWidth > 0)
                {
                    var powCalc = Math.Pow(sdata.LineWidth + target.BoundingRadius, 2);

                    if (pred.Value.ToVector2().DistanceSquared(end.ToVector2(), start.ToVector2(), true) <= powCalc || target.ServerPosition.ToVector2().DistanceSquared(end.ToVector2(), start.ToVector2(), true) <= powCalc)
                    {
                        return true;
                    }
                }
                else if (target.Distance(end) < 50 + target.BoundingRadius || pred.Value.Distance(end) < 50 + target.BoundingRadius)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CheckGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (!Player.InRange(args.EndPosition, 500, true))
            {
                return false;
            }

            if (args.Type == AntiGapcloser.GapcloserType.UnknowDash && (args.StartPosition == args.EndPosition || args.Speed == 0))
            {
                return false;
            }

            return true;
        }

        public static List<Vector3> CirclePoints(float circleLineSegmentN, float radius, Vector3 position)
        {
            var points = new List<Vector3>();

            for (var i = 1; i <= circleLineSegmentN; i++)
            {
                var angle = i * 2 * Math.PI / circleLineSegmentN;
                var point = new Vector3(position.X + radius * (float)Math.Cos(angle), position.Y + radius * (float)Math.Sin(angle), position.Z);

                points.Add(point);
            }

            return points;
        }

        public static void DrawLineRectangle(Vector3 start2, Vector3 end2, int radius ,float width, System.Drawing.Color color)
        {
            var start = start2.ToVector2();
            var end = end2.ToVector2();
            var dir = (end - start).Normalized();
            var pDir = dir.Perpendicular();

            var rightStartPos = start + pDir * radius;
            var leftStartPos = start - pDir * radius;
            var rightEndPos = end + pDir * radius;
            var leftEndPos = end - pDir * radius;

            var rStartPos = Drawing.WorldToScreen(new Vector3(rightStartPos.X, rightStartPos.Y, Player.Position.Z));
            var lStartPos = Drawing.WorldToScreen(new Vector3(leftStartPos.X, leftStartPos.Y, Player.Position.Z));
            var rEndPos = Drawing.WorldToScreen(new Vector3(rightEndPos.X, rightEndPos.Y, Player.Position.Z));
            var lEndPos = Drawing.WorldToScreen(new Vector3(leftEndPos.X, leftEndPos.Y, Player.Position.Z));

            Drawing.DrawLine(rStartPos, rEndPos, width, color);
            Drawing.DrawLine(lStartPos, lEndPos, width, color);
            Drawing.DrawLine(rStartPos, lStartPos, width, color);
            Drawing.DrawLine(lEndPos, rEndPos, width, color);
        }

        public static double GetIncomingDamage(AIHeroClient target, float time = 0.5f, bool skillshots = true)
        {
            var totalDamge = 0d;

            foreach (var damage in IncomingDamageList.Where(d => d.TargetNetworkId == target.NetworkId && Game.Time - time < d.Time))
            {
                if (skillshots)
                {
                    totalDamge += damage.Damage;
                }
                else if (!damage.Skillshot)
                {
                    totalDamge += damage.Damage;
                }
            }

            if (target.HasBuffOfType(BuffType.Poison))
            {
                totalDamge += target.Level * 5;
            }

            if (target.HasBuffOfType(BuffType.Damage))
            {
                totalDamge += target.Level * 6;
            }

            return totalDamge;
        }

        public static float GetKsDamage(AIHeroClient target, Spell qwer, bool includeIncomingDamage = true)
        {
            var totalDamage = qwer.GetDamage(target) - target.AllShield - target.HPRegenRate;

            if (totalDamage > target.Health)
            {
                if (target.CharacterName == "Blitzcrank" && !target.HasBuff("manabarrier") && !target.HasBuff("manabarriercooldown"))
                {
                    totalDamage -= 0.3f * target.MaxMana;
                }
            }

            if (includeIncomingDamage)
            {
                totalDamage += (float)GetIncomingDamage(target);
            }

            return totalDamage;
        }

        public static float GetPassiveTime(AIBaseClient target, string buffName)
        {
            return target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time)
                .Where(buff => buff.Name.ToLower() == buffName.ToLower())
                .Select(buff => buff.EndTime)
                .FirstOrDefault() - Game.Time;
        }
        public static void DrawTriangleOKTW(float radius, Vector3 position, System.Drawing.Color color, float bold = 1)
        {
            var positionV2 = Drawing.WorldToScreen(position);
            var a = new Vector2(positionV2.X + radius, positionV2.Y + radius / 2);
            var b = new Vector2(positionV2.X - radius, positionV2.Y + radius / 2);
            var c = new Vector2(positionV2.X, positionV2.Y - radius);
            Drawing.DrawLine(a[0], a[1], b[0], b[1], bold, color);
            Drawing.DrawLine(b[0], b[1], c[0], c[1], bold, color);
            Drawing.DrawLine(c[0], c[1], a[0], a[1], bold, color);
        }
        public static bool CanHarras()
        {
            //if (!Player.Spellbook.IsAutoAttacking && !Player.UnderTurret(true) && Orbwalking.CanMove(50))
            if (!Orbwalker.CanAttack() && !Player.IsUnderEnemyTurret() && Orbwalker.CanMove() )
                return true;
            else
                return false;
        }
        public static bool ShouldWait()
        {
            var attackCalc = (int)(Player.AttackDelay * 1000);
            return
                GameObjects.GetMinions(Player.Position, 0).Any(
                    minion => EnsoulSharp.SDK.HealthPrediction.GetPrediction(minion, attackCalc, 30) <= Player.GetAutoAttackDamage(minion));
        }
        public static bool CollisionYasuo(Vector3 from, Vector3 to)
        {
            if (!YasuoInGame)
                return false;

            if (Game.Time - yasuoWall.CastTime > 4)
                return false;

            var level = yasuoWall.WallLvl;
            var wallWidth = (350 + 50 * level);
            var wallDirection = (yasuoWall.CastPosition.ToVector2() - yasuoWall.YasuoPosition.ToVector2()).Normalized().Perpendicular();
            var wallStart = yasuoWall.CastPosition.ToVector2() + wallWidth / 2f * wallDirection;
            var wallEnd = wallStart - wallWidth * wallDirection;

            if (wallStart.Intersection(wallEnd, to.ToVector2(), from.ToVector2()).Intersects)
            {
                return true;
            }
            return false;
            
        }

        public static Vector3 GetTrapPos(float range)
        {
            foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValid && enemy.Distance(Player.Position) < range && (enemy.HasBuff("bardrstasis") || enemy.HasBuff("zhonyasringshield"))))
            {
                return enemy.Position;
            }

            foreach (var obj in ObjectManager.Get<EffectEmitter>().Where(obj => obj.IsValid && obj.Position.Distance(Player.Position) < range))
            {
                var name = obj.Name.ToLower();

                if (name.Contains("Gatemarker_Red".ToLower()) || name.Contains("global_ss_teleport_target_red.troy".ToLower())
                    || name.Contains("R_Tar_Ground_Enemy".ToLower()) || name.Contains("R_indicator_red".ToLower()))
                {
                    return obj.Position;
                }
            }

            return Vector3.Zero;
        }

        public static bool IsMovingInSameDirection(AIBaseClient source, AIBaseClient target)
        {
            var sourceLW = source.GetWaypoints().Last();

            if (sourceLW == source.Position.ToVector2() || !source.IsMoving)
            {
                return false;
            }

            var targetLW = target.GetWaypoints().Last();

            if (targetLW == target.Position.ToVector2() || !target.IsMoving)
            {
                return false;
            }

            return (sourceLW - source.Position.ToVector2()).AngleBetween(targetLW - target.Position.ToVector2()) < 20;
        }

        public static bool IsSpellHeroCollision(AIHeroClient t, Spell qwer, int extraWidth = 50)
        {
            foreach (var hero in GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(qwer.Range + qwer.Width, true, qwer.RangeCheckFrom) && e.NetworkId != t.NetworkId))
            {
                var pred = qwer.GetPrediction(hero);
                var powCalc = Math.Pow(qwer.Width + extraWidth + hero.BoundingRadius, 2);

                if (pred.UnitPosition.ToVector2().DistanceSquared(qwer.From.ToVector2(), qwer.GetPrediction(t).CastPosition.ToVector2(), true) <= powCalc)
                {
                    return true;
                }
                else if (pred.UnitPosition.ToVector2().DistanceSquared(qwer.From.ToVector2(), t.ServerPosition.ToVector2(), true) <= powCalc)
                {
                    return true;
                }
            }

            return false;
        }
        public static float GetEchoLudenDamage(AIHeroClient target)
        {
            float totalDamage = 0;

            if (Player.HasBuff("itemmagicshankcharge"))
            {
                if (Player.GetBuff("itemmagicshankcharge").Count == 100)
                {
                    totalDamage += (float)Player.CalculateDamage(target, DamageType.Magical, 100 + 0.1 * Player.FlatMagicDamageMod);
                }
            }
            return totalDamage;
        }

        public static bool ValidUlt(AIHeroClient target)
        {
            return !(Invulnerable.Check(target)
                || target.HaveSpellShield()
                || target.HasBuffOfType(BuffType.SpellImmunity)
                || target.HasBuffOfType(BuffType.PhysicalImmunity)
                || target.Health - GetIncomingDamage(target) < 1
                || target.HasBuff("JudicatorIntervention")
                || target.HasBuff("kindredrnodeathbuff")
                || target.HasBuff("Undying Rage")
                || target.HasBuffOfType(BuffType.Invulnerability));
        }

        class UnitIncomingDamage
        {
            public double Damage { get; set; }
            public bool Skillshot { get; set; }
            public int TargetNetworkId { get; set; }
            public float Time { get; set; }
        }
    }
    class YasuoWall
    {
        public Vector3 YasuoPosition { get; set; }
        public float CastTime { get; set; }
        public Vector3 CastPosition { get; set; }
        public float WallLvl { get; set; }

        public YasuoWall()
        {
            CastTime = 0;
        }
    }
}