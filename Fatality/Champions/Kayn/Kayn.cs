using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Damages.Spells;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using EnsoulSharp.SDK.Utility;
using Fatality.Utils;
using SharpDX;
using SPredictionMash1;

namespace Fatality.Champions.Kayn
{
    public class Kayn
    {
        private static Spell Q, W, W2, E, R, R2;
        private static Menu Config, menuQ, menuW, menuR, menuP, menuL, menuK, menuD, menuM;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Kayn")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 350);
            W = new Spell(SpellSlot.W, 700f);
            W.SetSkillshot(0.55f, 65f, 300f, false, SpellType.Line);
            W2 = new Spell(SpellSlot.W, 900f);
            W2.SetSkillshot(0f, 65f, 500f, false, SpellType.Line);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 550f);
            R2 = new Spell(SpellSlot.R, 750f);

            Config = new Menu("Kayn", "[Fatality] Kayn", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Kayn));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo"));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo"));
            Config.Add(menuW);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo"));
            menuR.Add(new MenuSlider("HP", "Your HP % to use R as Darkin form", 50, 1, 100));
            menuR.Add(new MenuSlider("Delay", "R Delay", 250, 0, 750));
            Config.Add(menuR);
            
            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("WPred", "W Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal", true));
            menuK.Add(new MenuBool("KsW", "Enable W Killsteal", true));
            menuK.Add(new MenuBool("KsW2", "Enable Assasin W Killsteal", true));
            menuK.Add(new MenuBool("KsR", "Enable R Killsteal", true));
            menuK.Add(new MenuBool("KsR2", "Enable Assasin R Killsteal", true));
            Config.Add(menuK);
            
            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuBool("AG", "AntiGapcloser", true));
            menuM.Add(new MenuBool("Int", "Interrupter", true));
            Config.Add(menuM);
            
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
            Game.OnUpdate += OnTick;
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
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicW();
                LogicQ();
                LogicR();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Jungleclear();
                Laneclear();
            }
        }

        private static void LogicQ()
        {
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");

            if (Q.IsReady() && useQ.Enabled)
            {
                var qtarget = Q.GetTarget();
                if (qtarget != null && qtarget.IsValidTarget())
                {
                    Q.Cast(qtarget.Position);
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
                if (W.Name == "KaynW")
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
                                    W.CastLine(wtarget);
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

            if (W2.IsReady() && useW.Enabled)
            {
                if (W.Name == "KaynAssW")
                {
                    var w2target = W2.GetTarget();
                    if (w2target != null && w2target.IsValidTarget())
                    {
                        switch (comb(menuP, "Pred"))
                        {
                            case 0:
                                var w2pred = W2.GetPrediction(w2target);
                                if (w2pred.Hitchance >= hitchance)
                                {
                                    W2.Cast(w2pred.CastPosition);
                                }

                                break;
                            
                            case 1:
                                {
                                    W2.CastLine(w2target);
                                }

                                break;
                            
                            case 2:
                                var w2preddd = SPredictionMash1.Prediction.GetPrediction(w2target, W2.Width, W2.Delay,
                                    W2.Speed, W2.Range, false, SpellType.Line);
                                if (w2preddd.HitChance >= hitchance)
                                {
                                    W2.Cast(w2preddd.CastPosition);
                                }

                                break;
                        }
                    }
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var Rhp = Config["Rsettings"].GetValue<MenuSlider>("HP");
            var delay = Config["Rsettings"].GetValue<MenuSlider>("Delay");

            if (useR.Enabled && R.IsReady())
            {
                if (W.Name == "KaynW")
                {
                    var rtarget = R.GetTarget();
                    if (rtarget != null && rtarget.IsValidTarget())
                    {
                        if (rtarget.HasBuff("kaynrenemymark"))
                        {
                            if (Me.HealthPercent <= Rhp.Value)
                            {
                                R.Cast(rtarget);
                            }
                        }
                    }
                }
            }
            
            if (useR.Enabled && R2.IsReady())
            {
                if (W.Name == "KaynAssW")
                {
                    var r2target = R2.GetTarget();
                    if (r2target != null && r2target.IsValidTarget())
                    {
                        if (r2target.HasBuff("kaynrenemymark"))
                        {
                            if (r2target.Health <= RDamage(r2target))
                            {
                                R2.Cast(r2target);
                            }
                        }
                    }
                }
            }
            
            if (R.Name == "KaynRJumpOut")
            {
                DelayAction.Add(delay.Value, () =>
                {
                    R.Cast(Game.CursorPos);
                    R2.Cast(Game.CursorPos);
                });
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
                    var qfarm = Q.GetCircularFarmLocation(minions);
                    if (qfarm.Position.IsValid())
                    {
                        Q.Cast(qfarm.Position);
                        return;
                    }
                }
            }
            
            if (lcW.Enabled && W.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                
                if (minions.Any())
                {
                    var wfarm = W.GetLineFarmLocation(minions);
                    if (wfarm.Position.IsValid())
                    {
                        W.Cast(wfarm.Position);
                        return;
                    }
                }
            }
        }
        
        private static void Jungleclear()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var JcWw = Config["Clear"].GetValue<MenuBool>("JcW");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(W.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast(mob.Position);
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;
            var ksW2 = Config["Killsteal"].GetValue<MenuBool>("KsW2").Enabled;
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;
            var ksR2 = Config["Killsteal"].GetValue<MenuBool>("KsR2").Enabled;
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
                            if (Qtarget.Health + Qtarget.AllShield + Qtarget.HPRegenRate <= Q.GetDamage(Qtarget))
                            {
                                Q.Cast(Qtarget.Position);
                            }
                        }
                    }
                }
            }

            foreach (var Wtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(W.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksW && W.IsReady())
                {
                    if (W.Name == "KaynW")
                    {
                        if (Wtarget != null)
                        {
                            if (Wtarget.DistanceToPlayer() <= W.Range)
                            {
                                if (Wtarget.Health + Wtarget.AllShield + Wtarget.HPRegenRate <= WDamage(Wtarget))
                                {
                                    switch (comb(menuP, "Pred"))
                                    {
                                        case 0:
                                            var wpred = W.GetPrediction(Wtarget);
                                            if (wpred.Hitchance >= HitChance.High)
                                            {
                                                W.Cast(wpred.CastPosition);
                                            }

                                            break;
                            
                                        case 1:
                                            {
                                                W.CastLine(Wtarget);
                                            }

                                            break;
                            
                                        case 2:
                                            var wpreddd = W.GetSPrediction(Wtarget);
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

            foreach (var W2target in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(W2.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksW2 && W2.IsReady())
                {
                    if (W.Name == "KaynAssW")
                    {
                        if (W2target != null)
                        {
                            if (W2target.DistanceToPlayer() <= W2.Range)
                            {
                                if (W2target.Health + W2target.AllShield + W2target.HPRegenRate <= WDamage(W2target))
                                {
                                    switch (comb(menuP, "Pred"))
                                    {
                                        case 0:
                                            var w2pred = W2.GetPrediction(W2target);
                                            if (w2pred.Hitchance >= HitChance.High)
                                            {
                                                W2.Cast(w2pred.CastPosition);
                                            }

                                            break;
                            
                                        case 1:
                                            {
                                                W2.CastLine(W2target);
                                            }

                                            break;
                            
                                        case 2:
                                            var w2preddd = SPredictionMash1.Prediction.GetPrediction(W2target, W2.Width, W2.Delay,
                                                W2.Speed, W2.Range, false, SpellType.Line);
                                            if (w2preddd.HitChance >= HitChance.High)
                                            {
                                                W2.Cast(w2preddd.CastPosition);
                                            }

                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            foreach (var Rtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(R.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability) && hero.HasBuff("kaynrenemymark")))
            {
                if (ksR && R.IsReady())
                {
                    if (W.Name == "KaynW")
                    {
                        if (Rtarget != null)
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

            foreach (var R2target in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(R2.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability) && hero.HasBuff("kaynrenemymark")))
            {
                if (ksR2 && R2.IsReady())
                {
                    if (W.Name == "KaynAssW")
                    {
                        if (R2target != null)
                        {
                            if (R2target.DistanceToPlayer() <= R2.Range)
                            {
                                if (R2target.Health + R2target.AllShield + R2target.HPRegenRate <= RDamage(R2target))
                                {
                                    R2.Cast(R2target);
                                }
                            }
                        }
                    }
                }
            }
            
            if (R.Name == "KaynRJumpOut")
            {
                DelayAction.Add(50, () =>
                {
                    R.Cast(Game.CursorPos);
                    R2.Cast(Game.CursorPos);
                });
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
                if (W.Name == "KaynW")
                {
                    CircleRender.Draw(Me.Position, W.Range, wcolor.Color, 2);
                }
                else
                {
                    if (W.Name == "KaynAssW")
                    {
                        CircleRender.Draw(Me.Position, W2.Range, wcolor.Color, 2); 
                    }
                }
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled)
            {
                var rcolor = Config["Draw"].GetValue<MenuColor>("colorR");
                if (W.Name == "KaynW")
                {
                    CircleRender.Draw(Me.Position, R.Range, rcolor.Color, 2);
                }
                else
                {
                    if (W.Name == "KaynAssW")
                    {
                        CircleRender.Draw(Me.Position, R2.Range, rcolor.Color, 2); 
                    }
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
                if (W.Name == "KaynW")
                {
                    if (W.IsReady())
                    {
                        if (sender.IsValid && sender.IsEnemy)
                        {
                            if (sender.IsDashing())
                            {
                                if (args.EndPosition.DistanceToPlayer() < 500)
                                {
                                    W.CastIfHitchanceEquals(sender, HitChance.Dash);
                                }
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
                if (W.Name == "KaynW")
                {
                    if (sender.IsEnemy && args.DangerLevel == Interrupter.DangerLevel.High && W.IsReady())
                    {
                        if (Me.Distance(sender.ServerPosition) < W.Range)
                        {
                            W.Cast(sender);
                        }
                    }
                }
            }
        }

        private static readonly float[] WBaseDamage = { 0f, 90f, 135f, 180f, 225f, 270f, 270f };
        private static readonly float[] RBaseDamage = { 0f, 150f, 250f, 350f, 350f };

        private static float WDamage(AIBaseClient target)
        {
            var wlevel = W.Level;
            var wBaseDamage = WBaseDamage[wlevel] + 1.3f * GameObjects.Player.TotalAttackDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, wBaseDamage);
        }

        private static float RDamage(AIBaseClient target)
        {
            var rlevel = R.Level;
            var rBaseDamage = RBaseDamage[rlevel] + 1.75 * GameObjects.Player.TotalAttackDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, rBaseDamage);
        }
    }
}