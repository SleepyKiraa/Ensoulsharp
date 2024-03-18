using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using EnsoulSharp.SDK.Utility;
using Fatality.Utils;
using SharpDX;

namespace Fatality.Champions.Xerath
{
    public class Xerath
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuP, menuL, menuK, menuD, menuM;
        private static AIHeroClient Me = ObjectManager.Player;
        private static EnsoulSharp.SDK.HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Xerath")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 735f);
            Q.SetSkillshot(0.5f, 60f, 3000f, false, SpellType.Line);
            Q.SetCharged("XerathArcanopulseChargeUp", "XerathArcanopulseChargeUp", 735, 1450, 1.5f);
            W = new Spell(SpellSlot.W, 1000f);
            W.SetSkillshot(0.5f, 120f, float.MaxValue, false, SpellType.Circle);
            E = new Spell(SpellSlot.E, 1125f);
            E.SetSkillshot(0.4f, 60f, 1400f, true, SpellType.Line);
            R = new Spell(SpellSlot.R, 5000f);
            R.SetSkillshot(0.6f, 100f, float.MaxValue, false, SpellType.Circle);

            Config = new Menu("Xerath", "[Fatality] Xerath", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Xerath));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo", true));
            menuQ.Add(new MenuList("Qmode", "Q Mode",
                new string[] { "Full Charge", "Fast Charge"}, 1));
            menuQ.Add(new MenuSlider("over", "Q Fast Charge Range", 30, 0, 350));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo", true));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo", true));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R settings");
            menuR.Add(new MenuSlider("Cusor", "Cusor Range", 400, 0, 2000));
            menuR.Add(new MenuSlider("Delay", "R Delay", 250, 0, 1000));
            Config.Add(menuR);

            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("WPred", "W Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("EPred", "E Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("RPred", "R Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);

            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal", true));
            menuK.Add(new MenuBool("KsW", "use W to Killsteal", true));
            menuK.Add(new MenuBool("KsE", "use E to Killsteal", true));
            Config.Add(menuK);

            menuL = new Menu("Clear", "Clear Settings");
            menuL.Add(new MenuSeparator("LaneClear", "Lane Clear"));
            menuL.Add(new MenuBool("LcW", "Use W to Lane Clear", true));
            menuL.Add(new MenuSeparator("JungleClear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcW", "Use W to Jungle Clear", true));
            menuL.Add(new MenuBool("JcE", "Use E to Jungle Clear", true));
            Config.Add(menuL);

            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuBool("AG", "AntiGapcloser", true));
            menuM.Add(new MenuBool("Int", "Interrupter", true));
            menuM.Add(new MenuBool("dash", "Auto W on Dashing Target", true));
            menuM.Add(new MenuBool("cc", "Auto W on CC", true));
            Config.Add(menuM);

            menuD = new Menu("Draw", "Draw settings");
            menuD.Add(new MenuBool("drawQ", "Q Range  (White)", true));
            menuD.Add(new MenuBool("drawW", "W Range  (Blue)", true));
            menuD.Add(new MenuBool("drawE", "E Range (Green)", true));
            menuD.Add(new MenuBool("drawR", "R Range (Red)", true));
            menuD.Add(new MenuBool("drawCusor", "R Cusor Range  (Red)", true));
            menuD.Add(new MenuBool("drawKill", "R Killable Message", true));
            menuD.Add(new MenuBool("drawBuff", "Draw R Buff Time", true));
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
            Game.OnUpdate += Updatetick;
            Render.OnDraw += OnDraw;
            Render.OnEndScene += OnEndScene;
            AntiGapcloser.OnGapcloser += Gap;
            Interrupter.OnInterrupterSpell += Int;
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void Updatetick(EventArgs args)
        {
            Killsteal();
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Me.HasBuff("XerathLocusOfPower2"))
            {
                Orbwalker.AttackEnabled = false;
                Orbwalker.MoveEnabled = false;
                
                switch (comb(menuP, "RPred"))
                {
                    case 0: hitchance = HitChance.Low; break;
                    case 1: hitchance = HitChance.Medium; break;
                    case 2: hitchance = HitChance.High; break;
                    case 3: hitchance = HitChance.VeryHigh; break;
                    default: hitchance = HitChance.High; break;
                }               

                var targets = GameObjects.EnemyHeroes.Where(i =>
                        i.Distance(Game.CursorPos) <= Config["Rsettings"].GetValue<MenuSlider>("Cusor").Value &&
                        !i.IsDead).OrderBy(i => i.Health);

                    if (targets != null)
                    {
                        var target = targets.Find(i =>
                            i.DistanceToCursor() <= Config["Rsettings"].GetValue<MenuSlider>("Cusor").Value);
                        if (target != null)
                        {
                            var rdelay = Config["Rsettings"].GetValue<MenuSlider>("Delay");
                            switch (comb(menuP, "Pred"))
                            {
                                case 0:
                                    if (Variables.GameTimeTickCount - R.LastCastAttemptTime >= rdelay.Value)
                                    {
                                        var rpred = R.GetPrediction(target);
                                        if (rpred.Hitchance >= hitchance)
                                        {
                                            R.Cast(rpred.CastPosition);
                                        }
                                    }

                                    break;
                                
                                case 1:
                                    if (Variables.GameTimeTickCount - R.LastCastAttemptTime >= rdelay.Value)
                                    {
                                        R.CastCircle(target);
                                    }

                                    break;
                                
                                case 2:
                                    if (Variables.GameTimeTickCount - R.LastCastAttemptTime >= rdelay.Value)
                                    {
                                        var rpreddd = SPredictionMash1.Prediction.GetPrediction(target, R.Width, R.Delay,
                                            R.Speed, R.Range, false, SpellType.Circle);
                                        if (rpreddd.HitChance >= hitchance)
                                        {
                                            R.Cast(rpreddd.CastPosition);
                                        }
                                    }

                                    break;
                            }
                        }
                    }
            }

            if (!Me.HasBuff("XerathLocusOfPower2"))
            {
                Orbwalker.AttackEnabled = true;
                Orbwalker.MoveEnabled = true;
            }
            
            if (Q.IsCharging && Orbwalker.ActiveMode != OrbwalkerMode.None)
            {
                Orbwalker.AttackEnabled = false;
                ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }

            if (Config["Misc"].GetValue<MenuBool>("dash").Enabled)
            {
                if (W.IsReady())
                {
                    foreach (var enemy in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(W.Range)))
                    {
                        W.CastIfHitchanceEquals(enemy, HitChance.Dash);
                    }
                }

                if (R.IsReady() && Me.HasBuff("XerathLocusOfPower2"))
                {
                    foreach (var renemy in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(R.Range)))
                    {
                        R.CastIfHitchanceEquals(renemy, HitChance.Dash);
                    }
                }
            }
            
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicW();
                LogicE();
                LogicQ();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Laneclear();
                Jungleclear();
            }
            movement();
        }

        private static void LogicQ()
        {
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");
            var overchargedRange = Config["Qsettings"].GetValue<MenuSlider>("over");

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

            if (!Me.HasBuff("XerathLocusOfPower2"))
            {
                if (useQ.Enabled && Q.IsReady())
                {
                    var qtarget = TargetSelector.GetTarget(Q.ChargedMaxRange, DamageType.Magical);
                    if (qtarget != null)
                    {
                        switch (comb(menuP, "Pred"))
                        {
                            case 0:
                                var pred = Q.GetPrediction(qtarget);
                                if (pred.Hitchance >= hitchance)
                                {
                                    switch (comb(menuQ, "Qmode"))
                                    {
                                        case 0:
                                            if (!Q.IsCharging)
                                            {
                                                Q.StartCharging();
                                                return;
                                            }

                                            if (Q.Range == Q.ChargedMaxRange)
                                            {
                                                var Pred2 = Q.GetPrediction(qtarget, true);
                                                if (Pred2.Hitchance >= hitchance)
                                                {
                                                    Q.ShootChargedSpell(Pred2.CastPosition);
                                                }
                                            }
                                            break;

                                        case 1:
                                            if (!Q.IsCharging)
                                            {
                                                Q.StartCharging();
                                            }

                                            if (Me.InRange(pred.UnitPosition + overchargedRange.Value * (pred.UnitPosition - Me.ServerPosition).Normalized(), Q.Range))
                                            {
                                                Q.ShootChargedSpell(pred.CastPosition);
                                            }
                                            break;
                                    }

                                }

                                break;

                            case 1:
                                var predd = Q.GetPrediction(qtarget);
                                if (predd.Hitchance >= hitchance)
                                {
                                    switch (comb(menuQ, "Qmode"))
                                    {
                                        case 0:
                                            if (!Q.IsCharging)
                                            {
                                                Q.StartCharging();
                                                return;
                                            }

                                            if (Q.Range == Q.ChargedMaxRange)
                                            {
                                                Q.CastLine(qtarget);
                                            }
                                            break;

                                        case 1:
                                            if (!Q.IsCharging)
                                            {
                                                Q.StartCharging();
                                                return;
                                            }

                                            if (Me.InRange(predd.UnitPosition + overchargedRange.Value * (predd.UnitPosition - Me.ServerPosition).Normalized(), Q.Range))
                                            {
                                                Q.CastLine(qtarget);
                                            }
                                            break;
                                    }                                  
                                }

                                break;

                            case 2:
                                var preddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width, Q.Delay,
                                    Q.Speed, Q.ChargedMaxRange, false, SpellType.Line);
                                var qpred = Q.GetPrediction(qtarget);
                                if (preddd.HitChance >= hitchance)
                                {
                                    switch (comb(menuQ, "Qmode"))
                                    {
                                        case 0:
                                            if (!Q.IsCharging)
                                            {
                                                Q.StartCharging();
                                                return;
                                            }

                                            if (Q.Range == Q.ChargedMaxRange)
                                            {
                                                var Predd2 = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width,
                                                    Q.Delay, Q.Speed, Q.ChargedMaxRange, false, SpellType.Line);
                                                if (Predd2.HitChance >= hitchance)
                                                {
                                                    Q.ShootChargedSpell(Predd2.CastPosition);
                                                }
                                            }
                                            break;

                                        case 1:
                                            if (!Q.IsCharging)
                                            {
                                                Q.StartCharging();
                                                return;
                                            }

                                            if (Me.InRange(qpred.UnitPosition + overchargedRange.Value * (qpred.UnitPosition - Me.ServerPosition).Normalized(), Q.Range))
                                            {
                                                Q.ShootChargedSpell(preddd.CastPosition);
                                            }
                                            break;
                                    }
                                }

                                break;
                        
                        }
                    }
                }
            }
        }

        private static void LogicW()
        {
            var wtarget = W.GetTarget();
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");
            if (wtarget == null) return;
            
            switch (comb(menuP, "WPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }          

            if (!Me.HasBuff("XerathLocusOfPower2"))
            {
                if (wtarget.InRange(W.Range))
                {
                    if (W.IsReady() && wtarget.IsValidTarget(W.Range) && useW.Enabled)
                    {
                        switch (comb(menuP, "Pred"))
                        {
                            case 0:
                                var Wpred = W.GetPrediction(wtarget, true);
                                if (Wpred.Hitchance >= hitchance)
                                {
                                    W.Cast(Wpred.CastPosition);
                                }

                                break;
                            
                            case 1:
                                {
                                    W.CastCircle(wtarget);
                                }

                                break;
                            
                            case 2:
                                var wpreddd = SPredictionMash1.Prediction.GetPrediction(wtarget, W.Width, W.Delay,
                                    W.Speed, W.Range, false, SpellType.Circle);
                                if (wpreddd.HitChance >= hitchance)
                                {
                                    W.Cast(wpreddd.CastPosition);
                                }
                                break;
                        }
                    }
                }
            }
        }

        private static void LogicE()
        {
            var Etarget = E.GetTarget();
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            if (Etarget == null) return;
            
            switch (comb(menuP, "EPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }          

            if (!Me.HasBuff("XerathLocusOfPower2"))
            {
                if (Etarget.InRange(E.Range) && useE.Enabled)
                {
                    if (E.IsReady() && Q.IsReady() || W.IsReady() && Etarget.IsValidTarget())
                    {
                        switch (comb(menuP, "Pred"))
                        {
                            case 0:
                                var Epred = E.GetPrediction(Etarget);
                                if (Epred.Hitchance >= hitchance)
                                {
                                    E.Cast(Epred.CastPosition);
                                }

                                break;
                            
                            case 1:
                                {
                                    E.CastLine(Etarget, 0f, 0f, false);
                                }

                                break;
                            
                            case 2:
                                var Epreddd = SPredictionMash1.Prediction.GetPrediction(Etarget, E.Width, E.Delay,
                                    E.Speed, E.Range, true, SpellType.Line);
                                if (Epreddd.HitChance >= hitchance)
                                {
                                    E.Cast(Epreddd.CastPosition);
                                }

                                break;
                        }
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;

            foreach (var qtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(Q.ChargedMaxRange) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (!Me.HasBuff("XerathLocusOfPower2"))
                {
                    if (ksQ && Q.IsReady())
                    {
                        if (qtarget != null)
                        {
                            if (qtarget.Health + qtarget.AllShield <= QDamage(qtarget))
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0:
                                        var prediction = Q.GetPrediction(qtarget);
                                        if (prediction.Hitchance >= HitChance.High)
                                        {
                                            if (!Q.IsCharging)
                                            {
                                                Q.StartCharging();
                                                return;
                                            }
                                    
                                            if (Q.Range == Q.ChargedMaxRange)
                                            {
                                                var Predd = Q.GetPrediction(qtarget);
                                                if (Predd.Hitchance >= HitChance.High)
                                                {
                                                    Q.ShootChargedSpell(Predd.CastPosition);
                                                }
                                            }
                                        }

                                        break;
                                    
                                    case 1:
                                        var predictionn = Q.GetPrediction(qtarget);
                                        if (predictionn.Hitchance >= HitChance.High)
                                        {
                                            if (!Q.IsCharging)
                                            {
                                                Q.StartCharging();
                                                return;
                                            }
                                    
                                            if (Q.Range == Q.ChargedMaxRange)
                                            {
                                                Q.CastLine(qtarget);
                                            }
                                        }

                                        break;
                                    
                                    case 2:
                                        var preddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width, Q.Delay, Q.Speed, Q.ChargedMaxRange, false, SpellType.Line);
                                        if (preddd.HitChance >= HitChance.High)
                                        {
                                            if (!Q.IsCharging)
                                            {
                                                Q.StartCharging();
                                                return;
                                            }

                                            if (Q.Range == Q.ChargedMaxRange)
                                            {
                                                var Predd2 = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width, Q.Delay, Q.Speed, Q.ChargedMaxRange, false, SpellType.Line);
                                                if (Predd2.HitChance >= HitChance.High)
                                                {
                                                    Q.ShootChargedSpell(Predd2.CastPosition);
                                                }
                                            }
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
                if (!Me.HasBuff("XerathLocusOfPower2"))
                {
                    if (ksW && W.IsReady() && wtarget.IsValidTarget(W.Range))
                    {
                        if (wtarget != null)
                        {
                            if (wtarget.Health + wtarget.AllShield <= WDamage(wtarget))
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0:
                                        var Wpred = W.GetPrediction(wtarget, true);
                                        if (Wpred.Hitchance >= HitChance.High)
                                        {
                                            W.Cast(Wpred.CastPosition);
                                        }

                                        break;
                                    
                                    case 1:
                                        {
                                            W.CastCircle(wtarget);
                                        }

                                        break;
                                    
                                    case 2:
                                        var wpreddd = SPredictionMash1.Prediction.GetPrediction(wtarget, W.Width, W.Delay,
                                            W.Speed, W.Range, false, SpellType.Circle);
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

            foreach (var etarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(E.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (!Me.HasBuff("XerathLocusOfPower2"))
                {
                    if (ksE && E.IsReady() && etarget.IsValidTarget(E.Range))
                    {
                        if (etarget != null)
                        {
                            if (etarget.Health + etarget.AllShield <= EDamage(etarget))
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
                                            E.CastLine(etarget);
                                        }

                                        break;
                                    
                                    case 2:
                                        var Epreddd = SPredictionMash1.Prediction.GetPrediction(etarget, E.Width, E.Delay,
                                            E.Speed, E.Range, true, SpellType.Line);
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
            var JcWw = Config["Clear"].GetValue<MenuBool>("JcW");
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(W.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast(mob.Position);
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range) E.Cast(mob.Position);
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (Config["Draw"].GetValue<MenuBool>("drawQ").Enabled )
            {
                CircleRender.Draw(Me.Position, Q.Range, Color.White, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawW").Enabled)
            {
                CircleRender.Draw(Me.Position, W.Range, Color.Blue, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled)
            {
                CircleRender.Draw(Me.Position, E.Range, Color.Green, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawCusor").Enabled)
            {
                if (Me.HasBuff("XerathLocusOfPower2"))
                {
                    CircleRender.Draw(Game.CursorPos, Config["Rsettings"].GetValue<MenuSlider>("Cusor").Value, Color.Red, 2);               
                }
            }

            if (Config["Draw"].GetValue<MenuBool>("drawKill").Enabled)
            {
                var mybarpos = Me.HPBarPosition;
                var offset = 0;
                foreach (var enemy in GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.IsValidTarget(R.Range)))
                {
                    if (R.Level == 1)
                    {
                        if (RDamage(enemy) * 3 > enemy.Health + enemy.AllShield + enemy.HPRegenRate)
                        {
                            Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f + offset, System.Drawing.Color.Red, "Ult can Kill: " + enemy.CharacterName + " HP left: " + enemy.Health);
                            offset += 15;
                        }
                    }

                    if (R.Level == 2)
                    {
                        if (RDamage(enemy) * 4 > enemy.Health + enemy.AllShield + enemy.HPRegenRate)
                        {
                            Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f + offset, System.Drawing.Color.Red, "Ult can Kill: " + enemy.CharacterName + " HP left: " + enemy.Health);
                            offset += 15;
                        }
                    }

                    if (R.Level == 3)
                    {
                        if (RDamage(enemy) * 5 > enemy.Health + enemy.AllShield + enemy.HPRegenRate)
                        {
                            Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f + offset, System.Drawing.Color.Red, "Ult can Kill: " + enemy.CharacterName + " HP left: " + enemy.Health);
                            offset += 15;
                        }
                    }
                    
                }
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawBuff").Enabled)
            {
                Vector2 ft = Drawing.WorldToScreen(Me.Position);
                var buff = Me.GetBuff("XerathLocusOfPower2");
                if (buff != null)
                {
                    var timer = buff.EndTime - Game.Time;
                    FatalityRenderRing.DrawText($"R Time: {timer:N1}", Drawing.Width * 0.43f, Drawing.Height * 0.57f, Color.Red);
                }
            }
        }

        private static void OnEndScene(EventArgs args)
        {
            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled)
            {
                MiniMap.DrawCircle(Me.Position, R.Range, System.Drawing.Color.Red);
            }
        }

        private static float GetComboDamage(AIHeroClient target)
        {
            var Damage = 0d;
            if (Q.IsReady())
            {
                Damage += QDamage(target);
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
                Damage += RDamage(target);
            }
            
            return (float)Damage;
        }

        private static void Gap(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("AG").Enabled && E.IsReady())
            {
                if (E.IsReady())
                {
                    if (sender.IsValid && sender.IsEnemy)
                    {
                        if (sender.IsDashing())
                        {
                            if (args.EndPosition.DistanceToPlayer() < 500)
                            {
                                E.CastIfHitchanceEquals(sender, HitChance.Dash);
                            }
                        }
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

        private static readonly float[] QBaseDamage = { 0f, 70f, 110f, 150f, 190f, 230f, 230f };
        private static readonly float[] WBaseDamage = { 0f, 60f, 95f, 130f, 165f, 200f, 200f };
        private static readonly float[] EBaseDamage = { 0f, 80f, 110f, 140f, 170f, 200f, 200f };
        private static readonly float[] RBaseDamage = { 0f, 200f, 250f, 300f, 300f };

        private static float QDamage(AIBaseClient target)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] + .85f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, qBaseDamage);
        }

        private static float WDamage(AIBaseClient target)
        {
            var wlevel = W.Level;
            var wBaseDamage = WBaseDamage[wlevel] + .6f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, wBaseDamage);
        }

        private static float EDamage(AIBaseClient target)
        {
            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel] + .45f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, eBaseDamage);
        }

        private static float RDamage(AIBaseClient target)
        {
            var rLevel = R.Level;
            var rBaseDamage = RBaseDamage[rLevel] + .45f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, rBaseDamage);
        }

        private static void movement()
        {
            var oncc = Config["Misc"].GetValue<MenuBool>("cc");

            if (W.IsReady())
            {
                var wtarget = TargetSelector.GetTarget(W.Range, DamageType.Magical);
                if (wtarget != null)
                {
                    if (oncc.Enabled)
                    {
                        if (wtarget.HasBuffOfType(BuffType.Snare) || wtarget.HasBuffOfType(BuffType.Stun) ||
                            wtarget.HasBuffOfType(BuffType.Suppression))
                        {
                            W.Cast(wtarget.Position);
                        }
                    }
                }
            }
        }
    }
}