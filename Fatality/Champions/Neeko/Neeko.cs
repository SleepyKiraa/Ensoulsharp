using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using SharpDX;
using SPredictionMash1;

namespace Fatality.Champions.Neeko
{
    public class Neeko
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuK, menuD, menuM, menuP, menuL;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static int colorindex = 0;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Neeko")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 800f);
            Q.SetSkillshot(0.25f, 125f, float.MaxValue, false, SpellType.Circle);

            W = new Spell(SpellSlot.W, 900f);

            E = new Spell(SpellSlot.E, 1000f);
            E.SetSkillshot(0.25f, 70f, 1300f, false, SpellType.Line);

            R = new Spell(SpellSlot.R, 600f);

            Config = new Menu("Neeko", "[Fatality] Neeko", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Neeko));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo"));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Auto W on R"));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo"));
            menuE.Add(new MenuBool("autoE", "Auto E on Dashing Targets"));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo"));
            menuR.Add(new MenuSlider("enemys", "Min Enemys To R", 2, 1, 5));
            menuR.Add(new MenuSlider("range", "Adjust R Range", 570, 300, 600));
            Config.Add(menuR);

            menuP = new Menu("Pred", "Prediction Settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)" }, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "Very High" }, 2));
            menuP.Add(new MenuList("EPred", "E Hitchance",
                new string[] { "Low", "Medium", "High", "Very High" }, 2));
            Config.Add(menuP);

            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal"));
            menuK.Add(new MenuBool("KsE", "Enable E Killsteal"));
            Config.Add(menuK);

            menuL = new Menu("Clear", "Clear Settings");
            menuL.Add(new MenuSeparator("LaneClear", "Lane Clear"));
            menuL.Add(new MenuBool("LcQ", "Use Q to Lane Clear", true));
            menuL.Add(new MenuSlider("LcQM", "Q Minions Hit", 3, 1, 5));
            menuL.Add(new MenuBool("LcE", "Use E to Lane Clear", true));
            menuL.Add(new MenuSlider("LcEM", "E Minions Hit", 3, 1, 5));
            menuL.Add(new MenuSeparator("JungleClear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
            menuL.Add(new MenuBool("JcE", "Use E to Jungle Clear", true));
            Config.Add(menuL);

            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuBool("AG", "AntiGapcloser", true));
            Config.Add(menuM);

            menuD = new Menu("Draw", "Draw Settings");
            menuD.Add(new MenuSeparator("mm", "Main Draw Settings"));
            menuD.Add(new MenuList("mode", "Draw Mode",
                new string[] { "static", "Rainbow" }, 0));
            menuD.Add(new MenuSlider("speed", "Change Rainbow Speed", 1000, 500, 1500));
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
            AntiGapcloser.OnGapcloser += Gap;
            Render.OnDraw += OnDraw;
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void OnTick(EventArgs args)
        {
            Killsteal();
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicR();
                LogicE();
                LogicQ();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Laneclear();
                Jungleclear();
            }

            if (Config["Wsettings"].GetValue<MenuBool>("useW").Enabled)
            {
                if (Me.HasBuff("NeekoR"))
                {
                    W.Cast(Game.CursorPos);
                }
            }

            if (Config["Esettings"].GetValue<MenuBool>("autoE").Enabled)
            {
                if (E.IsReady())
                {
                    foreach (var enemys in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(E.Range)))
                    {
                        E.CastIfHitchanceEquals(enemys, HitChance.Dash);
                    }
                }
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

            if (useQ.Enabled && Q.IsReady())
            {
                var qtarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
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
                            Q.CastCircle(qtarget);
                            break;

                        case 2:
                            var qpredd = Q.GetSPrediction(qtarget);
                            if (qpredd.HitChance >= hitchance)
                            {
                                Q.Cast(qpredd.CastPosition);
                            }

                            break;
                    }
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

            if (useE.Enabled && E.IsReady())
            {
                var etarget = TargetSelector.GetTarget(E.Range, DamageType.Magical);
                if (etarget != null && etarget.IsValidTarget())
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
                            E.CastLine(etarget);
                            break;

                        case 2:
                            var epredd = E.GetSPrediction(etarget);
                            if (epredd.HitChance >= hitchance)
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
            var renemys = Config["Rsettings"].GetValue<MenuSlider>("enemys");
            var range = Config["Rsettings"].GetValue<MenuSlider>("range");

            if (R.IsReady() && useR.Enabled)
            {
                if (Me.CountEnemyHeroesInRange(range.Value) >= renemys.Value)
                {
                    R.Cast();
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;
            foreach (var Qtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(Q.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksQ && Q.IsReady())
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
                                            Q.CastCircle(Qtarget);
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

            foreach (var Etarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(E.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksE && E.IsReady())
                {
                    if (Etarget != null)
                    {
                        if (Etarget.DistanceToPlayer() <= E.Range)
                        {
                            if (Etarget.Health + Etarget.AllShield + Etarget.HPRegenRate <= EDamage(Etarget))
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0:
                                        var epred = E.GetPrediction(Etarget);
                                        if (epred.Hitchance >= HitChance.High)
                                        {
                                            E.Cast(epred.CastPosition);
                                        }

                                        break;

                                    case 1:
                                        {
                                            E.CastLine(Etarget);
                                        }
                                        break;

                                    case 2:
                                        var epredd = E.GetSPrediction(Etarget);
                                        if (epredd.HitChance >= HitChance.High)
                                        {
                                            E.Cast(epredd.CastPosition);
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
            var lcQc = Config["Clear"].GetValue<MenuSlider>("LcQM");
            var lcE = Config["Clear"].GetValue<MenuBool>("LcE");
            var lcEc = Config["Clear"].GetValue<MenuSlider>("LcEM");

            if (lcQ.Enabled && Q.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var Qfarm = Q.GetCircularFarmLocation(minions);
                    if (Qfarm.Position.IsValid() && Qfarm.MinionsHit >= lcQc.Value)
                    {
                        Q.Cast(Qfarm.Position);
                    }
                }
            }

            if (lcE.Enabled && E.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var Efarm = E.GetLineFarmLocation(minions);
                    if (Efarm.Position.IsValid() && Efarm.MinionsHit >= lcEc.Value)
                    {
                        E.Cast(Efarm.Position);
                    }
                }
            }
        }

        private static void Jungleclear()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(W.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range) E.Cast(mob.Position);
            }
        }

        private static void OnDraw(EventArgs args)
        {
            var rainspeed = Config["Draw"].GetValue<MenuSlider>("speed");

            colorindex++;
            if (colorindex >= rainspeed.Value)
                colorindex = 0;

            switch (comb(menuD, "mode"))
            {
                case 0:
                    if (Config["Draw"].GetValue<MenuBool>("drawQ").Enabled && Q.Level > 0)
                    {
                        var colorQ = Config["Draw"].GetValue<MenuColor>("colorQ").Color;
                        CircleRender.Draw(Me.Position, Q.Range, colorQ, 2);
                    }

                    if (Config["Draw"].GetValue<MenuBool>("drawW").Enabled && W.Level > 0)
                    {
                        var colorW = Config["Draw"].GetValue<MenuColor>("colorW").Color;
                        CircleRender.Draw(Me.Position, W.Range, colorW, 2);
                    }

                    if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled && E.Level > 0)
                    {
                        var colorE = Config["Draw"].GetValue<MenuColor>("colorE").Color;
                        CircleRender.Draw(Me.Position, E.Range, colorE, 2);
                    }

                    if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled && R.Level > 0)
                    {
                        var colorR = Config["Draw"].GetValue<MenuColor>("colorR").Color;
                        var range = Config["Rsettings"].GetValue<MenuSlider>("range");
                        CircleRender.Draw(Me.Position, range.Value, colorR, 2);
                    }
                    break;

                case 1:
                    if (Config["Draw"].GetValue<MenuBool>("drawQ").Enabled && Q.Level > 0)
                    {
                        var colorQ = Base.PlusRender.GetFullColorList(rainspeed.Value);
                        CircleRender.Draw(Me.Position, Q.Range, colorQ[colorindex], 2);
                    }

                    if (Config["Draw"].GetValue<MenuBool>("drawW").Enabled && W.Level > 0)
                    {
                        var colorW = Base.PlusRender.GetFullColorList(rainspeed.Value);
                        CircleRender.Draw(Me.Position, W.Range, colorW[colorindex], 2);
                    }

                    if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled && E.Level > 0)
                    {
                        var coloE = Base.PlusRender.GetFullColorList(rainspeed.Value);
                        CircleRender.Draw(Me.Position, E.Range, coloE[colorindex], 2);
                    }

                    if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled && R.Level > 0)
                    {
                        var color = Base.PlusRender.GetFullColorList(rainspeed.Value);
                        var range = Config["Rsettings"].GetValue<MenuSlider>("range");
                        CircleRender.Draw(Me.Position, range.Value, color[colorindex], 2);
                    }
                    break;
            }
        }

        private static void Gap(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("AG").Enabled)
            {
                if (E.IsReady())
                {
                    if (sender.IsValid && sender.IsEnemy)
                    {
                        if (args.EndPosition.DistanceToPlayer() < 350)
                        {
                            E.CastIfHitchanceEquals(sender, HitChance.Dash);
                        }
                    }
                }
            }
        }

        private static readonly float[] QBaseDamage = { 0f, 160f, 255f, 350f, 445f, 540f, 540f };
        private static readonly float[] EBaseDamage = { 0f, 80f, 115f, 150f, 185f, 220f, 220f };

        private static float QDamage(AIBaseClient target)
        {
            var qlevel = Q.Level;
            var qBasedamage = QBaseDamage[qlevel] + .9f * Me.TotalMagicalDamage;
            return (float)Me.CalculateDamage(target, DamageType.Magical, qBasedamage);
        }

        private static float EDamage(AIBaseClient target)
        {
            var elevel = E.Level;
            var eBaseDamage = EBaseDamage[elevel] + .6f * Me.TotalMagicalDamage;
            return (float)Me.CalculateDamage(target, DamageType.Magical, eBaseDamage);
        }
    }
}