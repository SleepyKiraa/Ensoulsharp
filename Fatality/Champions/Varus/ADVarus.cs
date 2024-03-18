using System;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Core;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using EnsoulSharp.SDK.Utility;
using Fatality.Utils;
using SharpDX;
using SharpDX.Direct3D9;
using SPredictionMash1;

namespace Fatality.Champions.Varus
{
    public class ADVarus
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuP, menuK, menuD, menuM, menuL;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Varus")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 895f);
            Q.SetSkillshot(0.5f, 70f, 1900f, false, SpellType.Line);
            Q.SetCharged("VarusQ", "VarusQ", 895, 1595, 1.25f);
            W = new Spell(SpellSlot.W, 0f);
            E = new Spell(SpellSlot.E, 925f);
            E.SetSkillshot(0.25f, 100f, 1500f, false, SpellType.Circle);
            R = new Spell(SpellSlot.R, 1300f);
            R.SetSkillshot(0.25f, 120f, 1500f, false, SpellType.Line);

            Config = new Menu("Varus", "[Fatality] Varus", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Varus));
            
            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo", true));
            menuQ.Add(new MenuList("QMode", "Q Mode",
                new string[] { "Fast", "Full" }, 1));
            menuQ.Add(new MenuSlider("Wstacks", "W Stacks to use Q", 2, 0, 3));
            Config.Add(menuQ);
            
            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo"));
            menuW.Add(new MenuSlider("HP", "Target HP % To Use W", 50, 1, 100));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo", true));
            menuE.Add(new MenuSlider("Wstacks", "W Stacks to use E", 2, 0, 3));
            Config.Add(menuE);
            
            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo if Target is Killable"));
            menuR.Add(new MenuKeyBind("SemiR", "Semi R", Keys.T, KeyBindType.Press)).AddPermashow();
            Config.Add(menuR);
            
            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("EPred", "E Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("RPred", "R Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal"));
            menuK.Add(new MenuBool("KsE", "Enable E Killsteal", true));
            Config.Add(menuK);
            
            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuSeparator("LaneClear", "Lane Clear"));
            menuL.Add(new MenuBool("LcE", "Use E to Lane Clear", true));
            menuL.Add(new MenuSeparator("Jungleclear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcE", "Use E to Jungle Clear", true));
            Config.Add(menuL);

            
            menuM = new Menu("Misc", "Misc Settings");
            menuM.Add(new MenuBool("AG", "AntiGapcloser", true));
            Config.Add(menuM);
            
            menuD = new Menu("Draw", "Draw Settings");
            menuD.Add(new MenuBool("drawQ", "Draw Q Range (White)", true));
            menuD.Add(new MenuBool("drawW", "Draw W Range (Blue)", true));
            menuD.Add(new MenuBool("drawE", "Draw E Range (Green)", true));
            menuD.Add(new MenuBool("drawR", "Draw R Range (Red)", true));
            menuD.Add(new MenuBool("drawA", "Draw AA Tracker", true));
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
            GameEvent.OnGameTick += OnGameUpdate;
            Game.OnUpdate += Updatetick;
            Render.OnDraw += OnDraw;
            AntiGapcloser.OnGapcloser += Gap;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }
        
        private static void Updatetick(EventArgs args)
        {
            Killsteal();
        }

        private static void OnGameUpdate(EventArgs args)
        {
            if (Config["Rsettings"].GetValue<MenuKeyBind>("SemiR").Active)
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                SemiR();
            }

            if (Q.IsCharging && Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicW();
                LogicQ();
                LogicE();
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
            var stacks = Config["Qsettings"].GetValue<MenuSlider>("Wstacks");

            switch (comb(menuP, "QPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }           
            
            switch (comb(menuQ, "QMode"))
            {
                case 0:
                    if (Q.IsReady() && useQ.Enabled)
                    {
                        var qtarget = TargetSelector.GetTarget(Q.ChargedMaxRange, DamageType.Physical);
                        if (qtarget != null)
                        {
                            switch (comb(menuP, "Pred"))
                            {
                                case 0:
                                    var Qpred = Q.GetPrediction(qtarget);
                                    if (Qpred.Hitchance >= hitchance)
                                    {
                                        if (!Q.IsCharging && qtarget.GetBuffCount("VarusWDebuff") >= stacks.Value)
                                        {
                                            {
                                                Q.StartCharging();
                                                return;
                                            }
                                        }

                                        if (Q.IsCharging)
                                        {
                                            var Pred = Q.GetPrediction(qtarget);
                                            if (Pred.Hitchance >= hitchance)
                                            {
                                                Q.ShootChargedSpell(Pred.CastPosition);
                                            }
                                        }
                                    }
                                    break;
                                
                                case 1:
                                    var Qpredd = Q.GetPrediction(qtarget);
                                    if (Qpredd.Hitchance >= hitchance)
                                    {
                                        if (!Q.IsCharging && qtarget.GetBuffCount("VarusWDebuff") >= stacks.Value)
                                        {
                                            {
                                                Q.StartCharging();
                                                return;
                                            }
                                        }

                                        if (Q.IsCharging && qtarget.GetBuffCount("VarusWDebuff") >= stacks.Value)
                                        {
                                            Q.CastLine(qtarget);
                                        }
                                    }
                                    break;
                                
                                case 2:
                                    var Qpreddd = Q.GetSPrediction(qtarget);
                                    if (Qpreddd.HitChance >= hitchance)
                                    {
                                        if (!Q.IsCharging && qtarget.GetBuffCount("VarusWDebuff") >= stacks.Value)
                                        {
                                            {
                                                Q.StartCharging();
                                                return;
                                            }
                                        }

                                        if (Q.IsCharging)
                                        {
                                            var Preddd = Q.GetSPrediction(qtarget);
                                            if (Preddd.HitChance >= hitchance)
                                            {
                                                Q.ShootChargedSpell(Preddd.CastPosition);
                                            }
                                        }
                                    }                             

                                    break;
                            }
                        }
                    }

                    break;
                
                case 1:
                    if (Q.IsReady() && useQ.Enabled)
                    {
                        var qtarget = TargetSelector.GetTarget(Q.ChargedMaxRange, DamageType.Physical);
                        if (qtarget != null)
                        {
                            switch (comb(menuP, "Pred"))
                            {
                                case 0:
                                    var Qpred = Q.GetPrediction(qtarget);
                                    if (Qpred.Hitchance >= hitchance)
                                    {
                                        if (!Q.IsCharging && qtarget.GetBuffCount("VarusWDebuff") >= stacks.Value)
                                        {
                                            Q.StartCharging();
                                            return;
                                        }

                                        if (Q.Range == Q.ChargedMaxRange)
                                        {
                                            var Predq = Q.GetPrediction(qtarget);
                                            if (Predq.Hitchance >= hitchance)
                                            {
                                                Q.ShootChargedSpell(Predq.CastPosition);
                                            }
                                        }
                                    }
                                    break;
                                
                                case 1:
                                    var Qpredd = Q.GetPrediction(qtarget);
                                    if (Qpredd.Hitchance >= hitchance)
                                    {
                                        if (!Q.IsCharging && qtarget.GetBuffCount("VarusWDebuff") >= stacks.Value)
                                        {
                                            Q.StartCharging();
                                            return;
                                        }

                                        if (Q.Range == Q.ChargedMaxRange && qtarget.GetBuffCount("VarusWDebuff") >= stacks.Value)
                                        {
                                            Q.CastLine(qtarget);
                                        }
                                    }
                                    break;
                                
                                case 2:
                                    var Qpreddd = Q.GetSPrediction(qtarget);
                                    if (Qpreddd.HitChance >= hitchance)
                                    {
                                        if (!Q.IsCharging && qtarget.GetBuffCount("VarusWDebuff") >= stacks.Value)
                                        {
                                            {
                                                Q.StartCharging();
                                                return;
                                            }
                                        }

                                        if (Q.Range == Q.ChargedMaxRange)
                                        {
                                            var Preddd = Q.GetSPrediction(qtarget);
                                            if (Preddd.HitChance >= hitchance)
                                            {
                                                Q.ShootChargedSpell(Preddd.CastPosition);
                                            }
                                        }
                                    }

                                    break;
                            }
                        }
                    }
                    break;
            }
        }
        
        private static void LogicW()
        {
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");
            var hp = Config["Wsettings"].GetValue<MenuSlider>("HP");
            var qtarget = Q.GetTarget();

            if (W.IsReady() && useW.Enabled)
            {
                if (Q.IsCharging)
                {
                    if (qtarget != null && qtarget.IsValidTarget() && qtarget.HealthPercent <= hp.Value)
                    {
                        if (Variables.GameTimeTickCount - W.LastCastAttemptTime >= 7000)
                        {
                            W.Cast();
                        }
                    }
                }
            }
        }

        private static void LogicE()
        {
            var etarget = E.GetTarget();
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            var stacks = Config["Esettings"].GetValue<MenuSlider>("Wstacks");
            if (etarget == null) return;
            
            switch (comb(menuP, "EPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }           

            if (etarget.InRange(E.Range))
            {
                if (E.IsReady() && useE.Enabled && etarget.IsValidTarget())
                {
                    if (etarget.GetBuffCount("VarusWDebuff") >= stacks.Value)
                    {
                        switch (comb(menuP, "Pred"))
                        {
                            case 0:
                                var Epred = E.GetPrediction(etarget);
                                if (Epred.Hitchance >= hitchance)
                                {
                                    E.Cast(Epred.CastPosition);
                                }

                                break;
                            
                            case 1:
                                {
                                    E.CastCircle(etarget);
                                }

                                break;
                            
                            case 2:
                                var Epreddd = SPredictionMash1.Prediction.GetPrediction(etarget, E.Width, E.Delay,
                                    E.Speed, E.Range, false, SpellType.Circle);
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

        private static void LogicR()
        {
            var rtarget = R.GetTarget();
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            if (rtarget == null) return;
            
            switch (comb(menuP, "RPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }           

            if (rtarget.InRange(R.Range))
            {
                if (useR.Enabled && R.IsReady() && rtarget.IsValidTarget(R.Range))
                {
                    if (Q.IsReady() || E.IsReady())
                    {
                        if (Me.GetSpellDamage(rtarget, SpellSlot.Q) + EDamage(rtarget) +
                            RDamage(rtarget) >= rtarget.Health + rtarget.AllShield)
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
                                        R.CastCircle(rtarget);
                                    }

                                        break;
                                    
                                    case 2:
                                        var rpreddd = SPredictionMash1.Prediction.GetPrediction(rtarget, R.Width,
                                            R.Delay, R.Speed, R.Range, false, SpellType.Line);
                                        if (rpreddd.HitChance >= hitchance)
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
        
        private static void Laneclear()
        {
            var lcE = Config["Clear"].GetValue<MenuBool>("LcE");

            if (lcE.Enabled && E.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                
                if (minions.Any())
                {
                    var efarm = E.GetCircularFarmLocation(minions);
                    if (efarm.Position.IsValid())
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
        
        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;

            foreach (var qtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(Q.ChargedMaxRange) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksQ && Q.IsReady() )
                {
                    if (qtarget != null)
                    {
                        if (qtarget.Health + qtarget.AllShield <= QDamage(qtarget))
                        {
                            switch (comb(menuP, "Pred"))
                            {
                                case 0:
                                    var Qpred = Q.GetPrediction(qtarget);
                                    if (Qpred.Hitchance >= HitChance.High)
                                    {
                                        if (!Q.IsCharging)
                                        {
                                            Q.StartCharging();
                                            return;
                                        }

                                        if (Q.Range == Q.ChargedMaxRange)
                                        {
                                            var Predq = Q.GetPrediction(qtarget);
                                            if (Predq.Hitchance >= HitChance.High)
                                            {
                                                Q.ShootChargedSpell(Predq.CastPosition);
                                            }
                                        }
                                    }                                    
                                    break;
                                
                                case 1:
                                    var Qpredd = Q.GetPrediction(qtarget);
                                    if (Qpredd.Hitchance >= HitChance.High)
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
                                    var Qpreddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width, Q.Delay, Q.Speed, Q.Range, false, SpellType.Line);
                                    if (Qpreddd.HitChance >= HitChance.High)
                                    {
                                        if (!Q.IsCharging)
                                        {
                                            {
                                                Q.StartCharging();
                                                return;
                                            }
                                        }

                                        if (Q.Range == Q.ChargedMaxRange)
                                        {
                                            var Preddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width, Q.Delay, Q.Speed, Q.Range, false, SpellType.Line); ;
                                            if (Preddd.HitChance >= HitChance.High)
                                            {
                                                Q.ShootChargedSpell(Preddd.CastPosition);
                                            }
                                        }
                                    }

                                    break;
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
                if (ksE && E.IsReady() && etarget.IsValidTarget(E.Range))
                {
                    if (etarget != null)
                    {
                        if (etarget.DistanceToPlayer() <= E.Range)
                        {
                            if (etarget.Health + etarget.AllShield <= EDamage(etarget))
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0:
                                        var Epred = E.GetPrediction(etarget);
                                        if (Epred.Hitchance >= HitChance.High)
                                        {
                                            E.Cast(Epred.CastPosition);
                                        }

                                        break;
                                    
                                    case 1:
                                        {
                                            E.CastCircle(etarget);
                                        }
                                        break;
                                    
                                    case 2:
                                        var Epreddd = SPredictionMash1.Prediction.GetPrediction(etarget, E.Width, E.Delay,
                                            E.Speed, E.Range, false, SpellType.Circle);
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

        private static void OnDraw(EventArgs args)
        {
            if (Config["Draw"].GetValue<MenuBool>("drawQ").Enabled)
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

            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled)
            {
                CircleRender.Draw(Me.Position, R.Range, Color.Red, 2);
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawA").Enabled)
            {
                FatalityRenderRing.AALeft();
            }
        }

        private static void SemiR()
        {
            var rtarget = R.GetTarget();
            if (rtarget == null) return;
            
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
                var rped = SPredictionMash1.Prediction.GetPrediction(rtarget, R.Width, R.Delay, R.Speed, R.Range, false, SpellType.Line);
                if (rped.HitChance >= hitchance)
                {
                    R.Cast(rped.CastPosition);
                }
            }
        }

        private static void Gap(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("AG").Enabled && R.IsReady())
            {
                if (R.IsReady())
                {
                    if (sender.IsValid && sender.IsEnemy)
                    {
                        if (sender.IsDashing())
                        {
                            if (args.EndPosition.DistanceToPlayer() < 500)
                            {
                                R.CastIfHitchanceEquals(sender, HitChance.Dash);
                            }
                        }
                    }
                }
            }
        }

        private static readonly float[] QBaseDamage = { 0f, 15f, 70f, 125f, 180f, 235f, 235f };
        private static readonly float[] QScaling = { 0f, 1.25f, 1.30f, 1.35f, 1.40f, 1.45f, 1.45f };
        private static readonly float[] EBaseDamage = { 0f, 60f, 100f, 140f, 180f, 220f, 220f };
        private static readonly float[] RBaseDamage = { 0f, 150f, 250f, 350f, 350f };

        private static float QDamage(AIBaseClient target)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] + QScaling[qLevel] * GameObjects.Player.TotalAttackDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, qBaseDamage);
        }

        private static float EDamage(AIBaseClient target)
        {
            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel] + .9f * GameObjects.Player.TotalAttackDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, eBaseDamage);
        }

        private static float RDamage(AIBaseClient target)
        {
            var rLevel = R.Level;
            var rBaseDamage = RBaseDamage[rLevel] + 1f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, rBaseDamage);
        }
    }
}