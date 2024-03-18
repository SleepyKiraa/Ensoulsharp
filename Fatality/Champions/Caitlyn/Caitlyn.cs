using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Core;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using Fatality.Utils.Oktw;
using SharpDX;
using SPredictionMash1;

namespace Fatality.Champions.Caitlyn
{
    public class Caitlyn
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuK, menuD, menuP, menuM;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Caitlyn")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1300f);
            Q.SetSkillshot(0.625f, 60f, 2200f, false, SpellType.Line);
            W = new Spell(SpellSlot.W, 800f);
            W.SetSkillshot(0.25f, 15f, float.MaxValue, false, SpellType.Circle);
            E = new Spell(SpellSlot.E, 800f);
            E.SetSkillshot(0.35f, 70f, 1600f, true, SpellType.Line);
            R = new Spell(SpellSlot.R, 3500f);

            Config = new Menu("Caitlyn", "[Fatality] Caitlyn", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Caitlyn));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo"));
            menuQ.Add(new MenuBool("noAA", "Dont use Q in AA Range"));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("CC", "Auto W on CC"));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo"));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuKeyBind("SemiR", "SemiR if Target is Killable", Keys.T, KeyBindType.Press)).AddPermashow();
            Config.Add(menuR);
            
            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("EPred", "E Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal", true));
            menuK.Add(new MenuBool("KsE", "use E to Killsteal", true));
            menuK.Add(new MenuBool("KsR", "use R to Killsteal", true));
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
            menuD.Add(new MenuBool("kill", "Draw R Kill Message"));
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
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE();
                LogicQ();
            }

            if (Config["Rsettings"].GetValue<MenuKeyBind>("SemiR").Active)
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                SemiR();
            }
            
            movement();
        }

        private static void LogicQ()
        {
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");
            var noaa = Config["Qsettings"].GetValue<MenuBool>("noAA");
            var qtarget = Q.GetTarget();

            if (noaa.Enabled && qtarget != null && qtarget.InRange(Me.GetRealAutoAttackRange()))
            {
                return;
            }
            
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
                            var qpred = Q.GetPrediction(qtarget);
                            if (qpred.Hitchance >= hitchance)
                            {
                                Q.Cast(qpred.CastPosition);
                            }
                            break;
                        
                        case 1:
                            {
                                Q.CastLine(qtarget);
                            }
                            break;
                        
                        case 2:
                            var qpreddd = Q.GetSPrediction(qtarget);
                            if (qpreddd.HitChance >= hitchance)
                            {
                                Q.Cast(qpreddd.CastPosition);
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

            if (E.IsReady() && useE.Enabled)
            {
                var etarget = E.GetTarget();
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
                            {
                                E.CastLine(etarget, 0f, 0f, false);
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
            }
        }

        private static void SemiR()
        {
            if (R.IsReady())
            {
                var rtarget = R.GetTarget();
                if (rtarget != null && rtarget.IsValidTarget())
                {
                    if (rtarget.Health + rtarget.AllShield + rtarget.HPRegenRate <= RDamage(rtarget))
                    {
                        if (!rtarget.InRange(Me.GetRealAutoAttackRange()))
                        {
                            R.Cast(rtarget);
                        }
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
                        if (!Qtarget.InRange(Me.GetRealAutoAttackRange()))
                        {
                            if (Qtarget.DistanceToPlayer() < Q.Range)
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
                                        E.CastLine(Etarget, 0f, 0f , false);

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

            foreach (var Rtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(R.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (R.IsReady() && ksR)
                {
                    if (Rtarget != null)
                    {
                        if (!Rtarget.InRange(Me.GetRealAutoAttackRange()))
                        {
                            if (Rtarget.DistanceToPlayer() <= R.Range)
                            {
                                if (Rtarget.Health + Rtarget.AllShield + Rtarget.HPRegenRate <= RDamage(Rtarget))
                                {
                                    R.Cast(Rtarget);
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
                var qcolor = Config["Draw"].GetValue<MenuColor>("colorQ").Color;
                CircleRender.Draw(Me.Position, Q.Range, qcolor, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawW").Enabled)
            {
                var wcolor = Config["Draw"].GetValue<MenuColor>("colorW").Color;
                CircleRender.Draw(Me.Position, W.Range, wcolor, 2);
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled)
            {
                var ecolor = Config["Draw"].GetValue<MenuColor>("colorE").Color;
                CircleRender.Draw(Me.Position, E.Range, ecolor, 2);
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled)
            {
                var rcolor = Config["Draw"].GetValue<MenuColor>("colorR").Color;
                CircleRender.Draw(Me.Position, R.Range, rcolor, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("kill").Enabled)
            {
                var t = R.GetTarget();
                if (t != null && t.Health + t.AllShield + t.HPRegenRate <= RDamage(t))
                {
                    Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f, System.Drawing.Color.Red,
                        "Ult Can Kill: " + t.CharacterName + " have: " + t.Health + " HP");
                }
            }
        }
        
        private static void Gap(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (sender.IsMe)
            {
                return;
            }

            if (Config["Misc"].GetValue<MenuBool>("AG").Enabled && W.IsReady())
            {
                if (Utils.Oktw.OktwCommon.CheckGapcloser(sender, args))
                {
                    if (Me.Distance(args.EndPosition) < W.Range)
                    {
                        W.Cast(args.EndPosition);
                    }
                }
            }
        }
        
        private static void movement()
        {
            var oncc = Config["Wsettings"].GetValue<MenuBool>("CC");

            if (W.IsReady())
            {
                var wtarget = W.GetTarget();
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

        private static readonly float[] QBaseDamage = { 0f, 50f, 90f, 130f, 170f, 210f, 210f };
        private static readonly float[] QBonusDamage = { 0f, 1.25f, 1.45f, 1.65f, 1.85f, 2.05f, 2.05f };
        private static readonly float[] EBaseDamage = { 0f, 80f, 130f, 180f, 230f, 280f, 280f };
        private static readonly float[] RBaseDamage = { 0f, 300f, 525f, 750f, 750f };

        private static float QDamage(AIBaseClient target)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] + QBonusDamage[qLevel] * GameObjects.Player.TotalAttackDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, qBaseDamage);
        }

        private static float EDamage(AIBaseClient target)
        {
            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel] + .8f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, eBaseDamage);
        }
        
        private static float RDamage(AIBaseClient target)
        {
            var rlevel = R.Level;
            var rBaseDamage = RBaseDamage[rlevel] + 2f * GameObjects.Player.TotalAttackDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, rBaseDamage);
        }
    }
}