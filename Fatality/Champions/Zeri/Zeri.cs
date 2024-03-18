using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using log4net.Core;
using SharpDX;
using SharpDX.Direct3D11;
using SPredictionMash1;
using Collision = SPredictionMash1.Collision;

namespace Fatality.Champions.Zeri
{
    public class Zeri
    {
        private static Spell Q, W, wWall ,E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuP, menuL, menuK, menuD;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Zeri")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 725f);
            Q.SetSkillshot(0.25f, 40f, 2600f, true, SpellType.Line);

            W = new Spell(SpellSlot.W, 1200f);
            W.SetSkillshot(0.6f, 40f, 2500f, true, SpellType.Line);

            wWall = new Spell(SpellSlot.W, 1500f);
            wWall.SetSkillshot(0.75f, 100f, float.MaxValue, false, SpellType.Line);

            E = new Spell(SpellSlot.E, 300f);

            R = new Spell(SpellSlot.R, 825f);

            Config = new Menu("Zeri", "[Fatality] Zeri", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Zeri));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo"));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo"));
            menuW.Add(new MenuBool("useWall", "Use W on Walls"));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in combo"));
            menuE.Add(new MenuSlider("Hp", "Your HP % To use E", 50, 1, 100));
            menuE.Add(new MenuKeyBind("Jump", "Wall Jump Key", Keys.G, KeyBindType.Press)).AddPermashow();
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo"));
            menuR.Add(new MenuSlider("ene", "Min Enemys in R Range", 2, 1, 5));
            Config.Add(menuR);
            
            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("WPred", "W Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal", true));
            menuK.Add(new MenuBool("KsW", "Enable W Killsteal", true));
            menuK.Add(new MenuBool("KsR", "Enable R Killsteal", true));
            Config.Add(menuK);
            
            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuSeparator("LaneClear", "Lane Clear"));
            menuL.Add(new MenuBool("LcQ", "Use Q to Lane Clear", true));
            menuL.Add(new MenuSeparator("Jungleclear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
            menuL.Add(new MenuBool("JcW", "Use W to Jungle Clear", true));
            Config.Add(menuL);
            
            menuD = new Menu("Draw", "Draw Settings");
            menuD.Add(new MenuSeparator("qq", "Q Draw Settings"));
            menuD.Add(new MenuBool("drawQ", "Draw Q Range", true));
            menuD.Add(new MenuColor("colorQ", "Change Q Draw Color", Color.White));
            menuD.Add(new MenuSeparator("ww", "W Draw Settings"));
            menuD.Add(new MenuBool("drawW", "Draw W Range", true));
            menuD.Add(new MenuColor("colorW", "Change W Draw Color", Color.Blue));
            menuD.Add(new MenuSeparator("ee", "E Draw Settings"));
            menuD.Add(new MenuBool("drawE", "Draw E Range", true));
            menuD.Add(new MenuColor("colorE", "Change E Draw Color", Color.Green));
            menuD.Add(new MenuSeparator("rr", "R Draw Settings"));
            menuD.Add(new MenuBool("drawR", "Draw R Range", true));
            menuD.Add(new MenuColor("colorR", "Change R Draw Color", Color.Red));
            Config.Add(menuD);

            switch (comb(menuP, "Pred"))
            {
                case 0:
                    {
                        Game.Print("SDK Loaded");
                    }
                    break;

                case 1:
                    {
                        Game.Print("xcsoft Loaded");
                    }
                    break;

                case 2:
                    {
                        SPredictionMash1.Prediction.Initialize(Config, "Spred");
                        Game.Print("Spred Loaded!");
                    }
                    break;
            }

            Config.Add(new MenuSeparator("asdasd", "Made by Akane#8621"));

            Config.Attach();
            GameEvent.OnGameTick += OnUpdate;
            Game.OnUpdate += UpdateTick;
            Orbwalker.OnBeforeAttack += OnBeforeAA;
            Render.OnDraw += OnDraw;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void UpdateTick(EventArgs args)
        {
            Killsteal();
        }

        private static void OnUpdate(EventArgs args)
        {
            foreach (var obj in Me.Buffs)
            {
                if (obj.Name.Contains("LethalTempo"))
                {
                    if (obj.Count == 6)
                    {
                        Q.Range = 775;
                    }
                }
            }

            if (Me.HasBuff("zeriespecialrounds"))
            {
                Q.Collision = false;
            }

            if (Config["Esettings"].GetValue<MenuKeyBind>("Jump").Active)
            {
                jumpLogic();
            }

            W.Delay = 0.6f - Math.Max(0, Math.Min(0.2f, 0.02f * ((Me.AttackSpeedMod - 1) / 0.25f)));

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicQ();
                LogicE();
                LogicR();
                LogicW();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Laneclear();
                Jungleclear();
            }
        }

        private static void LogicQ()
        {
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");
            
            switch (comb(menuP, "QPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }                

            if (useQ.Enabled && Q.IsReady())
            {
                var qtarget = Q.GetTarget();
                if (qtarget != null && qtarget.IsValidTarget())
                {
                    switch (comb(menuP, "Pred"))
                    {
                        case 0:
                            var qpred = Q.GetPrediction(qtarget);
                            if (qpred.Hitchance >= hitchance)
                            {
                                Q.Cast(qpred.CastPosition);
                            }

                            break;
                        
                        case 1:
                            {
                                Q.CastLine(qtarget, 0f, 0f, false);
                            }

                            break;
                        
                        case 2:
                            if (!Me.HasBuff("zeriespecialrounds"))
                            {
                                var qpreddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width, Q.Delay, Q.Speed,
                                    Q.Range, true, SpellType.Line);
                                if (qpreddd.HitChance >= hitchance)
                                {
                                    Q.Cast(qpreddd.CastPosition);
                                }
                            }
                            else if (Me.HasBuff("zeriespecialrounds"))
                            {
                                var qpredddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width, Q.Delay, Q.Speed,
                                    Q.Range, false, SpellType.Line);
                                if (qpredddd.HitChance >= hitchance)
                                {
                                    Q.Cast(qpredddd.CastPosition);
                                }
                            }

                            break;
                    }
                }
            }
        }

        private static void LogicW()
        {
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");
            var wall = Config["Wsettings"].GetValue<MenuBool>("useWall");
            
            switch (comb(menuP, "WPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }               
            
            if (useW.Enabled && W.IsReady())
            {
                var wtarget = W.GetTarget();
                if (wtarget != null && wtarget.IsValidTarget())
                {
                    switch (comb(menuP, "Pred"))
                    {
                        case 0:
                            var wpred = W.GetPrediction(wtarget);
                            if (wpred.Hitchance >= hitchance)
                            {
                                W.Cast(wpred.CastPosition);
                            }

                            break;

                        case 1:
                            {
                                W.CastLine(wtarget, 0f, 0f, false);
                            }

                            break;
                        
                        case 2:
                            var wpreddd = SPredictionMash1.Prediction.GetPrediction(wtarget, W.Width, W.Delay, W.Speed,
                                W.Range, true, SpellType.Line);
                            if (wpreddd.HitChance >= hitchance)
                            {
                                W.Cast(wpreddd.CastPosition);
                            }

                            break;
                    }
                }
            }

            if (wall.Enabled && W.IsReady())
            {
                var target = TargetSelector.GetTarget(wWall.Range, DamageType.Physical);
                if (target != null)
                {
                    var pred = W.GetPrediction(target, false, -1,
                        new CollisionObjects[]
                        {
                            CollisionObjects.Heroes, CollisionObjects.Minions, CollisionObjects.Walls,
                            CollisionObjects.YasuoWall
                        });

                    var collisonobj = pred.CollisionObjects.Count;
                    if (collisonobj > 0)
                    {
                        wWall.Delay = 0.75f + W.Delay;
                        var Preds = SPredictionMash1.Prediction.GetPrediction(target, wWall.Width, wWall.Delay,
                            wWall.Speed, wWall.Range, false, SpellType.Line);
                        if (Preds.HitChance >= HitChance.High)
                        {
                            var walls = GetFirstWallPoint(Me.ServerPosition.ToVector2(),
                                Preds.UnitPosition.ToRawVector2());
                            if (walls != Vector2.Zero)
                            {
                                W.Cast(Preds.UnitPosition);
                            }
                        }
                    }
                }

            }
        }

        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            var HP = Config["Esettings"].GetValue<MenuSlider>("Hp");

            if (E.IsReady() && useE.Enabled)
            {
                var etarget = E.GetTarget(Q.Range);
                if (etarget != null && etarget.IsValidTarget())
                {
                    if (Me.HealthPercent >= HP.Value)
                    {
                        E.Cast(Game.CursorPos);
                    }
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var enemys = Config["Rsettings"].GetValue<MenuSlider>("ene");

            if (R.IsReady() && useR.Enabled)
            {
                if (Me.CountEnemyHeroesInRange(R.Range) >= enemys.Value)
                {
                    R.Cast();
                }
            }
        }
        
        private static void Laneclear()
        {
            var lcQ = Config["Clear"].GetValue<MenuBool>("LcQ");

            if (lcQ.Enabled && Q.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                
                if (minions.Any())
                {
                    var qfarm = Q.GetLineFarmLocation(minions);
                    if (qfarm.Position.IsValid())
                    {
                        Q.Cast(qfarm.Position);
                        return;
                    }
                }
            }
        }
        
        private static void Jungleclear()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var JcWw = Config["Clear"].GetValue<MenuBool>("JcW");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast(mob.Position);
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;
            foreach (var Qtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(Q.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksQ && Q.IsReady())
                {
                    if (Qtarget != null)
                    {
                        if (Qtarget.DistanceToPlayer() < Q.Range)
                        {
                            if (Qtarget.Health + Qtarget.AllShield + Qtarget.HPRegenRate <= Q.GetDamage(Qtarget) * 2)
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0:
                                        var qpred = Q.GetPrediction(Qtarget);
                                        if (qpred.Hitchance >= HitChance.High)
                                        {
                                            Q.Cast(qpred.CastPosition);
                                        }

                                        break;
                                    
                                    case 1:
                                        {
                                            Q.CastLine(Qtarget, 0f, 0f, false);
                                        }

                                        break;
                                    
                                    case 2:
                                        var qpreddd = SPredictionMash1.Prediction.GetPrediction(Qtarget, Q.Width,
                                            Q.Delay, Q.Speed, Q.Range, true, SpellType.Line);
                                        if (qpreddd.HitChance >= HitChance.High)
                                        {
                                            Q.Cast(qpreddd.CastPosition);
                                        }

                                        break;
                                }
                            }
                        }
                    }
                }
            }

            foreach (var wtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(W.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksW && W.IsReady())
                {
                    if (wtarget != null)
                    {
                        if (wtarget.DistanceToPlayer() <= W.Range)
                        {
                            if (wtarget.Health + wtarget.AllShield + wtarget.HPRegenRate <= WDamage(wtarget))
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0:
                                        var wpred = W.GetPrediction(wtarget);
                                        if (wpred.Hitchance >= HitChance.High)
                                        {
                                            W.Cast(wpred.CastPosition);
                                        }

                                        break;

                                    case 1:
                                        {
                                            W.CastLine(wtarget, 0f, 0f, false);
                                        }

                                        break;
                        
                                    case 2:
                                        var wpreddd = SPredictionMash1.Prediction.GetPrediction(wtarget, W.Width, W.Delay, W.Speed,
                                            W.Range, true, SpellType.Line);
                                        if (wpreddd.HitChance >= HitChance.High)
                                        {
                                            W.Cast(wpreddd.CastPosition);
                                        }

                                        break;
                                }
                            }
                        }
                    }
                }
            }

            foreach (var rtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(R.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksR && R.IsReady())
                {
                    if (rtarget != null)
                    {
                        if (rtarget.DistanceToPlayer() <= R.Range)
                        {
                            if (rtarget.Health + rtarget.AllShield + rtarget.HPRegenRate <= RDamage(rtarget))
                            {
                                R.Cast();
                            }
                        }
                    }
                }
            }
        }
        
        private static void OnDraw(EventArgs args)
        {
            if (Config["Draw"].GetValue<MenuBool>("drawQ").Enabled)
            {
                var color = Config["Draw"].GetValue<MenuColor>("colorQ").Color;
                CircleRender.Draw(Me.Position, Q.Range, color, 2);
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawW").Enabled)
            {
                var color = Config["Draw"].GetValue<MenuColor>("colorW").Color;
                CircleRender.Draw(Me.Position, W.Range, color, 2);
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled)
            {
                var color = Config["Draw"].GetValue<MenuColor>("colorE").Color;
                CircleRender.Draw(Me.Position, E.Range, color, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled)
            {
                var color = Config["Draw"].GetValue<MenuColor>("colorR").Color;
                CircleRender.Draw(Me.Position, R.Range, color, 2);
            }

            for (int i = 0; i < StartVec3.Count; i++)
            {
                CircleRender.Draw(StartVec3[i], 80f, Color.Yellow);
                var pos = Drawing.WorldToScreen(StartVec3[i]);
            }
        }
        
        private static Vector2 GetFirstWallPoint(Vector2 from, Vector2 to, float step = 25)
        {
            var direction = (to - from).Normalized();

            for (float d = 0; d < from.Distance(to); d = d + step)
            {
                var testPoint = from + d * direction;
                var flags = NavMesh.GetCollisionFlags(testPoint.X, testPoint.Y);
                if (flags.HasFlag(CollisionFlags.Wall) || flags.HasFlag(CollisionFlags.Building))
                {
                    return from + (d - step) * direction;
                }
            }
            return Vector2.Zero;
        }

        private static void OnBeforeAA(object sender, BeforeAttackEventArgs args)
        {
            if (!Me.HasBuff("zeriqpassiveready"))
            {
                args.Process = false;
                return;
            }
        }

        private static void jumpLogic()
        {
            if (!E.IsReady())
            {
                return;
            }

            if (Game.MapId == GameMapId.SummonersRift)
            {
                for (int i = 0; i < StartVec3.Count; i++)
                {
                    if (Game.CursorPos.Distance(StartVec3[i]) <= 50)
                    {
                        Me.IssueOrder(GameObjectOrder.MoveTo, StartVec3[i]);

                        if (Me.Distance(StartVec3[i]) <= 20)
                        {
                            Me.Spellbook.CastSpell(SpellSlot.E, EndVec3[i], EndVec3[i]);
                            break;
                        }
                    }
                }
            }
        }
        
        private static List<Vector3> StartVec3 = new List<Vector3>() { new Vector3(1164.776f, 455.3192f, 148.8625f),
            new Vector3(420.0431f, 816.72f, 183.5748f),
            new Vector3(630,4508,95.74805f),
            new Vector3(4776, 694,110.8725f),
            new Vector3(14468.89f, 13948.27f,166.4569f),
            new Vector3(14014, 14512,171.9777f),
            new Vector3(10572, 14356,91.42981f),
            new Vector3(14166, 10326,91.42981f),
            new Vector3(3974, 558,95.74805f),
            new Vector3(525.4004f, 3856.47f,95.74802f),
            new Vector3(10973.24f, 14356f,91.42984f),
            new Vector3(14271.69f, 11206.16f,91.42981f),
            new Vector3(7588, 2988,52.55599f),
            new Vector3(11959.84f, 7753.984f,52.33273f),
            new Vector3(11308, 5328,-57.65408f),
            new Vector3(3504f, 9616f,-33.35656f),
            new Vector3(7228f, 11924f,56.4768f),
            new Vector3(2930f, 7094f,50.69962f),
            new Vector3(12922f, 1208f, 48.41018f),
            new Vector3(1724f, 13756f, 58.19501f)
        };
        private static List<Vector3> EndVec3 = new List<Vector3>() { new Vector3(4344.15f, 537.4541f, 95.74805f),
            new Vector3(518.7524f, 4716.67f, 93.41431f),
            new Vector3(765.5391f, 10341.43f, 52.8374f),
            new Vector3(10689.7f,767.7703f,49.63037f),
            new Vector3(14256.93f,10190.89f,93.31934f),
            new Vector3(9547.33f, 14319.19f,55.56006f),
            new Vector3(6663.356f, 14085.54f,52.83838f),
            new Vector3(14138.41f, 5897.795f,52.70801f),
            new Vector3(883.776f, 212.4987f,174.2166f),
            new Vector3(188.0797f, 416.1705f,183.5747f),
            new Vector3(13641.23f, 14704.25f,165.5154f),
            new Vector3(14661.37f, 14413.14f,171.9775f),
            new Vector3(10283.4f, 2843.151f,49.19702f),
            new Vector3(11596.24f, 9116.619f,51.27246f),
            new Vector3(12777.13f, 3343.183f,51.36719f),
            new Vector3(2314.208f, 11500.38f,19.47461f),
            new Vector3(4703.729f, 12038.75f,56.43262f),
            new Vector3(3298.77f, 5223.558f,54.00513f),
            new Vector3(11596f, 944f, 51.07806f),
            new Vector3(3224f, 13956f, 52.8381f)
        };

        private static readonly float[] WBaseDamage = { 0f, 20f, 60f, 100f, 140f, 180f, 180f };
        private static readonly float[] RBaseDamage = { 0f, 175f, 275f, 375f, 375f };

        private static float WDamage(AIBaseClient target)
        {
            var wLevel = W.Level;
            var wBaseDamage = WBaseDamage[wLevel] +
                              (1.3f * GameObjects.Player.TotalAttackDamage +
                .25f * GameObjects.Player.TotalMagicalDamage);
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, wBaseDamage);
        }

        private static float RDamage(AIBaseClient target)
        {
            var rlevel = R.Level;
            var rBaseDamage = RBaseDamage[rlevel] +
                              (1f * GameObjects.Player.TotalAttackDamage +
                               1.10f * GameObjects.Player.TotalMagicalDamage);
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, rBaseDamage);
        }
    }
}