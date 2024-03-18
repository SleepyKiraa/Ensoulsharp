using System;
using System.Linq;
using System.Runtime.InteropServices;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using EnsoulSharp.SDK.Utility;
using Fatality.Utils;
using SharpDX;
using SharpDX.Direct3D11;
using SPredictionMash1;

namespace Fatality.Champions.Khazix
{
    public class Khazix
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuP, menuL, menuK, menuD;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static int colorindex = 0;
        private static bool BoolEvoQ, BoolEvoW, BoolEvoE;

        public static void OnGameload()
        {
            if (Me.CharacterName != "Khazix")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 325f);
            Q.SetTargetted(0.25f, 20f);
            W = new Spell(SpellSlot.W, 1025f);
            W.SetSkillshot(0.4f, 70f, 1700f, true, SpellType.Line);
            E = new Spell(SpellSlot.E, 700f);
            E.SetSkillshot(0.4f, 120f, 1000f, false, SpellType.Circle);
            R = new Spell(SpellSlot.R);

            Config = new Menu("Khazix", "[Fatality] Khazix", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Khazix));

            menuQ = new Menu("Qsettings", "Q settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo", true));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo", true));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo", true));
            menuE.Add(new MenuBool("noQ", "use E only when target is not in Q Range", true));
            menuE.Add(new MenuBool("safeE", "Enable E Safe Mode", true));
            menuE.Add(new MenuSeparator("eee", "Safe Mode Settings"));
            menuE.Add(new MenuBool("ignore", "Ignore Safe Settings if Target is Killable"));
            menuE.Add(new MenuSlider("targets", "Max Enemy Targets in Range on Main Target", 2, 1, 5));
            menuE.Add(new MenuSlider("targetrange", "Target Scan Range", 500, 100, 1000));
            menuE.Add(new MenuSlider("HP", "Your HP % To use E", 50, 1, 100));
            menuE.Add(new MenuKeyBind("turret", "Enable E Under Turret", Keys.T, KeyBindType.Toggle)).AddPermashow();
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo", true));
            menuR.Add(new MenuSlider("Enemys", "Min Enemys To use R", 1, 1, 5));
            menuR.Add(new MenuSlider("EnemyRange", " Scan Range for Enemys", 500, 500, 1500));
            menuR.Add(new MenuSeparator("stealth", "R Stealth Settings"));
            menuR.Add(new MenuBool("rblockaa", "Block AA when Invisible"));
            menuR.Add(new MenuBool("rblockQ", "Block Q when Invisible"));
            menuR.Add(new MenuBool("rblockW", "Block W when Invisible"));
            menuR.Add(new MenuBool("rblockE", "Block E when Invisible"));
            menuR.Add(new MenuSeparator("rr", "R Block Spells"));
            menuR.Add(new MenuBool("rblock", "Enable R Spell Block"));
            foreach (var enemys in GameObjects.EnemyHeroes.Where(x => x.IsEnemy))
            {
                if (enemys.CharacterName == "Darius")
                {
                    menuR.Add(new MenuBool("DariusR", "Block Darius R"));
                }

                if (enemys.CharacterName == "Garen")
                {
                    menuR.Add(new MenuBool("GarenR", "Block Garen R"));
                }

                if (enemys.CharacterName == "Leesin")
                {
                    menuR.Add(new MenuBool("LeesinR", "Block Leesin R"));
                }

                if (enemys.CharacterName == "Mordekaiser")
                {
                    menuR.Add(new MenuBool("MordeR", "Block Mordekaiser R"));
                }

                if (enemys.CharacterName == "Singed")
                {
                    menuR.Add(new MenuBool("SingedE", "Block Skarner E"));
                }

                if (enemys.CharacterName == "Skarner")
                {
                    menuR.Add(new MenuBool("SkarnerR", "Block Skarner R"));
                }
            }
            Config.Add(menuR);

            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)" }, 2)).AddPermashow();
            menuP.Add(new MenuList("WPred", "W Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("EPred", "E Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);

            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal", true));
            menuK.Add(new MenuBool("KsW", "use W to Killsteal", true));
            menuK.Add(new MenuBool("KsE", "use E to Killsteal", true));
            Config.Add(menuK);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuSeparator("LaneClear", "Lane Clear"));
            menuL.Add(new MenuBool("LcW", "Use W to Lane Clear", true));
            menuL.Add(new MenuSeparator("Jungleclear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
            menuL.Add(new MenuBool("JcW", "Use W to Jungle Clear", true));
            Config.Add(menuL);


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

            AIBaseClient.OnDoCast += Evade;
            GameEvent.OnGameTick += OnGameUpdate;
            Game.OnUpdate += Updatetick;
            Render.OnDraw += OnDraw;
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void Updatetick(EventArgs args)
        {
            Killsteal();

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE();
            }
        }

        private static void OnGameUpdate(EventArgs args)
        {
            if (Me.HasBuff("khazixrstealth") && Config["Rsettings"].GetValue<MenuBool>("rblockaa").Enabled)
            {
                var target = TargetSelector.GetTarget(Me.GetRealAutoAttackRange(), DamageType.Physical);
                if (target != null && target.IsValidTarget())
                {
                    if (Me.GetAutoAttackDamage(target) < target.Health)
                    {
                        Orbwalker.AttackEnabled = false;
                    }
                }
            }
            else
            {
                Orbwalker.AttackEnabled = true;
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

            if (Orbwalker.ActiveMode == OrbwalkerMode.Harass)
            {

            }
            Evo();
        }

        private static void LogicQ()
        {
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");
            var rblockq = Config["Rsettings"].GetValue<MenuBool>("rblockQ");

            if (rblockq.Enabled && Me.HasBuff("khazixrstealth"))
            {
                return;
            }

            if (Q.IsReady() && useQ.Enabled)
            {
                var qtarget = Q.GetTarget();
                if (qtarget != null && qtarget.IsValidTarget())
                {
                    Q.Cast(qtarget);
                }
            }
        }

        private static void LogicW()
        {
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");
            var rblockw = Config["Rsettings"].GetValue<MenuBool>("rblockW");

            if (rblockw.Enabled && Me.HasBuff("khazixrstealth"))
            {
                return;
            }

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
                var wtarget = TargetSelector.GetTarget(W.Range, DamageType.Physical);
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
                            W.CastLine(wtarget);
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
            var noQ = Config["Esettings"].GetValue<MenuBool>("noQ");
            var SafeE = Config["Esettings"].GetValue<MenuBool>("safeE");
            var ignore = Config["Esettings"].GetValue<MenuBool>("ignore");
            var target = Config["Esettings"].GetValue<MenuSlider>("targets");
            var targetrange = Config["Esettings"].GetValue<MenuSlider>("targetrange");
            var Health = Config["Esettings"].GetValue<MenuSlider>("HP");
            var turret = Config["Esettings"].GetValue<MenuKeyBind>("turret");
            var rblocke = Config["Rsettings"].GetValue<MenuBool>("rblockE");
            var etarget = TargetSelector.GetTarget(E.Range, DamageType.Physical);

            if (rblocke.Enabled && Me.HasBuff("khazixrstealth"))
            {
                return;
            }

            if (noQ.Enabled && etarget != null && etarget.InRange(Q.Range))
            {
                return;
            }

            if (!turret.Active && etarget != null && etarget.IsUnderEnemyTurret())
            {
                return;
            }

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
                if (etarget != null && etarget.IsValidTarget())
                {
                    if (!SafeE.Enabled)
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
                                E.CastCircle(etarget);
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

                    if (SafeE.Enabled)
                    {
                        if (ignore.Enabled)
                        {
                            if (GetComboDamage(etarget) >= etarget.Health)
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
                                        E.CastCircle(etarget);
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

                        if (!ignore.Enabled)
                        {
                            if (etarget.CountEnemyHeroesInRange(targetrange.Value) > target.Value ||
                                Me.HealthPercent < Health.Value)
                            {
                                return;
                            }

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
                                    E.CastCircle(etarget);
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
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var enemys = Config["Rsettings"].GetValue<MenuSlider>("Enemys");
            var scanrange = Config["Rsettings"].GetValue<MenuSlider>("EnemyRange");

            if (useR.Enabled && R.IsReady())
            {
                if (Me.CountEnemyHeroesInRange(scanrange.Value) >= enemys.Value)
                {
                    R.Cast();
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;
            foreach (var target in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(Q.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage")))
            {
                if (ksQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                {
                    if (target != null)
                    {
                        if (target.DistanceToPlayer() <= Q.Range)
                        {
                            if (!IsIsolated(target))
                            {
                                if (target.Health + target.AllShield <= QDamage(target))
                                {
                                    Q.Cast(target);
                                }
                            }
                            else if (IsIsolated(target))
                            {
                                if (target.Health + target.AllShield + target.HPRegenRate <= QIsolated(target))
                                {
                                    Q.Cast(target);
                                }
                            }
                        }
                    }
                }
            }

            foreach (var target2 in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(W.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage")))
            {
                if (ksW && W.IsReady() && target2.IsValidTarget(W.Range))
                {
                    if (target2 != null)
                    {
                        if (target2.DistanceToPlayer() <= W.Range)
                        {
                            if (target2.Health + target2.AllShield <= WDamage(target2))
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0://SDk
                                        var wpred = W.GetPrediction(target2, false);
                                        if (wpred.Hitchance >= HitChance.High)
                                        {
                                            W.Cast(wpred.CastPosition);
                                        }

                                        break;

                                    case 1://Oktw
                                        {
                                            W.CastLine(target2, 0f, 0f, false);
                                        }

                                        break;

                                    case 2:
                                        var wpreddd = SPredictionMash1.Prediction.GetPrediction(target2, W.Width, W.Delay, W.Speed,
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

            foreach (var target3 in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(E.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage")))
            {
                if (ksE && E.IsReady() && target3.IsValidTarget(E.Range))
                {
                    if (target3 != null)
                    {
                        if (target3.DistanceToPlayer() <= E.Range)
                        {
                            if (target3.Health + target3.AllShield <= EDamage(target3))
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0://SDk
                                        var Epred = E.GetPrediction(target3, false);
                                        if (Epred.Hitchance >= HitChance.High)
                                        {
                                            E.Cast(Epred.CastPosition);
                                        }

                                        break;

                                    case 1://Oktw
                                        {
                                            E.CastCircle(target3);
                                        }

                                        break;

                                    case 2:
                                        var Epreddd = SPredictionMash1.Prediction.GetPrediction(target3, E.Width, E.Delay, E.Speed,
                                            E.Range, false, SpellType.Circle);
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
            var lcw = Config["Clear"].GetValue<MenuBool>("LcW");

            if (lcw.Enabled && W.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();

                if (minions.Any())
                {
                    var Wfarm = W.GetLineFarmLocation(minions);
                    if (Wfarm.Position.IsValid())
                    {
                        W.Cast(Wfarm.Position);
                        return;
                    }
                }
            }
        }

        private static void Jungleclear()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var JcWw = Config["Clear"].GetValue<MenuBool>("JcW");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(W.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob);
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast(mob.Position);
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

                    break;
            }
        }

        private static void Evade(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            var ruse = Config["Rsettings"].GetValue<MenuBool>("rblock");

            foreach (var enemys in GameObjects.EnemyHeroes.Where(x => x.IsEnemy))
            {
                if (enemys.CharacterName == "Darius")
                {
                    if (Config["Rsettings"].GetValue<MenuBool>("DariusR").Enabled)
                    {
                        if (sender.CharacterName == "Darius" && args.Slot == SpellSlot.R)
                        {
                            if (args.Target.IsMe)
                            {
                                R.Cast();
                            }
                        }
                    }
                }

                if (enemys.CharacterName == "Garen")
                {
                    if (Config["Rsettings"].GetValue<MenuBool>("GarenR").Enabled)
                    {
                        if (sender.CharacterName == "Garen" && args.Slot == SpellSlot.R)
                        {
                            if (args.Target.IsMe)
                            {
                                R.Cast();
                            }
                        }
                    }
                }

                if (enemys.CharacterName == "Leesin")
                {
                    if (Config["Rsettings"].GetValue<MenuBool>("LeesinR").Enabled)
                    {
                        if (sender.CharacterName == "Leesin" && args.Slot == SpellSlot.R)
                        {
                            if (args.Target.IsMe)
                            {
                                R.Cast();
                            }
                        }
                    }
                }

                if (enemys.CharacterName == "Singed")
                {
                    if (Config["Rsettings"].GetValue<MenuBool>("SingedE").Enabled)
                    {
                        if (sender.CharacterName == "Singed" && args.Slot == SpellSlot.E)
                        {
                            if (args.Target.IsMe)
                            {
                                R.Cast();
                            }
                        }
                    }
                }

                if (enemys.CharacterName == "Skarner")
                {
                    if (Config["Rsettings"].GetValue<MenuBool>("SkarnerR").Enabled)
                    {
                        if (sender.CharacterName == "Skarner" && args.Slot == SpellSlot.R)
                        {
                            if (args.Target.IsMe)
                            {
                                R.Cast();
                            }
                        }
                    }
                }
            }
        }

        private static float GetComboDamage(AIHeroClient target)
        {
            var Damage = 0d;
            if (Q.IsReady())
            {
                if (!IsIsolated(target))
                {
                    Damage += QDamage(target);
                }
                else if (IsIsolated(target))
                {
                    Damage += QIsolated(target);
                }
            }

            if (W.IsReady())
            {
                Damage += WDamage(target);
            }

            if (E.IsReady())
            {
                Damage += EDamage(target);
            }

            if (R.IsReady())
            {
                Damage += R.GetDamage(target);
            }

            return (float)Damage;
        }

        private static void Evo()
        {
            if (!BoolEvoQ && Me.HasBuff("KhazixQEvo"))
            {
                Q.Range = 375f;
                BoolEvoQ = true;
            }

            if (!BoolEvoW && Me.HasBuff("KhazixWEvo"))
            {
                BoolEvoW = true;
            }

            if (!BoolEvoE && Me.HasBuff("KhazixEEvo"))
            {
                E.Range = 900f;
                BoolEvoE = true;
            }
        }

        private static readonly float[] QBaseDamage = { 0f, 60f, 85f, 110f, 135f, 160f, 160f };
        private static readonly float[] QIsolatedDamage = { 0f, 126f, 178.5f, 231f, 283.5f, 336f, 336f };
        private static readonly float[] WBaseDamage = { 0f, 85f, 115f, 145f, 175f, 205f, 205f };
        private static readonly float[] EBaseDamage = { 0f, 65f, 100f, 135f, 170f, 205f, 205f };

        private static float QDamage(AIBaseClient target)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] + 1.15f * GameObjects.Player.TotalAttackDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, qBaseDamage);
        }

        private static float QIsolated(AIBaseClient target)
        {
            var qLevel = Q.Level;
            var qIsolatedDamage = QIsolatedDamage[qLevel] + 2.41f * Me.GetBonusPhysicalDamage();
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, qIsolatedDamage);
        }

        private static float WDamage(AIBaseClient target)
        {
            var wLevel = W.Level;
            var wBaseDamage = WBaseDamage[wLevel] + 1f * GameObjects.Player.TotalAttackDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, wBaseDamage);
        }

        private static float EDamage(AIBaseClient target)
        {
            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel] + .2f * GameObjects.Player.TotalAttackDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, eBaseDamage);
        }

        private static bool IsIsolated(AIBaseClient enemy)
        {
            return
                !ObjectManager.Get<AIBaseClient>()
                    .Any(
                        x =>
                            (x.NetworkId != enemy.NetworkId) && (x.Team == enemy.Team) && (x.Distance(enemy) <= 500) &&
                            ((x.Type == GameObjectType.AIHeroClient) || (x.Type == GameObjectType.AIMinionClient) ||
                             (x.Type == GameObjectType.AITurretClient)));
        }
    }
}