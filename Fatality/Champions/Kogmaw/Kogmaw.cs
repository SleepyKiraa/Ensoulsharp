using System;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Input;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Damages.Spells;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using SharpDX;
using SPredictionMash1;

namespace Fatality.Champions.KogMaw
{
    public class KogMaw
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuK, menuD, menuP, menuL;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "KogMaw")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1200f);
            Q.SetSkillshot(0.25f, 70f, 1650f, true, SpellType.Line);

            W = new Spell(SpellSlot.W);

            E = new Spell(SpellSlot.E, 1360f);
            E.SetSkillshot(0.25f, 120f, 1400f, false, SpellType.Line);

            R = new Spell(SpellSlot.R, 1300f);
            R.SetSkillshot(1.1f, 120f, float.MaxValue, false, SpellType.Circle);

            Config = new Menu("KogMaw", "[Fatality] KogMaw", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Kogmaw));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo"));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo"));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo"));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo"));
            menuR.Add(new MenuBool("AARange", "Dont use R in AA Range"));
            menuR.Add(new MenuSlider("HP", "HP % To use R", 50, 1, 100));
            menuR.Add(new MenuSlider("Rstacks", "Max R Stacks", 3, 1, 9));
            menuR.Add(new MenuKeyBind("SemiR", "Semi R", Keys.T, KeyBindType.Press)).AddPermashow();
            Config.Add(menuR);
            
            menuP = new Menu("Pred", "Prediction Settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)" }, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "Very High" }, 2));
            menuP.Add(new MenuList("EPred", "E Hitchance",
                new string[] { "Low", "Medium", "High", "Very High" }, 2));
            menuP.Add(new MenuList("RPred", "R Hitchance",
                new string[] { "Low", "Medium", "High", "Very High" }, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal"));
            menuK.Add(new MenuBool("KsE", "Enable E Killsteal"));
            menuK.Add(new MenuBool("KsR", "Enable R Killsteal"));
            Config.Add(menuK);
            
            menuL = new Menu("Clear", "Clear Settings");
            menuL.Add(new MenuSeparator("LaneClear", "Lane Clear"));
            menuL.Add(new MenuBool("LcE", "Use E to Lane Clear", true));
            menuL.Add(new MenuSlider("Ecount", "E Minions Hit", 2, 1, 3));
            menuL.Add(new MenuSeparator("JungleClear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
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
            menuD.Add(new MenuSeparator("dd", "Draw Misc"));
            menuD.Add(new MenuBool("drawAA", "Draw AA Tracker"));
            menuD.Add(new MenuBool("drawB", "Draw W Buff Time"));
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
            Game.OnUpdate += OnTick;
            GameEvent.OnGameTick += OnUpdate;
            Render.OnDraw += OnDraw;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void OnTick(EventArgs args)
        {
            Killsteal();
        }

        private static void OnUpdate(EventArgs args)
        {
            R.Range = 1300f + 250f * (R.Level - 1);
            W.Range = 130f + 20f * W.Level;

            if (Config["Rsettings"].GetValue<MenuKeyBind>("SemiR").Active)
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                SemiR();
            }

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

            if (Me.IsWindingUp)
            {
                return;
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

            if (useW.Enabled && W.IsReady())
            {
                var wtarget = TargetSelector.GetTarget(Me.GetRealAutoAttackRange() + W.Range, DamageType.Magical);
                if (wtarget != null)
                {
                    W.Cast();
                }
            }
        }

        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            
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

            if (useE.Enabled && E.IsReady())
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
                            var epredd = E.GetSPrediction(etarget);
                            if (epredd.HitChance >= hitchance)
                            {
                                E.Cast(epredd.CastPosition);
                            }

                            break;
                    }
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var aa = Config["Rsettings"].GetValue<MenuBool>("AARange");
            var stacks = Config["Rsettings"].GetValue<MenuSlider>("Rstacks");
            var health = Config["Rsettings"].GetValue<MenuSlider>("HP");
            var Rtarget = TargetSelector.GetTarget(R.Range, DamageType.Magical);

            if (aa.Enabled)
            {
                if (Rtarget != null && Rtarget.IsValidTarget())
                {
                    if (Rtarget.InRange(Me.GetCurrentAutoAttackRange()))
                    {
                        return;
                    }
                }
            }

            if (Me.GetBuffCount("kogmawlivingartillerycost") >= stacks.Value)
            {
                return;
            }
            
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
                if (Rtarget != null && Rtarget.IsValidTarget() && Rtarget.HealthPercent <= health.Value)
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
                        {
                            R.CastCircle(Rtarget);
                        }
                            break;
                        
                        case 2:
                            var Rpredd = R.GetSPrediction(Rtarget);
                            if (Rpredd.HitChance >= hitchance)
                            {
                                R.Cast(Rpredd.CastPosition);
                            }

                            break;
                    }
                }
            }
        }

        private static void SemiR()
        {
            var Rtarget = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            if (R.IsReady())
            {
                if (Rtarget != null && Rtarget.IsValidTarget() )
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
                        {
                            R.CastCircle(Rtarget);
                        }
                            break;
                        
                        case 2:
                            var Rpredd = R.GetSPrediction(Rtarget);
                            if (Rpredd.HitChance >= hitchance)
                            {
                                R.Cast(Rpredd.CastPosition);
                            }

                            break;
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
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

            foreach (var Etarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(E.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (E.IsReady() && ksE)
                {
                    if (Etarget != null)
                    {
                        if (Etarget.DistanceToPlayer() <= E.Range)
                        {
                            if (Etarget.Health + Etarget.AllShield + Etarget.HPRegenRate <= EDamage(Etarget))
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0:
                                        var epred = E.GetPrediction(Etarget);
                                        if (epred.Hitchance >= HitChance.High)
                                        {
                                            E.Cast(epred.CastPosition);
                                        }
                                        break;

                                    case 1:
                                    {
                                        E.CastLine(Etarget);
                                    }
                                        break;

                                    case 2:
                                        var epredd = E.GetSPrediction(Etarget);
                                        if (epredd.HitChance >= HitChance.High)
                                        {
                                            E.Cast(epredd.CastPosition);
                                        }
                                        break;
                                } 
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
                            if (Rtarget.Health + Rtarget.AllShield + Rtarget.HPRegenRate <= RDamage(Rtarget))
                            {
                                switch (comb(menuP, "Pred"))
                                {
                                    case 0:
                                        var rpred = R.GetPrediction(Rtarget);
                                        if (rpred.Hitchance >= HitChance.High)
                                        {
                                            R.Cast(rpred.CastPosition);
                                        }
                                        break;

                                    case 1:
                                    {
                                        R.CastCircle(Rtarget);
                                    }
                                        break;

                                    case 2:
                                        var rpredd = R.GetSPrediction(Rtarget);
                                        if (rpredd.HitChance >= HitChance.High)
                                        {
                                            R.Cast(rpredd.CastPosition);
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private static void Laneclear()
        {
            var lcE = Config["Clear"].GetValue<MenuBool>("LcE");
            var lcEc = Config["Clear"].GetValue<MenuSlider>("Ecount");

            if (lcE.Enabled && E.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var Efarm = E.GetLineFarmLocation(minions);
                    if (Efarm.Position.IsValid() && Efarm.MinionsHit >= lcEc.Value)
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
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range) E.Cast(mob.Position);
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (Config["Draw"].GetValue<MenuBool>("drawQ").Enabled && Q.Level > 0)
            {
                var qcolor = Config["Draw"].GetValue<MenuColor>("colorQ").Color;
                CircleRender.Draw(Me.Position, Q.Range, qcolor, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawW").Enabled && W.Level > 0)
            {
                var wcolor = Config["Draw"].GetValue<MenuColor>("colorW").Color;
                CircleRender.Draw(Me.Position, Me.GetRealAutoAttackRange(), wcolor, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled && E.Level > 0)
            {
                var ecolor = Config["Draw"].GetValue<MenuColor>("colorE").Color;
                CircleRender.Draw(Me.Position, E.Range, ecolor, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled && R.Level > 0)
            {
                var rcolor = Config["Draw"].GetValue<MenuColor>("colorR").Color;
                CircleRender.Draw(Me.Position, R.Range, rcolor, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawAA").Enabled)
            {
                FatalityRenderRing.AALeft();
            }

            if (Config["Draw"].GetValue<MenuBool>("drawB").Enabled)
            {
                var buff = Me.GetBuff("KogMawBioArcaneBarrage");

                if (buff != null)
                {
                    var timer = buff.EndTime - Game.Time;
                    FatalityRenderRing.DrawText($"W Time: {timer:N1}", Drawing.Width * 0.43f, Drawing.Height * 0.57f, Color.Red);
                }
            }
        }

        private static readonly float[] QBaseDamage = { 0f, 90f, 140f, 190f, 240f, 290f, 290f };
        private static readonly float[] EBaseDamage = { 0f, 75f, 120f, 165f, 210f, 255f, 255f };
        private static readonly float[] RBaseDamage = { 0f, 100f, 140f, 180f, 180f };

        private static float QDamage(AIBaseClient target)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] + .7f * Me.TotalMagicalDamage;
            return (float)Me.CalculateDamage(target, DamageType.Magical, qBaseDamage);
        }

        private static float EDamage(AIBaseClient target)
        {
            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel] + .7f * Me.TotalMagicalDamage;
            return (float)Me.CalculateDamage(target, DamageType.Magical, eBaseDamage);
        }

        private static float RDamage(AIBaseClient target)
        {
            var rLevel = R.Level;
            var rBaseDamage = RBaseDamage[rLevel] + (.65f * Me.TotalAttackDamage + .35f * Me.TotalMagicalDamage);
            return (float)Me.CalculateDamage(target, DamageType.Magical, rBaseDamage);
        }
    }
}