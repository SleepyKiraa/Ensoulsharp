using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using EnsoulSharp.SDK.Utility;
using Fatality.Utils;
using SharpDX;
using SPredictionMash1;

namespace Fatality.Champions.Yasuo
{
    public class Yasuo
    {
        private static Spell Q, Q3, W, E, R;
        private static Menu Config, menuQ,menuE, menuR, menuL, menuK, menuD, menuM, menuP;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static AIHeroClient GetQCircleTarget => TargetSelector.GetTarget(300f, DamageType.Physical, true, Me.GetDashInfo().EndPos.ToVector3(), null);
        private static int QQDElay = 0;
        private static float QDealy => 0.4f * (1 - Math.Min(((Me.AttackSpeedMod - 1) * 100f / 1.67f * 0.01f), 0.67f));

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Yasuo")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 450f);
            Q.SetSkillshot(0.25f, 40f, 5000f, false, SpellType.Line);
            Q3 = new Spell(SpellSlot.Q, 1150f);
            Q3.SetSkillshot(0.25f, 90f, 1200f, false, SpellType.Line);
            W = new Spell(SpellSlot.W, 350f);
            E = new Spell(SpellSlot.E, 475f);
            R = new Spell(SpellSlot.R, 1400f);

            Config = new Menu("Yasuo", "[Fatality] Yasuo", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Yasuo));


            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo"));
            menuQ.Add(new MenuBool("useQ3", "Use Q3 in Combo"));
            menuQ.Add(new MenuBool("Exploit", "Enable Q Exploit")).AddPermashow();
            Config.Add(menuQ);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("Etarget", "Use E on Enemy"));
            menuE.Add(new MenuBool("useE", "Use E To Gapclose"));
            menuE.Add(new MenuSlider("GapRange", "Gapclose Scan Range", 500, 100, 1000));
            menuE.Add(new MenuSlider("GapHp", "Min HP % To Gapclose", 25, 1, 100));
            menuE.Add(new MenuKeyBind("Jump", "Wall Dash Key", Keys.G, KeyBindType.Press)).AddPermashow();
            menuE.Add(new MenuKeyBind("TowerE", "Enable E usage under Enemy Turrets", Keys.T, KeyBindType.Toggle)).AddPermashow();
            menuE.Add(new MenuList("EMode", "E Mode",
                new string[] { "Always", "Only When Q Is Ready", "Only Killable" }, 0));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo"));
            menuR.Add(new MenuBool("overkill", "Enable Overkill Protection"));
            menuR.Add(new MenuSlider("HP", "Target HP % to use R", 50, 1, 100));
            menuR.Add(new MenuSlider("Delay", "R Delay", 1, 1, 500));
            Config.Add(menuR);
            
            menuP = new Menu("Pred", "Prediction Settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] {"Spred (Press F5)"}, 0)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "Very High" }, 2));
            menuP.Add(new MenuList("Q3Pred", "Q3 Hitchance",
                new string[] { "Low", "Medium", "High", "Very High" }, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal"));
            menuK.Add(new MenuBool("KsQ3", "Enable Q3 Killsteal"));
            menuK.Add(new MenuBool("KsE", "Enable E Killsteal"));
            menuK.Add(new MenuBool("KsR", "Enable R Killsteal"));
            Config.Add(menuK);
            
            menuL = new Menu("Clear", "Clear Settings");
            menuL.Add(new MenuBool("LcQ", "Use Q to Lane Clear", true));
            menuL.Add(new MenuSeparator("JungleClear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
            menuL.Add(new MenuBool("JcE", "Use E to Jungle Clear", true));
            Config.Add(menuL);
            
            menuM = new Menu("Misc", "Misc Settings");
            menuM.Add(new MenuBool("AG", "AntiGapcloser", true));
            menuM.Add(new MenuBool("Int", "Interrupter", true));
            Config.Add(menuM);
            
            menuD = new Menu("Draw", "Draw Settings");
            menuD.Add(new MenuSeparator("qq", "Q Draw Settings"));
            menuD.Add(new MenuBool("drawQ", "Draw Q Range", true));
            menuD.Add(new MenuBool("drawQ3", "Draw Q3 Range", true));
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
            AntiGapcloser.OnGapcloser += Gap;
            Interrupter.OnInterrupterSpell += Int;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void UpdateTick(EventArgs args)
        {
            Killsteal();
            Q.Delay = QDealy;

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE();
                LogicQ();
                CastQ3();
                LogicR();
            }
        }

        private static void OnUpdate(EventArgs args)
        {                    

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Laneclear();
                Jungleclear();
            }
            
            if (Config["Esettings"].GetValue<MenuKeyBind>("Jump").Active)
            {
                JumpLogic();
            }
        }


        private static void LogicQ()
        {
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");            
            var expo = Config["Qsettings"].GetValue<MenuBool>("Exploit").Enabled;
            
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
            
            switch (comb(menuP, "Q3Pred"))
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

            if (Me.IsDashing())
            {
                if (GetQCircleTarget != null)
                {
                    var castposition = Me.Position.Extend(GetQCircleTarget.Position, expo ? 50000f : Q.Range);
                    if (Q.Cast(castposition))
                    {
                        QQDElay = Variables.GameTimeTickCount + 50;
                    }
                }
            }
            else
            {
                if (!Me.IsWindingUp)
                {
                    if (Q.Name != "YasuoQ3Wrapper")
                    {
                        var target = Q.GetTarget(Q.Width / 2);
                        if (target.IsValidTarget())
                        {
                            var pred = Q.GetSPrediction(target);
                            if (pred.HitChance >= hitchance)
                            {
                                if (Q.Cast(pred.CastPosition))
                                {
                                    QQDElay = Variables.GameTimeTickCount + 50;
                                }
                                return;
                            }
                        }
                    }
                }
            }            
        }

        private static void CastQ3()
        {
            var useQ3 = Config["Qsettings"].GetValue<MenuBool>("useQ3");

            switch (comb(menuP, "Q3Pred"))
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

            if (useQ3.Enabled && Q3.IsReady() && Q.Name == "YasuoQ3Wrapper")
            {
                var target = Q3.GetTarget();
                if (target != null)
                {
                    var predQ3 = Q3.GetSPrediction(target);
                    if (predQ3.HitChance >= hitchance)
                    {
                        if (Q3.Cast(predQ3.CastPosition))
                        {
                            QQDElay = Variables.GameTimeTickCount + 50;
                        }
                    }
                }
            }
        }

        private static void LogicE()
        {
            var ee = Config["Esettings"].GetValue<MenuBool>("Etarget");
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            var GapHP = Config["Esettings"].GetValue<MenuSlider>("GapHp");
            var scan = Config["Esettings"].GetValue<MenuSlider>("GapRange");
            var dive = Config["Esettings"].GetValue<MenuKeyBind>("TowerE");
            var etarget = TargetSelector.GetTarget(scan.Value, DamageType.Magical);
            var minion = ObjectManager.Get<AIMinionClient>().Where(i => !i.IsDead && i.IsValid() && !i.IsAlly && i.IsValidTarget(E.Range) && Me.Position.Extend(i.Position, E.Range).Distance(etarget) < Me.Distance(etarget)).OrderBy(i => Me.Position.Extend(i.Position, E.Range).Distance(etarget));            
            var etargett = E.GetTarget();           

            if (etargett != null)
            {
                var posafterE = Me.ServerPosition.Extend(etargett.ServerPosition, 475);               
                if (posafterE.IsUnderEnemyTurret() && !dive.Active)
                {
                    return;
                }
            }            

            if (E.IsReady() && ee.Enabled)
            {                
                if (etargett != null && etargett.IsValidTarget())
                {
                    switch (comb(menuE, "EMode"))
                    {
                        case 0:
                            if (etargett.DistanceToPlayer() >= 200)
                            {
                                E.Cast(etargett);
                            }
                            break;

                        case 1:
                            if (etargett.DistanceToPlayer() >= 200 && Q.IsReady())
                            {
                                E.Cast(etargett);
                            }
                            break;

                        case 2:
                            if (etargett.DistanceToPlayer() >= 200 && ComboDamage(etarget) >= etarget.Health + etarget.AllShield)
                            {
                                E.Cast(etargett);
                            }
                            break;
                    }
                }
            }           
            
            if (E.IsReady() && useE.Enabled && etarget != null && Me.HealthPercent >= GapHP.Value)
            {
                if (minion != null)
                {
                    foreach (var min in minion)
                    {
                        if (min != null)
                        {
                            if (!etarget.InRange(275))
                            {
                                var posafterEGap = Me.Position.Extend(min.ServerPosition, 475);
                                if (!posafterEGap.IsUnderEnemyTurret() && !dive.Active)
                                {
                                    E.Cast(min);
                                }
                                else if (dive.Active)
                                {
                                    E.Cast(min);
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
            var over = Config["Rsettings"].GetValue<MenuBool>("overkill");
            var ene = Config["Rsettings"].GetValue<MenuSlider>("enemys");
            var hp = Config["Rsettings"].GetValue<MenuSlider>("HP");

            if (useR.Enabled && R.IsReady())
            {
                foreach (var Targets in GameObjects.EnemyHeroes.Where(x => !x.IsDead && !x.IsZombie() && x.HasBuffOfType(BuffType.Knockup)))
                {
                    if (over.Enabled && Targets.HealthPercent <= 5 && Targets.InRange(Q.Range))
                    {
                        return;
                    }
                    
                    if (Targets.HealthPercent <= hp.Value)
                    {
                        DelayAction.Add(Config["Rsettings"].GetValue<MenuSlider>("Delay").Value, () =>
                        {
                            R.Cast(Targets.Position);
                        });
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
                    var qfarm = Q.GetLineFarmLocation(minions);
                    if (qfarm.Position.IsValid())
                    {
                        Q.Cast(qfarm.Position);
                        return;
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
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range) E.Cast(mob);
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksQ3 = Config["Killsteal"].GetValue<MenuBool>("KsQ3").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;

            foreach (var qtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(Q.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (Q.IsReady() && ksQ)
                {
                    if (Q.Name == "YasuoQ1Wrapper" || Q.Name == "YasuoQ2Wrapper")
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
                                            var qpreddd = SPredictionMash1.Prediction.GetPrediction(qtarget, Q.Width, Q.Delay, Q.Speed,
                                                Q.Range, false, SpellType.Line);
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
            }

            foreach (var q3target in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(Q3.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksQ3 && Q.IsReady())
                {
                    if (Q.Name == "YasuoQ3Wrapper")
                    {
                        if (q3target != null)
                        {
                            if (q3target.DistanceToPlayer() <= Q3.Range)
                            {
                                if (q3target.Health + q3target.AllShield + q3target.HPRegenRate <= QDamage(q3target))
                                {
                                    switch (comb(menuP, "Pred"))
                                    {
                                        case 0:
                                            var Q3preddd = SPredictionMash1.Prediction.GetPrediction(q3target, Q3.Width, Q3.Delay, Q3.Speed,
                                                Q3.Range, false, SpellType.Line);
                                            if (Q3preddd.HitChance >= HitChance.High)
                                            {
                                                Q3.Cast(Q3preddd.CastPosition);
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
                        if (Rtarget.DistanceToPlayer() <= R.Range && Rtarget.HasBuffOfType(BuffType.Knockup))
                        {
                            if (Rtarget.Health + Rtarget.AllShield + Rtarget.HPRegenRate <= RDamage(Rtarget))
                            {
                                R.Cast(Rtarget.Position);
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
                if (Q.Name == "YasuoQ1Wrapper" || Q.Name == "YasuoQ2Wrapper")
                {
                    var qcolor = Config["Draw"].GetValue<MenuColor>("colorQ").Color;
                    CircleRender.Draw(Me.Position, Q.Range, qcolor, 2);
                }
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawQ3").Enabled)
            {
                if (Q.Name == "YasuoQ3Wrapper")
                {
                    var qcolor = Config["Draw"].GetValue<MenuColor>("colorQ").Color;
                    CircleRender.Draw(Me.Position, Q3.Range, qcolor, 2);
                }
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
            
            for (int i = 0; i < StartVec3.Count; i++)
            {
                CircleRender.Draw(StartVec3[i], 80f, Color.Yellow);
                var pos = Drawing.WorldToScreen(StartVec3[i]);
            }
        }

        private static void JumpLogic()
        {
            if (!E.IsReady())
            {
                return;
            }

            if (Game.MapId == GameMapId.SummonersRift)
            {
                for (int i = 0; i < StartVec3.Count; i++)
                {
                    if (Game.CursorPos.Distance(StartVec3[i]) <= 50)
                    {
                        Me.IssueOrder(GameObjectOrder.MoveTo, StartVec3[i]);
                        {
                            if (Me.Position == StartVec3[i])
                            {
                                var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range)).OrderBy(x => x.MaxHealth)
                                    .ToList<AIBaseClient>();
                                if (mobs.Count > 0)
                                {
                                    var mon = mobs[0];
                                    E.Cast(mon);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static List<Vector3> StartVec3 = new List<Vector3>()
        {
            new Vector3(7662f, 8920f, 52.8726f),
            new Vector3(2276f, 8464f, 51.77735f),
            new Vector3(7262f, 5918f, 52.45433f),
            new Vector3(6536f, 12262f, 56.4768f),
            new Vector3(6624f, 11756f, 53.82994f),
            new Vector3(8222f, 3158f, 51.64838f),
            new Vector3(8272f, 2620f, 51.0923f),
            new Vector3(12538f, 6474f, 51.74707f),
            new Vector3(13172f, 6458f, 54.48246f),
            new Vector3(1674f, 8456f, 52.8381f)
        };

        private static List<Vector3> EndVec3 = new List<Vector3>()
        {
            new Vector3(7894f, 9332f, 52.44501f),
            new Vector3(1654f, 8406f, 52.8381f),
            new Vector3(7056f, 5488f, 54.6362f),
            new Vector3(6606f, 11640f, 53.83719f),
            new Vector3(6532f, 12274f, 56.4768f),
            new Vector3(8272f, 2686f, 51.13f),
            new Vector3(8172f, 3158f, 51.5508f),
            new Vector3(13152f, 6366f, 54.97446f),
            new Vector3(12696f, 6444f, 51.6936f),
            new Vector3(2148f, 8450f, 51.77731f)
        };

        private static readonly float[] QBaseDamage = { 0f, 20f, 45f, 70f, 95f, 120f, 120f };
        private static readonly float[] EBaseDamage = { 0f, 60f, 70f, 80f, 90f, 100f, 100f };
        private static readonly float[] RBaseDamage = { 0f, 200f, 350f, 500f, 500f };

        private static float QDamage(AIBaseClient target)
        {
            var qlevel = Q.Level;
            var qBaseDamage = QBaseDamage[qlevel] + 1.05f * GameObjects.Player.TotalAttackDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, qBaseDamage);
        }

        private static float EDamage(AIBaseClient target)
        {
            var elevel = E.Level;
            var eBaseDamage = EBaseDamage[elevel] +
                              (.2f * GameObjects.Player.TotalAttackDamage +
                               .6f * GameObjects.Player.TotalMagicalDamage);
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, eBaseDamage);
        }

        private static float RDamage(AIBaseClient target)
        {
            var rlevel = R.Level;
            var rbaseDamage = RBaseDamage[rlevel] + 1.5f * GameObjects.Player.TotalAttackDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, rbaseDamage);
        }

        private static void Gap(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("AG").Enabled && Q.IsReady())
            {
                if (Q.IsReady() && Q.Name == "YasuoQ3Wrapper")
                {
                    if (sender.IsValid && sender.IsEnemy)
                    {
                        if (sender.IsDashing())
                        {
                            if (args.EndPosition.DistanceToPlayer() < 500)
                            {
                                Q3.CastIfHitchanceEquals(sender, HitChance.Dash);
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
                if (Q.Name == "YasuoQ3Wrapper")
                {
                    if (sender.IsEnemy && args.DangerLevel == Interrupter.DangerLevel.High && Q.IsReady())
                    {
                        if (Me.Distance(sender.ServerPosition) < Q3.Range)
                        {
                            Q3.Cast(sender);
                        }
                    }
                }
            }
        }

        private static float ComboDamage(AIBaseClient target)
        {
            var damage = 0d;
            if (Q.IsReady())
            {
                damage += QDamage(target);
            }

            if (W.IsReady())
            {
                damage += W.GetDamage(target);
            }

            if (E.IsReady())
            {
                damage += EDamage(target);
            }

            if (R.IsReady())
            {
                damage += RDamage(target);
            }

            return (float)damage;
        }
    }
}