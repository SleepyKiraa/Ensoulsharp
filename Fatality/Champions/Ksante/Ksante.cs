using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using log4net.Util;
using SharpDX;
using SPredictionMash1;
using Geometry = EnsoulSharp.SDK.Geometry;

namespace Fatality.Champions.Ksante
{
    public class Ksante
    {
        private static Spell Q, Q2, W, E, R;
        private static Menu Config, menuQ, menuE, menuR, menuK, menuD, menuP, menuL;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "KSante")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 465f);
            Q.SetSkillshot(0.25f, 75, float.MaxValue, false, SpellType.Line);
            Q2 = new Spell(SpellSlot.Q, 825f);
            Q2.SetSkillshot(0.25f, 70f, 1800f, false, SpellType.Line);
            W = new Spell(SpellSlot.W, 100f);
            W.SetSkillshot(0f, 55f, 1000f, false, SpellType.Line);
            W.SetCharged("KSanteW", "KSanteW", 100, 450, 1f);
            E = new Spell(SpellSlot.E, 250f);
            R = new Spell(SpellSlot.R, 350f);

            Config = new Menu("KSante", "[Fatality] KSante", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.KSante));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo"));
            menuQ.Add(new MenuBool("useQ3", "Use Q3 in Combo"));
            Config.Add(menuQ);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo"));
            menuE.Add(new MenuSlider("HP", "Your HP % to use E", 50, 1, 100));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo"));
            menuR.Add(new MenuSlider("HP", "Target HP % to use R", 50, 1, 100));
            Config.Add(menuR);
            
            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("Q3Pred", "Q3 Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("WPred", "W Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal", true));
            menuK.Add(new MenuBool("KsQ3", "use Q3 to Killsteal", true));
            Config.Add(menuK);
            
            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuSeparator("LaneClear", "Lane Clear"));
            menuL.Add(new MenuBool("LcQ", "Use Q to Lane Clear", true));
            menuL.Add(new MenuSeparator("Jungleclear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
            Config.Add(menuL);
            
            menuD = new Menu("Draw", "Draw Settings");
            menuD.Add(new MenuSeparator("qq", "Q Draw Settings"));
            menuD.Add(new MenuBool("drawQ", "Draw Q Range", true));
            menuD.Add(new MenuBool("drawQ3", "Draw Q3 Range", true));
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
            if (Me.HasBuff("KSanteRTransform"))
            {
                E.Range = 400;
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE();
                LogicQ();
                LogicR();
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
            var useQ3 = Config["Qsettings"].GetValue<MenuBool>("useQ3");

            if (Me.IsWindingUp)
            {
                return;
            }

            switch (comb(menuP, "QPred"))
            {
                case 0:
                    hitchance = HitChance.Low;
                    break;
                case 1:
                    hitchance = HitChance.Medium;
                    break;
                case 2:
                    hitchance = HitChance.High;
                    break;
                case 3:
                    hitchance = HitChance.VeryHigh;
                    break;
                default:
                    hitchance = HitChance.High;
                    break;
            }            

            if (Q.IsReady() && useQ.Enabled && !Me.HasBuff("KSanteQ3"))
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
                                Q.CastLine(qtarget);
                            }

                            break;
                        
                        case 2:
                            var qpreddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width, Q.Delay, Q.Speed, Q.Range, false, SpellType.Line);
                            if (qpreddd.HitChance >= hitchance)
                            {
                                Q.Cast(qpreddd.CastPosition);
                            }

                            break;
                    }
                }
            }
            
            switch (comb(menuP, "Q3Pred"))
            {
                case 0:
                    hitchance = HitChance.Low;
                    break;
                case 1:
                    hitchance = HitChance.Medium;
                    break;
                case 2:
                    hitchance = HitChance.High;
                    break;
                case 3:
                    hitchance = HitChance.VeryHigh;
                    break;
                default:
                    hitchance = HitChance.High;
                    break;
            }            

            if (Q.IsReady() && useQ3.Enabled && Me.HasBuff("KSanteQ3"))
            {
                var q3target = Q2.GetTarget();
                if (q3target != null && q3target.IsValidTarget())
                {
                    switch (comb(menuP, "Pred"))
                    {
                        case 0:
                            var qpred = Q2.GetPrediction(q3target);
                            if (qpred.Hitchance >= hitchance)
                            {
                                Q2.Cast(qpred.CastPosition);
                            }

                            break;
                        
                        case 1:
                            {
                                Q2.CastLine(q3target);
                            }

                            break;
                        
                        case 2:
                            var qpreddd = SPredictionMash1.Prediction.GetPrediction(q3target, Q2.Width, Q2.Delay, Q2.Speed, Q2.Range, false, SpellType.Line);
                            if (qpreddd.HitChance >= hitchance)
                            {
                                Q2.Cast(qpreddd.CastPosition);
                            }

                            break;
                    }
                }
            }
        }

        private static void LogicW()
        {
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");
            
            switch (comb(menuP, "WPred"))
            {
                case 0:
                    hitchance = HitChance.Low;
                    break;
                case 1:
                    hitchance = HitChance.Medium;
                    break;
                case 2:
                    hitchance = HitChance.High;
                    break;
                case 3:
                    hitchance = HitChance.VeryHigh;
                    break;
                default:
                    hitchance = HitChance.High;
                    break;
            }

            if (useW.Enabled && W.IsReady())
            {
                var wtarget = TargetSelector.GetTarget(W.ChargedMaxRange, DamageType.Physical);
                if (wtarget != null && wtarget.IsValidTarget())
                {
                    switch (comb(menuP, "Pred"))
                    {
                        case 0:
                            var wpred = W.GetPrediction(wtarget);
                            if (wpred.Hitchance >= hitchance)
                            {
                                if (!W.IsCharging)
                                {
                                    W.StartCharging();
                                    return;
                                }

                                if (W.Range == W.ChargedMaxRange)
                                {
                                    var wpred2 = W.GetPrediction(wtarget);
                                    if (wpred2.Hitchance >= hitchance)
                                    {
                                        W.ShootChargedSpell(wpred2.CastPosition);
                                    }
                                }
                            }
                            break;
                        
                        case 1:
                            var wpredd = W.GetPrediction(wtarget);
                            if (wpredd.Hitchance > hitchance)
                            {
                                if (!W.IsCharging)
                                {
                                    W.StartCharging();
                                    return;
                                }

                                if (W.Range == W.ChargedMaxRange)
                                {
                                    W.CastLine(wtarget);
                                }
                            }

                            break;
                        
                        case 2:
                            var wpreddd = SPredictionMash1.Prediction.GetPrediction(wtarget, W.Width, W.Delay, W.Speed,
                                W.ChargedMaxRange, false, SpellType.Line);
                            if (wpreddd.HitChance >= hitchance)
                            {
                                if (!W.IsCharging)
                                {
                                    W.StartCharging();
                                    return;
                                }

                                if (W.Range == W.ChargedMaxRange)
                                {
                                    var wpreddd2 = SPredictionMash1.Prediction.GetPrediction(wtarget, W.Width, W.Delay, W.Speed,
                                        W.ChargedMaxRange, false, SpellType.Line);
                                    if (wpreddd2.HitChance >= hitchance)
                                    {
                                        W.Cast(wpreddd2.CastPosition);
                                    }
                                }
                            }

                            break;
                    }
                }
            }
        }

        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            var hp = Config["Esettings"].GetValue<MenuSlider>("HP");

            if (E.IsReady() && useE.Enabled)
            {
                var etarget = E.GetTarget(Q.Range);
                if (etarget != null && etarget.IsValidTarget())
                {
                    if (Me.HealthPercent >= hp.Value)
                    {
                        E.Cast(Game.CursorPos);
                    }
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var hp = Config["Rsettings"].GetValue<MenuSlider>("HP");

            if (R.IsReady() && useR.Enabled)
            {
                var rtarget = R.GetTarget();
                if (rtarget != null && rtarget.IsValidTarget())
                {
                    if (rtarget.HealthPercent <= hp.Value)
                    {
                        R.Cast(rtarget);
                    }
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
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksQ3 = Config["Killsteal"].GetValue<MenuBool>("KsQ3").Enabled;
            foreach (var qtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(Q.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksQ && Q.IsReady() && !Me.HasBuff("KSanteQ3"))
                {
                    if (qtarget != null)
                    {
                        if (qtarget.DistanceToPlayer() <= Q.Range)
                        {
                            if (qtarget.Health + qtarget.AllShield + qtarget.HPRegenRate <= Q.GetDamage(qtarget))
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0:
                                        var qpred = Q.GetPrediction(qtarget);
                                        if (qpred.Hitchance >= HitChance.High)
                                        {
                                            Q.Cast(qpred.CastPosition);
                                        }

                                        break;

                                    case 1:
                                        {
                                            Q.CastLine(qtarget);
                                        }

                                        break;

                                    case 2:
                                        var qpreddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width,
                                            Q.Delay, Q.Speed, Q.Range, false, SpellType.Line);
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

            foreach (var q3target in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(Q2.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksQ3 && Q.IsReady() && Me.HasBuff("KSanteQ3"))
                {
                    if (q3target != null)
                    {
                        if (q3target.DistanceToPlayer() <= Q2.Range)
                        {
                            if (q3target.Health + q3target.AllShield + q3target.HPRegenRate <= Q.GetDamage(q3target))
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0:
                                        var qpred = Q2.GetPrediction(q3target);
                                        if (qpred.Hitchance >= HitChance.High)
                                        {
                                            Q2.Cast(qpred.CastPosition);
                                        }

                                        break;
                        
                                    case 1:
                                        {
                                            Q2.CastLine(q3target);
                                        }

                                        break;
                        
                                    case 2:
                                        var qpreddd = SPredictionMash1.Prediction.GetPrediction(q3target, Q2.Width, Q2.Delay, Q2.Speed, Q2.Range, false, SpellType.Line);
                                        if (qpreddd.HitChance >= HitChance.High)
                                        {
                                            Q2.Cast(qpreddd.CastPosition);
                                        }

                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (Config["Draw"].GetValue<MenuBool>("drawQ").Enabled && !Me.HasBuff("KSanteQ3"))
            {
                var qcolor = Config["Draw"].GetValue<MenuColor>("colorQ").Color;
                CircleRender.Draw(Me.Position, Q.Range, qcolor, 2);
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawQ3").Enabled && Me.HasBuff("KSanteQ3"))
            {
                var qcolor = Config["Draw"].GetValue<MenuColor>("colorQ").Color;
                CircleRender.Draw(Me.Position, Q2.Range, qcolor, 2);
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawW").Enabled)
            {
                var wcolor = Config["Draw"].GetValue<MenuColor>("colorW").Color;
                CircleRender.Draw(Me.Position, W.Range, wcolor, 2);
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled)
            {
                var ecolor = Config["Draw"].GetValue<MenuColor>("colorE").Color;
                CircleRender.Draw(Me.Position, E.Range, ecolor, 2);
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled)
            {
                var rcolor = Config["Draw"].GetValue<MenuColor>("colorR").Color;
                CircleRender.Draw(Me.Position, R.Range, rcolor, 2);
            }
        }
    }
}