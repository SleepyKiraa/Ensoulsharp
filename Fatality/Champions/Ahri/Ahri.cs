using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using SharpDX;
using SPredictionMash1;

namespace Fatality.Champions.Ahri
{
    public class Ahri
    {
        private static Spell Q, W, E, R, flash;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuK, menuD, menuM, menuP;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static SpellSlot Flashslot;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Ahri")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 900f);
            Q.SetSkillshot(0.25f, 100f, 1550f, false, SpellType.Line);
            W = new Spell(SpellSlot.W, 725f);
            E = new Spell(SpellSlot.E, 1000f);
            E.SetSkillshot(0.25f, 70f, 1550f, true, SpellType.Line);
            R = new Spell(SpellSlot.R, 600f);

            flash = new Spell(SpellSlot.Summoner1, 400);
            flash = new Spell(SpellSlot.Summoner2, 400);
            Flashslot = Me.GetSpellSlot("SummonerFlash");

            Config = new Menu("Ahri", "[Fatality] Ahri", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Ahri));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo"));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo"));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo"));
            menuE.Add(new MenuKeyBind("FlashE", "Flash E", Keys.T, KeyBindType.Press)).AddPermashow();
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo", false));
            menuR.Add(new MenuSlider("HP", "Target HP % To use R", 50, 1, 100));
            Config.Add(menuR);
            
            menuP = new Menu("Pred", "Prediction Settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] {"SDK", "xcsoft", "Spred (Press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "Very High" }, 2));
            menuP.Add(new MenuList("EPred", "E Hitchance",
                new string[] { "Low", "Medium", "High", "Very High" }, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal"));
            menuK.Add(new MenuBool("KsW", "Enable W Killsteal"));
            menuK.Add(new MenuBool("KsE", "Enable E Killsteal"));
            Config.Add(menuK);
            
            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuBool("AG", "AntiGapcloser", true));
            menuM.Add(new MenuBool("Int", "Interrupter", true));
            menuM.Add(new MenuBool("dash", "Auto W on Dashing Target", true));
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
            GameEvent.OnGameTick += OnUpdate;
            Game.OnUpdate += UpdateTick;
            Render.OnDraw += OnDraw;
            AntiGapcloser.OnGapcloser += Gap;
            Interrupter.OnInterrupterSpell += Int;
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
            if (Config["Esettings"].GetValue<MenuKeyBind>("FlashE").Active)
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                FlashE();

                switch (comb(menuP, "Pred"))
                {
                    case 1:
                        var etarget = E.GetTarget();
                        if (etarget != null)
                        {
                            E.CastLine(etarget, 0f, 0f, false);
                        }
                        break;
                }                
            }

            if (Config["Misc"].GetValue<MenuBool>("dash").Enabled)
            {
                if (E.IsReady())
                {
                    foreach (var enemys in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(E.Range)))
                    {
                        E.CastIfHitchanceEquals(enemys, HitChance.Dash);
                    }
                }
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE();
                LogicQ();
                LogicW();
                LogicR();
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
                var qtarget = Q.GetTarget();
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
                            var qpreddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width, Q.Delay, Q.Speed,
                                Q.Range, false, SpellType.Line);
                            if (qpreddd.HitChance >= hitchance)
                            {
                                Q.Cast(qpreddd.CastPosition);
                            }

                            break;
                    }
                }
            }
        }

        private static void LogicW()
        {
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");

            if (useW.Enabled && W.IsReady())
            {
                var Wtarget = W.GetTarget();
                if (Wtarget != null && Wtarget.IsValidTarget())
                {
                    W.Cast();
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
                            var epreddd = SPredictionMash1.Prediction.GetPrediction(etarget, E.Width, E.Delay, E.Speed,
                                E.Range, true, SpellType.Line);
                            if (epreddd.HitChance >= hitchance)
                            {
                                E.Cast(epreddd.CastPosition);
                            }

                            break;
                    }
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var hp = Config["Rsettings"].GetValue<MenuSlider>("HP");

            if (useR.Enabled && R.IsReady())
            {
                var Rtarget = R.GetTarget();
                if (Rtarget != null && Rtarget.IsValidTarget())
                {
                    if (Rtarget.HealthPercent <= hp.Value)
                    {
                        R.Cast(Game.CursorPos);
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
                                            Q.CastLine(qtarget);
                                        }

                                        break;

                                    case 2:
                                        var qpreddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width,
                                            Q.Delay, Q.Speed,
                                            Q.Range, false, SpellType.Line);
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

            foreach (var wtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(W.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksW && W.IsReady())
                {
                    if (wtarget != null)
                    {
                        if (wtarget.DistanceToPlayer() <= W.Range)
                        {
                            if (wtarget.Health + wtarget.AllShield + wtarget.HPRegenRate <= WDamage(wtarget))
                            {
                                W.Cast();
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
                                        E.CastLine(etarget, 0f, 0f, false);

                                        break;

                                    case 2:
                                        var epreddd = SPredictionMash1.Prediction.GetPrediction(etarget, E.Width,
                                            E.Delay, E.Speed,
                                            E.Range, true, SpellType.Line);
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
        }

        private static readonly float[] QBaseDamage = { 0f, 40f, 65f, 90f, 115f, 140f, 140f };
        private static readonly float[] WBaseDamage = { 0f, 50f, 75f, 100f, 125f, 150f, 150f };
        private static readonly float[] EBaseDamage = { 0f, 80f, 110f, 140f, 170f, 200f, 200f };

        private static float QDamage(AIBaseClient target)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] + .45f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, qBaseDamage);
        }

        private static float WDamage(AIBaseClient target)
        {
            var wLevel = W.Level;
            var wBaseDamage = WBaseDamage[wLevel] + .3f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, wBaseDamage);
        }

        private static float EDamage(AIBaseClient target)
        {
            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel] + .6f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, eBaseDamage);
        }

        private static void FlashE()
        {
            if (Flashslot.IsReady() && E.IsReady())
            {
                var target = TargetSelector.GetTarget(1400, DamageType.Magical);
                if (target != null && target.InRange(1400))
                {
                    switch (comb(menuP, "Pred"))
                    {
                        case 0:
                            var epred = E.GetPrediction(target);
                            Me.Spellbook.CastSpell(Flashslot, target.Position);
                            if (epred.Hitchance >= HitChance.High)
                            {
                                E.Cast(epred.CastPosition);
                            }

                            break;

                        case 1:
                            Me.Spellbook.CastSpell(Flashslot, target.Position);
                            break;

                        case 2:
                            var epreddd = SPredictionMash1.Prediction.GetPrediction(target, E.Width,
                                E.Delay, E.Speed,
                                E.Range, true, SpellType.Line);
                            Me.Spellbook.CastSpell(Flashslot, target.Position);
                            if (epreddd.HitChance >= HitChance.High)
                            {
                                E.Cast(epreddd.CastPosition);
                            }

                            break;
                    }
                }
            }
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
    }
}