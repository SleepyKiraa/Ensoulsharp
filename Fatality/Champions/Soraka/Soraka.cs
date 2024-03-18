using System;
using System.Linq;
using System.Runtime.CompilerServices;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using SharpDX;
using SPredictionMash1;

namespace Fatality.Champions.Soraka
{
    public class Soraka
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuP, menuL, menuK, menuD, menuM;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Soraka")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 800f);
            Q.SetSkillshot(0.25f, 100f, float.MaxValue, false, SpellType.Circle);

            W = new Spell(SpellSlot.W, 550f);
            W.SetTargetted(0.25f, float.MaxValue);

            E = new Spell(SpellSlot.E, 925f);
            E.SetSkillshot(0.25f, 100f, float.MaxValue, false, SpellType.Circle);

            R = new Spell(SpellSlot.R, 25000);

            Config = new Menu("Soraka", "[Fatality] Soraka", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Soraka));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo"));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Auto W Allys"));
            menuW.Add(new MenuSlider("HP", "Your HP % To Allow W usage", 20, 1, 100));
            menuW.Add(new MenuSlider("allyHP", "Ally HP % TO use W", 50, 1, 100));
            menuW.Add(new MenuSeparator("white", "White List"));
            foreach (var allys in GameObjects.AllyHeroes.Where(x => x.IsAlly && !x.IsMe))
            {
                menuW.Add(new MenuBool(allys.CharacterName, "Use W on " + allys.CharacterName));
            }
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo"));
            menuE.Add(new MenuList("EMode", "E Mode",
                new string[] { "Always", "CC" }, 0));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Auto R"));
            menuR.Add(new MenuSlider("HP", "Ally HP % to use R", 20, 1, 100));
            menuR.Add(new MenuSlider("scan", "Enemys Scan Range", 500, 250, 1500));
            menuR.Add(new MenuSeparator("whitelist", "White List"));
            foreach (var allys in GameObjects.AllyHeroes.Where(x => x.IsAlly && !x.IsMe))
            {
                menuR.Add(new MenuBool(allys.CharacterName, "Use R on " + allys.CharacterName));
            }
            Config.Add(menuR);

            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)" }, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("EPred", "E Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);

            menuK = new Menu("Killsteal", "Killsteal settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal", true));
            menuK.Add(new MenuBool("KsE", "Enable E Killsteal", true));
            Config.Add(menuK);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuSeparator("LaneClear", "Lane Clear"));
            menuL.Add(new MenuBool("LcQ", "Use Q to Lane Clear", true));
            menuL.Add(new MenuSeparator("Jungleclear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
            Config.Add(menuL);

            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuBool("AG", "AntiGapcloser", true));
            menuM.Add(new MenuBool("Int", "Interrupter", true));
            Config.Add(menuM);

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

            Game.OnUpdate += OnUpdate;
            GameEvent.OnGameTick += OnTick;
            Render.OnDraw += OnDraw;
            AntiGapcloser.OnGapcloser += Gap;
            Interrupter.OnInterrupterSpell += Int;
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void OnUpdate(EventArgs args)
        {
            Killsteal();
        }

        private static void OnTick(EventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicQ();
                LogicE();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Laneclear();
                Jungleclear();
            }
            LogicR();
            LogicW();
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

            if (Q.IsReady() && useQ.Enabled)
            {
                var qtarget = Q.GetTarget();
                if (qtarget != null && qtarget.IsValidTarget())
                {
                    switch (comb(menuP, "Pred"))
                    {
                        case 0:
                            var pred = Q.GetPrediction(qtarget);
                            if (pred.Hitchance >= hitchance)
                            {
                                Q.Cast(pred.CastPosition);
                            }
                            break;

                        case 1:
                            {
                                Q.CastCircle(qtarget);
                            }
                            break;

                        case 2:
                            var preddd = Q.GetSPrediction(qtarget);
                            if (preddd.HitChance >= hitchance)
                            {
                                Q.Cast(preddd.CastPosition);
                            }
                            break;
                    }
                }
            }
        }

        private static void LogicW()
        {
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");
            var myhp = Config["Wsettings"].GetValue<MenuSlider>("HP");
            var allyHp = Config["Wsettings"].GetValue<MenuSlider>("allyHP");

            if (W.IsReady() && useW.Enabled)
            {
                if (Me.HealthPercent >= myhp.Value)
                {
                    foreach (var ally in GameObjects.AllyHeroes.Where(y => !y.IsRecalling() && y.InRange(W.Range) && !y.IsMe && y.HealthPercent <= allyHp.Value && !y.IsDead))
                    {
                        if (Config["Wsettings"].GetValue<MenuBool>(ally.CharacterName).Enabled)
                        {
                            W.Cast(ally);
                        }
                    }
                }
            }
        }

        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");

            switch (comb(menuP, "EPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (E.IsReady() && useE.Enabled)
            {
                var etarget = E.GetTarget();
                if (etarget != null && etarget.IsValidTarget())
                {
                    switch (comb(menuE, "EMode"))
                    {
                        case 0:
                            switch (comb(menuP, "Pred"))
                            {
                                case 0:
                                    var epred = E.GetPrediction(etarget);
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
                                    var epreddd = E.GetSPrediction(etarget);
                                    if (epreddd.HitChance >= hitchance)
                                    {
                                        E.Cast(epreddd.CastPosition);
                                    }

                                    break;
                            }
                            break;

                        case 1:
                            if (etarget.HasBuffOfType(BuffType.Stun) || etarget.HasBuffOfType(BuffType.Snare) || etarget.HasBuffOfType(BuffType.Suppression))
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0:
                                        var epred = E.GetPrediction(etarget);
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
                                        var epreddd = E.GetSPrediction(etarget);
                                        if (epreddd.HitChance >= hitchance)
                                        {
                                            E.Cast(epreddd.CastPosition);
                                        }

                                        break;
                                }
                            }
                            break;
                    }
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var HP = Config["Rsettings"].GetValue<MenuSlider>("HP");
            var scan = Config["Rsettings"].GetValue<MenuSlider>("scan");

            if (R.IsReady() && useR.Enabled)
            {
                foreach (var ally in GameObjects.AllyHeroes.Where(x => !x.IsRecalling() && x.CountEnemyHeroesInRange(scan.Value) > 0 && x.HealthPercent <= HP.Value && !x.IsMe && x.IsAlly && !x.IsDead))
                {
                    if (Config["Rsettings"].GetValue<MenuBool>(ally.CharacterName).Enabled)
                    {
                        R.Cast();
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;

            foreach (var qtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(Q.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksQ && Q.IsReady())
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
                                        var qpreddd = Q.GetSPrediction(qtarget);
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
                                        var qpreddd = E.GetSPrediction(etarget);
                                        if (qpreddd.HitChance >= HitChance.High)
                                        {
                                            E.Cast(qpreddd.CastPosition);
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
            var lcQ = Config["Clear"].GetValue<MenuBool>("LcQ");

            if (lcQ.Enabled && Q.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var Qfarm = Q.GetCircularFarmLocation(minions);
                    if (Qfarm.Position.IsValid())
                    {
                        Q.Cast(Qfarm.Position);
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
        }

        private static readonly float[] QBaseDamage = { 0f, 85f, 120f, 155f, 190f, 225f, 225f };
        private static readonly float[] EBaseDamage = { 0f, 70f, 95f, 120f, 145f, 170f, 170f };

        private static float QDamage(AIBaseClient target)
        {
            var qlevel = Q.Level;
            var qBaseDamage = QBaseDamage[qlevel] + .35f * Me.TotalMagicalDamage;
            return (float)Me.CalculateDamage(target, DamageType.Magical, qBaseDamage);
        }

        private static float EDamage(AIBaseClient target)
        {
            var elevel = E.Level;
            var eBaseDamage = EBaseDamage[elevel] + .4f * Me.TotalMagicalDamage;
            return (float)Me.CalculateDamage(target, DamageType.Magical, eBaseDamage);
        }

        private static void Gap(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("AG").Enabled && E.IsReady())
            {
                if (Utils.Oktw.OktwCommon.CheckGapcloser(sender, args))
                {
                    if (Me.Distance(sender.ServerPosition) < E.Range)
                    {
                        E.Cast(sender);
                    }
                }
            }
        }

        private static void Int(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("Int").Enabled)
            {
                if (sender.IsEnemy && args.DangerLevel == Interrupter.DangerLevel.High && E.IsReady())
                {
                    if (Me.Distance(sender.ServerPosition) < E.Range)
                    {
                        E.Cast(sender);
                    }
                }
            }
        }
    }
}
