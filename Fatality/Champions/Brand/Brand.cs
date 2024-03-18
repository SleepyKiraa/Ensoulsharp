using System;
using System.Linq;
using System.Windows.Controls;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using SharpDX;
using SPredictionMash1;
using Menu = EnsoulSharp.SDK.MenuUI.Menu;

namespace Fatality.Champions.Brand
{
    public class Brand
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuP, menuK, menuD, menuM, menuL;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static int colorindex = 0;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Brand")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1100f);
            Q.SetSkillshot(0.35f, 60f, 1600f, true, SpellType.Line);

            W = new Spell(SpellSlot.W, 900f);
            W.SetSkillshot(0.7f, 130f, float.MaxValue, false, SpellType.Circle);

            E = new Spell(SpellSlot.E, 675f);

            R = new Spell(SpellSlot.R, 750f);

            Config = new Menu("Brand", "[Fatality] Brand", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Brand));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo"));
            menuQ.Add(new MenuKeyBind("cc", "Only Q When Target Have Passive", Keys.G, KeyBindType.Toggle)).AddPermashow();
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo"));
            menuW.Add(new MenuBool("autoW", "Auto W on Dashing Targets"));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo"));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo"));
            menuR.Add(new MenuSlider("HP", "HP % To use R", 50, 1, 100));
            menuR.Add(new MenuSlider("bounce", "Min Enemys in Target Range to Bounce", 1, 1, 5));
            menuR.Add(new MenuSlider("bouncerange", "Bounce Check Range", 600, 100, 600));
            menuR.Add(new MenuKeyBind("SemiRR", "Semi R", Keys.T, KeyBindType.Press)).AddPermashow();
            Config.Add(menuR);
            
            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("WPred", "W Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal", true));
            menuK.Add(new MenuBool("KsW", "Enable W Killsteal", true));
            menuK.Add(new MenuBool("KsE", "Enable E Killsteal", true));
            menuK.Add(new MenuBool("KsR", "Enable R Killsteal", true));
            Config.Add(menuK);
            
            menuL = new Menu("Clear", "Clear Settings");
            menuL.Add(new MenuSeparator("LaneClear", "Lane Clear"));
            menuL.Add(new MenuBool("LcW", "Use W to Lane Clear", true));
            menuL.Add(new MenuSlider("LcWM", "W Minions Hit", 3, 1, 5));
            menuL.Add(new MenuSeparator("JungleClear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
            menuL.Add(new MenuBool("JcW", "Use W to Jungle Clear", true));
            menuL.Add(new MenuBool("JcE", "Use E to Jungle Clear", true));
            Config.Add(menuL);
            
            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuBool("AG", "AntiGapcloser", true));
            menuM.Add(new MenuBool("Int", "Interrupter", true));
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
            menuD.Add(new MenuBool("drawBounce", "Draw Bounce Range"));
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

            Game.OnUpdate += UpdateTick;
            GameEvent.OnGameTick += OnUpdate;
            AntiGapcloser.OnGapcloser += Gap;
            Interrupter.OnInterrupterSpell += Int;
            Render.OnDraw += OnDraw;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void UpdateTick(EventArgs args)
        {
            if (Config["Wsettings"].GetValue<MenuBool>("autoW").Enabled)
            {
                if (W.IsReady())
                {
                    foreach (var targets in GameObjects.EnemyHeroes.Where(x => x.IsEnemy && x.IsValidTarget(W.Range)))
                    {
                        W.CastIfHitchanceEquals(targets, HitChance.Dash);
                    }
                }
            }
            
            Killsteal();

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE();
            }
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
                Laneclear();
                Jungleclear();
            }

            if (Config["Rsettings"].GetValue<MenuKeyBind>("SemiRR").Active)
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                SemiR();
            }
        }

        private static void LogicQ()
        {
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");
            var qbuff = Config["Qsettings"].GetValue<MenuKeyBind>("cc");
            var qtarget = Q.GetTarget();

            if (qtarget != null && qbuff.Active)
            {
                if (!qtarget.HasBuff("BrandAblaze"))
                {
                    return;
                }
            }
            
            switch (comb(menuP, "QPred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (Q.IsReady() && useQ.Enabled)
            {
                if (qtarget != null)
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
                            Q.CastLine(qtarget);
                        }
                            break;
                        
                        case 2:
                            var qpredd = Q.GetSPrediction(qtarget);
                            if (qpredd.HitChance >= hitchance)
                            {
                                Q.Cast(qpredd.CastPosition);
                            }

                            break;
                    }
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
                var wtarget = W.GetTarget();
                if (wtarget != null && wtarget.IsValidTarget())
                {
                    switch (comb(menuP, "Pred"))
                    {
                        case 0:
                            var wpred = W.GetPrediction(wtarget, true);
                            if (wpred.Hitchance >= hitchance)
                            {
                                W.Cast(wpred.CastPosition);
                            }

                            break;

                        case 1:
                        {
                            W.CastCircle(wtarget);
                        }
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

        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");

            if (E.IsReady() && useE.Enabled)
            {
                var etarget = E.GetTarget();
                if (etarget != null && etarget.IsValidTarget())
                {
                    E.CastOnUnit(etarget);
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var hp = Config["Rsettings"].GetValue<MenuSlider>("HP");
            var bouncetargets = Config["Rsettings"].GetValue<MenuSlider>("bounce");
            var bounceRange = Config["Rsettings"].GetValue<MenuSlider>("bouncerange");

            if (R.IsReady() && useR.Enabled)
            {
                var rtarget = R.GetTarget();
                if (rtarget != null && rtarget.IsValidTarget())
                {
                    if (rtarget.HealthPercent <= hp.Value)
                    {
                        if (rtarget.CountEnemyHeroesInRange(bounceRange.Value) >= bouncetargets.Value)
                        {
                            R.CastOnUnit(rtarget);
                        }
                    }
                }
            }
        }

        private static void SemiR()
        {
            if (R.IsReady())
            {
                var rtarget = R.GetTarget();
                if (rtarget != null && rtarget.IsValidTarget())
                {
                    R.Cast(rtarget);
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;
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
                            if (Qtarget.Health + Qtarget.AllShield + Qtarget.HPRegenRate <= QDamage(Qtarget))
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0:
                                        var qpred = Q.GetPrediction(Qtarget);
                                        if (qpred.Hitchance >= HitChance.High)
                                        {
                                            Q.Cast(qpred.CastPosition);
                                        }

                                        break;

                                    case 1:
                                    {
                                        Q.CastLine(Qtarget);
                                    }
                                        break;

                                    case 2:
                                        var qpredd = Q.GetSPrediction(Qtarget);
                                        if (qpredd.HitChance >= HitChance.High)
                                        {
                                            Q.Cast(qpredd.CastPosition);
                                        }

                                        break;
                                }
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
                            if (!Wtarget.HasBuff("BrandAblaze"))
                            {
                                if (Wtarget.Health + Wtarget.AllShield + Wtarget.HPRegenRate <= WDamage(Wtarget))
                                {
                                    switch (comb(menuP, "Pred"))
                                    {
                                        case 0:
                                            var wpred = W.GetPrediction(Wtarget, true);
                                            if (wpred.Hitchance >= HitChance.High)
                                            {
                                                W.Cast(wpred.CastPosition);
                                            }

                                            break;

                                        case 1:
                                        {
                                            W.CastCircle(Wtarget);
                                        }
                                            break;
                        
                                        case 2:
                                            var wpredd = W.GetSPrediction(Wtarget);
                                            if (wpredd.HitChance >= HitChance.High)
                                            {
                                                W.Cast(wpredd.CastPosition);
                                            }

                                            break;
                                    }
                                }
                            }
                            else if (Wtarget.HasBuff("BrandAblaze"))
                            {
                                if (Wtarget.Health + Wtarget.AllShield + Wtarget.HPRegenRate <= WDamagePassive(Wtarget))
                                {
                                    switch (comb(menuP, "Pred"))
                                    {
                                        case 0:
                                            var wpred = W.GetPrediction(Wtarget, true);
                                            if (wpred.Hitchance >= HitChance.High)
                                            {
                                                W.Cast(wpred.CastPosition);
                                            }

                                            break;

                                        case 1:
                                        {
                                            W.CastCircle(Wtarget);
                                        }
                                            break;
                        
                                        case 2:
                                            var wpredd = W.GetSPrediction(Wtarget);
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
                            if (Etarget.Health + Etarget.AllShield + Etarget.HPRegenRate <= EDamage(Etarget))
                            {
                                E.Cast(Etarget);
                            }
                        }
                    }
                }
            }

            foreach (var Rtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(R.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksR && R.IsReady())
                {
                    if (Rtarget != null)
                    {
                        if (Rtarget.DistanceToPlayer() <= R.Range)
                        {
                            if (!Q.IsReady() && !W.IsReady() && !E.IsReady() ||
                                Rtarget.Health > Me.GetAutoAttackDamage(Rtarget))
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
        }
        
        private static void Laneclear()
        {
            var lcW = Config["Clear"].GetValue<MenuBool>("LcW");
            var lcWc = Config["Clear"].GetValue<MenuSlider>("LcWM");

            if (lcW.Enabled && W.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var Wfarm = W.GetCircularFarmLocation(minions);
                    if (Wfarm.Position.IsValid() && Wfarm.MinionsHit >= lcWc.Value)
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
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast(mob.Position);
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range) E.Cast(mob);
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
                    
                    if (Config["Draw"].GetValue<MenuBool>("drawW").Enabled && W.Level > 0)
                    {
                        var colorW = Config["Draw"].GetValue<MenuColor>("colorW").Color;
                        CircleRender.Draw(Me.Position, W.Range, colorW, 2);
                    }
                    
                    if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled && E.Level > 0)
                    {
                        var colorE = Config["Draw"].GetValue<MenuColor>("colorE").Color;
                        CircleRender.Draw(Me.Position, E.Range, colorE, 2);
                    }
                    
                    if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled && R.Level > 0)
                    {
                        var colorR = Config["Draw"].GetValue<MenuColor>("colorR").Color;
                        CircleRender.Draw(Me.Position, R.Range, colorR, 2);
                    }
                    
                    if (Config["Draw"].GetValue<MenuBool>("drawBounce").Enabled && R.Level > 0)
                    {
                        var color = Config["Draw"].GetValue<MenuColor>("colorR").Color;
                        var range = Config["Rsettings"].GetValue<MenuSlider>("bouncerange");
                        var rtarget = TargetSelector.GetTarget(R.Range * 2, DamageType.Magical);

                        if (rtarget != null && rtarget.IsValidTarget())
                        {
                            CircleRender.Draw(rtarget.Position, range.Value, color, 2);
                        }
                    }

                    break;
                
                case 1:
                    if (Config["Draw"].GetValue<MenuBool>("drawQ").Enabled && Q.Level > 0)
                    {
                        var colorQ = Base.PlusRender.GetFullColorList(rainspeed.Value);
                        CircleRender.Draw(Me.Position, Q.Range, colorQ[colorindex], 2);
                    }
                    
                    if (Config["Draw"].GetValue<MenuBool>("drawW").Enabled && W.Level > 0)
                    {
                        var colorW = Base.PlusRender.GetFullColorList(rainspeed.Value);
                        CircleRender.Draw(Me.Position, W.Range, colorW[colorindex], 2);
                    }
                    
                    if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled && E.Level > 0)
                    {
                        var coloE = Base.PlusRender.GetFullColorList(rainspeed.Value);
                        CircleRender.Draw(Me.Position, E.Range, coloE[colorindex], 2);
                    }
                    
                    if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled && R.Level > 0)
                    {
                        var color = Base.PlusRender.GetFullColorList(rainspeed.Value);
                        CircleRender.Draw(Me.Position, R.Range, color[colorindex], 2);
                        
                    }
                    
                    if (Config["Draw"].GetValue<MenuBool>("drawBounce").Enabled && R.Level > 0)
                    {
                        var color = Base.PlusRender.GetFullColorList(rainspeed.Value);
                        var range = Config["Rsettings"].GetValue<MenuSlider>("bouncerange");
                        var rtarget = TargetSelector.GetTarget(R.Range * 2, DamageType.Magical);

                        if (rtarget != null && rtarget.IsValidTarget())
                        {
                            CircleRender.Draw(rtarget.Position, range.Value, color[colorindex], 2);
                        }
                    }

                    break;
            }
        }

        private static readonly float[] QBaseDamage = { 0f, 80f, 110f, 140f, 170f, 200f, 200f };
        private static readonly float[] WBaseDamage = { 0f, 75f, 120f, 165f, 210f, 255f, 255f };
        private static readonly float[] WPassiveDamage = { 0f, 93.75f, 150f, 206.25f, 262.5f, 262.5f };
        private static readonly float[] EBaseDamage = { 0f, 70f, 95f, 120f, 145f, 170f, 170f };
        private static readonly float[] RBaseDamage = { 0f, 100f, 200f, 300f, 300f };

        private static float QDamage(AIBaseClient target)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] + .55f * Me.TotalMagicalDamage;
            return (float)Me.CalculateDamage(target, DamageType.Magical, qBaseDamage);
        }

        private static float WDamage(AIBaseClient target)
        {
            var wLevel = W.Level;
            var wBaseDamage = WBaseDamage[wLevel] + .6f * Me.TotalMagicalDamage;
            return (float)Me.CalculateDamage(target, DamageType.Magical, wBaseDamage);
        }

        private static float WDamagePassive(AIBaseClient target)
        {
            var wLevel = W.Level;
            var wBaseDamagePassive = WPassiveDamage[wLevel] + .75f * Me.TotalMagicalDamage;
            return (float)Me.CalculateDamage(target, DamageType.Magical, wBaseDamagePassive);
        }

        private static float EDamage(AIBaseClient target)
        {
            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel] + .45f * Me.TotalMagicalDamage;
            return (float)Me.CalculateDamage(target, DamageType.Magical, eBaseDamage);
        }

        private static float RDamage(AIBaseClient target)
        {
            var rlevel = R.Level;
            var rBaseDamage = RBaseDamage[rlevel] + .25f * Me.TotalMagicalDamage;
            return (float)Me.CalculateDamage(target, DamageType.Magical, rBaseDamage);
        }

        private static void Gap(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("AG").Enabled)
            {
                if (Q.IsReady() && sender.HasBuff("BrandAblaze"))
                {
                    if (sender.IsValid && sender.IsEnemy)
                    {
                        if (args.EndPosition.DistanceToPlayer() < 350)
                        {
                            Q.CastIfHitchanceEquals(sender, HitChance.Dash);
                        }
                    }
                }
            }
        }
        
        private static void Int(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("Int").Enabled)
            {
                if (sender.IsEnemy && args.DangerLevel == Interrupter.DangerLevel.High && Q.IsReady() && sender.HasBuff("BrandAblaze"))
                {
                    if (Me.Distance(sender.ServerPosition) < Q.Range)
                    {
                        Q.Cast(sender);
                    }
                }
            }
        }
    }
}