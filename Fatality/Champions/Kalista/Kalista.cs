using System;
using System.Linq;
using System.Media;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using SharpDX;
using SPredictionMash1;

namespace Fatality.Champions.Kalista
{
    public class Kalista
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuE, menuP, menuK, menuD, menuM;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Kalista")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1200f);
            Q.SetSkillshot(0.25f, 40f, 2400f, true, SpellType.Line);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 1000f);
            R = new Spell(SpellSlot.R, 1050f);

            Config = new Menu("Kalista", "[Fatality] Kalista", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Kalista));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo"));
            Config.Add(menuQ);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo if Target is Killable"));
            Config.Add(menuE);

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
            
            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuBool("exploit", "Fly Exploit (Needs At Least 2 Attack Speed)", true)).AddPermashow();
            Config.Add(menuM);
            
            menuD = new Menu("Draw", "Draw Settings");
            menuD.Add(new MenuSeparator("qq", "Q Draw Settings"));
            menuD.Add(new MenuBool("drawQ", "Draw Q Range", true));
            menuD.Add(new MenuColor("colorQ", "Change Q Draw Color", Color.White));
            menuD.Add(new MenuSeparator("ee", "E Draw Settings"));
            menuD.Add(new MenuBool("drawE", "Draw E Range", true));
            menuD.Add(new MenuBool("drawED", "Draw E Damage"));
            menuD.Add(new MenuColor("colorE", "Change E Draw Color", Color.Green));
            menuD.Add(new MenuSeparator("rr", "R Draw Settings"));
            menuD.Add(new MenuBool("drawR", "Draw R Range", true));
            menuD.Add(new MenuColor("colorR", "Change R Draw Color", Color.Red));
            menuD.Add(new MenuSeparator("aa", "AA Tracker"));
            menuD.Add(new MenuBool("drawAA", "Draw AA Tracker"));
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
            Exploit();           
        }

        private static void OnUpdate(EventArgs args)
        {
            Killsteal();
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicQ();
                LogicE();
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
                var Qtarget = Q.GetTarget();
                if (Qtarget != null && Qtarget.IsValidTarget())
                {
                    switch (comb(menuP, "Pred"))
                    {
                        case 0:
                            var qpred = Q.GetPrediction(Qtarget);
                            if (qpred.Hitchance >= hitchance)
                            {
                                Q.Cast(qpred.CastPosition);
                            }

                            break;
                        
                        case 1:
                            {
                                Q.CastLine(Qtarget, 0f, 1f, false);
                            }

                            break;
                        
                        case 2:
                            var qpreddd = SPredictionMash1.Prediction.GetPrediction(Qtarget, Q.Width, Q.Delay, Q.Speed,
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

        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");

            if (E.IsReady() && useE.Enabled)
            {
                foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget() && !x.IsDead && x.IsEnemy && x.HasBuff("kalistaexpungemarker")))
                {
                    if (target != null)
                    {
                        if (GetEDamage(target) >= target.Health + target.AllShield + target.HPRegenRate)
                        {
                            E.Cast();
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
                                        var qpred = Q.GetPrediction(qtarget);
                                        if (qpred.Hitchance >= HitChance.High)
                                        {
                                            Q.Cast(qpred.CastPosition);
                                        }

                                        break;
                                    
                                    case 1:
                                        {
                                            Q.CastLine(qtarget, 0f, 1f, false);
                                        }

                                        break;
                                    
                                    case 2:
                                        var qpreddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width,
                                            Q.Delay, Q.Speed, Q.Range, true, SpellType.Line);
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
                         !hero.HasBuffOfType(BuffType.Invulnerability) && hero.HasBuff("kalistaexpungemarker")))
            {
                if (E.IsReady() && ksE)
                {
                    if (etarget != null)
                    {
                        if (etarget.DistanceToPlayer() <= E.Range)
                        {
                            if (etarget.Health + etarget.AllShield + etarget.HPRegenRate <= GetEDamage(etarget))
                            {
                                E.Cast();
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
                var qcolor = Config["Draw"].GetValue<MenuColor>("colorQ").Color;
                CircleRender.Draw(Me.Position, Q.Range, qcolor, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled)
            {
                var ecolor = Config["Draw"].GetValue<MenuColor>("colorE").Color;
                CircleRender.Draw(Me.Position, E.Range, ecolor, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawED").Enabled)
            {            
                foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsVisible && !x.IsDead && x.HasBuff("kalistaexpungemarker") && x.IsVisibleOnScreen))
                {
                    float getTotaldamage = edamage(target);
                    float tdamage = getTotaldamage * 100 / target.Health;
                    int totaldamage = (int)Math.Ceiling(tdamage);
                    FatalityRenderRing.DrawText2(string.Format("{0}%", totaldamage), Drawing.WorldToScreen(target.Position - Drawing.Height * .03f), Color.White);
                }
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled)
            {
                var rcolor = Config["Draw"].GetValue<MenuColor>("colorR").Color;
                CircleRender.Draw(Me.Position, R.Range, rcolor, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawAA").Enabled)
            {
                FatalityRenderRing.AALeft();
            }
        }

        private static void Exploit()
        {
            var expo = Config["Misc"].GetValue<MenuBool>("exploit");

            if (!expo.Enabled)
            {
                return;
            }
            
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && Me.AttackSpeedMod > 2.0)
            {
                var target = Orbwalker.GetTarget();
                if (target != null)
                {
                    if (Variables.GameTimeTickCount >= Orbwalker.LastAutoAttackTick)
                    {
                        Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                    }

                    if (Variables.GameTimeTickCount > Orbwalker.LastAutoAttackTick + Me.AttackDelay * 1000)
                    {
                        Me.IssueOrder(GameObjectOrder.AttackUnit, target);
                    }
                }
            }
        }

        private static int edamage(AIBaseClient target)
        {
            return (int)(target.Health + target.AllShield - GetEDamage(target));
        }

        private static readonly float[] QBaseDamage = { 0f, 20f, 85f, 150f, 215f, 280f, 280f };
        private static readonly float[] EBaseDamage = {0, 20, 30, 40, 50, 60, 60};
        private static readonly float[] EStackBaseDamage = {0, 10, 16, 22, 28, 34, 34};
        private static readonly float[] EStackMultiplierDamage = {0, .232f, .2755f, .319f, .3625f, .406f};

        private static float QDamage(AIBaseClient target)
        {
            var qlevel = Q.Level;
            var qBaseDamage = QBaseDamage[qlevel] + 1f * GameObjects.Player.TotalAttackDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, qBaseDamage);
        }
        
        private static float GetEDamage(AIBaseClient target)
        {
            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel] + .7 * GameObjects.Player.TotalAttackDamage;
            var eStackDamage = EStackBaseDamage[eLevel] +
                               EStackMultiplierDamage[eLevel] * GameObjects.Player.TotalAttackDamage;
            var eStacksOnTarget = target.GetBuffCount("kalistaexpungemarker");
            if (eStacksOnTarget == 0)
            {
                return 0;
            }

            var total = eBaseDamage + eStackDamage * (eStacksOnTarget - 1);
            if (target is AIMinionClient minion && (minion.GetJungleType() & JungleType.Legendary) != 0)
            {
                total /= 2;
            }

            return (float) GameObjects.Player.CalculateDamage(target, DamageType.Physical, total);
        }
    }
}