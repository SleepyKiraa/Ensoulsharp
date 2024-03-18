using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using SharpDX;
using SPredictionMash1;

namespace Fatality.Champions.Sylas
{
    public class Sylas
    {
        private static Spell Q, W, E, E2, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuP, menuL, menuK, menuD;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static int colorindex = 0;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Sylas")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 775f);
            Q.SetSkillshot(0.6f, 90f, float.MaxValue, false, SpellType.Circle);

            W = new Spell(SpellSlot.W, 400f);
            W.SetTargetted(0f, float.MaxValue);

            E = new Spell(SpellSlot.E, 400f);

            E2 = new Spell(SpellSlot.E, 800f);
            E2.SetSkillshot(0.4f, 50f, 1600f, true, SpellType.Line);

            R = new Spell(SpellSlot.R, 950f);
            R.SetTargetted(0.25f, 2200);

            Config = new Menu("Sylas", "[Fatality] Sylas", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Sylas));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo", true));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo", true));
            menuW.Add(new MenuSlider("WHP", "Your HP % to use W", 50, 0, 100));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E1 in Combo",true));
            menuE.Add(new MenuBool("useE2", "use E2 in Combo",true));
            menuE.Add(new MenuList("EMode", "E Mode",
                new string[] { "Target", "Mouse" }, 0));
            menuE.Add(new MenuBool("ESafe", "Enable E Mouse Safe Mode"));
            menuE.Add(new MenuSlider("Eenemys", "Max Enemys in Cursor Range", 2, 1, 5));
            menuE.Add(new MenuSlider("EenemysRange", "Cursor Enemy Check Range", 300, 100, 1000));
            menuE.Add(new MenuSlider("HP", "Target HP % To use E", 50, 1, 100));
            menuE.Add(new MenuKeyBind("turret", "Enable E under Turret", Keys.T, KeyBindType.Toggle)).AddPermashow();
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo", true));
            menuR.Add(new MenuSeparator("Rlist", "R Whitelist"));
            foreach (var ene in GameObjects.EnemyHeroes)
            {
                menuR.Add(new MenuBool(ene.CharacterName.ToLower(), "Use R on " + ene.CharacterName));
            }
            Config.Add(menuR);

            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
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
            menuL.Add(new MenuBool("LcQ", "Use Q to Lane Clear", true));
            menuL.Add(new MenuSeparator("Jungleclear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
            menuL.Add(new MenuBool("JcW", "Use W to Jungle Clear", true));
            Config.Add(menuL);
            
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
            menuD.Add(new MenuColor("colorR", "Change R Draw Color", Color.Red));
            menuD.Add(new MenuSeparator("misc", "Misc Draw Settings"));
            menuD.Add(new MenuBool("drawBuff", "Draw R Buff Time", true));
            menuD.Add(new MenuBool("drawCursor", "Draw Cursor Check Range"));
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
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void Updatetick(EventArgs args)
        {
            Killsteal();
            
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicW();
                LogicE();
                LogicQ();
                LogicR();
            }
        }

        private static void OnGameUpdate(EventArgs args)
        {
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
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (Q.IsReady() && useQ.Enabled)
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
            var hp = Config["Wsettings"].GetValue<MenuSlider>("WHP");

            if (W.IsReady() && useW.Enabled)
            {
                var wtarget = W.GetTarget();
                if (wtarget != null && wtarget.IsValidTarget())
                {
                    if (Me.HealthPercent <= hp.Value)
                    {
                        W.Cast(wtarget);
                    }
                }
            }
        }

        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            var useE2 = Config["Esettings"].GetValue<MenuBool>("useE2");
            var SafeMode = Config["Esettings"].GetValue<MenuBool>("ESafe");
            var Eenemys = Config["Esettings"].GetValue<MenuSlider>("Eenemys");
            var Eenemyrange = Config["Esettings"].GetValue<MenuSlider>("EenemysRange");
            var HP = Config["Esettings"].GetValue<MenuSlider>("HP");
            var turret = Config["Esettings"].GetValue<MenuKeyBind>("turret");
            var E2target = E2.GetTarget();
            var eingange = TargetSelector.GetTarget(E.Range + E2.Range, DamageType.Magical);

            if (E2target != null && E2target.IsValidTarget() && !turret.Active)
            {
                if (E2target.IsUnderEnemyTurret())
                {
                    return;
                }
            }

            if (eingange != null && eingange.IsValidTarget())
            {
                if (eingange.HealthPercent > HP.Value)
                {
                    return;
                }
            }

            if (useE.Enabled && E.IsReady())
            {
                if (E.Name == "SylasE")
                {
                    var Etarget = TargetSelector.GetTarget(E.Range + E2.Range, DamageType.Magical);
                    if (Etarget != null && Etarget.IsValidTarget())
                    {
                        switch (comb(menuE, "EMode"))
                        {
                            case 0:
                                E.Cast(Etarget.ServerPosition);
                                break;
                            
                            case 1:
                                if (SafeMode.Enabled)
                                {
                                    if (Game.CursorPos.CountEnemyHeroesInRange(Eenemyrange.Value) <= Eenemys.Value)
                                    {
                                        E.Cast(Game.CursorPos);
                                    }
                                }
                                else if (!SafeMode.Enabled)
                                {
                                    E.Cast(Game.CursorPos);
                                }

                                break;
                        }
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

            if (useE2.Enabled && E.IsReady())
            {
                if (E.Name == "SylasE2")
                {
                    if (E2target != null && E2target.IsValidTarget())
                    {
                        switch (comb(menuP, "Pred"))
                        {
                            case 0:
                                var E2pred = E2.GetPrediction(E2target);
                                if (E2pred.Hitchance >= hitchance)
                                {
                                    E2.Cast(E2pred.CastPosition);
                                }

                                break;

                            case 1:
                            {
                                E2.CastLine(E2target);
                            }
                                break;
                            
                            case 2:
                                var E2predd = E2.GetSPrediction(E2target);
                                if (E2predd.HitChance >= hitchance)
                                {
                                    E2.Cast(E2predd.CastPosition);
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
            
            if (useR.Enabled && R.IsReady())
            {
                foreach (var Enemys in GameObjects.EnemyHeroes.Where(x => x.IsEnemy && !x.IsDead && !x.HasBuff("SylasR") && x.InRange(R.Range)))
                {
                    if (Config["Rsettings"].GetValue<MenuBool>(Enemys.CharacterName.ToLower()).Enabled)
                    {
                        R.Cast(Enemys);
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;
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
                                        Q.CastCircle(Qtarget);
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
                         hero.IsValidTarget(Q.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksW && W.IsReady())
                {
                    if (Wtarget != null)
                    {
                        if (Wtarget.DistanceToPlayer() <= W.Range)
                        {
                            if (Wtarget.Health + Wtarget.AllShield + Wtarget.HPRegenRate <= WDamage(Wtarget))
                            {
                                W.Cast(Wtarget);
                            }
                        }
                    }
                }
            }

            foreach (var Etarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(E.Range + E2.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksE && E.IsReady())
                {
                    if (Etarget != null)
                    {
                        if (Etarget.DistanceToPlayer() <= E.Range + E2.Range)
                        {
                            if (Etarget.Health + Etarget.AllShield + Etarget.HPRegenRate <= EDamage(Etarget))
                            {
                                if (E.Name == "SylasE")
                                {
                                    E.Cast(Etarget.ServerPosition);
                                }

                                if (E.Name == "SylasE2")
                                {
                                    switch (comb(menuP, "Pred"))
                                    {
                                        case 0:
                                            var E2pred = E2.GetPrediction(Etarget);
                                            if (E2pred.Hitchance >= HitChance.High)
                                            {
                                                E2.Cast(E2pred.CastPosition);
                                            }

                                            break;

                                        case 1:
                                        {
                                            E2.CastLine(Etarget);
                                        }
                                            break;
                            
                                        case 2:
                                            var E2predd = E2.GetSPrediction(Etarget);
                                            if (E2predd.HitChance >= HitChance.High)
                                            {
                                                E2.Cast(E2predd.CastPosition);
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
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast(mob);
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
                        CircleRender.Draw(Me.Position, E.Range + E2.Range, colorE, 2);
                    }
                    
                    if (Config["Draw"].GetValue<MenuBool>("drawCursor").Enabled && E.Level > 0)
                    {
                        var colorE = Config["Draw"].GetValue<MenuColor>("colorE").Color;
                        var Eenemyrange = Config["Esettings"].GetValue<MenuSlider>("EenemysRange");
                        CircleRender.Draw(Game.CursorPos, Eenemyrange.Value, colorE, 2);
                    }
                    
                    if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled && R.Level > 0)
                    {
                        var colorR = Config["Draw"].GetValue<MenuColor>("colorR").Color;
                        CircleRender.Draw(Me.Position, R.Range, colorR, 2);
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
                        CircleRender.Draw(Me.Position, E.Range + E2.Range, coloE[colorindex], 2);
                    }
                    
                    if (Config["Draw"].GetValue<MenuBool>("drawCursor").Enabled && E.Level > 0)
                    {
                        var colorE = Base.PlusRender.GetFullColorList(rainspeed.Value);
                        var Eenemyrange = Config["Esettings"].GetValue<MenuSlider>("EenemysRange");
                        CircleRender.Draw(Game.CursorPos, Eenemyrange.Value, colorE[colorindex], 2);
                    }
                    
                    if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled && R.Level > 0)
                    {
                        var color = Base.PlusRender.GetFullColorList(rainspeed.Value);
                        CircleRender.Draw(Me.Position, R.Range, color[colorindex], 2);
                        
                    }
                    break;
            }

            if (Config["Draw"].GetValue<MenuBool>("drawBuff").Enabled)
            {
                var buff1 = Me.GetBuff("SylasRBuff");

                if (buff1 != null)
                {
                    var timer = buff1.EndTime - Game.Time;
                    FatalityRenderRing.DrawText($"R Time: {timer:N1}", Drawing.Width * 0.43f, Drawing.Height * 0.57f, Color.Red);
                }
            }
        }

        private static readonly float[] QBaseDamage = { 0f, 110f, 185f, 260f, 335f, 410f, 410f };
        private static readonly float[] WBaseDamage = { 0f, 70f, 105f, 140f, 175f, 210f, 210f };
        private static readonly float[] EBaseDamage = { 0f, 80f, 130f, 180f, 230f, 280f, 280f };

        private static float QDamage(AIBaseClient target)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] + 1.3f * Me.TotalMagicalDamage;
            return (float)Me.CalculateDamage(target, DamageType.Magical, qBaseDamage);
        }

        private static float WDamage(AIBaseClient target)
        {
            var wLevel = W.Level;
            var wBaseDamage = WBaseDamage[wLevel] + .9f * Me.TotalMagicalDamage;
            return (float)Me.CalculateDamage(target, DamageType.Magical, wBaseDamage);
        }

        private static float EDamage(AIBaseClient target)
        {
            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel] + 1f * Me.TotalMagicalDamage;
            return (float)Me.CalculateDamage(target, DamageType.Magical, eBaseDamage);
        }
    }
}