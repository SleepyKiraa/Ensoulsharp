using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using SharpDX;
using SharpDX.Direct3D11;
using SPredictionMash1;

namespace Fatality.Champions.Jinx
{
    public class Jinx
    {
        private static Spell Q, Q2, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuK, menuD, menuM, menuP;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Jinx")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, Me.GetRealAutoAttackRange());
            Q2 = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1500f);
            W.SetSkillshot(0.6f, 60f, 3300f, true, SpellType.Line);
            E = new Spell(SpellSlot.E, 925f);
            E.SetSkillshot(0f, 50f, float.MaxValue, false, SpellType.Circle);
            R = new Spell(SpellSlot.R, 20000);
            R.SetSkillshot(0.6f, 100f, 2200f, false, SpellType.Line);

            Config = new Menu("Jinx", "[Fatality] Jinx", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Jinx));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Auto Switch on Combo"));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo"));
            menuW.Add(new MenuBool("noAA", "Dont W if Target is in AA Range"));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("CC", "Auto E on CC"));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuKeyBind("SemiR", "Semi R", Keys.T, KeyBindType.Press)).AddPermashow();
            Config.Add(menuR);
            
            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("WPred", "W Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("RPred", "R Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsW", "use W to Killsteal", true));
            menuK.Add(new MenuBool("KsR", "use R to Killsteal", true));
            Config.Add(menuK);
            
            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuBool("AG", "AntiGapcloser", true));
            Config.Add(menuM);
            
            menuD = new Menu("Draw", "Draw Settings");
            menuD.Add(new MenuSeparator("qq", "Q Draw Settings"));
            menuD.Add(new MenuBool("drawQ", "Draw Minigun Range"));
            menuD.Add(new MenuBool("drawQ2", "Draw Rocket Range"));
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
            GameEvent.OnGameTick += OnUpdate;
            Game.OnUpdate += UpdateTick;
            AntiGapcloser.OnGapcloser += Gap;
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
            Q2.Range = 525 + 50 + 30 * Q2.Level;

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicQ();
                LogicW();
            }

            if (Config["Rsettings"].GetValue<MenuKeyBind>("SemiR").Active)
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                LogicR();
            }

            OnCC();
        }

        private static void LogicQ()
        {
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");

            if (Q.IsReady() && useQ.Enabled)
            {
                var qtarget = Q.GetTarget();
                if (qtarget != null && qtarget.IsValidTarget())
                {
                    if (qtarget.DistanceToPlayer() <= 525 && !Me.HasBuff("jinxqicon"))
                    {
                        Q.Cast();
                    }
                }
            }

            if (Q2.IsReady() && useQ.Enabled)
            {
                var q2target = Q2.GetTarget();
                if (q2target != null && q2target.IsValidTarget())
                {
                    if (q2target.DistanceToPlayer() > 525 && q2target.DistanceToPlayer() <= 725 && !Me.HasBuff("JinxQ"))
                    {
                        Q.Cast();
                    }
                }
            }
        }

        private static void LogicW()
        {
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");
            var noaa = Config["Wsettings"].GetValue<MenuBool>("noAA");
            var wtarget = W.GetTarget();


            if (wtarget != null)
            {
                if (noaa.Enabled && wtarget.InRange(Q.Range) || wtarget.InRange(Q2.Range))
                {
                    return;
                }
            }
            
            switch (comb(menuP, "WPred"))
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

            if (W.IsReady() && useW.Enabled)
            {
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
                                W.CastLine(wtarget, 0f, 0f, false);
                            }

                            break;
                        
                        case 2:
                            var wpreddd = W.GetSPrediction(wtarget);
                            if (wpreddd.HitChance >= hitchance)
                            {
                                W.Cast(wpreddd.CastPosition);
                            }

                            break;
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
                var rtarget = TargetSelector.GetTarget(2000, DamageType.Physical);
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
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;

            foreach (var wtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(W.Range) && !hero.IsDead && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (wtarget != null)
                {
                    if (ksW && W.IsReady())
                    {
                        if (wtarget.DistanceToPlayer() > Q2.Range && wtarget.DistanceToPlayer() <= W.Range)
                        {
                            if (wtarget.Health + wtarget.AllShield + wtarget.HPRegenRate <= WDamage(wtarget))
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0:
                                        var wpred = W.GetPrediction(wtarget);
                                        if (wpred.Hitchance >= HitChance.High)
                                        {
                                            W.Cast(wpred.CastPosition);
                                        }

                                        break;
                        
                                    case 1:
                                        {
                                            W.CastLine(wtarget, 0f, 0f, false);
                                        }

                                        break;
                        
                                    case 2:
                                        var wpreddd = W.GetSPrediction(wtarget);
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

            foreach (var Rtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(2500) && !hero.IsDead && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksR && R.IsReady())
                {
                    if (Rtarget != null)
                    {
                        if (Rtarget.DistanceToPlayer() <= 2500)
                        {
                            if (Rtarget.InRange(Q2.Range) && Me.GetAutoAttackDamage(Rtarget) * 2 < Rtarget.Health)
                            {
                                return;
                            }

                            if (Rtarget.Health + Rtarget.AllShield + Rtarget.HPRegenRate <= RDamage(Rtarget))
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0:
                                        var rpred = R.GetPrediction(Rtarget);
                                        if (rpred.Hitchance >= HitChance.High)
                                        {
                                            R.Cast(rpred.CastPosition);
                                        }

                                        break;

                                    case 1:
                                        {
                                            R.CastLine(Rtarget);
                                        }

                                        break;
                        
                                    case 2:
                                        var rpreddd = R.GetSPrediction(Rtarget);
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
                if (Me.HasBuff("jinxqicon"))
                {
                    var colorQ = Config["Draw"].GetValue<MenuColor>("colorQ").Color;
                    CircleRender.Draw(Me.Position, Q.Range, colorQ, 2);
                }
            }

            if (Config["Draw"].GetValue<MenuBool>("drawQ2").Enabled)
            {
                if (Me.HasBuff("JinxQ"))
                {
                    var colorQ = Config["Draw"].GetValue<MenuColor>("colorQ").Color;
                    CircleRender.Draw(Me.Position, Q2.Range + Me.BoundingRadius, colorQ, 2);
                }
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

        private static void Gap(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (sender.IsMe)
            {
                return;
            }

            if (Config["Misc"].GetValue<MenuBool>("AG").Enabled && E.IsReady())
            {
                if (Utils.Oktw.OktwCommon.CheckGapcloser(sender, args))
                {
                    if (Me.Distance(args.EndPosition) < E.Range)
                    {
                        E.Cast(args.EndPosition);
                    }
                }
            }
        }

        private static readonly float[] WBaseDamage = { 0f, 10f, 60f, 110f, 160f, 210f, 210f };
        private static readonly float[] RBaseDamage = {0f, 300f, 450f, 600f, 600f};
        private static readonly float[] Multiplir = {0f, .25f, .3f, .35f, .35f};

        private static float WDamage(AIBaseClient target)
        {
            var wLevel = W.Level;
            var wBaseDamage = WBaseDamage[wLevel] + 1.6f * GameObjects.Player.TotalAttackDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, wBaseDamage);
        }
        
        private static float RDamage(AIBaseClient target)
        {
            var rLevel = R.Level;
            var rBaseDamage = RBaseDamage[rLevel] + 1.5f * Me.GetBonusPhysicalDamage() +
                              Multiplir[rLevel] * (target.MaxHealth - target.Health);
            var rDistance = ((int) Math.Ceiling(target.DistanceToPlayer() / 100) * 6 + 4) / 10;
            var total = rBaseDamage * (rDistance >= 1 ? 1 : rDistance);
            return (float) Me.CalculateDamage(target, DamageType.Physical, total);
        }

        private static void OnCC()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("CC");

            if (E.IsReady() && useE.Enabled)
            {
                var etarget = E.GetTarget();
                if (etarget != null && etarget.IsValidTarget())
                {
                    if (etarget.HasBuffOfType(BuffType.Stun) || etarget.HasBuffOfType(BuffType.Snare) ||
                        etarget.HasBuffOfType(BuffType.Suppression))
                    {
                        E.Cast(etarget.Position);
                    }
                }
            }
        }
    }
}