using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using Fatality.Utils.Oktw;
using SharpDX;
using SPredictionMash1;

namespace Fatality.Champions.Ashe
{
    public class Ashe
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuL, menuK, menuD, menuM, menuP;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Ashe")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1200f);
            W.SetSkillshot(0.25f, 20f, 2000f, true, SpellType.Cone);
            E = new Spell(SpellSlot.E, 5000f);
            E.SetSkillshot(0.25f, 10f, 1400f, false, SpellType.Line);
            R = new Spell(SpellSlot.R, 25000f);
            R.SetSkillshot(0.25f, 130f, 1500f, false, SpellType.Line);

            Config = new Menu("Ashe", "[Fatality] Ashe", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Ashe));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo", true));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo", true));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E when target enters bush", true));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo", true));
            menuR.Add(new MenuSlider("Rrange", "R Range", 2000, 1000, 5000));
            menuR.Add(new MenuKeyBind("SemiR", "Semi R", Keys.T, KeyBindType.Press)).AddPermashow();
            Config.Add(menuR);
            
            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred"}, 2)).AddPermashow();
            menuP.Add(new MenuList("WPred", "W Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("RPred", "R Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);

            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsW", "Enable W Killsteal", true));
            menuK.Add(new MenuBool("KsR", "Enable R Killsteal", false));
            Config.Add(menuK);
            
            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuSeparator("LaneClear", "Lane Clear"));
            menuL.Add(new MenuBool("LcQ", "Use Q to Lane Clear", true));
            menuL.Add(new MenuBool("LcW", "Use W to Lane Clear", true));
            menuL.Add(new MenuSeparator("Jungleclear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear"));
            menuL.Add(new MenuBool("JcW", "Use W to Jungle Clear", true));
            Config.Add(menuL);

            menuM = new Menu("Misc", "Misc Settings");
            menuM.Add(new MenuBool("AG", "AntiGapcloser", true));
            menuM.Add(new MenuBool("Int", "Interrupter", true));
            Config.Add(menuM);

            menuD = new Menu("Draw", "Draw Settings");
            menuD.Add(new MenuBool("drawW", "W Range  (Blue)", true));
            menuD.Add(new MenuBool("drawE", "E Range (Green)", true));
            menuD.Add(new MenuBool("drawR", "R Range  (Red)", true));
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
            GameEvent.OnGameTick += OnGameUpdate;
            Game.OnUpdate += UpdateTick;
            Orbwalker.OnAfterAttack += AfterAA;
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
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE();
            }
            
            
            Killsteal();
        }

        private static void OnGameUpdate(EventArgs args)
        {
            if (Config["Rsettings"].GetValue<MenuKeyBind>("SemiR").Active)
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                SemiR();
            }
            
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicW();
                LogicR();
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
            var qtarget = args.Target as AIHeroClient;
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");
            if (qtarget == null) return;

            if (qtarget.InRange(Me.GetRealAutoAttackRange() + 100))
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                {
                    if (Q.IsReady() && useQ.Enabled && Me.HasBuff("asheqcastready"))
                    {
                        Q.Cast();
                    }
                }
            }
        }

        private static void LogicW()
        {
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");
            
            switch (comb(menuP, "WPred"))
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

            if (useW.Enabled && W.IsReady())
            {
                var wtarget = W.GetTarget();
                if (wtarget != null && wtarget.IsValidTarget())
                {
                    switch (comb(menuP, "Pred"))
                    {
                        case 0:
                            var Wpred = W.GetPrediction(wtarget);
                            if (Wpred.Hitchance >= hitchance)
                            {
                                W.Cast(Wpred.CastPosition);
                            }
                            break;
                        
                        case 1:
                            {
                                W.CastCone(wtarget, 0f, 0f, false);
                            }

                            break;
                        
                        case 2:
                            var Wpreddd = SPredictionMash1.Prediction.GetPrediction(wtarget, W.Width, W.Delay, W.Speed,
                                W.Range, true, SpellType.Cone);
                            if (Wpreddd.HitChance >= hitchance)
                            {
                                W.Cast(Wpreddd.CastPosition);
                            }

                            break;
                    }
                }
            }
        }

        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");

            if (E.IsReady() && useE.Enabled)
            {
                var etarget = E.GetTarget(2000);
                if (etarget != null)
                {
                    var epred = E.GetPrediction(etarget);
                    if (NavMesh.GetCollisionFlags(epred.CastPosition) == CollisionFlags.Grass && !Me.IsWindingUp)
                    {
                        E.Cast(epred.CastPosition);
                    }
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var range = Config["Rsettings"].GetValue<MenuSlider>("Rrange");
            
            switch (comb(menuP, "RPred"))
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

            if (useR.Enabled && R.IsReady())
            {
                var Rtarget = TargetSelector.GetTarget(range.Value, DamageType.Magical);
                if (Rtarget != null)
                {
                    if (RDamage(Rtarget) + WDamage(Rtarget) + Me.GetAutoAttackDamage(Rtarget) * 2 >=
                        Rtarget.Health + Rtarget.AllShield)
                    {
                        switch (comb(menuP, "Pred"))
                        {
                            case 0:
                                var Rpred = R.GetPrediction(Rtarget);
                                if (Rpred.Hitchance >= hitchance)
                                {
                                    R.Cast(Rpred.CastPosition);
                                }

                                break;
                            
                            case 1:
                                R.CastLine(Rtarget);

                                break;
                            
                            case 2:
                                var rpreddd = SPredictionMash1.Prediction.GetPrediction(Rtarget, R.Width, R.Delay,
                                    R.Speed, range.Value, false, SpellType.Line);
                                if (rpreddd.HitChance >= HitChance.High)
                                {
                                    R.Cast(rpreddd.CastPosition);
                                }

                                break;
                        }
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;

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
                            switch (comb(menuP, "Pred"))
                            {
                                    case 0:
                                        var Wpred = W.GetPrediction(wtarget);
                                        if (Wpred.Hitchance >= HitChance.High)
                                        {
                                            W.Cast(Wpred.CastPosition);
                                        }

                                        break;

                                    case 1:
                                        {
                                        W.CastCone(wtarget, 0f, 0f, false);
                                        }
                                    break;

                                    case 2:
                                        var Wpreddd = SPredictionMash1.Prediction.GetPrediction(wtarget, W.Width, W.Delay, W.Speed,
                                            W.Range, true, SpellType.Cone);
                                        if (Wpreddd.HitChance >= hitchance)
                                        {
                                            W.Cast(Wpreddd.CastPosition);
                                        }

                                        break;
                            }
                        }
                    }
                }
            }

            foreach (var rtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(2000) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksR && R.IsReady())
                {
                    if (rtarget != null)
                    {
                        if (rtarget.Health + rtarget.AllShield <= RDamage(rtarget))
                        {
                            switch (comb(menuP, "Pred"))
                            {
                                case 0:
                                    var Rpred = R.GetPrediction(rtarget);
                                    if (Rpred.Hitchance >= HitChance.High)
                                    {
                                        R.Cast(Rpred.CastPosition);
                                    }

                                    break;
                                
                                case 1:
                                    {
                                        R.CastLine(rtarget);
                                    }

                                    break;
                                
                                case 2:
                                    var rpreddd = SPredictionMash1.Prediction.GetPrediction(rtarget, R.Width, R.Delay,
                                        R.Speed, 2000, false, SpellType.Line);
                                    if (rpreddd.HitChance >= HitChance.High)
                                    {
                                        R.Cast(rpreddd.CastPosition);
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
            var lcQ = Config["Clear"].GetValue<MenuBool>("LcQ");
            var lcw = Config["Clear"].GetValue<MenuBool>("LcW");

            if (lcw.Enabled && W.IsReady())
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

            if (lcQ.Enabled && Q.IsReady() && Me.HasBuff("asheqcastready"))
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Me.GetRealAutoAttackRange()) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    Q.Cast();
                }
            }
        }

        private static void Jungleclear()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ").Enabled;
            var JcWw = Config["Clear"].GetValue<MenuBool>("JcW").Enabled;
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(W.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq && Q.IsReady() && Me.Distance(mob.Position) < Me.GetRealAutoAttackRange() && Me.HasBuff("asheqcastready")) Q.Cast();
                if (JcWw && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast(mob.Position);
            }
        }

        private static void OnDraw(EventArgs args)
        {
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
                var range = Config["Rsettings"].GetValue<MenuSlider>("Rrange");
                CircleRender.Draw(Me.Position, range.Value, Color.Red, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawA").Enabled)
            {
                FatalityRenderRing.AALeft();
            }
        }

        private static float GetComboDamage(AIHeroClient target)
        {
            var Damage = 0d;
            if (Q.IsReady())
            {
                Damage += Me.GetAutoAttackDamage(target);
            }

            if (W.IsReady())
            {
                Damage += WDamage(target);
            }

            if (E.IsReady())
            {
                Damage += Me.GetSpellDamage(target, SpellSlot.E);
            }

            if (R.IsReady())
            {
                Damage += RDamage(target);
            }
            
            return (float)Damage;
        }

        private static void SemiR()
        {
            var RRange = Config["Rsettings"].GetValue<MenuSlider>("Rrange");

            if (R.IsReady())
            {
                var Rtarget = TargetSelector.GetTarget(RRange.Value, DamageType.Magical);
                if (Rtarget != null)
                {
                    switch (comb(menuP, "Pred"))
                    {
                        case 0:
                            var Rpred = R.GetPrediction(Rtarget);
                            if (Rpred.Hitchance >= HitChance.High)
                            {
                                R.Cast(Rpred.CastPosition);
                            }

                            break;
                        
                        case 1:
                            {
                                R.CastLine(Rtarget);
                            }

                            break;
                        
                        case 2:
                            var rpreddd = SPredictionMash1.Prediction.GetPrediction(Rtarget, R.Width, R.Delay,
                                R.Speed, RRange.Value, false, SpellType.Line);
                            if (rpreddd.HitChance >= HitChance.High)
                            {
                                R.Cast(rpreddd.CastPosition);
                            }

                            break;
                    }
                }
            }
        }

        private static void Gap(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("AG").Enabled && R.IsReady())
            {
                if (R.IsReady())
                {
                    if (sender.IsValid && sender.IsEnemy)
                    {
                        if (sender.IsDashing())
                        {
                            if (args.EndPosition.DistanceToPlayer() < 500)
                            {
                                R.CastIfHitchanceEquals(sender, HitChance.Dash);
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
                if (sender.IsEnemy && args.DangerLevel == Interrupter.DangerLevel.High && R.IsReady())
                {
                    if (Me.Distance(sender.ServerPosition) < 2000)
                    {
                        R.Cast(sender);
                    }
                }
            }
        }

        private static readonly float[] WBaseDamage = { 0f, 10f, 25f, 40f, 55f, 70f, 70f };
        private static readonly float[] RBaseDamage = { 0f, 200f, 400f, 600f, 600f };

        private static float WDamage(AIBaseClient target)
        {
            var wLevel = W.Level;
            var wBaseDamage = WBaseDamage[wLevel] + 1f * GameObjects.Player.TotalAttackDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, wBaseDamage);
        }

        private static float RDamage(AIBaseClient target)
        {
            var rLevel = R.Level;
            var rBaseDamage = RBaseDamage[rLevel] + 1f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, rBaseDamage);
        }
    }
}