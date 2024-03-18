using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using SharpDX;
using SPredictionMash1;

namespace Fatality.Champions.Blitzcrank
{
    public class Blitzcrank
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuE, menuP, menuK, menuD, menuM;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Blitzcrank")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1050);
            Q.SetSkillshot(0.55f, 100f, 1800f, true, SpellType.Line);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 600f);

            Config = new Menu("Blitzcrank", "[Fatality] Blitzcrank", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Blitzcrank));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo", true));
            menuQ.Add(new MenuBool("AutoQ", "Auto Q on Dash/CC", true));
            Config.Add(menuQ);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", " Use E in Combo", true));
            Config.Add(menuE);
            
            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal", true));
            menuK.Add(new MenuBool("KsR", "Enable R Killsteal", true));
            Config.Add(menuK);
            
            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuBool("AG", "AntiGapcloser", true));
            menuM.Add(new MenuBool("Int", "Interrupter", true));
            Config.Add(menuM);

            menuD = new Menu("Draw", "Draw Settings");
            menuD.Add(new MenuSeparator("qq", "Q Draw Settings"));
            menuD.Add(new MenuBool("drawQ", "Draw Q Range", true));
            menuD.Add(new MenuColor("colorQ", "Change Q Draw Color", Color.White));
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
            
            Orbwalker.OnBeforeAttack += BeforeAA;
            Game.OnUpdate += Updatetick;
            GameEvent.OnGameTick += OnGameUpdate;
            AntiGapcloser.OnGapcloser += Gap;
            Interrupter.OnInterrupterSpell += Int;
            Render.OnDraw += OnDraw;

        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }
        
        private static void Updatetick(EventArgs args)
        {
            Killsteal();

            if (Config["Qsettings"].GetValue<MenuBool>("AutoQ").Enabled)
            {
                if (Q.IsReady())
                {
                    foreach (var enemy in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(Q.Range)))
                    {
                        Q.CastIfHitchanceEquals(enemy, HitChance.Dash);
                    }
                }
            }
        }

        private static void OnGameUpdate(EventArgs args)
        {

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicQ();
            }
            
            CC();
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
                var Qtarget = Q.GetTarget();
                if (Qtarget != null && Qtarget.IsValidTarget())
                {
                    switch (comb(menuP, "Pred"))
                    {
                        case 0:
                            var qpred = Q.GetPrediction(Qtarget);
                            if (qpred.Hitchance >= hitchance)
                            {
                                Q.Cast(qpred.CastPosition);
                            }

                            break;
                        
                        case 1:
                            {
                                Q.CastLine(Qtarget, 0f, 0f, false);
                            }

                            break;
                        
                        case 2:
                            var qpreddd = SPredictionMash1.Prediction.GetPrediction(Qtarget, Q.Delay, Q.Delay, Q.Speed, Q.Range, true,
                                SpellType.Line);
                            if (qpreddd.HitChance >= hitchance)
                            {
                                Q.Cast(qpreddd.CastPosition);
                            }
                            break;
                    }
                }
            }
        }

        private static void BeforeAA(object sender, BeforeAttackEventArgs args)
        {
            LogicE(args);
        }

        private static void LogicE(BeforeAttackEventArgs args)
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");

            if (useE.Enabled && E.IsReady())
            {
                var etarget = args.Target as AIHeroClient;
                if (etarget != null && etarget.IsValidTarget())
                {
                    if (etarget.InRange(Me.GetRealAutoAttackRange() + Me.BoundingRadius))
                    {
                        if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                        {
                            E.Cast();
                        }
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
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
                                            Q.CastLine(Qtarget, 0f, 0f, false);
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
                        if (Rtarget.DistanceToPlayer() <= R.Range)
                        {
                            if (Rtarget.Health + Rtarget.AllShield + Rtarget.HPRegenRate <= RDamage(Rtarget))
                            {
                                R.Cast();
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
                var color = Config["Draw"].GetValue<MenuColor>("colorQ").Color;
                CircleRender.Draw(Me.Position, Q.Range, color, 2);
            }
            

            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled)
            {
                var color = Config["Draw"].GetValue<MenuColor>("colorR").Color;
                CircleRender.Draw(Me.Position, R.Range, color, 2);
            }
        }

        private static void CC()
        {
            var autoQ = Config["Qsettings"].GetValue<MenuBool>("AutoQ").Enabled;

            if (Q.IsReady() && autoQ)
            {
                var qtarget = Q.GetTarget();
                if (qtarget != null && qtarget.IsValidTarget())
                {
                    if (qtarget.HasBuffOfType(BuffType.Snare) || qtarget.HasBuffOfType(BuffType.Stun) ||
                        qtarget.HasBuffOfType(BuffType.Suppression))
                    {
                        Q.Cast(qtarget.Position);
                    }
                }
            }
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

        private static void Int(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("Int").Enabled)
            {
                if (sender.IsEnemy && args.DangerLevel == Interrupter.DangerLevel.High && Q.IsReady())
                {
                    if (Me.Distance(sender.ServerPosition) < Q.Range)
                    {
                        Q.Cast(sender);
                    }
                }
                
                if (sender.IsEnemy && args.DangerLevel == Interrupter.DangerLevel.High && R.IsReady())
                {
                    if (Me.Distance(sender.ServerPosition) < R.Range)
                    {
                        R.Cast();
                    }
                }
            }
        }

        private static readonly float[] QBaseDamage = { 0f, 105f, 150f, 195f, 240f, 285f, 285f };
        private static readonly float[] RBaseDamage = { 0f, 275f, 400f, 525f, 525f };

        private static float QDamage(AIBaseClient target)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] + 1.2f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, qBaseDamage);
        }

        private static float RDamage(AIBaseClient target)
        {
            var rLevel = R.Level;
            var rBaseDamage = RBaseDamage[rLevel] + 1f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, rBaseDamage);
        }
    }
}