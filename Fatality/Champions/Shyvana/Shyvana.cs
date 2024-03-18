using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using SharpDX;
using SPredictionMash1;

namespace Fatality.Champions.Shyvana
{
    public class Shyvana
    {
        private static Spell Q, W, E, E2, R;
        private static Menu Config, menuQ, menuW, menuE, menuP, menuL, menuK, menuD;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Shyvana")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 325f);
            E = new Spell(SpellSlot.E, 925f);
            E.SetSkillshot(0.25f, 60f, 1600f, false, SpellType.Line);
            E2 = new Spell(SpellSlot.E, 925f);
            E2.SetSkillshot(0.33f, 200f, 1575f, false, SpellType.Line);
            R = new Spell(SpellSlot.R, 850f);
            R.SetSkillshot(0.25f, 0f, 10f, false, SpellType.Line);

            Config = new Menu("Shyvana", "[Fatality] Shyvana", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Shyvana));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo", true));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo", true));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo", true));
            menuE.Add(new MenuBool("useE2", "Use Dragon E in Combo", true));
            Config.Add(menuE);
            
            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("EPred", "E Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("E2Pred", "E2 Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal", true));
            menuK.Add(new MenuBool("KsW", "Enable W Killsteal", true));
            menuK.Add(new MenuBool("KsE", "Enable E Killsteal", true));
            Config.Add(menuK);
            
            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuSeparator("LaneClear", "Lane Clear"));
            menuL.Add(new MenuBool("LcW", "Use W to Lane Clear", true));
            menuL.Add(new MenuBool("LcE", "Use E to Lane Clear", true));
            menuL.Add(new MenuSeparator("Jungleclear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
            menuL.Add(new MenuBool("JcW", "Use W to Jungle Clear", true));
            menuL.Add(new MenuBool("JcE", "Use E to Jungle Clear", true));
            Config.Add(menuL);
            
            menuD = new Menu("Draw", "Draw Settings");
            menuD.Add(new MenuBool("drawQ", "Draw Q Range (White)", true));
            menuD.Add(new MenuBool("drawW", "Draw W Range (Blue)", true));
            menuD.Add(new MenuBool("drawE", "Draw E Range (Green)", true));
            menuD.Add(new MenuBool("drawR", "Draw R Range (Red)", true));
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
            Orbwalker.OnAfterAttack += AfterAA;
            Render.OnDraw += OnDraw;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }
        
        private static void Updatetick(EventArgs args)
        {
            Killsteal();
        }

        private static void OnGameUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE();
                LogicW();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Laneclear();
                Jungleclear();
            }
        }

        private static void AfterAA(object sender, AfterAttackEventArgs args)
        {
            LogicQ(args);
        }

        private static void LogicQ(AfterAttackEventArgs args)
        {
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");

            if (useQ.Enabled && Q.IsReady())
            {
                var qtarget = args.Target as AIHeroClient;
                if (qtarget != null)
                {
                    if (qtarget.InRange(Me.GetRealAutoAttackRange()))
                    {
                        if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                        {
                            Q.Cast();
                        }
                    }
                }
            }
        }

        private static void LogicW()
        {
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");

            if (W.IsReady() && useW.Enabled)
            {
                var wtarget = TargetSelector.GetTarget(W.Range, DamageType.Magical);
                if (wtarget != null)
                {
                    W.Cast();
                }
            }
        }

        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            var useE2 = Config["Esettings"].GetValue<MenuBool>("useE2");
            
            switch (comb(menuP, "EPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }           

            if (E.IsReady() && useE.Enabled)
            {
                if (!Me.HasBuff("ShyvanaTransform"))
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
                                    E.CastLine(etarget);
                                }

                                break;
                            
                            case 2:
                                var epreddd = SPredictionMash1.Prediction.GetPrediction(etarget, E.Width, E.Delay,
                                    E.Speed, E.Range, false, SpellType.Line);
                                if (epreddd.HitChance >= hitchance)
                                {
                                    E.Cast(epreddd.CastPosition);
                                }

                                break;
                        }
                    }
                }
                
                switch (comb(menuP, "E2Pred"))
                {
                    case 0: hitchance = HitChance.Low; break;
                    case 1: hitchance = HitChance.Medium; break;
                    case 2: hitchance = HitChance.High; break;
                    case 3: hitchance = HitChance.VeryHigh; break;
                    default: hitchance = HitChance.High; break;
                }               

                if (E.IsReady() && useE2.Enabled)
                {
                    if (Me.HasBuff("ShyvanaTransform"))
                    {
                        var e2taarget = E2.GetTarget();
                        if (e2taarget != null && e2taarget.IsValidTarget())
                        {
                            switch (comb(menuP, "Pred"))
                            {
                                case 0:
                                    var e2pred = E2.GetPrediction(e2taarget, true);
                                    if (e2pred.Hitchance >= hitchance)
                                    {
                                        E2.Cast(e2pred.CastPosition);
                                    }

                                    break;
                                
                                case 1:
                                    {
                                        E2.CastCircle(e2taarget);
                                    }

                                    break;
                                
                                case 2:
                                    var e2preddd = SPredictionMash1.Prediction.GetPrediction(e2taarget, E2.Width,
                                        E2.Delay, E2.Speed, E2.Range, false, SpellType.Circle);
                                    if (e2preddd.HitChance >= hitchance)
                                    {
                                        E2.Cast(e2preddd.CastPosition);
                                    }

                                    break;
                            }
                        }
                    }
                }
            }
        }
        
        private static void Laneclear()
        {
            var lcW = Config["Clear"].GetValue<MenuBool>("LcW");
            var lcE = Config["Clear"].GetValue<MenuBool>("LcE");

            if (lcW.Enabled && W.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                
                if (minions.Any())
                {
                    var wfarm = W.GetCircularFarmLocation(minions);
                    if (wfarm.Position.IsValid())
                    {
                        W.Cast();
                        return;
                    }
                }
            }
            
            if (lcE.Enabled && E.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                
                if (minions.Any())
                {
                    var efarm = E.GetLineFarmLocation(minions);
                    if (efarm.Position.IsValid())
                    {
                        E.Cast(efarm.Position);
                        return;
                    }
                }
            }
        }
        
        private static void Jungleclear()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var JcWw = Config["Clear"].GetValue<MenuBool>("JcW");
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Me.GetRealAutoAttackRange()) Q.Cast();
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast();
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range) E.Cast(mob.Position);
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;

            foreach (var Qtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(Me.GetRealAutoAttackRange()) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksQ && Q.IsReady())
                {
                    if (Qtarget != null)
                    {
                        if (Qtarget.DistanceToPlayer() <= Me.GetRealAutoAttackRange())
                        {
                            if (Qtarget.Health + Qtarget.AllShield <= QDamage(Qtarget))
                            {
                                Q.Cast();
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
                if (W.IsReady() && ksW)
                {
                    if (Wtarget != null)
                    {
                        if (Wtarget.DistanceToPlayer() <= W.Range)
                        {
                            if (Wtarget.Health + Wtarget.AllShield <= WDamage(Wtarget))
                            {
                                W.Cast();
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
                            if (Etarget.Health + Etarget.AllShield <= EDamage(Etarget))
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0:
                                        var Epred = E.GetPrediction(Etarget);
                                        if (Epred.Hitchance >= HitChance.High)
                                        {
                                            E.Cast(Epred.CastPosition);
                                        }

                                        break;
                                    
                                    case 1:
                                        {
                                            E.CastLine(Etarget);
                                        }

                                        break;
                                    
                                    case 2:
                                        var epreddd = SPredictionMash1.Prediction.GetPrediction(Etarget, E.Width, E.Delay,
                                            E.Speed, E.Range, false, SpellType.Line);
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
                CircleRender.Draw(Me.Position, Q.Range, Color.White, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawW").Enabled)
            {
                CircleRender.Draw(Me.Position, W.Range, Color.Blue, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled)
            {
                CircleRender.Draw(Me.Position, E.Range, Color.Green, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled)
            {
                CircleRender.Draw(Me.Position, R.Range, Color.Red, 2);
            }
        }

        private static float GetComboDamage(AIHeroClient target)
        {
            var Damage = 0d;
            if (Q.IsReady())
            {
                Damage += QDamage(target);
            }

            if (W.IsReady())
            {
                Damage += WDamage(target);
            }

            if (E.IsReady())
            {
                Damage += EDamage(target);
            }

            if (R.IsReady())
            {
                Damage += RDamage(target);
            }

            return (float)Damage;
        }

        private static readonly float[] QBaseDamage = { 0f, 20f, 35f, 50f, 65f, 80f, 80f };
        private static readonly float[] WBaseDamage = { 0f, 10f, 16f, 22f, 28f, 35f, 35f };
        private static readonly float[] EBaseDamage = { 0f, 60f, 110f, 140f, 180f, 220f, 220f };
        private static readonly float[] RBaseDamage = { 0f, 150f, 250f, 350f, 350f };

        private static float QDamage(AIBaseClient target)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] + .25f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, qBaseDamage);
        }

        private static float WDamage(AIBaseClient target)
        {
            var wLevel = W.Level;
            var wBaseDamage = WBaseDamage[wLevel] + .15 * GameObjects.Player.TotalAttackDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, wBaseDamage);
        }

        private static float EDamage(AIBaseClient target)
        {
            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel] + (.4f * GameObjects.Player.TotalAttackDamage +
                              .9f * GameObjects.Player.TotalMagicalDamage);
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, eBaseDamage);
        }

        private static float RDamage(AIBaseClient target)
        {
            var rLevel = R.Level;
            var rBaseDamage = RBaseDamage[rLevel] + 1.3f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, rBaseDamage);
        }
    }
}