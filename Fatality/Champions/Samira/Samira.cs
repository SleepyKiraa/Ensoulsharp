using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using SharpDX;
using SPredictionMash1;

namespace Fatality.Champions.Samira
{
    public class Samira
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuK, menuD, menuP, menuL, evade, evade2, champion;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;
        private static readonly List<Targets> DetectedTargets = new List<Targets>();
        private static readonly List<SpellData> Spells = new List<SpellData>();

        public static void OnGameload()
        {
            if (Me.CharacterName != "Samira")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 950f);
            Q.SetSkillshot(0.25f, 60f, 2600f, true, SpellType.Line);
            W = new Spell(SpellSlot.W, 325f);
            E = new Spell(SpellSlot.E, 600f);
            R = new Spell(SpellSlot.R, 600f);

            Config = new Menu("Samira", "[Fatality] Samira", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Samira));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q In Combo"));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo"));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo"));
            menuE.Add(new MenuSlider("Health", "Target HP % to use E on Always mode", 50, 1, 100));
            menuE.Add(new MenuList("EMode", "E Mode",
                new string[] { "Always", "Killable" }, 1));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo"));
            Config.Add(menuR);
            
            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal", true));
            menuK.Add(new MenuBool("KsW", "Enable W Killsteal", true));
            menuK.Add(new MenuBool("KsE", "Enable E Killsteal", true));
            Config.Add(menuK);
            
            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuSeparator("LaneClear", "Lane Clear"));
            menuL.Add(new MenuBool("LcQ", "Use Q to Lane Clear", true));
            menuL.Add(new MenuSeparator("Jungleclear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
            Config.Add(menuL);
            
            LoadSpellData();
            evade = new Menu("Evade", "Evade Settings");
            evade.Add(new MenuBool("EvadeW", "Use W to Evade"));
            evade.Add(new MenuSlider("EvadeHP", "HP To use Auto W Block", 50, 1, 100));
            evade2 = evade.Add(new Menu("EvadeTargetList", "Evade List"));
            foreach (var spell in Spells.Where(i => GameObjects.EnemyHeroes.Any(a => a.CharacterName == i.ChampionName)))
            {
                champion = evade2.Add(new Menu("scc" + spell.ChampionName, spell.ChampionName));
                champion.Add(new MenuBool(spell.MissileName,
                    spell.MissileName + " (" + spell.Slot + ")",
                    true));
            }
            Config.Add(evade);
            
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
            menuD.Add(new MenuSeparator("mm", "Misc Draw Settings"));
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
            Game.OnUpdate += updatetick;
            Render.OnDraw += OnDraw;
            Game.OnUpdate += OnUpdateTarget;
            GameObject.OnCreate += ObjSpellMissileOnCreate;
            GameObject.OnDelete += ObjSpellMissileOnDelete;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void updatetick(EventArgs args)
        {
         Killsteal();   
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Me.HasBuff("SamiraR") || Me.HasBuff("SamiraW"))
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicR();
                LogicE();
                LogicQ();
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
            var Qtarget = Q.GetTarget();
            

            if (Qtarget != null)
            {
                if (Qtarget.DistanceToPlayer() <= 400)
                {
                    Q.Collision = false;
                }
                else
                {
                    Q.Collision = true;
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

            if (useQ.Enabled && Q.IsReady())
            {
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
                                Q.CastLine(Qtarget, 0f, 0f, false);
                            }

                            break;
                        
                        case 2:
                            var qpreddd = Q.GetSPrediction(Qtarget);
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
                if (wtarget != null == W.IsReady())
                {
                    W.Cast();
                }
            }
        }

        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            var ehp = Config["Esettings"].GetValue<MenuSlider>("Health");

            switch (comb(menuE, "EMode"))
            {
                case 0:
                    if (E.IsReady() && useE.Enabled)
                    {
                        var etarget = E.GetTarget();
                        if (etarget != null && etarget.IsValidTarget())
                        {
                            if (etarget.HealthPercent <= ehp.Value)
                            {
                                E.Cast(etarget);
                            }
                        }
                    }

                    break;
                
                case 1:
                    if (E.IsReady() && useE.Enabled)
                    {
                        var etarget = E.GetTarget();
                        if (etarget != null && etarget.IsValidTarget())
                        {
                            if (etarget.Health <= GetComboDamage(etarget))
                            {
                                E.Cast(etarget);
                            }
                        }
                    }

                    break;
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");

            if (R.IsReady() && useR.Enabled && Me.HasBuff("samirarreadybuff"))
            {
                var rtarget = R.GetTarget();
                if (rtarget != null && rtarget.IsValidTarget())
                {
                    R.Cast();
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
                                            Q.CastLine(Qtarget, 0f, 0f, false);
                                        }

                                        break;
                        
                                    case 2:
                                        var qpreddd = Q.GetSPrediction(Qtarget);
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

            foreach (var Wtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(W.Range) && !hero.HasBuff("JudicatorIntervention") &&
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
                                W.Cast();
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
            var lcq = Config["Clear"].GetValue<MenuBool>("LcQ");

            if (lcq.Enabled && Q.IsReady())
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
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ").Enabled;
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(W.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (Config["Draw"].GetValue<MenuBool>("drawQ").Enabled)
            {
                var colorQ = Config["Draw"].GetValue<MenuColor>("colorQ").Color;
                CircleRender.Draw(Me.Position, Q.Range, colorQ, 2);
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawW").Enabled)
            {
                var colorW = Config["Draw"].GetValue<MenuColor>("colorW").Color;
                CircleRender.Draw(Me.Position, W.Range, colorW, 2);
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled)
            {
                var colorE = Config["Draw"].GetValue<MenuColor>("colorE").Color;
                CircleRender.Draw(Me.Position, E.Range, colorE, 2);
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled)
            {
                var colorR = Config["Draw"].GetValue<MenuColor>("colorR").Color;
                CircleRender.Draw(Me.Position, R.Range, colorR, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawAA").Enabled)
            {
                FatalityRenderRing.AALeft();
            }
        }
        
            private static void LoadSpellData()
            {
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Ahri",
                        SpellNames = new[] { "ahriwdamagemissileback1", "ahriwdamagemissilefront1", "ahriwdamagemissileright1" },
                        Slot = SpellSlot.W
                    }
                    );
                Spells.Add(
                    new SpellData
                    { ChampionName = "Ahri", SpellNames = new[] { "ahritumblemissile" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData
                    { ChampionName = "Akshan", SpellNames = new[] { "akshanrmissile" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData { ChampionName = "Anivia", SpellNames = new[] { "frostbite" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { ChampionName = "Annie", SpellNames = new[] { "annieq" }, Slot = SpellSlot.Q });

                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Brand",
                        SpellNames = new[] { "brandr", "brandrmissile" },
                        Slot = SpellSlot.R
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Caitlyn",
                        SpellNames = new[] { "caitlynrmissile" },
                        Slot = SpellSlot.R
                    });

                Spells.Add(
                    new SpellData { ChampionName = "Elise", SpellNames = new[] { "elisehumanq" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Ezreal",
                        SpellNames = new[] { "ezrealemissile" },
                        Slot = SpellSlot.E
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "FiddleSticks",
                        SpellNames = new[] { "fiddlesticksqmissilefear" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(
                    new SpellData { ChampionName = "Gangplank", SpellNames = new[] { "gangplankqproceed" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData { ChampionName = "Janna", SpellNames = new[] { "sowthewind" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData { ChampionName = "Kassadin", SpellNames = new[] { "nulllance" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Katarina",
                        SpellNames = new[] { "katarinaq", "katarinaqdaggerarc" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Kindred",
                        SpellNames = new[] { "kindrede" },
                        Slot = SpellSlot.E
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Leblanc",
                        SpellNames = new[] { "leblancq", "leblancrq" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Lillia",
                        SpellNames = new[] { "lilliarexpungemissile" },
                        Slot = SpellSlot.R
                    });
                //-------------
                Spells.Add(new SpellData { ChampionName = "Lulu", SpellNames = new[] { "luluw" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                    { ChampionName = "Malphite", SpellNames = new[] { "seismicshard" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "MissFortune",
                        SpellNames = new[] { "missfortunericochetshot", "missfortunershotextra" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Morgana",
                        SpellNames =  new[] {"morganaq"},
                        Slot =  SpellSlot.Q
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Nami",
                        SpellNames = new[] { "namiwenemy", "namiwmissileenemy" },
                        Slot = SpellSlot.W
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Ryze",
                        SpellNames = new[] { "ryzee" },
                        Slot = SpellSlot.E
                    });
                Spells.Add(
                    new SpellData { ChampionName = "Shaco", SpellNames = new[] { "twoshivpoison" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { ChampionName = "Sona", SpellNames = new[] { "sonaqmissile" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData { ChampionName = "Syndra", SpellNames = new[] { "syndrarspell" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData { ChampionName = "Teemo", SpellNames = new[] { "blindingdart" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    { ChampionName = "Tristana", SpellNames = new[] { "tristanae" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData
                    { ChampionName = "Tristana", SpellNames = new[] { "tristanar" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData
                    { ChampionName = "TwistedFate", SpellNames = new[] { "bluecardattack" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                    { ChampionName = "TwistedFate", SpellNames = new[] { "goldcardattack" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                    { ChampionName = "TwistedFate", SpellNames = new[] { "redcardattack" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Urgot",
                        SpellNames = new[] { "urgotrrecastmissile" },
                        Slot = SpellSlot.R
                    });
                Spells.Add(
                    new SpellData { ChampionName = "Vayne", SpellNames = new[] { "vaynecondemnmissile" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData
                    { ChampionName = "Veigar", SpellNames = new[] { "veigarr" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData
                    { ChampionName = "Viktor", SpellNames = new[] { "viktorpowertransfer" }, Slot = SpellSlot.Q });
            }
        
            private static void ObjSpellMissileOnCreate(GameObject sender, EventArgs args)
            {
                var missile = sender as MissileClient;
                if (missile == null || !missile.IsValid)
                {
                    return;
                }

                var unit = missile.SpellCaster as AIHeroClient;
                if (unit == null || !unit.IsValid || unit.Team == Me.Team)
                {
                    return;
                }

                var spellData =
                Spells.FirstOrDefault(
                    i =>
                    i.SpellNames.Contains(missile.SData.Name.ToLower())
                    && evade2["scc" + i.ChampionName][i.MissileName] != null && evade2["scc" + i.ChampionName][i.MissileName].GetValue<MenuBool>().Enabled);

                if (spellData == null //MenuManager.LaneClearMenu["E"].Cast<CheckBox>().CurrentValue
                    && !missile.SData.Name.ToLower().Contains("crit"))
                {
                    spellData = new SpellData
                    { ChampionName = unit.CharacterName, SpellNames = new[] { missile.SData.Name } };
                }
                if (spellData == null || (missile.Target != null && !missile.Target.IsMe))
                {
                    return;
                }

                if (missile.SData.Name.ToLower().Contains("basicattack") || Me.HealthPercent > evade["EvadeHP"].GetValue<MenuSlider>().Value)
                {
                    return;
                }
                
                DetectedTargets.Add(new Targets { Start = unit.Position, Obj = missile });
            }

            private static void ObjSpellMissileOnDelete(GameObject sender, EventArgs args)
            {
                var missile = sender as MissileClient;
                if (missile == null || !missile.IsValid)
                {
                    return;
                }
                var caster = missile.SpellCaster as AIHeroClient;
                if (caster == null || !caster.IsValid || caster.Team == Me.Team)
                {
                    return;
                }

                DetectedTargets.RemoveAll(i => i.Obj.NetworkId == missile.NetworkId);

            }
            
            private static void OnUpdateTarget(EventArgs args)
            {
                if (Me.IsDead)
                {
                    return;
                }
                if (Me.HasBuffOfType(BuffType.SpellImmunity) || Me.HasBuffOfType(BuffType.SpellShield))
                {
                    return;
                }
                if (!W.IsReady(200))
                {
                    return;
                }
                foreach (var target in
                         DetectedTargets.Where(i => Me.Distance(i.Obj.Position) < 700))
                {
                    //如果有风墙阻挡的话
                    if (Collisions.HasYasuoWindWallCollision(target.Obj.Position, Me.ServerPosition))
                    {
                        continue;
                    }
                    if (W.IsReady() && evade["EvadeW"].GetValue<MenuBool>().Enabled && W.IsInRange(target.Obj.Position))
                    {
                        W.Cast();
                    }
                }
            }
            
            private class SpellData
            {
                #region Fields

                public string ChampionName;

                public SpellSlot Slot;

                public string[] SpellNames = { };

                #endregion

                #region Public Properties

                public string MissileName
                {
                    get
                    {
                        return this.SpellNames.FirstOrDefault();
                    }
                }

                #endregion
            }

            private class Targets
            {
                #region Fields

                public MissileClient Obj;

                public Vector3 Start;

                #endregion
            }

            private static readonly float[] QBaseDamage = { 0f, 5f, 10f, 15f, 20f, 20f };
            private static readonly float[] QBonusDamage = { 0f, .85f, .95f, 1.05f, 1.15f, 1.25f, 1.25f };
            private static readonly float[] WBaseDamage = { 0f, 20f, 35f, 50f, 65f, 80f, 80f };
            private static readonly float[] EBaseDamage = { 0f, 50f, 60f, 70f, 80f, 90f, 90f };
            private static readonly float[] RBaseDamage = { 0f, 5f, 15f, 25f, 25f };

            private static float QDamage(AIBaseClient target)
            {
                var qLevel = Q.Level;
                var qBaseDamage = QBaseDamage[qLevel] + QBonusDamage[qLevel] * GameObjects.Player.TotalAttackDamage;
                return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, qBaseDamage);
            }

            private static float WDamage(AIBaseClient target)
            {
                var wLevel = W.Level;
                var wBaseDamage = WBaseDamage[wLevel] + .8f * GameObjects.Player.TotalAttackDamage;
                return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, wBaseDamage);
            }

            private static float EDamage(AIBaseClient target)
            {
                var eLevel = E.Level;
                var eBasedamage = EBaseDamage[eLevel] + .2f * GameObjects.Player.TotalAttackDamage;
                return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, eBasedamage);
            }

            private static float RDamage(AIBaseClient target)
            {
                var rLevel = R.Level;
                var rBaseDamage = RBaseDamage[rLevel] + .5f * GameObjects.Player.TotalAttackDamage;
                return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, rBaseDamage);
            }
            
            private static float GetComboDamage(AIHeroClient target)
            {
                var Damage = 0d;
                if (Q.IsReady())
                {
                    Damage += QDamage(target);
                }
            
                if (W.IsReady())
                {
                    Damage += WDamage(target);
                }

                if (E.IsReady())
                {
                    Damage += EDamage(target);
                }

                if (Me.HasBuff("samirarreadybuff"))
                {
                    Damage += RDamage(target) * 10;
                }

                return (float)Damage;
            }
    }
}