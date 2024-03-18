using System;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Media;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Damages.Spells;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using EnsoulSharp.SDK.Utility;
using Fatality.Utils;
using SPredictionMash1;
using Color = SharpDX.Color;
using Geometry = EnsoulSharp.SDK.Geometry;

namespace Fatality.Champions.Jhin
{
    public class Jhin
    {
        private static Spell Q, W, E, R;
        private static Menu Config, Combo, menuW, menuE, menuR, menuK, menuD, menuM, menuP, menuL;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static int colorindex = 0;
        private static Geometry.Sector LastRCone;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Jhin")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 550f);
            Q.SetTargetted(0.25f, 1800f);
            W = new Spell(SpellSlot.W, 2500f);
            W.SetSkillshot(0.8f, 50f, float.MaxValue, false, SpellType.Line);
            E = new Spell(SpellSlot.E, 750f);
            E.SetSkillshot(0.25f, 60f, float.MaxValue, false, SpellType.Circle);
            R = new Spell(SpellSlot.R, 3500f);
            R.SetSkillshot(1f, 80f, 5000f, false, SpellType.Line);

            Config = new Menu("Jhin", "[Fatality] Jhin", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Jhin));

            Combo = new Menu("Combo", "Combo Settings");
            Combo.Add(new MenuBool("useQ", "Enable Q", true));
            Combo.Add(new MenuBool("useW", "Enable W", true));
            menuW = new Menu("Wsettings", "W Config");
            menuW.Add(new MenuBool("marked", "Only use W when Target is Marked", true));
            menuW.Add(new MenuBool("waa", "Dont use W when target is in AA Range", true));
            Combo.Add(menuW);
            Combo.Add(new MenuBool("useE", "Enable Auto E", true));
            Combo.Add(new MenuBool("useR", "Enable R", true));
            Config.Add(Combo);

            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                    new string[] { "SDK", "xcsoft", "Spred (Press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("WPred", "W Hitchance",
                    new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("RPred", "R Hitchance",
                    new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal", true));
            menuK.Add(new MenuBool("KsW", "use W to Killsteal", true));
            Config.Add(menuK);
            
            menuL = new Menu("Clear", "Clear Settings");
            menuL.Add(new MenuSeparator("LaneClear", "Lane Clear"));
            menuL.Add(new MenuBool("LhQ", "Use Q to Last Hit", true));
            menuL.Add(new MenuBool("LcE", "Use E to Lane Clear", true));
            menuL.Add(new MenuSeparator("JungleClear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
            menuL.Add(new MenuBool("JcE", "Use E to Jungle Clear", true));
            Config.Add(menuL);
            
            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuBool("AG", "AntiGapcloser", true));
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
            menuD.Add(new MenuSeparator("misc", "Misc Draw Settings"));
            menuD.Add(new MenuBool("drawA", "Draw AA Tracker"));
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

            Game.OnUpdate += OnTick;
            GameEvent.OnGameTick += OnupDate;
            AntiGapcloser.OnGapcloser += Gap;
            AIBaseClient.OnDoCast += OnProcessSpellCast;
            Orbwalker.OnAfterAttack += OnAfterAA;
            Render.OnDraw += OnDraw;
            Render.OnEndScene += OnEndScene;
            Config.Attach();
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void OnTick(EventArgs args)
        {
            Killsteal();
            LogicR();
        }

        private static void OnupDate(EventArgs args)
        {
            if (R.Name == "JhinRShot")
            {
                Orbwalker.AttackEnabled = false;
                Orbwalker.MoveEnabled = false;
            }
            else
            {
                Orbwalker.AttackEnabled = true;
                Orbwalker.MoveEnabled = true;
            }
            
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicW();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LastHit)
            {
                LastHit();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Laneclear();
                Jungleclear();
            }
            
            AutoE();
        }

        private static void LogicQ()
        {
            var useQ = Config["Combo"].GetValue<MenuBool>("useQ");

            if (R.Name == "JhinRShot")
            {
                return;
            }

            if (Q.IsReady() && useQ.Enabled)
            {
                var qtarget = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                if (qtarget != null && qtarget.IsValidTarget())
                {
                    Q.Cast(qtarget);
                }
            }
        }

        private static void LogicW()
        {
            var useW = Config["Combo"].GetValue<MenuBool>("useW");
            var wtarget = TargetSelector.GetTarget(W.Range, DamageType.Physical);

            if (Combo["Wsettings"].GetValue<MenuBool>("waa").Enabled)
            {
                if (wtarget != null && wtarget.InRange(Me.GetRealAutoAttackRange()))
                {
                    return;
                }
            }

            if (Combo["Wsettings"].GetValue<MenuBool>("marked").Enabled)
            {
                if (wtarget != null && !wtarget.HasBuff("jhinespotteddebuff"))
                {
                    return;
                }
            }

            if (R.Name == "JhinRShot")
            {
                return;
            }
            
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
                if (wtarget != null)
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
                            W.CastLine(wtarget);
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

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;

            foreach (var qtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(Q.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (R.Name != "JhinRShot")
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
            }
            
            foreach (var wtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(W.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (R.Name != "JhinRShot")
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
                                            W.CastLine(wtarget);
                                            break;
                                        
                                        case 2:
                                            var wpredd = W.GetSPrediction(wtarget);
                                            if (wpredd.HitChance >= HitChance.High)
                                            {
                                                W.Cast(wpredd.CastPosition);
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

                    if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled && E.Level > 0)
                    {
                        var colorE = Config["Draw"].GetValue<MenuColor>("colorE").Color;
                        CircleRender.Draw(Me.Position, E.Range, colorE, 2);
                    }
                    
                    if (Config["Draw"].GetValue<MenuBool>("drawA").Enabled)
                    {
                        FatalityRenderRing.AALeft();
                    }

                    break;
                
                case 1:
                    if (Config["Draw"].GetValue<MenuBool>("drawQ").Enabled && Q.Level > 0)
                    {
                        var colorQ = Base.PlusRender.GetFullColorList(rainspeed.Value);
                        CircleRender.Draw(Me.Position, Q.Range, colorQ[colorindex], 2);
                    }
                    
                    
                    if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled && E.Level > 0)
                    {
                        var coloE = Base.PlusRender.GetFullColorList(rainspeed.Value);
                        CircleRender.Draw(Me.Position, E.Range, coloE[colorindex], 2);
                    }

                    if (Config["Draw"].GetValue<MenuBool>("drawA").Enabled)
                    {
                        FatalityRenderRing.AALeft();
                    }

                    break;
            }
        }

        private static void AutoE()
        {
            var useE = Config["Combo"].GetValue<MenuBool>("useE");

            if (E.IsReady() && useE.Enabled)
            {
                var etarget = TargetSelector.GetTarget(E.Range, DamageType.Physical);
                if (etarget != null && etarget.IsValidTarget())
                {
                    if (etarget.HasBuffOfType(BuffType.Snare) || etarget.HasBuffOfType(BuffType.Stun))
                    {
                        E.Cast(etarget);
                    }
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Combo"].GetValue<MenuBool>("useR");

            if (R.Name != "JhinRShot")
            {
                return;
            }

            if (R.IsReady() && useR.Enabled)
            {
                var rtarget = GameObjects.EnemyHeroes
                    .Where(x => x.IsEnemy && x.IsValidTarget() && LastRCone.IsInside(x)).ToList();
                var target = TargetSelector.GetTarget(rtarget, DamageType.Physical);
                if (target != null)
                {
                    var pred = R.GetSPrediction(target);
                    if (pred.HitChance >= HitChance.High)
                    {
                        R.Cast(pred.CastPosition);
                    }
                }
            }
        }

        private static void LastHit()
        {
            var lh = Config["Clear"].GetValue<MenuBool>("LhQ");

            if (Q.IsReady() && lh.Enabled)
            {
                foreach (var minion in GameObjects.EnemyMinions.Where(x => x.IsEnemy && x.InRange(Q.Range)))
                {
                    if (minion.Health <= QDamage(minion))
                    {
                        Q.Cast(minion);
                    }
                }
            }
        }
        
        private static void Laneclear()
        {
            var lcE = Config["Clear"].GetValue<MenuBool>("LcE");

            if (lcE.Enabled && E.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var Efarm = E.GetCircularFarmLocation(minions);
                    if (Efarm.Position.IsValid())
                    {
                        E.Cast(Efarm.Position);
                    }
                }
            }
        }
        
        private static void Jungleclear()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(W.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob);
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range) E.Cast(mob.Position);
            }
        }

        private static void OnEndScene(EventArgs args)
        {
            if (Config["Draw"].GetValue<MenuBool>("drawW").Enabled && W.Level > 0)
            {
                MiniMap.DrawCircle(Me.Position, W.Range, System.Drawing.Color.Blue);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled && R.Level > 0)
            {
                MiniMap.DrawCircle(Me.Position, R.Range, System.Drawing.Color.Red);
            }
        }

        private static void OnAfterAA(object sender, AfterAttackEventArgs args)
        {
            LogicQ();
        }

        private static readonly float[] QBaseDamage = { 0f, 45f, 70f, 95f, 120f, 145f, 145f };
        private static readonly float[] QBonusDamage = { 0f, .35f, .425f, .5f, .575f, .65f, .65f };
        private static readonly float[] WBaseDamage = { 0f, 60f, 95f, 130f, 165f, 200f, 200f };

        private static float QDamage(AIBaseClient target)
        {
            var qlevel = Q.Level;
            var qBaseDamage = QBaseDamage[qlevel] + (QBonusDamage[qlevel] * Me.GetBonusPhysicalDamage() + .6f * Me.TotalMagicalDamage);
            return (float)Me.CalculateDamage(target, DamageType.Physical, qBaseDamage);
        }

        private static float WDamage(AIBaseClient target)
        {
            var wlevel = W.Level;
            var wBaseDamage = WBaseDamage[wlevel] + .5f * Me.TotalAttackDamage;
            return (float)Me.CalculateDamage(target, DamageType.Physical, wBaseDamage);
        }

        private static void Gap(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("AG").Enabled)
            {
                if (W.IsReady() && sender != null && sender.HasBuff("jhinespotteddebuff"))
                {
                    if (sender.IsValid && sender.IsEnemy)
                    {
                        if (args.EndPosition.DistanceToPlayer() < 500)
                        {
                            W.CastIfHitchanceEquals(sender, HitChance.Dash);
                        }
                    }
                }
            }
        }

        private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                switch (args.Slot)
                {
                    case SpellSlot.R:
                        if (args.SData.Name == "JhinR")
                        {
                            LastRCone = new Geometry.Sector(Me.ServerPosition,
                                Me.ServerPosition.Extend(Game.CursorPos, R.Range), (float)(60f * Math.PI / 180f), R.Range);
                        }

                        break;
                }
            }
        }
    }
}