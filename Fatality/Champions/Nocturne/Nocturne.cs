using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Evade;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using EnsoulSharp.SDK.Utility;
using Fatality.Utils;
using log4net.Util;
using SharpDX;
using SPredictionMash1;

namespace Fatality.Champions.Nocturne
{
    public class Nocturne
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuK, menuD, menuP, menuL;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Nocturne")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1200f);
            Q.SetSkillshot(0.25f, 60f, 1600f, false, SpellType.Line);

            W = new Spell(SpellSlot.W, 300f);

            E = new Spell(SpellSlot.E, 425f);
            E.SetTargetted(0f, float.MaxValue);

            R = new Spell(SpellSlot.R, 2500f);

            Config = new Menu("Nocturne", "[Fatality] Nocturne", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Nocturne));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo"));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("Wblock", "Auto Block Spells"));
            menuW.Add(new MenuSeparator("ss", "Target Spells"));
            foreach(var enemys in GameObjects.EnemyHeroes.Where(x => x.IsEnemy))
            {
                if (enemys.CharacterName == "Alistar")
                {
                    menuW.Add(new MenuBool("AlistarW", "Block Alistar W"));
                }

                if (enemys.CharacterName == "Blitzcrank")
                {
                    menuW.Add(new MenuBool("BlitzcrankE", "Block Blitzcrank E"));
                }

                if (enemys.CharacterName == "Caitlyn")
                {
                    menuW.Add(new MenuBool("CaitlynR", "Block Caitlyn R"));
                }

                if (enemys.CharacterName == "Darius")
                {
                    menuW.Add(new MenuBool("DariusR", "Block Darius R"));
                }

                if (enemys.CharacterName == "Garen")
                {
                    menuW.Add(new MenuBool("GarenR", "Block Garen R"));
                }

                if (enemys.CharacterName == "Jayce")
                {
                    menuW.Add(new MenuBool("JayceE", "Block Jayce E"));
                }

                if (enemys.CharacterName == "Kindred")
                {
                    menuW.Add(new MenuBool("KindredE", "Block Kindred E"));
                }

                if (enemys.CharacterName == "Leesin")
                {
                    menuW.Add(new MenuBool("LeesinR", "Block Leesin R"));
                }

                if (enemys.CharacterName == "Mordekaiser")
                {
                    menuW.Add(new MenuBool("MordeR", "Block Mordekaiser R"));
                }

                if (enemys.CharacterName == "Pantheon")
                {
                    menuW.Add(new MenuBool("PantheonW", "Block Pantheon W"));
                }

                if (enemys.CharacterName == "Renekton")
                {
                    menuW.Add(new MenuBool("RenektonW", "Block Renekton W"));
                }

                if (enemys.CharacterName == "Singed")
                {
                    menuW.Add(new MenuBool("SingedE", "Block Singed E"));
                }

                if (enemys.CharacterName == "Skarner")
                {
                    menuW.Add(new MenuBool("SkarnerR", "Block Skarner R"));
                }

                if (enemys.CharacterName == "Tristana")
                {
                    menuW.Add(new MenuBool("TristanaE", "Block Tristana E"));
                    menuW.Add(new MenuBool("TristanaR", "Block Tristana R"));
                }

                if (enemys.CharacterName == "Vayne")
                {
                    menuW.Add(new MenuBool("VayneE", "Block Vayne E"));
                }

                if (enemys.CharacterName == "Veigar")
                {
                    menuW.Add(new MenuBool("VeigarR", "Block Veigar R"));
                }
            }
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo"));
            Config.Add(menuE);

            menuP = new Menu("Pred", "Prediction settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] { "SDK", "xcsoft", "Spred (Press F5)" }, 2)).AddPermashow();
            menuP.Add(new MenuList("QPred", "Q Hitchance",
                new string[] { "Low", "Medium", "High", "very High" }, 2));
            Config.Add(menuP);

            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal"));
            Config.Add(menuK);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuSeparator("LaneClear", "Lane Clear"));
            menuL.Add(new MenuBool("LcQ", "Use Q to Lane Clear", true));
            menuL.Add(new MenuSeparator("Jungleclear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
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
            menuD.Add(new MenuSeparator("mm", "Misc Draw Settings"));
            menuD.Add(new MenuBool("kill", "Draw Killable Text"));
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

            AIBaseClient.OnDoCast += Evade;
            GameEvent.OnGameTick += OnUpdate;
            Render.OnDraw += OnDraw;
            Render.OnEndScene += OnEndScene;
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void OnUpdate(EventArgs args)
        {
            Killsteal();
            if (R.Level == 2)
            {
                R.Range = 3250f;
            }
            else if (R.Level == 3)
            {
                R.Range = 4000f;
            }            

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE();
                LogicQ();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Laneclear();
                Jungleclear();
            }           

            if (Me.IsDead || !W.IsReady() || !Config["Wsettings"].GetValue<MenuBool>("Wblock").Enabled)
            {
                return;
            }

            var buffs = ObjectManager.Player.Buffs;

            foreach (var buff in buffs)
            {
                var time = buff.EndTime;

                switch ( buff.Name.ToLower())
                {
                    case "karthusfallenonetarget":
                        if ((time - Game.Time) * 1000 <= 300)
                        {
                            W.Cast();
                        }
                        break;
                    case "nautilusgrandlinetarget":
                        if ((time - Game.Time) * 1000 <= 300)
                        {
                            W.Cast();
                        }
                        break;
                }
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
                            var pred = Q.GetPrediction(qtarget);
                            if (pred.Hitchance >= hitchance)
                            {
                                Q.Cast(pred.CastPosition);
                            }
                            break;

                        case 1:
                            {
                                Q.CastLine(qtarget);
                            }
                            break;

                        case 2:
                            var preddd = Q.GetSPrediction(qtarget);
                            if (preddd.HitChance >= hitchance)
                            {
                                Q.Cast(preddd.CastPosition);
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
                if (etarget != null&& etarget.IsValidTarget())
                {
                    E.Cast(etarget);
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;

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
                                        var pred = Q.GetPrediction(Qtarget);
                                        if (pred.Hitchance >= HitChance.High)
                                        {
                                            Q.Cast(pred.CastPosition);
                                        }
                                        break;

                                    case 1:
                                        {
                                            Q.CastLine(Qtarget);
                                        }
                                        break;

                                    case 2:
                                        var preddd = Q.GetSPrediction(Qtarget);
                                        if (preddd.HitChance >= HitChance.High)
                                        {
                                            Q.Cast(preddd.CastPosition);
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
            
            if (Config["Draw"].GetValue<MenuBool>("kill").Enabled)
            {
                var rtarget = R.GetTarget();
                if (rtarget != null && rtarget.IsValidTarget())
                {
                    if (rtarget.Health + rtarget.AllShield <= ComboDamage(rtarget))
                    {
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f, System.Drawing.Color.Red,
                        "Ult can kill: " + rtarget.CharacterName + " have: " + rtarget.Health + " HP");
                    }
                }
            }
        }

        private static void OnEndScene (EventArgs args)
        {
            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled)
            {
                MiniMap.DrawCircle(Me.Position, R.Range, System.Drawing.Color.Red);
            }
        }       

        private static readonly float[] QBaseDamage = { 0f, 65f, 110f, 155f, 200f, 245f, 245f };

        private static float QDamage(AIBaseClient target)
        {
            var qlevel = Q.Level;
            var qBaseDamage = QBaseDamage[qlevel] + .85 * Me.TotalAttackDamage;
            return (float)Me.CalculateDamage(target, DamageType.Physical, qBaseDamage);
        }

        private static float ComboDamage(AIBaseClient enemy)
        {
            var damage = 0d;
            if (Q.IsReady())
                damage += Me.GetSpellDamage(enemy, SpellSlot.Q);
            if (W.IsReady())
                damage += Me.GetSpellDamage(enemy, SpellSlot.W);
            if (E.IsReady())
                damage += Me.GetSpellDamage(enemy, SpellSlot.E);
            if (R.IsReady())
                damage += Me.GetSpellDamage(enemy, SpellSlot.R);
            damage += Me.GetAutoAttackDamage(enemy) * 2;

            return (float)damage;
        }

        private static void Evade(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            var wuse = Config["Wsettings"].GetValue<MenuBool>("Wblock");

            if (!wuse.Enabled || !W.IsReady() || sender == null)
            {
                return;
            }

            foreach (var enemys in GameObjects.EnemyHeroes.Where(x => x.IsEnemy))
            {
                if (enemys.CharacterName == "Alistar")
                {
                    if (Config["Wsettings"].GetValue<MenuBool>("AlistarW").Enabled)
                    {
                        if (sender.CharacterName == "Alistar" && args.Slot == SpellSlot.W)
                        {
                            if (args.Target.IsMe)
                            {
                                W.Cast();
                            }
                        }
                    }
                }

                if (enemys.CharacterName == "Blitzcrank")
                {
                    if (Config["Wsettings"].GetValue<MenuBool>("BlitzcrankE").Enabled)
                    {
                        if (sender.CharacterName == "Blitzcrank" && args.Slot == SpellSlot.E)
                        {
                            if (args.Target.IsMe)
                            {
                                W.Cast();
                            }
                        }
                    }
                }

                if (enemys.CharacterName == "Caitlyn")
                {
                    if (Config["Wsettings"].GetValue<MenuBool>("CaitlynR").Enabled)
                    {
                        if (sender.CharacterName == "Caitlyn" && args.Slot == SpellSlot.R)
                        {
                            if (args.Target.IsMe)
                            {
                                DelayAction.Add(400, () =>
                                {
                                    W.Cast();
                                });
                            }
                        }
                    }
                }

                if (enemys.CharacterName == "Darius")
                {
                    if (Config["Wsettings"].GetValue<MenuBool>("DariusR").Enabled)
                    {
                        if (sender.CharacterName == "Darius" && args.Slot == SpellSlot.R)
                        {
                            if (args.Target.IsMe)
                            {
                                W.Cast();
                            }
                        }
                    }
                }

                if (enemys.CharacterName == "Garen")
                {
                    if (Config["Wsettings"].GetValue<MenuBool>("GarenR").Enabled)
                    {
                        if (sender.CharacterName == "Garen" && args.Slot == SpellSlot.R)
                        {
                            if (args.Target.IsMe)
                            {
                                W.Cast();
                            }
                        }
                    }
                }

                if (enemys.CharacterName == "Jayce")
                {
                    if (Config["Wsettings"].GetValue<MenuBool>("JayceE").Enabled)
                    {
                        if (sender.CharacterName == "Jayce" && args.Slot == SpellSlot.E)
                        {
                            if (args.Target.IsMe)
                            {
                                W.Cast();
                            }
                        }
                    }
                }

                if (enemys.CharacterName == "Kindred")
                {
                    if (Config["Wsettings"].GetValue<MenuBool>("KindredE").Enabled)
                    {
                        if (sender.CharacterName == "Kindred" && args.Slot == SpellSlot.E)
                        {
                            if (args.Target.IsMe)
                            {
                                W.Cast();
                            }
                        }
                    }
                }

                if (enemys.CharacterName == "Leesin")
                {
                    if (Config["Wsettings"].GetValue<MenuBool>("LeesinR").Enabled)
                    {
                        if (sender.CharacterName == "Leesin" && args.Slot == SpellSlot.R)
                        {
                            if (args.Target.IsMe)
                            {
                                W.Cast();
                            }
                        }
                    }
                }

                if (enemys.CharacterName == "Mordekaiser")
                {
                    if (Config["Wsettings"].GetValue<MenuBool>("MordeR").Enabled)
                    {
                        if (sender.CharacterName == "Mordekaiser" && args.Slot == SpellSlot.R)
                        {
                            if (args.Target.IsMe)
                            {
                                W.Cast();
                            }
                        }
                    }
                }

                if (enemys.CharacterName == "Pantheon")
                {
                    if (Config["Wsettings"].GetValue<MenuBool>("PantheonW").Enabled)
                    {
                        if (sender.CharacterName == "Pantheon" && args.Slot == SpellSlot.W)
                        {
                            if (args.Target.IsMe)
                            {
                                W.Cast();
                            }
                        }
                    }
                }

                if (enemys.CharacterName == "Renekton")
                {
                    if (Config["Wsettings"].GetValue<MenuBool>("RenektonW").Enabled)
                    {
                        if (sender.CharacterName == "Renekton" && args.Slot == SpellSlot.W)
                        {
                            if (args.Target.IsMe)
                            {
                                W.Cast();
                            }
                        }
                    }
                }

                if (enemys.CharacterName == "Singed")
                {
                    if (Config["Wsettings"].GetValue<MenuBool>("SingedE").Enabled)
                    {
                        if (sender.CharacterName == "Singed" && args.Slot == SpellSlot.E)
                        {
                            if (args.Target.IsMe)
                            {
                                W.Cast();
                            }
                        }
                    }
                }

                if (enemys.CharacterName == "Skarner")
                {
                    if (Config["Wsettings"].GetValue<MenuBool>("SkarnerR").Enabled)
                    {
                        if (sender.CharacterName == "Skarner" && args.Slot == SpellSlot.R)
                        {
                            if (args.Target.IsMe)
                            {
                                W.Cast();
                            }
                        }
                    }
                }

                if (enemys.CharacterName == "Tristana")
                {
                    if (Config["Wsettings"].GetValue<MenuBool>("TristanaR").Enabled)
                    {
                        if (sender.CharacterName == "Tristana" && args.Slot == SpellSlot.R)
                        {
                            if (args.Target.IsMe)
                            {
                                W.Cast();
                            }
                        }
                    }

                    if (Config["Wsettings"].GetValue<MenuBool>("TristanaE").Enabled)
                    {
                        if (sender.CharacterName == "Tristana" && args.Slot == SpellSlot.E)
                        {
                            if (args.Target.IsMe)
                            {
                                W.Cast();
                            }
                        }
                    }
                }

                if (enemys.CharacterName == "Vayne")
                {
                    if (Config["Wsettings"].GetValue<MenuBool>("VayneE").Enabled)
                    {
                        if (sender.CharacterName == "Vayne" && args.Slot == SpellSlot.E)
                        {
                            if (args.Target.IsMe)
                            {
                                W.Cast();
                            }
                        }
                    }
                }

                if (enemys.CharacterName == "Veigar")
                {
                    if (Config["Wsettings"].GetValue<MenuBool>("VeigarR").Enabled)
                    {
                        if (sender.CharacterName == "Veigar" && args.Slot == SpellSlot.R)
                        {
                            if (args.Target.IsMe)
                            {
                                W.Cast();
                            }
                        }
                    }
                }
            }                                  
        }        
    }
}
