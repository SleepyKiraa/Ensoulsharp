using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using SharpDX;
using Geometry = EnsoulSharp.SDK.Geometry;
using SPredictionMash1;
using Fatality.Utils.Oktw;

namespace Fatality.Champions.Lucian
{
    public class Lucian
    {
        private static Spell Q, Q2, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuK, menuM, menuD, menuP, menuL;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Lucian")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 500f + Me.BoundingRadius);
            Q2 = new Spell(SpellSlot.Q, 1000f - Me.BoundingRadius);
            Q2.SetSkillshot(0.25f, 60f, float.MaxValue, false, SpellType.Line);
            W = new Spell(SpellSlot.W, 900f);
            W.SetSkillshot(0.25f, 55f, 1600f, true, SpellType.Line);
            E = new Spell(SpellSlot.E, 425f);
            R = new Spell(SpellSlot.R, 1140f);
            R.SetSkillshot(0.25f, 110f, float.MaxValue, true, SpellType.Line);

            Config = new Menu("Lucian", "[Fatality] Lucian", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Lucian));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo", true));
            menuQ.Add(new MenuBool("useQextend", "Use Extend Q in Combo", true));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo", true));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo"));
            menuE.Add(new MenuKeyBind("turret", "Dont use or dash under Enemy Turret", Keys.T, KeyBindType.Toggle)).AddPermashow();
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuKeyBind("useR", "Force R", Keys.T, KeyBindType.Press)).AddPermashow();
            Config.Add(menuR);
            
            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("WPred", "W Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);

            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal"));
            menuK.Add(new MenuBool("KsQE", "Enable EQ Killsteal"));
            menuK.Add(new MenuBool("KsW", "Enable W Killsteal"));
            Config.Add(menuK);
            
            menuL = new Menu("Clear", "Clear Settings");
            menuL.Add(new MenuSeparator("LaneClear", "Lane Clear"));
            menuL.Add(new MenuBool("LcQ", "Use Q to Lane Clear", true));
            menuL.Add(new MenuBool("LcW", "Use W to Lane Clear", true));
            menuL.Add(new MenuSeparator("JungleClear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
            menuL.Add(new MenuBool("JcW", "Use W to Jungle Clear", true));
            menuL.Add(new MenuBool("JcE", "Use E to Jungle Clear", true));
            Config.Add(menuL);
            
            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuList("Mode", "Combo Mode",
                new string[] { "Q - W - E", "E - Q - W" }, 1)).AddPermashow();
            Config.Add(menuM);

            menuD = new Menu("Draw", "Draw Settings");
            menuD.Add(new MenuSeparator("asdadsa", "Q Draw Settings"));
            menuD.Add(new MenuBool("drawQ", "Q Range (White)", true));
            menuD.Add(new MenuColor("colorQ", "Q Draw Color", Color.White));
            menuD.Add(new MenuSeparator("gggg", "W Draw Settings"));
            menuD.Add(new MenuBool("drawW", "W Range (Blue)"));
            menuD.Add(new MenuColor("colorW", "W Draw Color", Color.Blue));
            menuD.Add(new MenuSeparator("hhh", "E Draw Settings"));
            menuD.Add(new MenuBool("drawE", "E Range (Green)"));
            menuD.Add(new MenuColor("colorE", "E Draw Color", Color.Green));
            menuD.Add(new MenuSeparator("kkk", "R Draw Settings"));
            menuD.Add(new MenuBool("drawR", "R Range (Red)"));
            menuD.Add(new MenuColor("colorR", "R Draw Color", Color.Red));
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

            GameEvent.OnGameTick += OnUpdate;
            Game.OnUpdate += TickUpdate;
            Spellbook.OnCastSpell += OnCastSpell;
            Render.OnDraw += OnDraw;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void TickUpdate(EventArgs args)
        {
            Killsteal();
        }

        private static void OnUpdate(EventArgs args)
        {
            var useR = Config["Rsettings"].GetValue<MenuKeyBind>("useR");
            if (useR.Active)
            {
                LogicR();
            }

            if (Me.HasBuff("LucianR"))
            {
                if (TargetSelector.SelectedTarget != null && TargetSelector.SelectedTarget.Path.Length > 0)
                    Orbwalker.Orbwalk(null, TargetSelector.SelectedTarget.Path.Last());
            }
            
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {

                switch (comb(menuM, "Mode"))
                {
                    case 0:
                        QExtendLogic();
                        LogicQ();
                        LogicW();
                        LogicE();
                        break;
                    
                    case 1:
                        QExtendLogic();
                        LogicE();
                        LogicQ();
                        LogicW();
                        break;
                }
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Laneclear();
                Jungleclear();
            }
        }

        private static void LogicQ()
        {
            if (HasPassive())
            {
                return;
            }

            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");

            if (Q.IsReady() && useQ.Enabled)
            {
                var Qtarget = Q.GetTarget();
                if (Qtarget != null && Qtarget.IsValidTarget())
                {
                    Q.Cast(Qtarget);
                }
            }
        }

        private static void LogicW()
        {
            if (HasPassive())
            {
                return;
            }
            
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");
            
            switch (comb(menuP, "WPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }           

            if (useW.Enabled && W.IsReady())
            {
                var wtarget = W.GetTarget();
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
                            var wpreddd = SPredictionMash1.Prediction.GetPrediction(wtarget, W.Width, W.Delay, W.Speed,
                                W.Range, true, SpellType.Line);
                            if (wpreddd.HitChance >= hitchance)
                            {
                                W.Cast(wpreddd.CastPosition);
                            }

                            break;
                    }
                }
            }
        }

        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            var turret = Config["Esettings"].GetValue<MenuKeyBind>("turret");
            var posAfterE = Me.Position.Extend(Game.CursorPos, 425f);
            var Etarget = E.GetTarget(Me.GetRealAutoAttackRange());

            if (HasPassive())
            {
                return;
            }

            if (posAfterE.IsUnderEnemyTurret() && turret.Active)
            {
                return;
            }

            if (useE.Enabled && E.IsReady())
            {
                if (Etarget != null && Etarget.IsValidTarget())
                {
                    E.Cast(Game.CursorPos);
                }
            }
        }

        private static void LogicR()
        {
            if (R.IsReady())
            {
                var target = TargetSelector.GetTarget(R.Range, DamageType.Physical);
                if (target != null && target.IsValid && target is AIHeroClient && !Me.HasBuff("LucianR"))
                {
                    R.Cast(target.ServerPosition);
                }
            }
        }

        private static void QExtendLogic()
        {
            var t1 = TargetSelector.GetTarget(Q2.Range, DamageType.Physical);
            if (t1 == null) return;

                if (Q.IsReady() && Config["Qsettings"].GetValue<MenuBool>("useQextend").Enabled)
                {   

                if (t1.DistanceToPlayer() <= Q.Range)
                {
                    return;
                }

                if (t1.IsValidTarget(Q2.Range))
                {
                    var predpos = Q2.GetPrediction(t1);
                    if (predpos.Hitchance < HitChance.High)
                        return;
                    foreach (var unit in from unit in GetHittableTargets()
                             let polygon =
                                 new Geometry.Rectangle(
                                     Me.ServerPosition,
                                     Me.ServerPosition.Extend(
                                         unit.ServerPosition,
                                         Q2.Range),
                                     Q2.Width)
                             where polygon.IsInside(predpos.CastPosition) && Q.IsInRange(unit)
                             select unit)
                    {
                        Q.CastOnUnit(unit);
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksEQ = Config["Killsteal"].GetValue<MenuBool>("KsQE").Enabled;
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;
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
                                Q.Cast(qtarget);
                            }
                        }
                    }
                }
            }

            var qetarget = Q.GetTarget(E.Range);
            if (ksEQ && Q.IsReady() && E.IsReady())
            {
                if (qetarget != null)
                {
                    if (!qetarget.HasBuff("JudicatorIntervention") && !qetarget.HasBuff("kindredrnodeathbuff") && !qetarget.HasBuff("Undying Rage") && !qetarget.HasBuffOfType(BuffType.Invulnerability))
                    {
                        if (qetarget.Health + qetarget.AllShield + qetarget.HPRegenRate <= QDamage(qetarget))
                        {
                            E.Cast(qetarget.Position);
                            Q.Cast(qetarget);
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
                                        var wpreddd = SPredictionMash1.Prediction.GetPrediction(wtarget, W.Width, W.Delay, W.Speed,
                                            W.Range, true, SpellType.Line);
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
        }
        
        private static void Laneclear()
        {
            var lcQ = Config["Clear"].GetValue<MenuBool>("LcQ");
            var lcw = Config["Clear"].GetValue<MenuBool>("LcW");

            if (lcQ.Enabled && Q.IsReady())
            {
                var minions = GameObjects.EnemyMinions.FirstOrDefault(x => x.IsValidTarget(Q.Range) && x.IsMinion());
                if (minions == null)
                {
                    return;
                }

                Q.CastOnUnit(minions);
            }
            
            if (lcw.Enabled && W.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var Wfarm = W.GetLineFarmLocation(minions);
                    if (Wfarm.Position.IsValid())
                    {
                        W.Cast(Wfarm.Position);
                    }
                }
            }
        }
        
        private static void Jungleclear()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var JcWw = Config["Clear"].GetValue<MenuBool>("JcW");
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(W.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob);
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast(mob.Position);
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range) E.Cast(Game.CursorPos);
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (Config["Draw"].GetValue<MenuBool>("drawQ").Enabled)
            {
                var qcolor = Config["Draw"].GetValue<MenuColor>("colorQ");
                CircleRender.Draw(Me.Position, Q.Range, qcolor.Color, 2);
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawW").Enabled)
            {
                var wcolor = Config["Draw"].GetValue<MenuColor>("colorW");
                CircleRender.Draw(Me.Position, W.Range, wcolor.Color, 2);
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled)
            {
                var ecolor = Config["Draw"].GetValue<MenuColor>("colorE");
                CircleRender.Draw(Me.Position, E.Range, ecolor.Color, 2);
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled)
            {
                var rcolor = Config["Draw"].GetValue<MenuColor>("colorR");
                CircleRender.Draw(Me.Position, R.Range, rcolor.Color, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawA").Enabled)
            {
                FatalityRenderRing.AALeft();
            }
        }

        private static bool HasPassive()
        {
            return Me.HasBuff("LucianPassiveBuff");
        }

        private static List<AIBaseClient> GetHittableTargets()
        {
            var unitList = new List<AIBaseClient>();
            var minions = Cache.GetMinions(
                Me.ServerPosition,
                Q.Range);

            unitList.AddRange(minions);

            return unitList;
        }

        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot == SpellSlot.E)
            {
                Orbwalker.ResetAutoAttackTimer();
            }
        }

        private static readonly float[] QBaseDamage = { 0f, 95f, 125f, 155f, 185f, 215f, 215f };
        private static readonly float[] QBonusDamage = { 0f, .6f, .75f, .9f, 1.05f, 1.2f, 1.2f };
        private static readonly float[] WBaseDamage = { 0f, 75f, 110f, 145f, 180f, 215f, 215f };

        private static float QDamage(AIBaseClient target)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] + QBonusDamage[qLevel] + GameObjects.Player.GetBonusPhysicalDamage();
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, qBaseDamage);
        }

        private static float WDamage(AIBaseClient target)
        {
            var wLevel = W.Level;
            var wBaseDamage = WBaseDamage[wLevel] + .9f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, wBaseDamage);
        }
    }
}