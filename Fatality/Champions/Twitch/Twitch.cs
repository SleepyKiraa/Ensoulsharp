using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Core;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using log4net.Util;
using SharpDX;
using SPredictionMash1;

namespace Fatality.Champions.Twitch
{
    public class Twitch
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuK, menuD, menuP, menuL;
        private static AIHeroClient Me = ObjectManager.Player;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Twitch")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 950f);
            W.SetSkillshot(0.25f, 80f, 1400f, false, SpellType.Circle);
            E = new Spell(SpellSlot.E, 1200f);
            R = new Spell(SpellSlot.R, Me.GetRealAutoAttackRange() + 300f);

            Config = new Menu("Twitch", "[Fatality] Twitch", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Twitch));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("AutoQ", "Auto Q After Kill"));
            menuQ.Add(new MenuKeyBind("QB", "Invis Backport", Keys.G, KeyBindType.Press)).AddPermashow();
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo"));
            menuW.Add(new MenuBool("noQ", "Dont W when you Invisible"));
            menuW.Add(new MenuBool("noR", "Dont W when R is Active"));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E if Target is Killable"));
            menuE.Add(new MenuSlider("Estack", "E on X Stacks (7 = Disable)", 6, 1, 7));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo"));
            menuR.Add(new MenuSlider("enemys", "Min Enemys in R Range", 2, 1, 5));
            Config.Add(menuR);
            
            menuP = new Menu("Pred", "Prediction Settings");
            menuP.Add(new MenuList("Pred", "Prediction Selection",
                new string[] {"SDK", "xcsoft", "Spred (Press F5)"}, 2)).AddPermashow();
            menuP.Add(new MenuList("WPred", "W Hitchance",
                new string[] { "Low", "Medium", "High", "Very High" }, 2));
            Config.Add(menuP);
            
            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsE", "Enable E Killsteal"));
            Config.Add(menuK);
            
            menuL = new Menu("Clear", "Clear Settings");
            menuL.Add(new MenuBool("LcW", "Use W to Lane Clear", true));
            menuL.Add(new MenuSeparator("JungleClear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcW", "Use W to Jungle Clear", true));
            menuL.Add(new MenuBool("JcE", "Use E to Jungle Clear", true));
            Config.Add(menuL);
            
            menuD = new Menu("Draw", "Draw Settings");
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
            menuD.Add(new MenuBool("drawED", "Draw E Damage"));
            menuD.Add(new MenuBool("Buffs", "Draw Buff Times"));
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
            Game.OnNotify += Notify;
            Render.OnDraw += OnDraw;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void UpdateTick(EventArgs args)
        {
            Killsteal();
            if (Config["Qsettings"].GetValue<MenuKeyBind>("QB").Active)
            {
                backport();
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE();
                LogicW();
                LogicR();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Laneclear();
                Jungleclear();
            }
        }

        private static void LogicW()
        {
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");
            var noQ = Config["Wsettings"].GetValue<MenuBool>("noQ");
            var noR = Config["Wsettings"].GetValue<MenuBool>("noR");

            if (noQ.Enabled && Me.HasBuff("TwitchHideInShadows"))
            {
                return;
            }

            if (noR.Enabled && Me.HasBuff("TwitchFullAutomatic"))
            {
                return;
            }
            
            switch (comb(menuP, "WPred"))
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

            if (W.IsReady() && useW.Enabled)
            {
                var wtarget = W.GetTarget();
                if (wtarget != null && wtarget.IsValidTarget())
                {
                    switch (comb(menuP, "Pred"))
                    {
                        case 0:
                            var wpred = W.GetPrediction(wtarget, true);
                            if (wpred.Hitchance >= HitChance.High)
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
                            var wpreddd = SPredictionMash1.Prediction.GetPrediction(wtarget, W.Width, W.Delay, W.Speed,
                                W.Range, false, SpellType.Circle);
                            if (wpreddd.HitChance >= hitchance)
                            {
                                W.Cast(wpreddd.CastPosition);
                            }

                            break;
                    }
                }
            }
        }

        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            var stacks = Config["Esettings"].GetValue<MenuSlider>("Estack");

            if (E.IsReady() && useE.Enabled)
            {
                foreach (var target in GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.IsValidTarget() && x.HasBuff("TwitchDeadlyVenom")))
                {
                    if (target.Health + target.AllShield + target.HPRegenRate <= GetEDamage(target))
                    {
                        E.Cast();
                    }
                }
            }

            if (E.IsReady())
            {
                var Etarget = E.GetTarget();
                if (Etarget != null && Etarget.IsValidTarget())
                {
                    if (Etarget.GetBuffCount("TwitchDeadlyVenom") >= stacks.Value)
                    {
                        E.Cast();
                    }
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var ene = Config["Rsettings"].GetValue<MenuSlider>("enemys");

            if (useR.Enabled && R.IsReady())
            {
                if (Me.CountEnemyHeroesInRange(R.Range) >= ene.Value)
                {
                    R.Cast();
                }
            }
        }
        
        private static void Laneclear()
        {
            var lcW = Config["Clear"].GetValue<MenuBool>("LcW");

            if (lcW.Enabled && W.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                
                if (minions.Any())
                {
                    var wfarm = W.GetCircularFarmLocation(minions);
                    if (wfarm.Position.IsValid())
                    {
                        W.Cast(wfarm.Position);
                        return;
                    }
                }
            }
        }
        
        private static void Jungleclear()
        {
            var JcWw = Config["Clear"].GetValue<MenuBool>("JcW");
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(W.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast(mob.Position);
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range && mob.GetBuffCount("TwitchDeadlyVenom") == 6) E.Cast();
            }
        }

        private static void Killsteal()
        {
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;
            {
                foreach (var qtarget in GameObjects.EnemyHeroes.Where(hero =>
                             hero.IsValidTarget(E.Range) && !hero.HasBuff("JudicatorIntervention") &&
                             !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                             !hero.HasBuffOfType(BuffType.Invulnerability) && hero.HasBuff("TwitchDeadlyVenom")))
                {
                    if (ksE && E.IsReady())
                    {
                        if (qtarget != null)
                        {
                            if (qtarget.DistanceToPlayer() <= E.Range)
                            {
                                if (qtarget.Health + qtarget.AllShield + qtarget.HPRegenRate <= GetEDamage(qtarget))
                                {
                                    E.Cast();
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
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

            if (Config["Draw"].GetValue<MenuBool>("drawAA").Enabled)
            {
                FatalityRenderRing.AALeft();
            }

            if (Config["Draw"].GetValue<MenuBool>("drawED").Enabled)
            {
                foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsVisible && !x.IsDead && x.HasBuff("TwitchDeadlyVenom")))
                {
                    float getTotaldamage = edamage(target);
                    float tdamage = getTotaldamage * 100 / target.Health;
                    int totaldamage = (int)Math.Ceiling(tdamage);
                    FatalityRenderRing.DrawText2(string.Format("{0}%", totaldamage), Drawing.WorldToScreen(target.Position - Drawing.Height * .03f), Color.White);
                }
            }

            if (Config["Draw"].GetValue<MenuBool>("Buffs").Enabled)
            {
                var buff = Me.GetBuff("TwitchHideInShadows");
                var buff2 = Me.GetBuff("TwitchFullAutomatic");
                if (buff != null)
                {
                    var timer = buff.EndTime - Game.Time;
                    FatalityRenderRing.DrawText($"Q Time: {timer:N1}",Drawing.Width * 0.43f, Drawing.Height * 0.57f, Color.Red);
                }
                
                if (buff2 != null)
                {
                    var timer2 = buff2.EndTime - Game.Time;
                    FatalityRenderRing.DrawText($"R Time: {timer2:N1}",Drawing.Width * 0.43f, Drawing.Height * 0.60f, Color.Red);
                }
            }
        }

        private static void Notify(GameNotifyEventArgs args)
        {
            switch (args.EventId)
            {
                case GameEventId.OnChampionKill:
                    if (Config["Qsettings"].GetValue<MenuBool>("AutoQ").Enabled)
                    {
                        if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                        {
                            if (Q.IsReady() && Me.CountEnemyHeroesInRange(Me.GetRealAutoAttackRange()) >= 1)
                            {
                                Q.Cast();
                            }
                        }
                    }

                    break;
            }
        }

        private static int edamage(AIBaseClient target)
        {
            return (int)(target.Health + target.AllShield - GetEDamage(target));
        }
        
        
        private static float GetEDamage(AIBaseClient target)
        {
            var eStacksOnTarget = target.GetBuffCount("TwitchDeadlyVenom");
            var eLevel = E.Level;
            var baseDamage = new[] { 0, 20, 30, 40, 50, 60 }[eLevel];
            var PhysicalDamage = new[] { 0, 15, 20, 25, 30, 35 }[eLevel] + 0.35f * (Me.TotalAttackDamage - Me.BaseAttackDamage);
            var MagicDamage = 0.3f * Me.TotalMagicalDamage;
            if (eStacksOnTarget == 0)
            {
                return 0;
            }

            var damage = (float)Me.CalculateMixedDamage(target, baseDamage + (PhysicalDamage * eStacksOnTarget), MagicDamage * eStacksOnTarget);

            if (target is AIMinionClient minion && (minion.GetJungleType() & JungleType.Legendary) != 0)
            {
                damage /= 2;
            }

            return damage;
        }      

        private static void backport()
        {
            if (Q.IsReady())
            {
                Q.Cast();
                Me.Spellbook.CastSpell(SpellSlot.Recall);
            }
        }
    }
}