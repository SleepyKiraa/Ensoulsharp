using System;
using System.Linq;
using System.Media;
using System.Runtime.Remoting.Messaging;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using Fatality.Utils.Oktw;
using SharpDX;

namespace Fatality.Champions.Vayne
{
    public class Vayne
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuK, menuD, menuM;
        private static AIHeroClient Me = ObjectManager.Player;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Vayne")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 300f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 550f);
            E.SetTargetted(0.25f, 2000f);
            R = new Spell(SpellSlot.R);

            Config = new Menu("Vayne", "[Fatality] Vayne", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Vayne));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo", true));
            menuQ.Add(new MenuBool("QGap", "Use Q to Gapclose", true));
            menuQ.Add(new MenuSlider("wstacks", "Min W stacks to cast Q", 0, 0, 2));
            menuQ.Add(new MenuSlider("noQ", "Dont Q if more Targets then selected in Range (6 is Disabling this)", 2, 2, 6));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("forceW", "Force Orbwalker on W Target", true));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "use E if Target is Stunnable", true));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo", false));
            menuR.Add(new MenuSlider("rtargets", "Min R Targets to Use R", 2, 1, 5));
            menuR.Add(new MenuSlider("Rrange", "Scan Range for R Targets", 800, 500, 1500));
            Config.Add(menuR);

            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsE", "Enable E Killsteal", true));
            Config.Add(menuK);
            
            menuD = new Menu("Draw", "Draw Settings");
            menuD.Add(new MenuBool("drawQ", "Q Range  (White)", true));
            menuD.Add(new MenuBool("drawE", "E Range (Green)", true));
            menuD.Add(new MenuBool("drawR", "R Scan Range  (Red)", true));
            menuD.Add(new MenuBool("drawB", "Draw Buff Time", true));
            menuD.Add(new MenuBool("drawA", "Draw AA Tracker", true));
            Config.Add(menuD);

            menuM = new Menu("Misc", "Misc Settings");
            menuM.Add(new MenuBool("Gap", "AntiGapcloser", true));
            menuM.Add(new MenuBool("Int", "Interrupter", true));
            Config.Add(menuM);
            
            Config.Add(new MenuSeparator("asdasd", "Made by Akane#8621"));

            Config.Attach();

            GameEvent.OnGameTick += OnGameUpdate;
            Game.OnUpdate += Updatetick;
            Render.OnDraw += OnDraw;
            AntiGapcloser.OnGapcloser += Gap;
            Interrupter.OnInterrupterSpell += Int;
            AIBaseClient.OnDoCast += OnDoCast;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }
        
        private static void Updatetick(EventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE();
            }
            
            Killsteal();
        }

        private static void OnGameUpdate(EventArgs args)
        {
            if (Config["Wsettings"].GetValue<MenuBool>("forceW").Enabled)
            {
                var heros = TargetSelector.GetTargets(Me.GetCurrentAutoAttackRange(), DamageType.Physical);
                Orbwalker.ForceTarget = heros.FirstOrDefault(x => x.HasBuff("VayneSilveredDebuff"));
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicR();
                LogicQ();
            }
        }

        private static void LogicQ()
        {
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");
            var gap = Config["Qsettings"].GetValue<MenuBool>("QGap");
            var stacks = Config["Qsettings"].GetValue<MenuSlider>("wstacks");
            var noqtarget = Config["Qsettings"].GetValue<MenuSlider>("noQ");

            if (Me.IsWindingUp)
            {
                return;
            }

            if (gap.Enabled && Me.CountEnemyHeroesInRange(Me.GetRealAutoAttackRange() + Q.Range) >= noqtarget.Value)
            {
                return;
            }

            if (Q.IsReady() && useQ.Enabled)
            {
                if (gap.Enabled)
                {
                    var qtarget = TargetSelector.GetTarget(Q.Range + Me.GetRealAutoAttackRange(), DamageType.Physical);
                    if (qtarget != null)
                    {
                        if (qtarget.GetBuffCount("VayneSilveredDebuff") >= stacks.Value)
                        {
                            Q.Cast(Game.CursorPos);
                        }
                    }
                }

                if (!gap.Enabled)
                {
                    var qtarget2 = TargetSelector.GetTarget(Q.Range + 250, DamageType.Physical);
                    if (qtarget2 != null)
                    {
                        if (qtarget2.GetBuffCount("VayneSilveredDebuff") >= stacks.Value)
                        {
                            Q.Cast(Game.CursorPos);
                        }
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
                if (etarget != null)
                {
                    if (Condemcheck(Me.ServerPosition, etarget))
                    {
                        E.CastOnUnit(etarget);
                        return;
                    }
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var rtargets = Config["Rsettings"].GetValue<MenuSlider>("rtargets");
            var scan = Config["Rsettings"].GetValue<MenuSlider>("Rrange");

            if (R.IsReady() && useR.Enabled)
            {
                if (Me.CountEnemyHeroesInRange(scan.Value) >= rtargets.Value)
                {
                    R.Cast();
                }
            }
        }

        private static void Killsteal()
        {
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;


            foreach (var etarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(E.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksE && E.IsReady())
                {
                    if (etarget != null)
                    {
                        if (etarget.DistanceToPlayer() <= E.Range)
                        {
                            if (etarget.Health + etarget.AllShield + etarget.HPRegenRate <= EDamage(etarget))
                            {
                                E.Cast(etarget);
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
                CircleRender.Draw(Me.Position, Q.Range, Color.White, 2);
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled)
            {
                CircleRender.Draw(Me.Position, E.Range, Color.Green, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled)
            {
                var range = Config["Rsettings"].GetValue<MenuSlider>("Rrange").Value;
                CircleRender.Draw(Me.Position, range, Color.Red, 2);
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawA").Enabled)
            {
                FatalityRenderRing.AALeft();
            }
            
            if (Config["Draw"].GetValue<MenuBool>("drawB").Enabled)
            {
                var buff = Me.GetBuff("VayneInquisition");
                if (buff != null)
                {
                    var timer = buff.EndTime - Game.Time;
                    FatalityRenderRing.DrawText($"R Time: {timer:N1}", Drawing.Width * 0.43f, Drawing.Height * 0.57f, Color.Red);
                }
            }
        }

        private static bool Condemcheck(Vector3 start, AIBaseClient target)
        {
            var targetpos = target.ServerPosition;
            var predpos = E.GetPrediction(target).UnitPosition;
            var pushDistance = start == Me.ServerPosition ? 450 : 440;

            for (var i = 0; i <= pushDistance; i += 20)
            {
                var targetpoint = targetpos.Extend(start, -i);
                var predpoint = targetpos.Extend(start, -i);

                if (predpoint.IsWall() && targetpoint.IsWall())
                {
                    return true;
                }
            }

            return false;
        }

        private static void Gap(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (sender.IsMe)
            {
                return;
            }

            if (Config["Misc"].GetValue<MenuBool>("Gap").Enabled && E.IsReady())
            {
                if (args.EndPosition.DistanceToPlayer() < 400)
                {
                    E.Cast(sender);
                }
            }
        }

        private static void Int(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("Int").Enabled)
            {
                if (Me.Distance(sender.ServerPosition) < E.Range)
                {
                    E.Cast(sender);
                }
            }
        }

        private static readonly float[] EBaseDamage = { 0f, 50f, 85f, 120f, 155f, 190f, 190f };

        private static float EDamage(AIBaseClient target)
        {
            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel] + .5f * Me.TotalAttackDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, eBaseDamage);
        }

        private static void OnDoCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender == null || !sender.IsValid || sender.GetType() != typeof(AIHeroClient) || !sender.IsEnemy ||
                !E.IsReady())
            {
                return;
            }

            if (sender.CharacterName == "Alistar" && args.Slot == SpellSlot.W && args.Target != null &&
                args.Target.IsValid && args.Target.IsMe)
            {
                E.CastOnUnit(sender);
            }
            
            if (sender.CharacterName == "Pantheon" && args.Slot == SpellSlot.W && args.Target != null &&
                args.Target.IsValid && args.Target.IsMe)
            {
                E.CastOnUnit(sender);
            }
        }
    }
}