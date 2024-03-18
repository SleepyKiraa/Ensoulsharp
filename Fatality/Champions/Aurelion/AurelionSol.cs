using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Tracing;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using SharpDX;
using SPredictionMash1;
using HealthPrediction = EnsoulSharp.SDK.HealthPrediction;

namespace Fatality.Champions.Aurelion
{
    public class AurelionSol
    {
        private static Spell Q, W, E, R, R2;
        private static Menu Config, menuQ, menuE, menuR, menuP, menuK, menuD, menuL;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "AurelionSol")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 750f);
            W = new Spell(SpellSlot.W, 1200f);
            E = new Spell(SpellSlot.E, 750f);
            R = new Spell(SpellSlot.R, 1250f);
            R2 = new Spell(SpellSlot.R, 1250f);

            Q.SetSkillshot(0.25f, 40f, float.MaxValue, true, SpellType.Line);
            E.SetSkillshot(0.25f, 70f, float.MaxValue, false, SpellType.Circle);
            R.SetSkillshot(1.25f, 135f, float.MaxValue, false, SpellType.Circle);
            R2.SetSkillshot(2f, 190f, float.MaxValue, false, SpellType.Circle);

            Config = new Menu("Aurelion", "[Fatality] Aurelion Sol", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.AurelionSol));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo"));
            Config.Add(menuQ);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo"));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo"));
            menuR.Add(new MenuBool("useR2", "Use Big R in Combo"));
            Config.Add(menuR);

            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)" }, 2)).AddPermashow();
            menuP.Add(new MenuList("EPred", "E Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("RPred", "R Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);

            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsE", "Enable E Killsteal", true));
            Config.Add(menuK);

            menuL = new Menu("Clear", "Clear Settings");
            menuL.Add(new MenuBool("LcE", "Use Q to Lane Clear", true));
            menuL.Add(new MenuSlider("Ecount", "E Minions Hit", 2, 1, 3));
            menuL.Add(new MenuSeparator("JungleClear", "Jungle Clear"));
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

            Game.OnUpdate += OnTick;
            GameEvent.OnGameTick += OnUpdate;
            Orbwalker.OnBeforeAttack += OnBeforeAA;
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
                LogicR();
                LogicQ();
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            Q.Range = 750f + 10f * Me.Level - 1;
            Q.Collision = true;
            E.Range = 750f + 10f * Me.Level - 1;
            E.Width = 70f + 16.93f * Me.GetBuffCount("AurelionSolPassive");
            W.Range = 1200f + 7.5f * Me.GetBuffCount("AurelionSolPassive");
            R.Width = 135f + 16.93f * Me.GetBuffCount("AurelionSolPassive");
            R2.Width = 190f + 21.85f * Me.GetBuffCount("AurelionSolPassive");


            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                if (Me.HasBuff("AurelionSolQ") && Me.CountEnemyHeroesInRange(Q.Range) == 0)
                {
                    Me.IssueOrder(GameObjectOrder.MoveTo, Me.ServerPosition.Extend(Game.CursorPos, 100));
                }
            }

            if (Me.HasBuff("AurelionSolQ") || Me.HasBuff("AurelionSolW"))
            {
                Orbwalker.AttackEnabled = false;
                Orbwalker.MoveEnabled = false;
            }
            else
            {
                Orbwalker.AttackEnabled = true;
                Orbwalker.MoveEnabled = true;
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
            var qtarget = Q.GetTarget();
            if (qtarget == null) return;

            if (Q.IsReady() && useQ.Enabled)
            {
                if (!Me.HasBuff("AurelionSolQ"))
                {
                    if (Me.CountEnemyHeroesInRange(Q.Range) > 0)
                    {
                        if (qtarget != null && qtarget.IsValidTarget())
                        {
                            Q.Cast(qtarget.ServerPosition);
                        }
                    }
                }

                if (Me.HasBuff("AurelionSolQ") && Me.Spellbook.IsChanneling)
                {
                    Me.Spellbook.UpdateChargedSpell(SpellSlot.Q, qtarget.ServerPosition, false);
                }
            }
        }

        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");

            switch (comb(menuP, "EPred"))
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

            if (Me.HasBuff("AurelionSolQ"))
            {
                return;
            }

            if (E.IsReady() && useE.Enabled)
            {
                var etarget = E.GetTarget();
                if (etarget != null && etarget.IsValidTarget())
                {
                    switch (comb(menuP, "Pred"))
                    {
                        case 0:
                            var epred = E.GetPrediction(etarget, true);
                            if (epred.Hitchance >= hitchance)
                            {
                                E.Cast(epred.CastPosition);
                            }
                            break;

                        case 1:
                            {
                                E.CastCircle(etarget);
                            }
                            break;

                        case 2:
                            var epredd = E.GetSPrediction(etarget);
                            if (epredd .HitChance >= hitchance)
                            {
                                E.Cast(epredd.CastPosition);
                            }
                            break;
                    }
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var useR2 = Config["Rsettings"].GetValue<MenuBool>("useR2");

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

            if (Me.HasBuff("AurelionSolQ"))
            {
                return;
            }

            if (useR.Enabled && R.IsReady())
            {
                if (R.Name == "AurelionSolR")
                {
                    var rtarget = R.GetTarget();
                    if (rtarget != null && rtarget.IsValidTarget())
                    {
                        if (rtarget.HasBuff("JudicatorIntervention") || rtarget.HasBuff("kindredrnodeathbuff") || rtarget.HasBuff("Undying Rage") || rtarget.HasBuffOfType(BuffType.Invulnerability))
                        {
                            return;
                        }

                        if (RDamage(rtarget) + EDamage(rtarget) >= rtarget.Health)
                        {
                            switch (comb(menuP, "Pred"))
                            {
                                case 0:
                                    var rpred = R.GetPrediction(rtarget, true);
                                    if (rpred.Hitchance >= hitchance)
                                    {
                                        R.Cast(rpred.CastPosition);
                                    }
                                    break;

                                case 1:
                                    {
                                        R.CastCircle(rtarget);
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

                if (useR2.Enabled && R.IsReady())
                {
                    if (R.Name == "AurelionSolR2")
                    {
                        var r2target = R2.GetTarget();
                        if (r2target != null && r2target.IsValidTarget())
                        {
                            if (r2target.HasBuff("JudicatorIntervention") || r2target.HasBuff("kindredrnodeathbuff") || r2target.HasBuff("Undying Rage") || r2target.HasBuffOfType(BuffType.Invulnerability))
                            {
                                return;
                            }

                            if (R2Damage(r2target) + EDamage(r2target) >= r2target.Health)
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0:
                                        var r2pred = R2.GetPrediction(r2target, true);
                                        if (r2pred.Hitchance >= hitchance)
                                        {
                                            R2.Cast(r2pred.CastPosition);
                                        }
                                        break;

                                    case 1:
                                        {
                                            R2.CastCircle(r2target);
                                        }
                                        break;

                                    case 2:
                                        var r2predd = R2.GetSPrediction(r2target);
                                        if (r2predd.HitChance >= hitchance)
                                        {
                                            R2.Cast(r2predd.CastPosition);
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }              
            }
        }

        private static void Killsteal()
        {
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;

            foreach (var etarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(E.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksE && E.IsReady())
                {
                    if (etarget != null)
                    {
                        if (etarget.DistanceToPlayer() <= E.Range)
                        {
                            if (etarget.Health + etarget.AllShield + etarget.HPRegenRate <= EDamage(etarget))
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0:
                                        var epred = E.GetPrediction(etarget);
                                        if (epred.Hitchance >= HitChance.High)
                                        {
                                            E.Cast(epred.CastPosition);
                                        }

                                        break;

                                    case 1:
                                        {
                                            E.CastCircle(etarget);
                                        }

                                        break;

                                    case 2:
                                        var Epreddd = E.GetSPrediction(etarget);
                                        if (Epreddd.HitChance >= HitChance.High)
                                        {
                                            E.Cast(Epreddd.CastPosition);
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
            var lcE = Config["Clear"].GetValue<MenuBool>("LcE");
            var lcEc = Config["Clear"].GetValue<MenuSlider>("Ecount");

            if (lcE.Enabled && E.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();

                if (minions.Any())
                {
                    var efarm = E.GetCircularFarmLocation(minions);
                    if (efarm.Position.IsValid() && efarm.MinionsHit >= lcEc.Value)
                    {
                        E.Cast(efarm.Position);
                        return;
                    }
                }
            }
        }

        private static void Jungleclear()
        {
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range) E.Cast(mob.Position);
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (Config["Draw"].GetValue<MenuBool>("drawQ").Enabled)
            {
                var colorQ = Config["Draw"].GetValue<MenuColor>("colorQ").Color;
                CircleRender.Draw(Me.Position, Q.Range, colorQ, 2);
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

        private static readonly float[] EBaseDamage = {0f, 50f, 75f, 100f, 125f, 150f, 150f};
        private static readonly float[] RBaseDamage = { 0f, 150f, 250f, 350f, 350f };
        private static readonly float[] R2BaseDamage = { 0f, 187.5f, 312.5f, 437.5f, 437.5f };

        private static float EDamage(AIBaseClient target)
        {
            var elevel = E.Level;
            var eBaseDamage = EBaseDamage[elevel] + 1.25f * Me.TotalMagicalDamage;
            return (float)Me.CalculateDamage(target, DamageType.Magical, eBaseDamage);
        }

        private static float RDamage(AIBaseClient target)
        {
            var rLevel = R.Level;
            var rBaseDamage = RBaseDamage[rLevel] + .65f * Me.TotalMagicalDamage;
            return (float)Me.CalculateDamage(target, DamageType.Magical, rBaseDamage);
        }

        private static float R2Damage(AIBaseClient target)
        {
            var r2Level = R.Level;
            var r2BaseDamage = R2BaseDamage[r2Level] + .8125f * Me.TotalMagicalDamage;
            return (float)Me.CalculateDamage(target, DamageType.Magical, r2BaseDamage);
        }

        private static void OnBeforeAA(object sender, BeforeAttackEventArgs args)
        {
            if (Me.HasBuff("AurelionSolQ") || Me.HasBuff("AurelionSolW"))
            {
                args.Process = false;
            }
        }
    }
}
