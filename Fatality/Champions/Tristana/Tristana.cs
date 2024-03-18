using System;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using SharpDX;
using SharpDX.Direct3D9;

namespace Fatality.Champions.Tristana
{
    public static class Tristana
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuE, menuK, menuD, menuM;
        private static AIHeroClient Me = ObjectManager.Player;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Tristana")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 0);
            W = new Spell(SpellSlot.W, 900f);
            W.SetSkillshot(0.25f, 175f, 1100f, false, SpellType.Circle);
            E = new Spell(SpellSlot.E, 525f);
            E.SetTargetted(0.25f, 2400f);
            R = new Spell(SpellSlot.R, 525f);
            R.SetTargetted(0.25f, 2000f);

            Config = new Menu("Tristana", "[Fatality] Tristana", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Tristana));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo", true));
            Config.Add(menuQ);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo"));
            Config.Add(menuE);

            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsER", "Enable ER Killsteal", true));
            menuK.Add(new MenuBool("KsW", "Enable W Killsteal", false));
            menuK.Add(new MenuBool("KsR", "Enable R Killsteal", true));
            Config.Add(menuK);

            menuM = new Menu("Misc", "Misc Settings");
            menuM.Add(new MenuBool("AG", "AntiGapcloser", true));
            menuM.Add(new MenuBool("Int", "Interrupter", true));
            Config.Add(menuM);
            
            menuD = new Menu("Draw", "Draw settings");
            menuD.Add(new MenuBool("drawQ", "Q Range  (White)", true));
            menuD.Add(new MenuBool("drawW", "W Range  (Blue)", true));
            menuD.Add(new MenuBool("drawE", "E Range (Green)", true));
            menuD.Add(new MenuBool("drawR", "R Range  (Red)", true));
            menuD.Add(new MenuBool("drawA", "Draw AA Tracker", true));
            Config.Add(menuD);
            Config.Add(new MenuSeparator("asdasd", "Made by Akane#8621"));

            Config.Attach();
            Game.OnUpdate += UpdateTick;
            GameEvent.OnGameTick += OnGameUpdate;
            Render.OnDraw += OnDraw;
            Orbwalker.OnBeforeAttack += BeforeAA;
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
        }

        private static void OnGameUpdate(EventArgs args)
        {
            var heros = TargetSelector.GetTargets(Me.GetCurrentAutoAttackRange(), DamageType.Physical);
            Orbwalker.ForceTarget = heros.FirstOrDefault(x => x.IsCharged());
            
            E.Range = Me.GetRealAutoAttackRange();
            R.Range = Me.GetRealAutoAttackRange();
        }

        private static void BeforeAA(object sender, BeforeAttackEventArgs args)
        {
            LogicE(args);
            LogicQ(args);
        }

        private static void LogicQ(BeforeAttackEventArgs args)
        {
            var qtarget = args.Target as AIHeroClient;
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");
            if (qtarget == null) return;

            if (qtarget.InRange(Me.GetRealAutoAttackRange()))
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                {
                    if (qtarget.IsValidTarget(Me.GetRealAutoAttackRange()) && useQ.Enabled && Q.IsReady())
                    {
                        Q.Cast();
                    }
                }
            }
        }

        private static void LogicE(BeforeAttackEventArgs args)
        {
            var etarget = args.Target as AIHeroClient;
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            if (etarget == null) return;

            if (etarget.InRange(Me.GetRealAutoAttackRange()))
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                {
                    if (etarget.IsValidTarget(Me.GetRealAutoAttackRange()) && useE.Enabled && E.IsReady())
                    {
                        E.Cast(etarget);
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;
            var ksER = Config["Killsteal"].GetValue<MenuBool>("KsER").Enabled;
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;
            foreach (var Wtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(W.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksW && W.IsReady() && Wtarget.IsValidTarget(W.Range))
                {
                    if (Wtarget != null)
                    {
                        if (Wtarget.DistanceToPlayer() <= W.Range)
                        {
                            if (Wtarget.Health + Wtarget.AllShield <= Me.GetSpellDamage(Wtarget, SpellSlot.W))
                            {
                                var wpred = W.GetPrediction(Wtarget);
                                if (wpred.Hitchance >= HitChance.High)
                                {
                                    W.Cast(wpred.CastPosition);
                                }
                            }
                        }
                    }
                }
            }

            foreach (var ERtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(Me.GetRealAutoAttackRange()) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksER && R.IsReady() && ERtarget.IsValidTarget(Me.GetRealAutoAttackRange()))
                {
                    if (ERtarget != null)
                    {
                        if (ERtarget.DistanceToPlayer() <= Me.GetRealAutoAttackRange())
                        {
                            if (ERtarget.Health + ERtarget.AllShield <= EDamage(ERtarget) + RDamage(ERtarget))
                            {
                                R.Cast(ERtarget);
                            }
                        }
                    }
                }
            }

            foreach (var Rtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(Me.GetRealAutoAttackRange()) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (ksR && R.IsReady() && Rtarget.IsValidTarget(Me.GetRealAutoAttackRange()))
                {
                    if (Rtarget != null)
                    {
                        if (Rtarget.DistanceToPlayer() <= Me.GetRealAutoAttackRange())
                        {
                            if (Rtarget.Health + Rtarget.AllShield <= RDamage(Rtarget))
                            {
                                R.Cast(Rtarget);
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

            if (Config["Draw"].GetValue<MenuBool>("drawW").Enabled)
            {
                CircleRender.Draw(Me.Position, W.Range, Color.Blue, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled)
            {
                CircleRender.Draw(Me.Position, E.Range, Color.Green, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled)
            {
                CircleRender.Draw(Me.Position, R.Range, Color.Red, 2);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawA").Enabled)
            {
                FatalityRenderRing.AALeft();
            }
        }

        private static float GetComboDamage(AIHeroClient target)
        {
            var Damage = 0d;
            if (Q.IsReady())
            {
                Damage += Q.GetDamage(target);
            }

            if (W.IsReady())
            {
                Damage += W.GetDamage(target);
            }

            if (E.IsReady())
            {
                Damage += EDamage(target);
            }

            if (R.IsReady())
            {
                Damage += RDamage(target);
            }
            
            return (float)Damage;
        }

        private static readonly float[] EBaseDamage = { 0f, 70f, 80f, 90f, 100f, 110f, 110f };
        private static readonly float[] EMultiplier = { 0f, .5f, .75f, 1f, 1.25f, 1.5f, 1.5f };
        private static readonly float[] EStack = { 0f, 21f, 24f, 27f, 30f, 33f, 33f };
        private static readonly float[] EStackMultiplier = { 0f, .15f, .22f, .30f, .37f, .45f, .45f };
        private static readonly float[] RBaseDamage = { 0f, 300f, 400f, 500f, 500f };

        private static float EDamage(AIBaseClient target)
        {
            if (!target.IsCharged())
            {
                return 0;
            }

            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel] + EMultiplier[eLevel] * Me.GetBonusPhysicalDamage() +
                              .5f * Me.TotalMagicalDamage;
            var eBonusDamage = EStack[eLevel] + EStackMultiplier[eLevel] + Me.TotalAttackDamage +
                               .15 * Me.TotalMagicalDamage;
            var total = eBaseDamage + eBonusDamage * target.EBuffCount();
            return (float)Me.CalculateDamage(target, DamageType.Physical, total);
        }

        private static float RDamage(AIBaseClient target)
        {
            var rLevel = R.Level;
            var rBaseDamage = RBaseDamage[rLevel] + Me.TotalMagicalDamage;
            return (float) Me.CalculateDamage(target, DamageType.Magical, rBaseDamage);
        }

        private static bool IsCharged(this AIBaseClient target)
        {
            return target.HasBuff("TristanaECharge");
        }

        private static Boolean HasEBuff(AIBaseClient target)
        {
            return target.HasBuff("TristanaECharge");
        }

        private static int EBuffCount(this AIBaseClient target)
        {
            return target.GetBuffCount("TristanaECharge");
        }

        private static void Gap(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (sender.IsMe)
            {
                return;
            }

            if (Config["Misc"].GetValue<MenuBool>("AG").Enabled && R.IsReady())
            {
                if (args.EndPosition.DistanceToPlayer() < 350)
                {
                    R.Cast(sender);
                }
            }
        }

        private static void Int(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("Int").Enabled)
            {
                if (sender.IsEnemy && args.DangerLevel == Interrupter.DangerLevel.High && R.IsReady())
                {
                    if (Me.Distance(sender.ServerPosition) < Me.GetRealAutoAttackRange())
                    {
                        R.Cast(sender);
                    }
                }
            }
        }
    }
}