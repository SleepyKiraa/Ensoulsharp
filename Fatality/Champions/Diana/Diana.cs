using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Properties;
using Fatality.Utils;
using SharpDX;
using SharpDX.Direct3D11;
using SPredictionMash1;

namespace Fatality.Champions.Diana
{
    public class Diana
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuP, menuK, menuL, menuD;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Diana")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 900f);
            Q.SetSkillshot(0.25f, 50f, 1900f, false, SpellType.Circle);
            W = new Spell(SpellSlot.W, 200f);
            E = new Spell(SpellSlot.E, 825f);
            R = new Spell(SpellSlot.R, 475f);

            Config = new Menu("Diana", "[Fatality] Diana", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Diana));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo"));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo"));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo"));
            menuE.Add(new MenuBool("qek", "Only use E if Q is Ready or Target is Killable"));
            menuE.Add(new MenuSlider("HP", "Your HP % To use E", 50, 1, 100));
            menuE.Add(new MenuKeyBind("buff", "Only use E on Marked Targets", Keys.G, KeyBindType.Toggle)).AddPermashow();
            menuE.Add(new MenuKeyBind("turret", "Enable E under Turret", Keys.T, KeyBindType.Toggle)).AddPermashow();
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo"));
            menuR.Add(new MenuSlider("ene", "Min Enemys in R Range", 2, 1, 5));
            Config.Add(menuR);
            
            menuP = new Menu("Pred", "Prediction Settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] {"SDK", "xcsoft", "Spred (Press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "Very High" }, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal"));
            menuK.Add(new MenuBool("KsW", "Enable W Killsteal"));
            menuK.Add(new MenuBool("KsE", "Enable E Killsteal"));
            Config.Add(menuK);
            
            menuL = new Menu("Clear", "Clear Settings");
            menuL.Add(new MenuBool("LcQ", "Use Q to Lane Clear", true));
            menuL.Add(new MenuSeparator("JungleClear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
            menuL.Add(new MenuBool("JcW", "Use W to Jungle Clear", true));
            menuL.Add(new MenuBool("JcE", "Use E to Jungle Clear", true));
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
            Game.OnUpdate += UpdateTick;
            Render.OnDraw += OnDraw;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void UpdateTick(EventArgs args)
        {
            Killsteal();
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicQ();
                LogicE();
                LogicR();
                LogicW();
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

            if (useQ.Enabled && Q.IsReady())
            {
                var qtarget = Q.GetTarget();
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

            if (useW.Enabled && W.IsReady())
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
            var buff = Config["Esettings"].GetValue<MenuKeyBind>("buff");
            var qek = Config["Esettings"].GetValue<MenuBool>("qek");
            var tur = Config["Esettings"].GetValue<MenuKeyBind>("turret");
            var hpvalue = Config["Esettings"].GetValue<MenuSlider>("HP");
            var Etarget = E.GetTarget();

            if (buff.Active && Etarget != null && !Etarget.HasBuff("dianamoonlight"))
            {
                return;
            }

            if (tur.Active)
            {
                if (Etarget != null && Etarget.IsUnderEnemyTurret())
                {
                    return;
                }
            }

            if (Me.HealthPercent < hpvalue.Value)
            {
                return;
            }

            if (useE.Enabled && E.IsReady())
            {
                if (Etarget != null && Etarget.IsValidTarget())
                {
                    if (!qek.Enabled)
                    {
                        E.Cast(Etarget);
                    }

                    if (qek.Enabled)
                    {
                        if (Q.IsReady() || EDamage(Etarget) + Me.GetAutoAttackDamage(Etarget) >= Etarget.Health)
                        {
                            E.Cast(Etarget);
                        }
                    }
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var ene = Config["Rsettings"].GetValue<MenuSlider>("ene");

            if (useR.Enabled && R.IsReady())
            {
                var Rtarget = R.GetTarget();
                if (Rtarget != null && Rtarget.IsValidTarget())
                {
                    if (Me.CountEnemyHeroesInRange(R.Range) >= ene.Value)
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
                                        var qpred = Q.GetPrediction(qtarget);
                                        if (qpred.Hitchance >= HitChance.High)
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
                                        var qpreddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width,
                                            Q.Delay, Q.Speed,
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
                            if (wtarget.Health + wtarget.AllShield + wtarget.HPRegenRate <= WDamage(wtarget) * 3)
                            {
                                W.Cast();
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
                            if (etarget.Health + etarget.AllShield + etarget.HPRegenRate <= EDamage(etarget))
                            {
                                E.Cast(etarget);
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
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast();
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

        private static readonly float[] QBaseDamage = { 0f, 60f, 95f, 130f, 165f, 200f, 200f };
        private static readonly float[] WBaseDamage = { 0f, 18f, 30f, 42f, 54f, 66f, 66f };
        private static readonly float[] EBaseDamage = { 0f, 50f, 70f, 90f, 110f, 130f, 130f };

        private static float QDamage(AIBaseClient target)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] + .7f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, qBaseDamage);
        }
        
        private static float WDamage(AIBaseClient target)
        {
            var wLevel = W.Level;
            var wBaseDamage = WBaseDamage[wLevel] + .15f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, wBaseDamage);
        }
        
        private static float EDamage(AIBaseClient target)
        {
            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel] + .5f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, eBaseDamage);
        }
    }
}