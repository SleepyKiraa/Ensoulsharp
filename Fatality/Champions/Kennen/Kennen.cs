using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using SharpDX;
using SPredictionMash1;

namespace Fatality.Champions.Kennen
{
    public class Kennen
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuP, menuL, menuK, menuD, menuM;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Kennen")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 950);
            Q.SetSkillshot(0.175f, 50f, 1700f, true, SpellType.Line);
            W = new Spell(SpellSlot.W, 770f);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 550f);

            Config = new Menu("Kennen", "[Fatality] Kennen", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Kennen));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo", true));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo", true));
            menuW.Add(new MenuSlider("Wstacks", "W stacks to use W", 2, 1, 2));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo",true));
            menuE.Add(new MenuSlider("Erange", "Range to use E", 300, 100, 500));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo", true));
            menuR.Add(new MenuSlider("ene", "Min Enemys in R Range to Use R", 2, 1, 5));
            Config.Add(menuR);
            
            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal", true));
            menuK.Add(new MenuBool("KsW", "Enable W Killsteal", true));
            Config.Add(menuK);
            
            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuSeparator("LaneClear", "Lane Clear"));
            menuL.Add(new MenuBool("LcQ", "Use Q to Lane Clear", true));
            menuL.Add(new MenuSeparator("Jungleclear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
            Config.Add(menuL);
            
            menuM = new Menu("Misc", "Misc Settings");
            menuM.Add(new MenuBool("AG", "AntiGapcloser", true));
            Config.Add(menuM);
            
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
            Render.OnDraw += OnDraw;
            AntiGapcloser.OnGapcloser += Gap;
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
                LogicR();
                LogicQ();
                LogicW();
                LogicE();
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
                var qtarget = Q.GetTarget();
                if (qtarget != null && qtarget.IsValidTarget())
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
                                Q.CastLine(qtarget, 0f, 0f, false);
                            }

                            break;
                        
                        case 2:
                            var qpreddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width, Q.Delay, Q.Speed,
                                Q.Range, true, SpellType.Line);
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
            var stack = Config["Wsettings"].GetValue<MenuSlider>("Wstacks");

            if (useW.Enabled && W.IsReady())
            {
                var wtarget = TargetSelector.GetTarget(W.Range, DamageType.Magical);
                if (wtarget != null)
                {
                    if (wtarget.GetBuffCount("kennenmarkofstorm") >= stack.Value)
                    {
                        W.Cast();
                    }
                }
            }
        }
        
        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            var range = Config["Esettings"].GetValue<MenuSlider>("Erange");

            if (E.IsReady() && useE.Enabled)
            {
                var Etarget = TargetSelector.GetTarget(range.Value, DamageType.Magical);
                if (Etarget != null)
                {
                    if (!Me.HasBuff("KennenLightningRush"))
                    {
                        E.Cast();
                    }
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var enemys = Config["Rsettings"].GetValue<MenuSlider>("ene");

            if (R.IsReady() && useR.Enabled)
            {
                var rtarget = TargetSelector.GetTarget(R.Range, DamageType.Magical);
                if (rtarget != null)
                {
                    if (Me.CountEnemyHeroesInRange(R.Range) >= enemys.Value)
                    {
                        R.Cast();
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
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
                        if (qtarget.Health + qtarget.AllShield <= QDamage(qtarget))
                        {
                            if (qtarget.DistanceToPlayer() <= Q.Range)
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0://Sdk
                                        var qpred = Q.GetPrediction(qtarget);
                                        if (qpred.Hitchance >= HitChance.High)
                                        {
                                            Q.Cast(qpred.CastPosition);
                                        }

                                        break;
                                    
                                    case 1://Oktw
                                        {
                                            Q.CastLine(qtarget, 0f ,0f, false);
                                        }

                                        break;
                                    
                                    case 2:
                                        var qpreddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width, Q.Delay, Q.Speed,
                                            Q.Range, true, SpellType.Line);
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
                        if (wtarget.Health + wtarget.AllShield <= WDamage(wtarget))
                        {
                            if (wtarget.DistanceToPlayer() <= W.Range)
                            {
                                if (wtarget.HasBuff("kennenmarkofstorm"))
                                {
                                    W.Cast();
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private static void Laneclear()
        {
            var lcq = Config["Clear"].GetValue<MenuBool>("LcQ");

            if (lcq.Enabled && Q.IsReady())
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
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ").Enabled;
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
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
                var erange = Config["Esettings"].GetValue<MenuSlider>("Erange");
                CircleRender.Draw(Me.Position, erange.Value, Color.Green, 2);
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
        
        private static void Gap(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (sender.IsMe)
            {
                return;
            }

            if (Config["Misc"].GetValue<MenuBool>("AG").Enabled && Q.IsReady() || W.IsReady())
            {
                if (sender.GetBuffCount("kennenmarkofstorm") == 2)
                {
                    if (args.EndPosition.DistanceToPlayer() < 450)
                    {
                        Q.Cast(args.EndPosition);
                        W.Cast(sender);
                    }
                }
            }
        }

        private static readonly float[] QBaseDamage = { 0f, 75f, 125f, 175f, 225f, 275f, 275f };
        private static readonly float[] WBaseDamage = { 0f, 70f, 95f, 120f, 145f, 170f, 170f };
        private static readonly float[] EBaseDamage = { 0f, 80f, 120f, 160f, 200f, 240f, 240f };
        private static readonly float[] RBaseDamage = { 0f, 300f, 562f, 825f, 825f };

        private static float QDamage(AIBaseClient target)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] + .85f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, qBaseDamage);
        }

        private static float WDamage(AIBaseClient target)
        {
            var wLevel = W.Level;
            var wBaseDamage = WBaseDamage[wLevel] + .8f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, wBaseDamage);
        }

        private static float EDamage(AIBaseClient target)
        {
            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel] + .8f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, eBaseDamage);
        }

        private static float RDamage(AIBaseClient target)
        {
            var rLevel = R.Level;
            var rBaseDamage = RBaseDamage[rLevel] + 1.68f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, rBaseDamage);
        }
    }
}