using System;
using System.Linq;
using System.Runtime.CompilerServices;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using EnsoulSharp.SDK.Utility;
using Fatality.Utils;
using SharpDX;
using SPredictionMash1;

namespace Fatality.Champions.Riven
{
    public class Riven
    {
        private static Spell Q, Q2, Q3, W, E, R, R2;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuP, menuL, menuK, menuD;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        
        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Riven")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 300f);

            Q2 = new Spell(SpellSlot.Q, 300f);

            Q3 = new Spell(SpellSlot.Q, 300f);
            Q3.SetSkillshot(0.4f, 50f, float.MaxValue, false, SpellType.Circle);

            W = new Spell(SpellSlot.W, 250f);

            E = new Spell(SpellSlot.E, 290f);

            R = new Spell(SpellSlot.R);

            R2 = new Spell(SpellSlot.R, 1100f);
            R2.SetSkillshot(0.25f, 100f, 1600f, false, SpellType.Cone);

            Config = new Menu("Riven", "[Fatality] Riven", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Riven));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo"));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo"));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo"));
            menuE.Add(new MenuKeyBind("Egap", "Try To Gapclose with E",Keys.G, KeyBindType.Toggle)).AddPermashow();
            menuE.Add(new MenuList("Emode", "E Mode",
                new String[] { "Cursor", "Target" }, 1)).AddPermashow();
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuKeyBind("useR", "Toggle R Usage", Keys.T, KeyBindType.Toggle)).AddPermashow();
            menuR.Add(new MenuSlider("HP", "Target HP % To use R"));
            menuR.Add(new MenuSlider("Range", "Min Range To Cast R1", 500,
                125, 1100));
            Config.Add(menuR);
            
            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)" }, 2)).AddPermashow();
            menuP.Add(new MenuList("R2Pred", "R2 Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal settings");
            menuK.Add(new MenuBool("KsW", "Enable W Killsteal", true));
            menuK.Add(new MenuBool("KsR2", "Enable R2 Killsteal", true));
            Config.Add(menuK);
            
            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuSeparator("LaneClear", "Lane Clear"));
            menuL.Add(new MenuBool("LcQ", "Use Q to Lane Clear", true));
            menuL.Add(new MenuBool("LcW", "Use W to Lane Clear", true));
            menuL.Add(new MenuSeparator("Jungleclear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
            menuL.Add(new MenuBool("JcW", "Use W to Jungle Clear", true));
            Config.Add(menuL);
            
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
            menuD.Add(new MenuBool("drawR2", "Draw R2 Range", true));
            menuD.Add(new MenuBool("drawRText", "Draw R Toggle Text", true));
            menuD.Add(new MenuColor("colorR", "Change R Draw Color", Color.Red));
            menuD.Add(new MenuSeparator("mm", "Misc Draw Settings"));
            menuD.Add(new MenuBool("drawGap", "Drawp Gapclose Range Text"));
            menuD.Add(new MenuBool("drawB", "Draw Buff Times"));
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

            Game.OnUpdate += OnTick;
            GameEvent.OnGameTick += OnUpdate;
            AIBaseClient.OnPlayAnimation += Init;
            Render.OnDraw += OnDraw;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void OnTick(EventArgs args)
        {
            Killsteal();
            
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicR();
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicW();
                LogicE();
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
            var qtarget = TargetSelector.GetTarget(Q.Range + Q2.Range + Q3.Range, DamageType.Physical);

            if (Me.IsWindingUp)
            {
                return;
            }

            if (qtarget != null)
            {
                if (Me.GetBuffCount("RivenPassiveAABoost") > 2)
                {
                    return;
                }
            }

            if (useQ.Enabled && Q.IsReady())
            {
                if (qtarget != null)
                {
                    if (!Me.HasBuff("RivenTriCleave"))
                    {
                        Q.Cast(qtarget);
                    }

                    if (Me.GetBuffCount("RivenTriCleave") == 1)
                    {
                        Q2.Cast(qtarget);
                    }

                    if (Me.GetBuffCount("RivenTriCleave") == 2)
                    {
                        Q3.Cast(qtarget);
                    }
                }
            }
        }

        private static void LogicW()
        {
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");

            if (W.IsReady() && useW.Enabled)
            {
                var wtarget = W.GetTarget();
                if (wtarget != null && wtarget.IsValidTarget())
                {
                    W.Cast();
                }
            }
        }

        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            var gapE = Config["Esettings"].GetValue<MenuKeyBind>("Egap");

            if (gapE.Active && E.IsReady())
            {
                if (Q.IsReady() && !Me.HasBuff("RivenTriCleave"))
                {
                    var target = TargetSelector.GetTarget(Q.Range + Q2.Range + Q3.Range + E.Range, DamageType.Physical);
                    if (target != null && target.IsValidTarget())
                    {
                        if (!target.InRange(Q.Range + Q2.Range + E.Range) && !target.InRange(Me.GetCurrentAutoAttackRange()))
                        {
                            E.Cast(target.ServerPosition);
                        }
                    }
                }

                if (Q.IsReady() && Me.GetBuffCount("RivenTriCleave") == 1)
                {
                    var target2 = TargetSelector.GetTarget(Q.Range + Q2.Range + E.Range, DamageType.Physical);
                    if (target2 != null && target2.IsValidTarget())
                    {
                        if (!target2.InRange(Q.Range + E.Range) && !target2.InRange(Me.GetCurrentAutoAttackRange()))
                        {
                            E.Cast(target2.ServerPosition);
                        }
                    }
                }
                
                if (Q.IsReady() && Me.GetBuffCount("RivenTriCleave") == 2)
                {
                    var target3 = TargetSelector.GetTarget(Q.Range  + E.Range, DamageType.Physical);
                    if (target3 != null && target3.IsValidTarget())
                    {
                        if (!target3.InRange(Me.GetCurrentAutoAttackRange()))
                        {
                            E.Cast(target3.ServerPosition);
                        }
                    }
                }
            }

            if (useE.Enabled && E.IsReady())
            {
                var etarget = E.GetTarget();
                if (etarget != null && etarget.IsValidTarget())
                {
                    switch (comb(menuE, "Emode"))
                    {
                        case 0:
                            E.Cast(Game.CursorPos);
                            break;
                            
                        case 1:
                            E.Cast(etarget.Position);
                            break;
                    }
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuKeyBind>("useR");
            var HP = Config["Rsettings"].GetValue<MenuSlider>("HP");
            var Range = Config["Rsettings"].GetValue<MenuSlider>("Range");

            if (useR.Active && R.IsReady() && !Me.HasBuff("rivenwindslashready"))
            {
                var r1target = TargetSelector.GetTarget(Range.Value, DamageType.Physical);
                if (r1target != null && r1target.IsValidTarget())
                {
                    if (r1target.HealthPercent <= HP.Value)
                    {
                        R.Cast();
                    }

                }
            }
            
            switch (comb(menuP, "R2Pred"))
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

            if (useR.Active && R.IsReady() && Me.HasBuff("rivenwindslashready"))
            {
                var r2target = R2.GetTarget();
                if (r2target != null && r2target.IsValidTarget())
                {
                    if (r2target.Health <= R2Damage(r2target))
                    {
                        switch (comb(menuP, "Pred"))
                        {
                            case 0:
                                var rpred = R2.GetPrediction(r2target);
                                if (rpred.Hitchance >= hitchance)
                                {
                                    R2.Cast(rpred.CastPosition);
                                }

                                break;

                            case 1:
                            {
                                R2.CastCone(r2target);
                            }
                                break;
                            
                            case 2:
                                var rpredd = R2.GetSPrediction(r2target);
                                if (rpredd.HitChance >= hitchance)
                                {
                                    R2.Cast(rpredd.CastPosition);
                                }

                                break;
                        }
                    }
                }
            }
            
        }

        private static void QReset(int time)
        {
            DelayAction.Add(time, () =>
            {
                Game.SendEmote(EmoteId.Dance);
                Orbwalker.ResetAutoAttackTimer();
                Me.IssueOrder(GameObjectOrder.MoveTo, Me.Position.Extend(Game.CursorPos, +10));
            });
        }

        private static void Init(AIBaseClient sender, AIBaseClientPlayAnimationEventArgs args)
        {
            if (!sender.IsMelee)
            {
                return;
            }

            if (args.Animation.Contains("Spell1a"))
            {
                QReset(280);
            }
            else if (args.Animation.Contains("Spell1b"))
            {
                QReset(280);
            }
            else if (args.Animation.Contains("Spell1c"))
            {
                QReset(280);
            }
        }

        private static void Killsteal()
        {
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;
            var ksR2 = Config["Killsteal"].GetValue<MenuBool>("KsR2").Enabled;
            foreach (var Wtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(W.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (W.IsReady() && ksW)
                {
                    if (Wtarget != null)
                    {
                        if (Wtarget.DistanceToPlayer() <= W.Range)
                        {
                            if (Wtarget.Health + Wtarget.AllShield + Wtarget.HPRegenRate <= WDamage(Wtarget))
                            {
                                W.Cast();
                            }
                        }
                    }
                }
            }

            foreach (var R2target in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(R2.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksR2 && R2.IsReady())
                {
                    if (Me.HasBuff("rivenwindslashready"))
                    {
                        if (R2target != null)
                        {
                            if (R2target.DistanceToPlayer() <= R2.Range)
                            {
                                if (R2target.Health + R2target.AllShield + R2target.HPRegenRate <= R2Damage(R2target))
                                {
                                    switch (comb(menuP, "Pred"))
                                    {
                                        case 0:
                                            var rpred = R2.GetPrediction(R2target);
                                            if (rpred.Hitchance >= HitChance.High)
                                            {
                                                R2.Cast(rpred.CastPosition);
                                            }

                                            break;

                                        case 1:
                                        {
                                            R2.CastCone(R2target);
                                        }
                                            break;
                            
                                        case 2:
                                            var rpredd = R2.GetSPrediction(R2target);
                                            if (rpredd.HitChance >= HitChance.High)
                                            {
                                                R2.Cast(rpredd.CastPosition);
                                            }

                                            break;
                                    }
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
            var lcW = Config["Clear"].GetValue<MenuBool>("LcW");

            if (lcQ.Enabled && Q.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var Qfarm = Q.GetCircularFarmLocation(minions);
                    if (Qfarm.Position.IsValid())
                    {
                        Q.Cast(Qfarm.Position);
                    }
                }
            }
            
            if (lcW.Enabled && W.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var Wfarm = W.GetCircularFarmLocation(minions);
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
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast();
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
            
            if (Config["Draw"].GetValue<MenuBool>("drawR2").Enabled)
            {
                var colorR = Config["Draw"].GetValue<MenuColor>("colorR").Color;
                CircleRender.Draw(Me.Position, R2.Range, colorR, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawRText").Enabled)
            {
                if (!Config["Rsettings"].GetValue<MenuKeyBind>("useR").Active)
                {
                    FatalityRenderRing.DrawText2("R Disabled!", Drawing.WorldToScreen(Me.Position - Drawing.Height * 0.05f), Color.Red);
                }

                if (Config["Rsettings"].GetValue<MenuKeyBind>("useR").Active)
                {
                    FatalityRenderRing.DrawText2("R Enabled!", Drawing.WorldToScreen(Me.Position - Drawing.Height * 0.05f), Color.LimeGreen);
                }
            }

            if (Config["Draw"].GetValue<MenuBool>("drawB").Enabled)
            {
                var PassiveBuff = Me.GetBuff("RivenPassiveAABoost");
                var QBuff = Me.GetBuff("RivenTriCleave");
                var R2Buff = Me.GetBuff("rivenwindslashready");

                if (PassiveBuff != null)
                {
                    var timer = PassiveBuff.EndTime - Game.Time;
                    FatalityRenderRing.DrawText2($"Passive Time: {timer:N1}", Drawing.WorldToScreen(Me.Position - Drawing.Height * 0.1f), Color.Red);
                }

                if (QBuff != null)
                {
                    var timer2 = QBuff.EndTime - Game.Time;
                    FatalityRenderRing.DrawText2($"    Q Time: {timer2:N1}", Drawing.WorldToScreen(Me.Position - Drawing.Height * 0.13f), Color.Red);
                }

                if (R2Buff != null)
                {
                    var timer3 = R2Buff.EndTime - Game.Time;
                    FatalityRenderRing.DrawText2($"        R2 Time: {timer3:N1}", Drawing.WorldToScreen(Me.Position - Drawing.Height * 0.16f), Color.Red);
                }
            }

            if (Config["Draw"].GetValue<MenuBool>("drawGap").Enabled)
            {
                if (!Me.HasBuff("RivenTriCleave") && E.IsReady())
                {
                    var gaptarget =
                        TargetSelector.GetTarget(E.Range + Q.Range + Q2.Range + Q3.Range, DamageType.Physical);
                    if (gaptarget != null && !gaptarget.InRange(Me.GetCurrentAutoAttackRange()))
                    {
                        if (!gaptarget.InRange(E.Range + Q.Range + Q2.Range) &&
                            gaptarget.InRange(Q.Range + Q2.Range + Q3.Range + E.Range))
                        {
                            FatalityRenderRing.DrawText2("Can Gapclose With E + Full Q", Drawing.WorldToScreen(gaptarget.Position), Color.White);
                        }
                    }
                    
                    if (gaptarget != null && !gaptarget.InRange(Me.GetCurrentAutoAttackRange()))
                    {
                        if (!gaptarget.InRange(E.Range + Q.Range) &&
                            gaptarget.InRange(Q.Range + Q2.Range + E.Range))
                        {
                            FatalityRenderRing.DrawText2("Can Gapclose With E + 2x Q", Drawing.WorldToScreen(gaptarget.Position), Color.White);
                        }
                    }
                    
                    if (gaptarget != null && !gaptarget.InRange(Me.GetCurrentAutoAttackRange()))
                    {
                        if (gaptarget.InRange(Q.Range + E.Range))
                        {
                            FatalityRenderRing.DrawText2("Can Gapclose With E + 1x Q", Drawing.WorldToScreen(gaptarget.Position), Color.White);
                        }
                    }
                }
            }
        }

        private static readonly float[] WBaseDamage = { 0f, 65f, 95f, 125f, 155f, 185f, 185f };

        private static float WDamage(AIBaseClient target)
        {
            var wLevel = W.Level;
            var wbaseDamage = WBaseDamage[wLevel] + 1F * Me.TotalAttackDamage;
            return (float)Me.CalculateDamage(target, DamageType.Physical, wbaseDamage);
        }

        private static float R2Damage(AIBaseClient target)
        {
            return (float)Me.CalculateDamage(target, DamageType.Physical,
                (new double[] { 100, 150, 200 }[Me.Spellbook.GetSpell(SpellSlot.R).Level - 1] +
                 0.6 * Me.TotalAttackDamage) *
                (1 + (target.MaxHealth - target.Health) /
                    target.MaxHealth > 0.75
                        ? 0.75
                        : (target.MaxHealth - target.Health) / target.MaxHealth) * 8 / 3);
        }
    }
}