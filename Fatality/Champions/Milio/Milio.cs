using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using SharpDX;
using SPredictionMash1;
using Menu = EnsoulSharp.SDK.MenuUI.Menu;

namespace Fatality.Champions.Milio
{
    public class Milio
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuE, menuR, menuP, menuK, menuD, menuM, menuL;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static int colorindex = 0;
        private static readonly Dictionary<float, float> IncDamage = new Dictionary<float, float>();
        private static readonly Dictionary<float, float> InstDamage = new Dictionary<float, float>();

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Milio")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1000f);
            Q.SetSkillshot(0.25f, 125f, 1200f, true, SpellType.Line);

            W = new Spell(SpellSlot.W, 700f);

            E = new Spell(SpellSlot.E, 650f);

            R = new Spell(SpellSlot.R, 700f);

            Config = new Menu("Milio", "[Fatality] Milio", true);

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo"));
            Config.Add(menuQ);

            menuE = new Menu("Esettings", "E settings");
            menuE.Add(new MenuBool("useE", "Auto E on Incomming Damage"));
            menuE.Add(new MenuSlider("mindamage", "Min Incomming Damage to Auto E", 300, 100, 1000));

            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R settings");
            menuR.Add(new MenuBool("useR", "Auto R on CC"));
            menuR.Add(new MenuSlider("allies", "Min allies in R Range", 1, 1, 5));
            Config.Add(menuR);
            
            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal", true));
            Config.Add(menuK);
            
            menuL = new Menu("Clear", "Clear Settings"); ;
            menuL.Add(new MenuSeparator("JungleClear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
            Config.Add(menuL);
            
            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuBool("AG", "AntiGapcloser", true));
            menuM.Add(new MenuBool("Int", "Interrupter", true));
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

            AIBaseClient.OnProcessSpellCast += OnProcessSpellcast;
            Game.OnUpdate += OnTick;
            GameEvent.OnGameTick += OnUpdate;
            AntiGapcloser.OnGapcloser += Gap;
            Interrupter.OnInterrupterSpell += Int;
            Render.OnDraw += OnDraw;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void OnTick(EventArgs args)
        {
            Killsteal();
            LogicE();
            LogicR();
            
            foreach (var entry in IncDamage.Where(entry => entry.Key < Game.Time).ToArray())
            {
                IncDamage.Remove(entry.Key);
            }
            
            foreach (var entry in InstDamage.Where(entry => entry.Key < Game.Time).ToArray())
            {
                InstDamage.Remove(entry.Key);
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogciQ();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Jungleclear();
            }
        }

        private static void LogciQ()
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
                            Q.CastLine(qtarget);
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
            var mindamage = Config["Esettings"].GetValue<MenuSlider>("mindamage");

            if (E.IsReady() && useE.Enabled)
            {
                if (IncomingDamage >= mindamage.Value)
                {
                    E.Cast(Me);
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var allies = Config["Rsettings"].GetValue<MenuSlider>("allies");

            if (R.IsReady() && useR.Enabled)
            {
                foreach (var alliess in GameObjects.AllyHeroes.Where(x => x.IsAlly && x.HasBuffOfType(BuffType.Stun) || x.HasBuffOfType(BuffType.Asleep) || x.HasBuffOfType(BuffType.Charm) || x.HasBuffOfType(BuffType.Snare) || x.HasBuffOfType(BuffType.Suppression) || x.HasBuffOfType(BuffType.Taunt) && x.InRange(R.Range) && !x.IsDead))
                {
                    if (Me.CountAllyHeroesInRange(R.Range) >= allies.Value && alliess.IsValid && alliess.InRange(R.Range))
                    {
                        R.Cast();
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
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
                                        Q.CastLine(Qtarget);
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
        }
        
        private static void Jungleclear()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(W.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
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
                        CircleRender.Draw(Me.Position, R.Range, colorR, 2);
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
                        CircleRender.Draw(Me.Position, R.Range, color[colorindex], 2);
                        
                    }
                    break;
            }
        }

        private static void Gap(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("AG").Enabled)
            {
                if (Q.IsReady())
                {
                    if (sender.IsValid && sender.IsEnemy)
                    {
                        if (args.EndPosition.DistanceToPlayer() < 350)
                        {
                            Q.CastIfHitchanceEquals(sender, HitChance.Dash);
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
            }
        }

        private static readonly float[] QBaseDamage = { 0f, 90f, 135f, 180f, 225f, 270f, 270f };

        private static float QDamage(AIBaseClient target)
        {
            var qlevel = Q.Level;
            var qBaseDamage = QBaseDamage[qlevel] + .9f * Me.TotalMagicalDamage;
            return (float)Me.CalculateDamage(target, DamageType.Magical, qBaseDamage);
        }
        
        private static void OnProcessSpellcast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsEnemy)
            {
                if (Orbwalker.IsAutoAttack(args.SData.Name) && args.Target != null &&
                    args.Target.NetworkId == Me.NetworkId)
                {
                    IncDamage[Me.ServerPosition.Distance(sender.ServerPosition) / args.SData.MissileSpeed + Game.Time] =
                        (float)sender.GetAutoAttackDamage(Me);
                }
                else
                {
                    var attacker = sender as AIHeroClient;
                    if (attacker != null)
                    {
                        var slot = attacker.GetSpellSlotFromName(args.SData.Name);

                        if (slot != SpellSlot.Unknown)
                        {
                            if (slot == attacker.GetSpellSlotFromName("SummonerDot") && args.Target != null &&
                                args.Target.NetworkId == Me.NetworkId)
                            {
                                // Ingite damage (dangerous)
                                InstDamage[Game.Time + 2] =
                                    (float)attacker.GetSummonerSpellDamage(Me, SummonerSpell.Ignite);
                            }
                            else
                            {
                                switch (slot)
                                {
                                    case SpellSlot.Q:
                                    case SpellSlot.W:
                                    case SpellSlot.E:
                                    case SpellSlot.R:
                                        if ((args.Target != null && args.Target.NetworkId == Me.NetworkId) ||
                                            args.End.Distance(Me.ServerPosition) <
                                            Math.Pow(args.SData.LineWidth, 1))
                                        {
                                            // Instant damage to target
                                            InstDamage[Game.Time + 2] = (float)attacker.GetSpellDamage(Me, slot);
                                        }

                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }
         
         private static float IncomingDamage
         {
             get { return IncDamage.Sum(e => e.Value) + InstDamage.Sum(e => e.Value); }
         }
    }
}