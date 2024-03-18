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

namespace Fatality.Champions.Pyke
{
    public class Pyke
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuE, menuR, menuP, menuK, menuD;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Pyke")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 400f);
            Q.SetSkillshot(0.5f, 60f, 2000f, true, SpellType.Line);
            Q.SetCharged("PykeQ", "PykeQ", 400, 1100, 1f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 550f);
            R = new Spell(SpellSlot.R, 750f);
            R.SetSkillshot(0.5f, 60f, float.MaxValue, false, SpellType.Circle);

            Config = new Menu("Pyke", "[Fatality] Pyke", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Pyke));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo", true));
            menuQ.Add(new MenuBool("Qstab", "Use Short Q in Combo", true));
            menuQ.Add(new MenuBool("Qshot", "Use Long Q In Combo", true));
            Config.Add(menuQ);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("Egap", "Use E To Gapclose in R Range", true));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo", true));
            Config.Add(menuR);
            
            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("RPred", "R Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal", true));
            menuK.Add(new MenuBool("KsR", "Enable R Killsteal", true));
            Config.Add(menuK);
            
            menuD = new Menu("Draw", "Draw Settings");
            menuD.Add(new MenuBool("drawQ", "Draw Q Range (White)", true));
            menuD.Add(new MenuBool("drawW", "Draw W Range (Blue)", true));
            menuD.Add(new MenuBool("drawE", "Draw E Range (Green)", true));
            menuD.Add(new MenuBool("drawR", "Draw R Range (Red)", true));
            menuD.Add(new MenuBool("drawB", "Draw R Buff Time", true));
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
            Render.OnDraw += OnDraw;
            GameEvent.OnGameTick += OnGameUpdate;
            Game.OnUpdate += UpdateTick;
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void UpdateTick(EventArgs args)
        {
            Killsteal();
        }

        private static void OnGameUpdate(EventArgs args)
        {
            if (R.IsReady())
            {
                foreach (var enemys in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(E.Range + R.Range)))
                {
                    if (enemys.Health <= RDamage(enemys))
                    {
                        R.CastIfHitchanceEquals(enemys, HitChance.Dash);
                    }
                }
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicQ();
                LogicE();
                LogicR();
            }
        }

        private static void LogicQ()
        {
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");
            var stab = Config["Qsettings"].GetValue<MenuBool>("Qstab");
            var Long = Config["Qsettings"].GetValue<MenuBool>("Qshot");
            
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
                var qtarget = TargetSelector.GetTarget(Q.ChargedMaxRange, DamageType.Physical);
                if (qtarget != null)
                {
                    if (stab.Enabled && qtarget.DistanceToPlayer() <= 400)
                    {
                        Q.Cast(qtarget);
                    }

                    if (Long.Enabled && qtarget.DistanceToPlayer() >= 401)
                    {
                        switch (comb(menuP, "Pred"))
                        {
                            case 0://SDK
                                var pred = Q.GetPrediction(qtarget);
                                if (pred.Hitchance >= hitchance)
                                {
                                    if (!Q.IsCharging)
                                    {
                                        Q.StartCharging();
                                        return;
                                    }

                                    if (Q.Range == Q.ChargedMaxRange)
                                    {
                                        var Pred2 = Q.GetPrediction(qtarget, false);
                                        if (Pred2.Hitchance >= hitchance)
                                        {
                                            Q.ShootChargedSpell(Pred2.CastPosition);
                                        }
                                    }
                                }

                                break;
                            
                            case 1://Oktw
                                var predd = Q.GetPrediction(qtarget);
                                if (predd.Hitchance >= hitchance)
                                {
                                    if (!Q.IsCharging)
                                    {
                                        Q.StartCharging();
                                        return;
                                    }

                                    if (Q.Range == Q.ChargedMaxRange)
                                    {
                                        Q.CastLine(qtarget, 0f, 0f, false);
                                    }
                                }

                                break;
                            
                            case 2:
                                var preddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width, Q.Delay, Q.Speed, Q.ChargedMaxRange, true, SpellType.Line);
                                if (preddd.HitChance >= hitchance)
                                {
                                    if (!Q.IsCharging)
                                    {
                                        Q.StartCharging();
                                        return;
                                    }

                                    if (Q.Range == Q.ChargedMaxRange)
                                    {
                                        var Pred22 = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width, Q.Delay, Q.Speed, Q.ChargedMaxRange, true, SpellType.Line);;
                                        if (Pred22.HitChance >= hitchance)
                                        {
                                            Q.ShootChargedSpell(Pred22.CastPosition);
                                        }
                                    }
                                }

                                break;
                        }
                    }
                }
            }
        }

        private static void LogicE()
        {
            var egap = Config["Esettings"].GetValue<MenuBool>("Egap");

            if (E.IsReady() && egap.Enabled && R.IsReady())
            {
                var ertarget = TargetSelector.GetTarget(E.Range + R.Range, DamageType.Physical);
                if (ertarget != null && ertarget.IsValidTarget())
                {
                    if (!R.IsInRange(ertarget) && ertarget.Health <= RDamage(ertarget))
                    {
                        E.Cast(ertarget.Position);
                    }
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
            
            if (useR.Enabled && R.IsReady())
            {
                var rtarget = TargetSelector.GetTarget(R.Range, DamageType.Physical);
                if (rtarget != null)
                {
                    if (rtarget.Health <= RDamage(rtarget))
                    {
                        if (!rtarget.HasBuff("Undying Rage") || !rtarget.HasBuff("JudicatorIntervention") ||
                                !rtarget.HasBuff("kindredrnodeathbuff") ||
                                !rtarget.HasBuffOfType(BuffType.Invulnerability))
                        {
                            switch (comb(menuP, "Pred"))
                            {
                                case 0://SDk
                                    var rpred = R.GetPrediction(rtarget, true);
                                    if (rpred.Hitchance >= hitchance)
                                    {
                                        R.Cast(rpred.CastPosition);
                                    }

                                    break;
                                
                                case 1://oktw
                                    {
                                        R.CastCircle(rtarget);
                                    }

                                    break;
                                
                                case 2:
                                    var rpreddd = SPredictionMash1.Prediction.GetPrediction(rtarget, R.Width, R.Delay,
                                        R.Speed, R.Range, false, SpellType.Circle);
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

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;

            foreach (var qtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(Q.ChargedMaxRange) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
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
                                            Q.CastLine(qtarget, 0f, 0f, false);
                                        }
                                    }

                                    break;
                                
                                case 2:
                                    var preddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width, Q.Delay, Q.Speed, Q.ChargedMaxRange, true, SpellType.Line);
                                    if (preddd.HitChance >= HitChance.High)
                                    {
                                        if (!Q.IsCharging)
                                        {
                                            Q.StartCharging();
                                            return;
                                        }

                                        if (Q.Range == Q.ChargedMaxRange)
                                        {
                                            var Pred22 = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width, Q.Delay, Q.Speed, Q.ChargedMaxRange, true, SpellType.Line);;
                                            if (Pred22.HitChance >= HitChance.High)
                                            {
                                                Q.ShootChargedSpell(Pred22.CastPosition);
                                            }
                                        }
                                    }

                                    break;
                            }
                        }
                    }
                }
            }

            foreach (var rtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(Q.ChargedMaxRange) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksR && R.IsReady())
                {
                    if (rtarget != null)
                    {
                        if (rtarget.DistanceToPlayer() < R.Range)
                        {
                            if (rtarget.Health <= RDamage(rtarget))
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0:
                                        var rped = R.GetPrediction(rtarget);
                                        if (rped.Hitchance >= HitChance.High)
                                        {
                                            R.Cast(rped.CastPosition);
                                        }

                                        break;
                                    
                                    case 1:
                                        {
                                            R.CastCircle(rtarget);
                                        }

                                        break;
                                    
                                    case 2:
                                        var rpreddd = SPredictionMash1.Prediction.GetPrediction(rtarget, R.Width, R.Delay,
                                            R.Speed, R.Range, false, SpellType.Circle);
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

        private static void OnDraw(EventArgs args)
        {
            if (Config["Draw"].GetValue<MenuBool>("drawQ").Enabled)
            {
                CircleRender.Draw(Me.Position, Q.ChargedMaxRange, Color.White, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled)
            {
                CircleRender.Draw(Me.Position, E.Range, Color.Green, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled)
            {
                CircleRender.Draw(Me.Position, R.Range, Color.Red, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawB").Enabled)
            {
                var buff = Me.GetBuff("pykerrecast");
                if (buff != null)
                {
                    var timer = buff.EndTime - Game.Time;
                    FatalityRenderRing.DrawText($"R Time: {timer:N1}", Drawing.Width * 0.43f, Drawing.Height * 0.57f, Color.Red);
                }
            }
        }

        private static readonly float[] QBaseDamage = { 0f, 100f, 150f, 200f, 250f, 300f, 300f };
        private static readonly float[] RBaseDamage =
            { 0f, 0f, 0f, 0f, 0f, 250f, 290f, 330f, 370f, 400f, 430f, 450f, 470f, 190f, 510f, 530f, 540f, 550f, 550f };

        private static float QDamage(AIBaseClient target)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] + .6f * GameObjects.Player.TotalAttackDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, qBaseDamage);
        }
        
        private static float RDamage(AIBaseClient target)
        {
            var rbaseDamage = RBaseDamage[Me.Level] + (1.5f * Me.PhysicalLethality + .8f * GameObjects.Player.TotalAttackDamage);
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, rbaseDamage);
        }
    }
}