using System;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using Fatality.Utils.Oktw;
using SharpDX;
using SPredictionMash1;

namespace Fatality.Champions.Ezreal
{
    public class Ezreal
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuR, menuP, menuL, menuK, menuD, menuM;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static int colorindex = 0;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Ezreal")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1200f);
            Q.SetSkillshot(0.3f, 60f, 2000f, true, SpellType.Line);

            W = new Spell(SpellSlot.W, 1200f);
            W.SetSkillshot(0.3f, 80f, 1700f, false, SpellType.Line);

            E = new Spell(SpellSlot.E, 475f);

            R = new Spell(SpellSlot.R);
            R.SetSkillshot(1f, 160f, 2000f, false, SpellType.Line);

            Config = new Menu("Ezreal", "[Fatality Beta] Ezreal", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Ezreal));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo", true));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo", true));
            menuW.Add(new MenuBool("forceW", "Force Orbwalker on W Target", true));
            Config.Add(menuW);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo", true));
            menuR.Add(new MenuBool("Raa", "Disable R in Q Range", true));
            menuR.Add(new MenuSlider("Rrange", "R Range", 2000, 500, 25000));
            menuR.Add(new MenuKeyBind("SemiR", "Semi R", Keys.T, KeyBindType.Press)).AddPermashow();
            Config.Add(menuR);
            
            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("WPred", "W Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("RPred", "R Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal", true));
            menuK.Add(new MenuBool("KsR", "Enable R Killsteal", true));
            Config.Add(menuK);
            
            menuL = new Menu("Clear", "Clear Settings");
            menuL.Add(new MenuSeparator("LaneClear", "Lane Clear"));
            menuL.Add(new MenuBool("LcQ", "Use Q to Lane Clear", true));
            menuL.Add(new MenuSeparator("Jungleclear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
            Config.Add(menuL);

            menuM = new Menu("Misc", "Misc Settings");
            menuM.Add(new MenuBool("AG", "AntiGapcloser"));
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
            GameEvent.OnGameTick += OnGameUpdate;
            Game.OnUpdate += Updatetick;
            AntiGapcloser.OnGapcloser += Gap;
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
                LogicW();
            }
        }

        private static void OnGameUpdate(EventArgs args)
        {
            if (Config["Wsettings"].GetValue<MenuBool>("forceW").Enabled)
            {
                var heros = TargetSelector.GetTargets(Me.GetCurrentAutoAttackRange(), DamageType.Physical);
                Orbwalker.ForceTarget = heros.FirstOrDefault(x => x.HasBuff("ezrealwattach"));
            }

            if (Config["Rsettings"].GetValue<MenuKeyBind>("SemiR").Active)
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                SemiR();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicR();
                LogicQ();
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
            var priotarget = GameObjects.EnemyHeroes.FirstOrDefault(x => x.IsValidTarget(Q.Range) && x.HasBuff("ezrealwattach"));
            
            switch (comb(menuP, "QPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (Me.IsWindingUp)
            {
                return;
            }

            if (Q.IsReady() && useQ.Enabled)
            {
                if (priotarget != null)
                {
                    switch (comb(menuP, "Pred"))
                    {
                        case 0:
                            var Qpred = Q.GetPrediction(priotarget);
                            if (Qpred.Hitchance >= hitchance)
                            {
                                Q.Cast(Qpred.CastPosition);
                            }
                            break;

                        case 1:
                        {
                            Q.CastLine(priotarget);
                        }
                            break;
                        
                        case 2:
                            var Qpredd = Q.GetSPrediction(priotarget);
                            if (Qpredd.HitChance >= hitchance)
                            {
                                Q.Cast(Qpredd.CastPosition);
                            }

                            break;
                    }
                }

                var qtarget = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                if (priotarget == null & qtarget != null && qtarget.IsValidTarget())
                {
                    switch (comb(menuP, "Pred"))
                    {
                        case 0:
                            var Qpred = Q.GetPrediction(qtarget);
                            if (Qpred.Hitchance >= hitchance)
                            {
                                Q.Cast(Qpred.CastPosition);
                            }
                            break;

                        case 1:
                        {
                            Q.CastLine(qtarget);
                        }
                            break;
                        
                        case 2:
                            var Qpredd = Q.GetSPrediction(qtarget);
                            if (Qpredd.HitChance >= hitchance)
                            {
                                Q.Cast(Qpredd.CastPosition);
                            }

                            break;
                    }
                }
            }
        }

        private static void LogicW()
        {
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");
            
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
                var wtarget = TargetSelector.GetTarget(W.Range, DamageType.Magical);
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
                            W.CastLine(wtarget);
                        }
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

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var Raa = Config["Rsettings"].GetValue<MenuBool>("Raa");
            var range = Config["Rsettings"].GetValue<MenuSlider>("Rrange");
            var rtarget = TargetSelector.GetTarget(range.Value, DamageType.Magical);

            if (rtarget != null && Raa.Enabled)
            {
                if (rtarget.InRange(Q.Range))
                {
                    return;
                }
            }
            
            switch (comb(menuP, "RPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (R.IsReady() && useR.Enabled)
            {
                if (rtarget != null && rtarget.IsValidTarget() && rtarget.InRange(range.Value))
                {
                    if (RDamage(rtarget) >= rtarget.Health && OktwCommon.ValidUlt(rtarget))
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
                                var rpredd = R.GetSPrediction(rtarget);
                                if (rpredd.HitChance >= hitchance)
                                {
                                    R.Cast(rpredd.CastPosition);
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
                                        var Qpred = Q.GetPrediction(Qtarget);
                                        if (Qpred.Hitchance >= HitChance.High)
                                        {
                                            Q.Cast(Qpred.CastPosition);
                                        }

                                        break;

                                    case 1:
                                    {
                                        Q.CastLine(Qtarget);
                                    }
                                        break;

                                    case 2:
                                        var Qpredd = Q.GetSPrediction(Qtarget);
                                        if (Qpredd.HitChance >= HitChance.High)
                                        {
                                            Q.Cast(Qpredd.CastPosition);
                                        }

                                        break;
                                }
                            }
                        }
                    }
                }
            }

            foreach (var Rtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(1200) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksR && R.IsReady() && Rtarget.Health > Me.GetAutoAttackDamage(Rtarget))
                {
                    if (Rtarget != null)
                    {
                        if (Rtarget.DistanceToPlayer() <= 1200)
                        {
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
                                        var rpredd = R.GetSPrediction(Rtarget);
                                        if (rpredd.HitChance >= HitChance.High)
                                        {
                                            R.Cast(rpredd.CastPosition);
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
            var lcQ = Config["Clear"].GetValue<MenuBool>("LcQ");

            if (lcQ.Enabled && Q.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var Qfarm = Q.GetLineFarmLocation(minions);
                    if (Qfarm.Position.IsValid())
                    {
                        Q.Cast(Qfarm.Position);
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
                        var Range = Config["Rsettings"].GetValue<MenuSlider>("Rrange").Value;
                        CircleRender.Draw(Me.Position, Range, colorR, 2);
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
                        var Range = Config["Rsettings"].GetValue<MenuSlider>("Rrange").Value;
                        CircleRender.Draw(Me.Position, Range, color[colorindex], 2);
                        
                    }
                    break;
            }
        }

        private static void SemiR()
        {
            var range = Config["Rsettings"].GetValue<MenuSlider>("Rrange");
            if (R.IsReady())
            {
                var rtarget = TargetSelector.GetTarget(range.Value, DamageType.Magical);
                if (rtarget.InRange(range.Value))
                {
                    var pred = R.GetSPrediction(rtarget);
                    if (pred.HitChance >= HitChance.High)
                    {
                        R.Cast(pred.CastPosition);
                    }
                }
            }
        }

        private static void Gap(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("AG").Enabled)
            {
                if (E.IsReady() && sender.IsEnemy)
                {
                    if (args.EndPosition.DistanceToPlayer() < E.Range && args.Target.IsMe)
                    {
                        E.Cast(Me.Position.Extend(args.EndPosition, -E.Range));
                    }
                }
            }
        }

        private static readonly float[] QBaseDamage = { 0f, 20f, 45f, 70f, 95f, 120f, 120f };
        private static readonly float[] RBaseDamage = { 0f, 350f, 500f, 650f, 650f };

        private static float QDamage(AIBaseClient target)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] + (1.3f * Me.TotalAttackDamage + .15f * Me.TotalMagicalDamage);
            return (float)Me.CalculateDamage(target, DamageType.Physical, qBaseDamage);
        }

        private static float RDamage(AIBaseClient target)
        {
            var rLevel = R.Level;
            var rBaseDamage = RBaseDamage[rLevel] + (1f * Me.TotalAttackDamage + .9f * Me.TotalMagicalDamage);
            return (float)Me.CalculateDamage(target, DamageType.Magical, rBaseDamage);
        }
    }
}