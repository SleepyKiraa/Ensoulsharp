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

namespace Fatality.Champions.Corki
{
    public class Corki
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ,menuE, menuR, menuK, menuD, menuP, menuL;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Corki")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 825f);
            Q.SetSkillshot(0.25f, 80f, 1000, false, SpellType.Circle);

            W = new Spell(SpellSlot.W, 600f);

            E = new Spell(SpellSlot.E, 690f);

            R = new Spell(SpellSlot.R, 1300f);
            R.SetSkillshot(0.175f, 40f, 2000f, true, SpellType.Line);

            Config = new Menu("Corki", "[Fatality] Corki", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Corki));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo"));
            Config.Add(menuQ);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo"));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo"));
            Config.Add(menuR);
            
            menuP = new Menu("Pred", "Prediction Settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] {"SDK", "xcsoft", "Spred (Press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "Very High" }, 2));
            menuP.Add(new MenuList("RPred", "R Hitchance",
                new string[] { "Low", "Medium", "High", "Very High" }, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal"));
            menuK.Add(new MenuBool("KsR", "Enable R Killsteal"));
            Config.Add(menuK);
            
            menuL = new Menu("Clear", "Clear Settings");
            menuL.Add(new MenuBool("LcQ", "Use Q to Lane Clear", true));
            menuL.Add(new MenuSeparator("JungleClear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
            menuL.Add(new MenuBool("JcE", "Use E to Jungle Clear", true));
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
            if (Me.HasBuff("mbcheck2"))
            {
                R.Range = 1500;
            }
            else
            {
                R.Range = 1300;
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicR();
                LogicQ();
                LogicE();
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

            if (Q.IsReady() && useQ.Enabled)
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
                                Q.CastCircle(qtarget);
                            }

                            break;
                        
                        case 2:
                            var qpreddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width, Q.Delay, Q.Speed,
                                Q.Range, false, SpellType.Circle);
                            if (qpreddd.HitChance >= hitchance)
                            {
                                Q.Cast(qpreddd.CastPosition);
                            }

                            break;
                    }
                }
            }
            
        }

        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");

            if (E.IsReady() && useE.Enabled)
            {
                var etarget = E.GetTarget();
                if (etarget != null && etarget.IsValidTarget())
                {
                    E.Cast(etarget);
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            
            switch (comb(menuP, "RPred"))
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

            if (R.IsReady() && useR.Enabled)
            {
                var rtarget = R.GetTarget();
                if (rtarget != null && rtarget.IsValidTarget())
                {
                    switch (comb(menuP, "Pred"))
                    {
                        case 0:
                            var rpred = R.GetPrediction(rtarget);
                            if (rpred.Hitchance >= hitchance)
                            {
                                R.Cast(rpred.CastPosition);
                            }

                            break;
                        
                        case 1:
                            {
                                R.CastLine(rtarget, 0f, 0f, false);
                            }

                            break;
                        
                        case 2:
                            var rpreddd = SPredictionMash1.Prediction.GetPrediction(rtarget, R.Width, R.Delay, R.Speed,
                                R.Range, true, SpellType.Line);
                            if (rpreddd.HitChance >= hitchance)
                            {
                                R.Cast(rpreddd.CastPosition);
                            }

                            break;
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
                    var qfarm = Q.GetCircularFarmLocation(minions);
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
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range) E.Cast(mob);
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;

            foreach (var qtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(Q.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (Q.IsReady() && ksQ)
                {
                    if (qtarget != null)
                    {
                        if (qtarget.DistanceToPlayer() <= Q.Range)
                        {
                            if (qtarget.Health + qtarget.AllShield + qtarget.HPRegenRate <= QDamage(qtarget))
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
                                            Q.CastCircle(qtarget);
                                        }

                                        break;
                        
                                    case 2:
                                        var qpreddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width, Q.Delay, Q.Speed,
                                            Q.Range, false, SpellType.Circle);
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

            foreach (var rtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(R.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (R.IsReady() && ksR)
                {
                    if (rtarget != null)
                    {
                        if (rtarget.DistanceToPlayer() <= R.Range)
                        {
                            if (!Me.HasBuff("mbcheck2"))
                            {
                                if (rtarget.Health + rtarget.AllShield + rtarget.HPRegenRate <= RDamage(rtarget))
                                {
                                    switch (comb(menuP, "Pred"))
                                    {
                                        case 0:
                                            var rpred = R.GetPrediction(rtarget);
                                            if (rpred.Hitchance >= HitChance.High)
                                            {
                                                R.Cast(rpred.CastPosition);
                                            }

                                            break;
                        
                                        case 1:
                                            R.CastLine(rtarget, 0f, 0f, false);

                                            break;
                        
                                        case 2:
                                            var rpreddd = SPredictionMash1.Prediction.GetPrediction(rtarget, R.Width, R.Delay, R.Speed,
                                                R.Range, true, SpellType.Line);
                                            if (rpreddd.HitChance >= HitChance.High)
                                            {
                                                R.Cast(rpreddd.CastPosition);
                                            }

                                            break;
                                    }
                                }
                            }
                            else if (Me.HasBuff("mbcheck2"))
                            {
                                if (rtarget.Health + rtarget.AllShield + rtarget.HPRegenRate <= RDamageBig(rtarget))
                                {
                                    switch (comb(menuP, "Pred"))
                                    {
                                        case 0:
                                            var rpred = R.GetPrediction(rtarget);
                                            if (rpred.Hitchance >= HitChance.High)
                                            {
                                                R.Cast(rpred.CastPosition);
                                            }

                                            break;
                        
                                        case 1:
                                            {
                                                R.CastLine(rtarget);
                                            }

                                            break;
                        
                                        case 2:
                                            var rpreddd = SPredictionMash1.Prediction.GetPrediction(rtarget, R.Width, R.Delay, R.Speed,
                                                R.Range, true, SpellType.Line);
                                            if (rpreddd.HitChance >= HitChance.High)
                                            {
                                                R.Cast(rpreddd.CastPosition);
                                            }

                                            break;
                                    }
                                }
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
                var qcolor = Config["Draw"].GetValue<MenuColor>("colorQ").Color;
                CircleRender.Draw(Me.Position, Q.Range, qcolor, 2);
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

        private static readonly float[] QBaseDamage = { 0f, 75f, 120f, 165f, 210f, 255f, 255f };
        private static readonly float[] RBaseDamage = { 0f, 80f, 115f, 150f, 150f };
        private static readonly float[] RBonus = { 0f, .15f, .45f, .75f, .75f };
        private static readonly float[] RBigBaseDamage = { 0f, 160f, 230f, 300f, 300f };
        private static readonly float[] RBigBonus = { 0f, .3f, .9f, 1.5f, 1.5f };

        private static float QDamage(AIBaseClient target)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] +
                              (.7f * GameObjects.Player.TotalAttackDamage +
                               .5f * GameObjects.Player.TotalMagicalDamage);
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, qBaseDamage);
        }

        private static float RDamage(AIBaseClient target)
        {
            var rLevel = R.Level;
            var rBaseDamage = RBaseDamage[rLevel] + (RBonus[rLevel] * GameObjects.Player.TotalAttackDamage +
                                                     .12 * GameObjects.Player.TotalMagicalDamage);
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, rBaseDamage);
        }

        private static float RDamageBig(AIBaseClient target)
        {
            var rLevel = R.Level;
            var rBaseDamageBig = RBigBaseDamage[rLevel] + (RBigBonus[rLevel] * GameObjects.Player.TotalAttackDamage +
                                                           .24 * GameObjects.Player.TotalMagicalDamage);
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, rBaseDamageBig);
        }
    }
}