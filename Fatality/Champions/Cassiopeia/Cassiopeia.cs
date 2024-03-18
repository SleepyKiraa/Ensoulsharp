using System;
using System.Data;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using SharpDX;
using SPredictionMash1;

namespace Fatality.Champions.Cassiopeia
{
    public class Cassiopeia
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuP, menuL, menuK, menuD, menuM;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Cassiopeia")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 850f);
            Q.SetSkillshot(0.65f, 50f, float.MaxValue, false, SpellType.Circle);
            W = new Spell(SpellSlot.W, 700f);
            W.SetSkillshot(0.25f, 80f, 3000f, false, SpellType.Arc);
            E = new Spell(SpellSlot.E, 700f);
            R = new Spell(SpellSlot.R, 850f);

            Config = new Menu("Cassiopeia", "[Fatality] Casiopeia", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Cassiopeia));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo", true));
            menuQ.Add(new MenuKeyBind("onlypoison", "Only use Q if Target is not Poisoned", Keys.T, KeyBindType.Toggle)).AddPermashow();
            menuQ.Add(new MenuBool("dash", "Auto Q on Dashing Target"));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo"));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo"));
            menuE.Add(new MenuKeyBind("onlypoisonn", "Only use E if Target is Poisoned", Keys.G, KeyBindType.Toggle)).AddPermashow();
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo"));
            menuR.Add(new MenuSlider("HP", "Target HP % to use R", 50, 1, 100));
            Config.Add(menuR);

            menuP = new Menu("Pred", "Prediction Settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] {"SDK", "xcsoft", "Spred (Press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "Very High" }, 2));
            Config.Add(menuP);

            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal"));
            menuK.Add(new MenuBool("KsE", "Enable E Killsteal"));
            Config.Add(menuK);
            
            menuL = new Menu("Clear", "Clear Settings");
            menuL.Add(new MenuBool("LcQ", "Use Q to Lane Clear", true));
            menuL.Add(new MenuBool("LhE", "Use E to Last Hit"));
            menuL.Add(new MenuSeparator("JungleClear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
            menuL.Add(new MenuBool("JcE", "Use E to Jungle Clear", true));
            Config.Add(menuL);

            menuM = new Menu("Misc", "Misc Settings");
            menuM.Add(new MenuBool("AG", "AntiGapcloser"));
            menuM.Add(new MenuBool("Int", "interrupter"));
            menuM.Add(new MenuKeyBind("DisableAA", "Disable AA", Keys.Z, KeyBindType.Toggle)).AddPermashow();
            menuM.Add(new MenuSlider("level", "Level to Disable AA", 6, 1, 18));
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
            Game.OnUpdate += Updatetick;
            AntiGapcloser.OnGapcloser += Gap;
            Interrupter.OnInterrupterSpell += Int;
            Orbwalker.OnBeforeAttack += OnBeforeAA;
            Render.OnDraw += OnDraw;
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void Updatetick(EventArgs args)
        {
            Killsteal();

            if (Config["Qsettings"].GetValue<MenuBool>("dash").Enabled)
            {
                if (Q.IsReady())
                {
                    foreach (var enemys in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(Q.Range)))
                    {
                        Q.CastIfHitchanceEquals(enemys, HitChance.Dash);
                    }
                }
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LastHit)
            {
                LastHit();
            }
        }

        private static void OnUpdate(EventArgs args)
        {

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE();
                LogicQ();
                LogicW();
                LogicR();
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
            var poison = Config["Qsettings"].GetValue<MenuKeyBind>("onlypoison");
            var qtarget = Q.GetTarget();
            if (qtarget != null)

             if (poison.Active && qtarget.HasBuffOfType(BuffType.Poison))
             {
                return;
             }
            
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
                                Q.CastCircle(qtarget);
                            }

                            break;
                        
                        case 2:
                            var qpreddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width, Q.Delay, Q.Speed,
                                Q.Range, false, SpellType.Circle);
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

            if (W.IsReady() && useW.Enabled)
            {
                var wtarget = W.GetTarget();
                if (wtarget != null && wtarget.IsValidTarget())
                {
                    W.Cast(wtarget.Position);
                }
            }
        }

        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            var poison = Config["Esettings"].GetValue<MenuKeyBind>("onlypoisonn");
            var etarget = E.GetTarget();
            if (etarget == null) return;

            if (poison.Active && !etarget.HasBuffOfType(BuffType.Poison))
            {
                return;
            }

            if (E.IsReady() && useE.Enabled)
            {
                if (etarget != null && etarget.IsValidTarget())
                {
                    E.Cast(etarget);
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var Hp = Config["Rsettings"].GetValue<MenuSlider>("HP");

            if (R.IsReady() && useR.Enabled)
            {
                var Rtarget = R.GetTarget();
                if (Rtarget != null && Rtarget.IsValidTarget())
                {
                    if (Rtarget.HealthPercent <= Hp.Value)
                    {
                        if (Rtarget.IsFacing(Me))
                        {
                            R.Cast(Rtarget);
                        }
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
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
                                        var pred = Q.GetPrediction(qtarget);
                                        if (pred.Hitchance >= HitChance.High)
                                        {
                                            Q.Cast(pred.CastPosition);
                                        }

                                        break;
                                    
                                    case 1:
                                        {
                                            Q.CastCircle(qtarget);
                                        }

                                        break;
                                    
                                    case 2:
                                        var qpreddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width, Q.Delay, Q.Speed,
                                            Q.Range, false, SpellType.Circle);
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
                            if (!etarget.HasBuffOfType(BuffType.Poison))
                            {
                                if (etarget.Health + etarget.AllShield + etarget.HPRegenRate <= EDamage(etarget))
                                {
                                    E.Cast(etarget);
                                }
                            }
                            else if (etarget.HasBuffOfType(BuffType.Poison))
                            {
                                if (etarget.Health + etarget.AllShield + etarget.HPRegenRate <= EPoisonDamage(etarget))
                                {
                                    E.Cast(etarget);
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
                    var Qfarm = Q.GetCircularFarmLocation(minions);
                    if (Qfarm.Position.IsValid())
                    {
                        Q.Cast(Qfarm.Position);
                    }
                }
            }            
        }

        private static void LastHit()
        {
            var lhe = Config["Clear"].GetValue<MenuBool>("LhE");

            if (lhe.Enabled && E.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion()).OrderBy(a => a.Health).FirstOrDefault();
                if (minions == null)
                {
                    return;
                }

                if (!minions.HasBuffOfType(BuffType.Poison))
                {
                    if (minions.Health <= EDamage(minions))
                    {
                        E.Cast(minions);
                    }
                }

                if (minions.HasBuffOfType(BuffType.Poison))
                {
                    if (minions.Health <= EPoisonDamage(minions))
                    {
                        E.Cast(minions);
                    }
                }
            }
        }
        
        private static void Jungleclear()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range) E.Cast(mob);
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

        private static void Gap(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (sender.IsMe || sender.IsAlly)
            {
                return;
            }

            if (Config["Misc"].GetValue<MenuBool>("AG").Enabled && W.IsReady())
            {
                if (args.EndPosition.DistanceToPlayer() <= 400)
                {
                    W.Cast(args.EndPosition);
                }
            }

            if (Config["Misc"].GetValue<MenuBool>("AG").Enabled && R.IsReady())
            {
                if (args.EndPosition.DistanceToPlayer() <= R.Range)
                {
                    if (sender.IsFacing(Me))
                    {
                        R.Cast(sender);
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
                    if (Me.Distance(sender.ServerPosition) < R.Range)
                    {
                        if (sender.IsFacing(Me))
                        {
                            R.Cast(sender);
                        }
                    }
                }
            }
        }

        private static readonly float[] QBaseDamage = { 0f, 75f, 110f, 145f, 180f, 215f, 215f };
        private static readonly float[] EBaseDamage = { 0f, 52f, 56f, 60f, 64f, 68f, 72f, 76f, 80f, 84f, 88f, 92f, 96f, 100f, 104f, 108f, 112f, 116f, 120f, 120f };
        private static readonly float[] EPoisonDamageee = { 0f, 20f, 40f, 60f, 80f, 100f, 100f };

        private static float QDamage(AIBaseClient target)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] + .9f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, qBaseDamage);
        }

        private static float EDamage(AIBaseClient target)
        {
            var eLevel = Me.Level;
            var eBaseDamage = EBaseDamage[eLevel] + .10f * Me.TotalMagicalDamage;
            return (float)Me.CalculateDamage(target, DamageType.Magical, eBaseDamage);
        }

        private static float EPoisonDamage(AIBaseClient target)
        {
            var eLevel = Me.Level;
            var e2Level = E.Level;
            var EPoisonDamagee = EBaseDamage[eLevel] + (.10f * Me.TotalMagicalDamage + EPoisonDamageee[e2Level] + .6f * Me.TotalMagicalDamage);
            return (float)Me.CalculateDamage(target, DamageType.Magical, EPoisonDamagee);
        }

        private static void OnBeforeAA(object sender, BeforeAttackEventArgs args)
        {
            var disable = Config["Misc"].GetValue<MenuKeyBind>("DisableAA");
            var level = Config["Misc"].GetValue<MenuSlider>("level");
            var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);

            if (target != null)
            {
                if (!disable.Active || Me.GetAutoAttackDamage(target) * 2 >= target.Health)
                {
                    return;
                }
            }

            if (Me.Level >= level.Value && Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                args.Process = false;
            }
        }
    }
}