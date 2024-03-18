using System;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Input;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using SharpDX;
using SPredictionMash1;

namespace Fatality.Champions.Olaf
{
    public class Olaf
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuK, menuD, menuP, menuL;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static GameObject Axe;
        private static float axestart;
        private static float axeend;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Olaf")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1000f);
            Q.SetSkillshot(0.30f, 90f, 1600f, false, SpellType.Line);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 325f);
            R = new Spell(SpellSlot.R);

            Config = new Menu("Olaf", "[Fatality] Olaf", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Olaf));

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
            menuR.Add(new MenuBool("useR", "Auto Use R on CC"));
            menuR.Add(new MenuSlider("enemys", "Min enemys in Range To auto R CC", 1, 1, 5));
            menuR.Add(new MenuSlider("Rrange", "Range to check for enemys", 1000, 500, 2000));
            Config.Add(menuR);

            menuP = new Menu("Pred", "Prediction Settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)" }, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "Very High" }, 2));
            Config.Add(menuP);

            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal"));
            menuK.Add(new MenuBool("KsE", "Enable E Killsteal"));
            Config.Add(menuK);

            menuL = new Menu("Clear", "Clear Settings");
            menuL.Add(new MenuSeparator("LaneClear", "Lane Clear"));
            menuL.Add(new MenuBool("LcQ", "Use Q to Lane Clear", true));
            menuL.Add(new MenuSeparator("JungleClear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
            menuL.Add(new MenuBool("JcW", "Use W to Jungle Clear", true));
            menuL.Add(new MenuBool("JcE", "Use E to Jungle Clear", true));
            Config.Add(menuL);

            menuD = new Menu("Draw", "Draw Settings");
            menuD.Add(new MenuSeparator("qq", "Q Draw Settings"));
            menuD.Add(new MenuBool("drawQ", "Draw Q Range", true));
            menuD.Add(new MenuBool("drawAxe", "Draw Axe Position"));
            menuD.Add(new MenuBool("drawAxeBuff", "Draw Remaning Axe Time"));
            menuD.Add(new MenuColor("colorQ", "Change Q Draw Color", Color.White));
            menuD.Add(new MenuSeparator("ee", "E Draw Settings"));
            menuD.Add(new MenuBool("drawE", "Draw E Range", true));
            menuD.Add(new MenuColor("colorE", "Change E Draw Color", Color.Green));
            menuD.Add(new MenuSeparator("rr", "R Draw Settings"));
            menuD.Add(new MenuBool("drawR", "Draw R Range", true));
            menuD.Add(new MenuColor("colorR", "Change R Draw Color", Color.Red));
            menuD.Add(new MenuSeparator("dd", "Draw Misc"));
            menuD.Add(new MenuBool("drawB", "Draw W and R Buff Time"));
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
            Orbwalker.OnAfterAttack += AfterAA;
            Game.OnUpdate += OnTick;
            GameEvent.OnGameTick += OnUpdate;
            Spellbook.OnCastSpell += OnCastSpell;
            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += OnDelete;
            Render.OnDraw += OnDraw;
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {               
                LogicQ();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Laneclear();
                Jungleclear();
            }
        }

        private static void AfterAA(object sender, AfterAttackEventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicW(args);
            }

            LogicE(args);
        }

        private static void OnTick(EventArgs args)
        {
            LogicR();
            Killsteal();
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

        private static void LogicW(AfterAttackEventArgs args)
        {
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");

            if (W.IsReady() && useW.Enabled)
            {
                var wtarget = W.GetTarget(Me.GetRealAutoAttackRange());
                if (wtarget != null && wtarget.IsValidTarget())
                {
                    W.Cast();
                }
            }
        }

        private static void LogicE(AfterAttackEventArgs args)
        {
            var etarget = args.Target as AIHeroClient;
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            if (etarget == null) return;

            if (E.IsReady() && useE.Enabled)
            {
                if (etarget.InRange(E.Range) && etarget.IsValidTarget())
                {
                    if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                    {
                        E.Cast(etarget);
                    }
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var enemys = Config["Rsettings"].GetValue<MenuSlider>("enemys");
            var range = Config["Rsettings"].GetValue<MenuSlider>("Rrange");

            if (R.IsReady() && useR.Enabled)
            {
                if (Me.HasBuffOfType(BuffType.Stun) || Me.HasBuffOfType(BuffType.Snare) || Me.HasBuffOfType(BuffType.Charm) || Me.HasBuffOfType(BuffType.Asleep) || Me.HasBuffOfType(BuffType.Suppression) || Me.HasBuffOfType(BuffType.Taunt))
                {
                    if (Me.CountEnemyHeroesInRange(range.Value) >= enemys.Value)
                    {
                        R.Cast();
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
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

            if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled)
            {
                var ecolor = Config["Draw"].GetValue<MenuColor>("colorE").Color;
                CircleRender.Draw(Me.Position, E.Range, ecolor, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled)
            {
                var range = Config["Rsettings"].GetValue<MenuSlider>("Rrange");
                var rcolor = Config["Draw"].GetValue<MenuColor>("colorR").Color;
                CircleRender.Draw(Me.Position, range.Value, rcolor, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawB").Enabled)
            {
                var buff = Me.GetBuff("OlafFrenziedStrikes");
                var buff2 = Me.GetBuff("OlafRagnarok");
                
                if (buff != null)
                {
                    var timer = buff.EndTime - Game.Time;
                    FatalityRenderRing.DrawText($"W Time: {timer:N1}", Drawing.Width * 0.43f, Drawing.Height * 0.57f, Color.Red);
                }

                if (buff2 != null)
                {
                    var timer2 = buff2.EndTime - Game.Time;
                    FatalityRenderRing.DrawText($"R Time: {timer2:N1}", Drawing.Width * 0.43f, Drawing.Height * 0.60f, Color.Red);
                }
            }

            if (Config["Draw"].GetValue<MenuBool>("drawAxe").Enabled)
            {
                if ( Axe != null)
                {
                    CircleRender.Draw(Axe.Position, 200, Color.Red, 2);                   
                }
            }

            if (Config["Draw"].GetValue<MenuBool>("drawAxeBuff").Enabled)
            {
                if (Axe != null)
                {
                    var pos = Drawing.WorldToScreen(Axe.Position);
                    var time = axeend - Game.Time;
                    FatalityRenderRing.DrawText($"Axe Time: {time:N1}", (int)pos.X * 0.9f, (int)pos.Y * 1.1f, Color.White);
                }

            }
        }

        private static readonly float[] QBaseDamage = { 0f, 70f, 120f, 170f, 220f, 270f, 270f };
        private static readonly float[] EBaseDamage = { 0f, 70f, 115f, 160f, 205f, 250f, 250f };

        private static float QDamage(AIBaseClient target)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] + 1f * Me.TotalAttackDamage;
            return (float)Me.CalculateDamage(target, DamageType.Physical, qBaseDamage);
        }

        private static float EDamage(AIBaseClient target)
        {
            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel] + .5f * Me.TotalAttackDamage;
            return (float)Me.CalculateTrueDamage(target, eBaseDamage);
        }

        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot == SpellSlot.W)
            {
                Orbwalker.ResetAutoAttackTimer();
            }
        }

        private static void OnCreate(GameObject obj, EventArgs args)
        {
            if (obj.Name.Contains("Olaf_Base_Q_Axe_Ally") || obj.Name.Contains("Olaf_Skin03_Q_Axe_Ally") || obj.Name.Contains("Olaf_Skin04_Q_Axe_Ally") || obj.Name.Contains("Olaf_Skin06_Q_Axe_Ally") || obj.Name.Contains("Olaf_Skin15_Q_Axe_Ally") || obj.Name.Contains("Olaf_Skin16_Q_Axe_Ally") || obj.Name.Contains("Olaf_Skin25_Q_Axe_Ally") || obj.Name.Contains("Olaf_Skin35_Q_Axe_Ally"))
            {
                Axe = obj;
                axestart = Game.Time;
                axeend = Game.Time + 9;
                Console.WriteLine("Axe Created");
            }
        }

        private static void OnDelete(GameObject obj, EventArgs args)
        {
            if (obj.Name.Contains("Olaf_Base_Q_Axe_Ally") || obj.Name.Contains("Olaf_Skin03_Q_Axe_Ally") || obj.Name.Contains("Olaf_Skin04_Q_Axe_Ally") || obj.Name.Contains("Olaf_Skin06_Q_Axe_Ally") || obj.Name.Contains("Olaf_Skin15_Q_Axe_Ally") || obj.Name.Contains("Olaf_Skin16_Q_Axe_Ally") || obj.Name.Contains("Olaf_Skin25_Q_Axe_Ally") || obj.Name.Contains("Olaf_Skin35_Q_Axe_Ally"))
            {
                Axe = null;
                Console.WriteLine("Axe Deleted");
            }
        }
    }
}
