using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using SharpDX;
using SPredictionMash1;

namespace Fatality.Champions.Yone
{
    public class Yone
    {
        private static Spell Q, Q3, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuK, menuD, menuP, menuL;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Yone")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 450f);
            Q.SetSkillshot(0.25f, 40f, 1500f, false, SpellType.Line);

            Q3 = new Spell(SpellSlot.Q, 1050f);
            Q3.SetSkillshot(0.25f, 80f, 1500f, false, SpellType.Line);

            W = new Spell(SpellSlot.W, 600f);
            W.SetSkillshot(0.5f, 80f, float.MaxValue, false, SpellType.Cone);

            E = new Spell(SpellSlot.E, 0f);

            R = new Spell(SpellSlot.R, 1000f);
            R.SetSkillshot(0.75f, 112, float.MaxValue, false, SpellType.Line);

            Config = new Menu("Yone", "[Fatality] Yone", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Yone));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo"));
            menuQ.Add(new MenuBool("useQ3", "Use Q3 in Combo"));
            menuQ.Add(new MenuKeyBind("turret", "Enable Q3 under Turret", Keys.G, KeyBindType.Toggle)).AddPermashow();
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo"));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Force E if Q3 is Ready"));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo"));
            menuR.Add(new MenuSlider("enemys", "Min Enemys Hit to R", 2, 1, 5));
            menuR.Add(new MenuKeyBind("SemiR", "Semi R", Keys.T, KeyBindType.Press)).AddPermashow();
            Config.Add(menuR);

            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)" }, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("Q3Pred", "Q3 Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("WPred", "W Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("RPred", "R Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);

            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal", true));
            menuK.Add(new MenuBool("KsQ3", "Enable Q3 Killsteal", true));
            menuK.Add(new MenuBool("KsW", "Enable W Killsteal", true));
            Config.Add(menuK);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuSeparator("LaneClear", "Lane Clear"));
            menuL.Add(new MenuBool("LcQ", "Use Q to Lane Clear", true));
            menuL.Add(new MenuBool("LcW", "Use W to Lane Clear", true));
            menuL.Add(new MenuSeparator("Jungleclear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
            menuL.Add(new MenuBool("JcW", "Use W to Jungle Clear", true));
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

            Game.OnUpdate += OnTick;
            GameEvent.OnGameTick += OnUpdate;
            Render.OnDraw += OnDraw;
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void OnTick(EventArgs args)
        {
            Killsteal();

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE();
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Config["Rsettings"].GetValue<MenuKeyBind>("SemiR").Active)
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                SemiR();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicR();
                LogicQ();
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
            var useQ3 = Config["Qsettings"].GetValue<MenuBool>("useQ3");
            var turret = Config["Qsettings"].GetValue<MenuKeyBind>("turret");
            var forceE = Config["Esettings"].GetValue<MenuBool>("useE");
            var Q3target = Q3.GetTarget();

            if (Q3target != null && !turret.Active && Me.HasBuff("yoneq3ready"))
            {
                var PositionafterQ3 = Me.Position.Extend(Q3target.ServerPosition, Q3.Range);
                if (PositionafterQ3.IsUnderEnemyTurret())
                {
                    return;
                }
            }

            switch (comb(menuP, "QPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            switch (comb(menuP, "Q3Pred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (useQ.Enabled && Q.IsReady() && !Me.HasBuff("yoneq3ready"))
            {
                var Qtarget = Q.GetTarget();
                if (Qtarget != null && Qtarget.IsValidTarget())
                {
                    switch (comb(menuP, "Pred"))
                    {
                        case 0:
                            var qpred = Q.GetPrediction(Qtarget);
                            if (qpred.Hitchance >= hitchance)
                            {
                                Q.Cast(qpred.CastPosition);
                            }
                            break;

                        case 1:
                            {
                                Q.CastLine(Qtarget);
                            }
                            break;

                        case 2:
                            var qpredd = Q.GetSPrediction(Qtarget);
                            if (qpredd.HitChance >= hitchance)
                            {
                                Q.Cast(qpredd.CastPosition);
                            }
                            break;
                    }
                }
            }

            if (Q.IsReady() && useQ3.Enabled && Me.HasBuff("yoneq3ready"))
            {
                if (Q3target != null && Q3target.IsValidTarget())
                {
                    switch (comb(menuP, "Pred"))
                    {
                        case 0:
                            var q3pred = Q3.GetPrediction(Q3target);
                            if (q3pred.Hitchance >= hitchance)
                            {
                                Q3.Cast(q3pred.CastPosition);
                            }
                            break;

                        case 1:
                            {
                                Q3.CastLine(Q3target);
                            }
                            break;

                        case 2:
                            var q3predd = Q3.GetSPrediction(Q3target);
                            if (q3predd.HitChance >= hitchance)
                            {
                                Q3.Cast(q3predd.CastPosition);
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
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (W.IsReady() && useW.Enabled)
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
                                W.CastCone(wtarget);
                            }
                            break;

                        case 2:
                            var wpredd = W.GetSPrediction(wtarget);
                            if (wpredd.HitChance >= hitchance)
                            {
                                W.Cast(wpredd.CastPosition);
                            }
                            break;
                    }
                }
            }
        }

        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            var Q3target = Q3.GetTarget();

            if (Q3target != null && Q3target.IsValidTarget())
            {
                if (E.IsReady() && useE.Enabled && !Me.HasBuff("YoneE"))
                {
                    if (Me.HasBuff("yoneq3ready"))
                    {
                        E.Cast(Q3target.ServerPosition);
                    }
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var enemys = Config["Rsettings"].GetValue<MenuSlider>("enemys");

            switch (comb(menuP, "RPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (R.IsReady() && useR.Enabled)
            {
                var rtarget = R.GetTarget();
                if (rtarget != null && rtarget.IsValidTarget())
                {
                    switch (comb(menuP, "Pred"))
                    {
                        case 0:
                            var rpred = R.GetPrediction(rtarget, true);
                            if (rpred.Hitchance >= hitchance && rpred.AoeTargetsHitCount >= enemys.Value)
                            {
                                R.Cast(rpred.CastPosition);
                            }
                            break;

                        case 1:
                            {
                                var rpredd = R.GetPrediction(rtarget, true);
                                if (rpredd.Hitchance >= HitChance.VeryHigh && rpredd.AoeTargetsHitCount >= enemys.Value)
                                {
                                    R.CastLine(rtarget);
                                }
                            }
                            break;

                        case 2:
                            var rpreddd = R.GetAoeSPrediction();
                            if (rpreddd.HitCount >= enemys.Value)
                            {
                                R.SPredictionCast(rtarget, hitchance);
                            }
                            break;
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksQ3 = Config["Killsteal"].GetValue<MenuBool>("KsQ3").Enabled;
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;

            foreach (var Qtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(Q.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksQ && Q.IsReady() && !Me.HasBuff("yoneq3ready"))
                {
                    if (Qtarget != null)
                    {
                        if (Qtarget.DistanceToPlayer() <= Q.Range)
                        {
                            if (Qtarget.Health + Qtarget.AllShield + Qtarget.HPRegenRate <= QDamage(Qtarget))
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
                                            Q.CastLine(Qtarget);
                                        }
                                        break;

                                    case 2:
                                        var qpredd = Q.GetSPrediction(Qtarget);
                                        if (qpredd.HitChance >= HitChance.High)
                                        {
                                            Q.Cast(qpredd.CastPosition);
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            foreach (var Q3target in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(Q3.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (Q.IsReady() && ksQ3 && Me.HasBuff("yoneq3ready"))
                {
                    if (Q3target != null)
                    {
                        if (Q3target.DistanceToPlayer() <= Q3.Range)
                        {
                            if (Q3target.Health + Q3target.AllShield + Q3target.HPRegenRate <= QDamage(Q3target))
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0:
                                        var qpred = Q3.GetPrediction(Q3target);
                                        if (qpred.Hitchance >= HitChance.High)
                                        {
                                            Q3.Cast(qpred.CastPosition);
                                        }
                                        break;

                                    case 1:
                                        {
                                            Q3.CastLine(Q3target);
                                        }
                                        break;

                                    case 2:
                                        var qpredd = Q3.GetSPrediction(Q3target);
                                        if (qpredd.HitChance >= HitChance.High)
                                        {
                                            Q3.Cast(qpredd.CastPosition);
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            foreach (var Wtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(W.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (W.IsReady() && ksW)
                {
                    if (Wtarget != null)
                    {
                        if (Wtarget.DistanceToPlayer() <= W.Range)
                        {
                            if (Wtarget.Health + Wtarget.AllShield + Wtarget.HPRegenRate <= W.GetDamage(Wtarget))
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0:
                                        var wpred = W.GetPrediction(Wtarget);
                                        if (wpred.Hitchance >= HitChance.High)
                                        {
                                            W.Cast(wpred.CastPosition);
                                        }
                                        break;

                                    case 1:
                                        {
                                            W.CastCone(Wtarget);
                                        }
                                        break;

                                    case 2:
                                        var wpredd = W.GetSPrediction(Wtarget);
                                        if (wpredd.HitChance >= HitChance.High)
                                        {
                                            W.Cast(wpredd.CastPosition);
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void Laneclear()
        {
            var lcq = Config["Clear"].GetValue<MenuBool>("LcQ");
            var lcw = Config["Clear"].GetValue<MenuBool>("LcW");

            if (lcq.Enabled && Q.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var Qfarm = Q.GetLineFarmLocation(minions);
                    if (Qfarm.Position.IsValid())
                    {
                        Q.Cast(Qfarm.Position);
                    }
                }
            }

            if (lcw.Enabled && W.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var Wfarm = W.GetCircularFarmLocation(minions);
                    if (Wfarm.Position.IsValid())
                    {
                        W.Cast(Wfarm.Position);
                    }
                }
            }
        }

        private static void Jungleclear()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ").Enabled;
            var JcWw = Config["Clear"].GetValue<MenuBool>("JcW").Enabled;
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
                if (JcWw && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast(mob.Position);
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (Config["Draw"].GetValue<MenuBool>("drawQ").Enabled && !Me.HasBuff("yoneq3ready"))
            {
                var colorQ = Config["Draw"].GetValue<MenuColor>("colorQ").Color;
                CircleRender.Draw(Me.Position, Q.Range, colorQ, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawQ3").Enabled && Me.HasBuff("yoneq3ready"))
            {
                var colorQ = Config["Draw"].GetValue<MenuColor>("colorQ").Color;
                CircleRender.Draw(Me.Position, Q3.Range, colorQ, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawW").Enabled)
            {
                var colorW = Config["Draw"].GetValue<MenuColor>("colorW").Color;
                CircleRender.Draw(Me.Position, W.Range, colorW, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled)
            {
                var colorE = Config["Draw"].GetValue<MenuColor>("colorE").Color;
                CircleRender.Draw(Me.Position, E.Range, colorE, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled)
            {
                var colorR = Config["Draw"].GetValue<MenuColor>("colorR").Color;
                CircleRender.Draw(Me.Position, R.Range, colorR, 2);
            }
        }

        private static readonly float[] QBaseDamage = { 0f, 20f, 40f, 60f, 80f, 100, 100f };

        private static float QDamage(AIBaseClient target)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] + 1.05f * Me.TotalAttackDamage;
            return (float)Me.CalculateDamage(target, DamageType.Physical, qBaseDamage);
        }

        private static void SemiR()
        {
            switch (comb(menuP, "RPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (R.IsReady())
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
                                R.CastLine(rtarget);
                            }
                            break;

                        case 2:
                            var rpredd = R.GetSPrediction(rtarget);
                            if (rpredd.HitChance >= hitchance)
                            {
                                R.Cast(rpredd.CastPosition);
                            }
                            break;
                    }
                }
            }
        }
    }
}
