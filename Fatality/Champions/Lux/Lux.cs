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
using HealthPrediction = EnsoulSharp.SDK.HealthPrediction;

namespace Fatality.Champions.Lux
{
    public class Lux
    {
        private static Spell Q, W, E, R;
        
        private static Menu Config, menuQ, menuW, menuE, menuR, menuP, menuK, menuD, menuM;

        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static GameObject LuxE;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Lux")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1300f);
            Q.SetSkillshot(0.25f, 70f, 1200f, false, SpellType.Line);
            W = new Spell(SpellSlot.W, 1175f);
            W.SetSkillshot(0.25f, 100f, 2400f, false, SpellType.Line);
            E = new Spell(SpellSlot.E, 1100f);
            E.SetSkillshot(0.25f, 150f, 1200f, false, SpellType.Circle);
            R = new Spell(SpellSlot.R, 3400f);
            R.SetSkillshot(1f, 100f, float.MaxValue, false, SpellType.Line);

            Config = new Menu("Lux", "[Fatality] Lux", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Lux));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo"));
            menuQ.Add(new MenuBool("AQ", "Auto Q Dashing Enemys"));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("AutoW", "Auto W on Incomming Damage"));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in combo"));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuKeyBind("SemiR", "Semi R", Keys.T, KeyBindType.Press)).AddPermashow();
            Config.Add(menuR);

            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)" }, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("EPred", "E Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("RPred", "R Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);

            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal", true));
            menuK.Add(new MenuBool("KsE", "Enable E Killsteal", true));
            menuK.Add(new MenuBool("KsR", "Enable R Killsteal", true));
            Config.Add(menuK);

            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuBool("AG", "AntiGapcloser", true));
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
            GameObject.OnDelete += EGone;
            GameEvent.OnGameTick += OnUpdate;
            GameObject.OnCreate += EFound;           
            Game.OnUpdate += UpdateTick;
            Render.OnDraw += OnDraw;
            AntiGapcloser.OnGapcloser += Gap;
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
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE();
                LogicQ();
            }

            if (Config["Qsettings"].GetValue<MenuBool>("AQ").Enabled)
            {
                if (Q.IsReady())
                {
                    foreach (var enemys in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(Q.Range)))
                    {
                        Q.CastIfHitchanceEquals(enemys, HitChance.Dash);
                    }
                }
            }

            if (Config["Rsettings"].GetValue<MenuKeyBind>("SemiR").Active)
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                LogicR();
            }

            LogicW();
        }

        private static void LogicQ()
        {
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");
            var qtarget = Q.GetTarget();

            if (qtarget == null)
            {
                return;
            }

            var qpred = Q.GetPrediction(qtarget);
            var qpredd = Q.GetPrediction(qtarget);
            var qpreddd = Q.GetSPrediction(qtarget);

            var col = Q.GetCollision(Me.Position.ToVector2(),
                new List<Vector2> { qpred.CastPosition.ToVector2() });
            var col2 = Q.GetCollision(Me.Position.ToVector2(),
                new List<Vector2> { qpredd.CastPosition.ToVector2() });
            var col3 = Q.GetCollision(Me.Position.ToVector2(),
                new List<Vector2> { qpreddd.CastPosition.ToRawVector2() });
            var minions = col.Where(x => !(x is AIHeroClient)).OrderBy(x => x.IsMinion()).Take(2)
                .Count(x => x.IsMinion());
            var minions2 = col2.Where(x => !(x is AIHeroClient)).OrderBy(x => x.IsMinion()).Take(2)
                .Count(x => x.IsMinion());
            var minions3 = col3.Where(x => !(x is AIHeroClient)).OrderBy(x => x.IsMinion()).Take(2)
                .Count(x => x.IsMinion());


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
                if (qtarget != null && qtarget.IsValidTarget())
                {
                    switch (comb(menuP, "Pred"))
                    {
                        case 0:
                            if (minions < 2)
                            {
                                if (qpred.Hitchance >= hitchance)
                                {
                                    Q.Cast(qpred.CastPosition);
                                }
                            }

                            break;

                        case 1:
                            if (minions2 < 2)
                            {
                                Q.CastLine(qtarget, 0f, 1f, false);
                            }

                            break;

                        case 2:
                            if (minions3 < 2)
                            {
                                if (qpreddd.HitChance >= hitchance)
                                {
                                    Q.Cast(qpreddd.CastPosition);
                                }
                            }

                            break;
                    }
                }
            }
        }

        private static void LogicW()
        {
            var useW = Config["Wsettings"].GetValue<MenuBool>("AutoW");

            if (W.IsReady() && useW.Enabled)
            {
                if (HealthPrediction.GetPrediction(Me, 0) <= 350)
                {
                    W.Cast(Me.Position);
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

            if (E.IsReady() && useE.Enabled)
            {
                var etarget = E.GetTarget();
                if (etarget != null && etarget.IsValidTarget() && LuxE == null)
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
                            var epreddd = E.GetSPrediction(etarget);
                            if (epreddd.HitChance >= hitchance)
                            {
                                E.Cast(epreddd.CastPosition);
                            }

                            break;
                    }
                }

                if (LuxE != null)
                {
                    if (etarget != null && LuxE != null && LuxE.Position.CountEnemyHeroesInRange(270) >= 1)
                    {
                        E.Cast();
                        Console.WriteLine("E Detonated Reason: Enemy Found");
                    }
                }
            }
        }

        private static void LogicR()
        {
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
                            var rpreddd = R.GetSPrediction(rtarget);
                            if (rpreddd.HitChance >= hitchance)
                            {
                                R.Cast(rpreddd.CastPosition);
                            }

                            break;
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;

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
                                            Q.CastLine(Qtarget, 0f, 1f, false);
                                        }

                                        break;

                                    case 2:
                                        var qpreddd = Q.GetSPrediction(Qtarget);
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
                                        var epred = E.GetPrediction(Etarget, true);
                                        if (epred.Hitchance >= HitChance.High)
                                        {
                                            E.Cast(epred.CastPosition);
                                        }

                                        break;

                                    case 1:
                                        E.CastCircle(Etarget);

                                        break;

                                    case 2:
                                        var epreddd = E.GetSPrediction(Etarget);
                                        if (epreddd.HitChance >= HitChance.High)
                                        {
                                            E.Cast(epreddd.CastPosition);
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
                                        {
                                            R.CastLine(rtarget);
                                        }

                                        break;

                                    case 2:
                                        var rpreddd = R.GetSPrediction(rtarget);
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

        private static readonly float[] QBaseDamage = { 0f, 80f, 120f, 160f, 200f, 240f, 240f };
        private static readonly float[] EBaseDamage = { 0f, 70f, 120f, 170f, 220f, 270f, 270f };
        private static readonly float[] RBaseDamage = { 0f, 300f, 400f, 500f, 500f };

        private static float QDamage(AIBaseClient target)
        {
            var qlevel = Q.Level;
            var qBaseDamage = QBaseDamage[qlevel] + .6f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, qBaseDamage);
        }

        private static float EDamage(AIBaseClient target)
        {
            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel] + .8f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, eBaseDamage);
        }

        private static float RDamage(AIBaseClient target)
        {
            var rLevel = R.Level;
            var rBaseDamage = RBaseDamage[rLevel] + 1.2f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, rBaseDamage);
        }

        private static void Gap(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("AG").Enabled && Q.IsReady())
            {
                if (Q.IsReady())
                {
                    if (sender.IsValid && sender.IsEnemy)
                    {
                        if (sender.IsDashing())
                        {
                            if (args.EndPosition.DistanceToPlayer() < 500)
                            {
                                Q.CastIfHitchanceEquals(sender, HitChance.Dash);
                            }
                        }
                    }
                }
            }
        }       

        private static void EFound(GameObject sender, EventArgs args)
        {
            if (sender.Name == "Lux_Base_E_tar_aoe_green" || sender.Name == "Lux_Skin05_E_tar_aoe_green" || sender.Name == "Lux_Skin06_E_tar_aoe_green" || sender.Name == "Lux_Skin07_E_tar_aoe_green" || sender.Name == "Lux_Skin08_E_tar_aoe_green" || sender.Name == "Lux_Skin15_E_tar_aoe_green" || sender.Name == "Lux_Skin16_E_tar_aoe_green" || sender.Name == "Lux_Skin17_E_tar_aoe_green" || sender.Name == "Lux_Skin18_E_tar_aoe_green" || sender.Name == "Lux_Skin19_E_tar_aoe_green" || sender.Name == "Lux_Skin29_E_tar_aoe_green" || sender.Name == "Lux_Skin40_E_tar_aoe_green" || sender.Name == "Lux_Skin42_E_tar_aoe_green")
            {
                LuxE = sender;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Lux E Found");
                Console.ResetColor();
            }
        }

        private static void EGone(GameObject sender, EventArgs args)
        {
            if (sender.Name == "Lux_Base_E_tar_nova" || sender.Name == "Lux_Skin05_E_tar_nova" || sender.Name == "Lux_Skin06_E_tar_nova" || sender.Name == "Lux_Skin07_E_tar_nova" || sender.Name == "Lux_Skin08_E_tar_nova" || sender.Name == "Lux_Skin15_E_tar_nova" || sender.Name == "Lux_Skin16_E_tar_nova" || sender.Name == "Lux_Skin17_E_tar_nova" || sender.Name == "Lux_Skin18_E_tar_nova" || sender.Name == "Lux_Skin19_E_tar_nova" || sender.Name == "Lux_Skin29_E_tar_nova" || sender.Name == "Lux_Skin40_E_tar_nova" || sender.Name == "Lux_Skin42_E_tar_nova")
            {
                LuxE = null;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Lux E Exploded");
                Console.ResetColor();
            }
        }

    }
}