using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using SharpDX;
using SPredictionMash1;

namespace Fatality.Champions.Akali
{
    public class Akali
    {
        private static Spell Q, W, E, E2, R, R2;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuK, menuD, menuP;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Akali")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 500f);
            Q.SetSkillshot(0.25f, 100f, float.MaxValue, false, SpellType.Cone);
            W = new Spell(SpellSlot.W, 250f);
            E = new Spell(SpellSlot.E, 825f);
            E.SetSkillshot(0.4f, 80f, 1800f, true, SpellType.Line);
            E2 = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 675f);
            R2 = new Spell(SpellSlot.R, 800f);
            R2.SetSkillshot(0f, 60f, 3000f, false, SpellType.Line);

            Config = new Menu("Akali", "[Fatality] Akali", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Akali));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo", true));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo", true));
            menuW.Add(new MenuSlider("Mana", "Only use W if Energy is Under %", 50, 0, 100));
            menuW.Add(new MenuSlider("HP", "Only Use W if your HP is Under %", 50, 1, 100));
            menuW.Add(new MenuSlider("Enemys", "Use W If x Enemys Are Near", 2, 1, 5));
            menuW.Add(new MenuSlider("Scan", "Enemy Scan Range", 500, 100, 1000));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo", true));
            menuE.Add(new MenuBool("useE2", "Use E2 in Combo", true));
            menuE.Add(new MenuBool("qek", "Only use E2 when Q is Ready or Target is Killable"));
            menuE.Add(new MenuSlider("hp", "Target HP % to use E", 50, 1, 100));
            menuE.Add(new MenuSlider("range", "E2 Range", 1500, 500, 2500));
            menuE.Add(new MenuKeyBind("noturret", "Dont E2 when Target is under Turret", Keys.T, KeyBindType.Toggle))
                .AddPermashow();
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo", true));
            menuR.Add(new MenuBool("useR2", "use R2 in Combo", true));
            menuR.Add(new MenuSlider("hp", "Target HP % to use R", 50, 1, 100));
            menuR.Add(new MenuKeyBind("noturret", "Dont R if target is under Turret", Keys.G, KeyBindType.Toggle))
                .AddPermashow();
            Config.Add(menuR);

            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)" }, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("EPred", "E Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            menuP.Add(new MenuList("R2Pred", "R2 Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);

            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal", true));
            menuK.Add(new MenuBool("KsE1", "Enable E1 Killsteal", true));
            menuK.Add(new MenuBool("KsE2", "Enable E2 Killsteal", true));
            menuK.Add(new MenuBool("KsR1", "Enable R1 Killsteal", true));
            menuK.Add(new MenuBool("KsR2", "Enable R2 Killsteal", true));
            Config.Add(menuK);

            menuD = new Menu("Draw", "Draw Settings");
            menuD.Add(new MenuSeparator("ss", "Q Draw Settings"));
            menuD.Add(new MenuBool("drawQ", "Q Range  (White)", true));
            menuD.Add(new MenuColor("colorQ", "Q Draw Color", Color.White));
            menuD.Add(new MenuSeparator("sss", "E Draw Settings"));
            menuD.Add(new MenuBool("drawE", "E Range (Green)", true));
            menuD.Add(new MenuBool("drawE2", "E2 Range (Green)", true));
            menuD.Add(new MenuColor("colorE", "E Draw Color", Color.Green));
            menuD.Add(new MenuSeparator("ssss", "R Draw Color"));
            menuD.Add(new MenuBool("drawR", "R Range  (Red)", true));
            menuD.Add(new MenuBool("drawR2", "R2 Range  (Red)", true));
            menuD.Add(new MenuColor("colorR", "R Draw Color", Color.Red));
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

        private static void OnGameUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicR();
                LogicE();
                LogicQ();
                LogicW();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                
            }
        }

        private static void LogicQ()
        {
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");
            
            if (Me.IsDashing() || Me.IsWindingUp)
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
                                Q.CastCone(qtarget);
                            }

                            break;
                        
                        case 2:
                            var qpreddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width, Q.Delay, Q.Speed,
                                Q.Range, false, SpellType.Cone);
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
            var mana = Config["Wsettings"].GetValue<MenuSlider>("Mana");
            var hp = Config["Wsettings"].GetValue<MenuSlider>("HP");
            var Enemys = Config["Wsettings"].GetValue<MenuSlider>("Enemys");
            var scan = Config["Wsettings"].GetValue<MenuSlider>("Scan");

            if (useW.Enabled && W.IsReady())
            {
                var wtarget = W.GetTarget(350f);
                if (wtarget != null && wtarget.IsValidTarget())
                {
                    if (Me.Mana <= mana.Value || Me.HealthPercent <= hp.Value)
                    {
                        W.Cast(Me.Position);
                    }

                    if (Me.CountEnemyHeroesInRange(scan.Value) >= Enemys.Value)
                    {
                        W.Cast(Me.Position);
                    }
                }
            }
        }

        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            var useE2 = Config["Esettings"].GetValue<MenuBool>("useE2");
            var hp = Config["Esettings"].GetValue<MenuSlider>("hp");
            var range = Config["Esettings"].GetValue<MenuSlider>("range");
            var turret = Config["Esettings"].GetValue<MenuKeyBind>("noturret");
            var qek = Config["Esettings"].GetValue<MenuBool>("qek");
            var e2target = E2.GetTarget(range.Value);

            if (turret.Active && e2target.IsUnderEnemyTurret() && E.Name == "AkaliEb")
            {
                return;
            }

            if (Me.IsWindingUp)
            {
                return;
            }
            
            switch (comb(menuP, "EPred"))
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

            if (useE.Enabled && E.IsReady() && E.Name == "AkaliE")
            {
                var etarget = E.GetTarget();
                if (etarget != null && etarget.IsValidTarget())
                {
                    if (etarget.HealthPercent <= hp.Value)
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
                                    E.CastLine(etarget, 0f, 0f, false);
                                }

                                break;
                            
                            case 2:
                                var epreddd = SPredictionMash1.Prediction.GetPrediction(etarget, E.Width, E.Delay,
                                    E.Speed, E.Range, true, SpellType.Line);
                                if (epreddd.HitChance >= hitchance)
                                {
                                    E.Cast(epreddd.CastPosition);
                                }

                                break;
                        }
                    }
                }
            }

            if (useE2.Enabled && E2.IsReady() && E.Name == "AkaliEb")
            {
                if (e2target != null && e2target.IsValidTarget())
                {
                    if (e2target.HealthPercent <= hp.Value)
                    {
                        if (e2target.InRange(range.Value))
                        {
                            if (!qek.Enabled)
                            {
                                E2.Cast();
                            }

                            if (qek.Enabled)
                            {
                                if (Q.IsReady() || E.GetDamage(e2target, 1) + Me.GetAutoAttackDamage(e2target) >= e2target.Health)
                                {
                                    E2.Cast();
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var useR2 = Config["Rsettings"].GetValue<MenuBool>("useR2");
            var Hp = Config["Rsettings"].GetValue<MenuSlider>("hp");
            var turret = Config["Rsettings"].GetValue<MenuKeyBind>("noturret");
            var rtarget = R.GetTarget();
            var r2target = R2.GetTarget();

            if (Me.IsWindingUp)
            {
                return;
            }
            
            if (turret.Active && rtarget.IsUnderEnemyTurret() && R.Name == "AkaliR")
            {
                return;
            }

            if (turret.Active && r2target.IsUnderEnemyTurret() && R.Name == "AkaliRb")
            {
                return;
            }

            if (Me.IsDashing())
            {
                return;
            }

            if (R.IsReady() && useR.Enabled && R.Name == "AkaliR")
            {
                if (rtarget != null)
                {
                    if (rtarget.HealthPercent <= Hp.Value)
                    {
                        R.Cast(rtarget);
                    }
                }
            }
            
            switch (comb(menuP, "R2Pred"))
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

            if (R.IsReady() && useR2.Enabled && R.Name == "AkaliRb")
            {
                if (r2target != null)
                {
                    if (r2target.HealthPercent <= Hp.Value)
                    {
                        switch (comb(menuP, "Pred"))
                        {
                            case 0:
                                var rpred = R2.GetPrediction(r2target);
                                if (rpred.Hitchance >= hitchance)
                                {
                                    R2.Cast(rpred.CastPosition);
                                }

                                break;
                            
                            case 1:
                                {
                                    R2.CastLine(r2target);
                                }

                                break;
                            
                            case 2:
                                var rpreddd = SPredictionMash1.Prediction.GetPrediction(r2target, R2.Width, R2.Delay,
                                    R2.Speed, R2.Range, false, SpellType.Line);
                                if (rpreddd.HitChance >= hitchance)
                                {
                                    R2.Cast(rpreddd.CastPosition);
                                }

                                break;
                        }
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksE1 = Config["Killsteal"].GetValue<MenuBool>("KsE1").Enabled;
            var ksE2 = Config["Killsteal"].GetValue<MenuBool>("KsE2").Enabled;
            var ksR1 = Config["Killsteal"].GetValue<MenuBool>("KsR1").Enabled;
            var ksR2 = Config["Killsteal"].GetValue<MenuBool>("KsR2").Enabled;
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
                                            Q.CastCone(qtarget);
                                        }

                                        break;
                                    
                                    case 2:
                                        var qpreddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width, Q.Delay, Q.Speed,
                                            Q.Range, false, SpellType.Cone);
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
                if (E.IsReady() && ksE1 && E.Name == "AkaliE")
                {
                    if (etarget != null)
                    {
                        if (etarget.DistanceToPlayer() <= E.Range)
                        {
                            if (etarget.Health + etarget.AllShield + etarget.HPRegenRate <= E.GetDamage(etarget) + E.GetDamage(etarget, 1))
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0:
                                        var epred = E.GetPrediction(etarget);
                                        if (epred.Hitchance >= HitChance.High)
                                        {
                                            E.Cast(epred.CastPosition);
                                        }

                                        break;

                                    case 1:
                                        {
                                            E.CastLine(etarget, 0f, 0f, false);
                                        }

                                        break;
                                    
                                    case 2:
                                        var epreddd = SPredictionMash1.Prediction.GetPrediction(etarget, E.Width, E.Delay,
                                            E.Speed, E.Range, true, SpellType.Line);
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

            foreach (var e2target in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(1500) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (E.IsReady() && ksE2 && E.Name == "AkaliEb")
                {
                    if (e2target != null)
                    {
                        if (e2target.DistanceToPlayer() <= 1500)
                        {
                            if (e2target.Health + e2target.AllShield + e2target.HPRegenRate <= E.GetDamage(e2target, 1))
                            {
                                E2.Cast();
                            }
                        }
                    }
                }
            }

            foreach (var r1target in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(R.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (R.IsReady() && ksR1 && R.Name == "AkaliR")
                {
                    if (r1target != null)
                    {
                        if (r1target.DistanceToPlayer() <= R.Range)
                        {
                            if (r1target.Health + r1target.AllShield + r1target.HPRegenRate <=
                                R.GetDamage(r1target) + R2.GetDamage(r1target, 1))
                            {
                                R.Cast(r1target);
                            }
                        }
                    }
                }
            }

            foreach (var rtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(R2.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksR2 && R.IsReady() && R.Name == "AkaliRb")
                {
                    if (rtarget != null)
                    {
                        if (rtarget.DistanceToPlayer() <= R2.Range)
                        {
                            if (rtarget.Health + rtarget.AllShield + rtarget.HPRegenRate <= R.GetDamage(rtarget, 1))
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0:
                                        var rpred = R2.GetPrediction(rtarget);
                                        if (rpred.Hitchance >= HitChance.High)
                                        {
                                            R2.Cast(rpred.CastPosition);
                                        }

                                        break;
                                    
                                    case 1:
                                        {
                                            R2.CastLine(rtarget);
                                        }

                                        break;
                                    
                                    case 2:
                                        var rpreddd = SPredictionMash1.Prediction.GetPrediction(rtarget, R2.Width, R2.Delay,
                                            R2.Speed, R2.Range, false, SpellType.Line);
                                        if (rpreddd.HitChance >= HitChance.High)
                                        {
                                            R2.Cast(rpreddd.CastPosition);
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
                var qcolor = Config["Draw"].GetValue<MenuColor>("colorQ");
                CircleRender.Draw(Me.Position, Q.Range, qcolor.Color, 2);
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled)
            {
                var ecolor = Config["Draw"].GetValue<MenuColor>("colorE");
                CircleRender.Draw(Me.Position, E.Range, ecolor.Color, 2);
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawE2").Enabled)
            {
                var e2range = Config["Esettings"].GetValue<MenuSlider>("range").Value;
                var ecolor = Config["Draw"].GetValue<MenuColor>("colorE");
                if (E.Name == "AkaliEb")
                {
                    CircleRender.Draw(Me.Position, e2range, ecolor.Color, 2);
                }
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled)
            {
                var rcolor = Config["Draw"].GetValue<MenuColor>("colorR");
                CircleRender.Draw(Me.Position, R.Range, rcolor.Color, 2);
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawR2").Enabled)
            {
                var rcolor = Config["Draw"].GetValue<MenuColor>("colorR");
                if (R.Name == "AkaliRb")
                {
                    CircleRender.Draw(Me.Position, R2.Range, rcolor.Color, 2);
                }
            }
        }

        private static readonly float[] QBaseDamage = { 0f, 30f, 55f, 80f, 105f, 130f, 130f };

        private static float QDamage(AIBaseClient target)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] +
                              (.65f * GameObjects.Player.TotalAttackDamage +
                               .6f * GameObjects.Player.TotalMagicalDamage);
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, qBaseDamage);
        }
    }
}