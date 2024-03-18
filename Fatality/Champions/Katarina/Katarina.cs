using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils;
using Fatality.Utils.Oktw;
using log4net.Repository.Hierarchy;
using SharpDX;

namespace Fatality.Champions.Katarina
{
    public class Katarina
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuK, menuD, menuL;
        private static AIHeroClient Me = ObjectManager.Player;
        private static List<MyDaggerManager> Daggers = new List<MyDaggerManager>();

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Katarina")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 625f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 725f);
            R = new Spell(SpellSlot.R, 550f);

            Config = new Menu("Katarina", "[Fatality] Katarina", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Katarina));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo"));
            menuQ.Add(new MenuList("ComboMode", "Combo Mode",
                new string[] { "Q - E - W", "E - Q - W", "E - W - R - Q" }, 1)).AddPermashow();
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuBool("useW", "Use W in Combo"));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo"));
            menuE.Add(new MenuKeyBind("tower", "Enable E on Targets under Turret", Keys.G, KeyBindType.Toggle)).AddPermashow();
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R Settings");
            menuR.Add(new MenuBool("useR", "Use R in Combo"));
            menuR.Add(new MenuBool("cancel", "Cancel R if no Targets In R Range"));
            menuR.Add(new MenuList("RMode", "R Mode",
                new string[] { "Always", "Min Targets", "Killable" }, 2)).AddPermashow();
            menuR.Add(new MenuSlider("targets", "Min R Targets To R if R Mode Min targets Selected", 2, 1, 5));
            Config.Add(menuR);

            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal"));
            menuK.Add(new MenuBool("KsE", "Enable E Killsteal"));
            Config.Add(menuK);

            menuL = new Menu("Clear", "Clear Settings");
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
            menuD.Add(new MenuColor("colorR", "Change R Draw Color", Color.Red));
            menuD.Add(new MenuSeparator("mm", "Misc Draw Settings"));
            menuD.Add(new MenuBool("drawD", "Draw Dagger Damage Range"));
            Config.Add(menuD);

            Config.Add(new MenuSeparator("asdasd", "Made by Akane#8621"));

            Config.Attach();
            Game.OnUpdate += TickUpdate;
            GameEvent.OnGameTick += OnUpdate;
            GameObject.OnCreate += (sender, Args) => OnCreate(sender);
            GameObject.OnDelete += (sender, Args) => OnDestroy(sender);
            Render.OnDraw += OnDraw;
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void TickUpdate(EventArgs args)
        {
            Killsteal();
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Config["Rsettings"].GetValue<MenuBool>("cancel").Enabled && Me.HasBuff("katarinarsound") && Me.CountEnemyHeroesInRange(R.Range) == 0)
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Me.ServerPosition.Extend(Game.CursorPos, 100));
            }

            if (Me.HasBuff("katarinarsound"))
            {
                Orbwalker.AttackEnabled = false;
                Orbwalker.MoveEnabled = false;
            }
            else
            {
                Orbwalker.AttackEnabled = true;
                Orbwalker.MoveEnabled = true;
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                switch (comb(menuQ, "ComboMode"))
                {
                    case 0:
                        LogicQ();
                        LogicE();
                        LogicW();
                        LogicR();
                        break;

                    case 1:
                        LogicE();
                        LogicQ();
                        LogicW();
                        LogicR();
                        break;

                    case 2:
                        LogicE();
                        LogicW();
                        LogicR();
                        LogicQ();
                        break;
                }
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Harass)
            {
                LogicQ();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Jungleclear();
            }
        }

        private static void LogicQ()
        {
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");

            if (Me.HasBuff("katarinarsound"))
            {
                return;
            }

            if (Q.IsReady() && useQ.Enabled)
            {
                var qtarget = Q.GetTarget();
                if (qtarget != null && qtarget.IsValidTarget())
                {
                    Q.Cast(qtarget);
                }
            }
        }

        private static void LogicW()
        {
            var useW = Config["Wsettings"].GetValue<MenuBool>("useW");

            if (Me.HasBuff("katarinarsound"))
            {
                return;
            }

            if (W.IsReady() && useW.Enabled)
            {
                var wtarget = W.GetTarget(300);
                if (wtarget != null && wtarget.IsValidTarget())
                {
                    if (wtarget.DistanceToPlayer() <= 200)
                    {
                        W.Cast();
                    }
                }
            }
        }

        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");
            var turr = Config["Esettings"].GetValue<MenuKeyBind>("tower");
            var etarget = E.GetTarget();

            if (etarget != null && etarget.IsValidTarget())
            {
                if (!turr.Active && etarget.IsUnderEnemyTurret())
                {
                    return;
                }
            }

            if (Me.HasBuff("katarinarsound"))
            {
                return;
            }

            

            if (E.IsReady() && useE.Enabled)
            {              
                foreach (var dagger in Daggers.Where(x => !x.Dagger.IsDead && x.Dagger.IsValid).Select(x => x.Position))
                {
                    if (dagger.CountEnemyHeroesInRange(340) > 0 && dagger != Vector3.Zero)
                    {
                        E.Cast(dagger);
                    }

                    if (dagger.CountEnemyHeroesInRange(340) == 0)
                    {
                        if (etarget != null && etarget.IsValidTarget())
                        {
                            E.Cast(etarget.Position);
                        }
                    }
                }
            }
        }

        private static void LogicR()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("useR");
            var target = Config["Rsettings"].GetValue<MenuSlider>("targets");

            if (useR.Enabled && R.IsReady())
            {
                switch (comb(menuR, "RMode"))
                {
                    case 0:
                        foreach (var ene in GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.DistanceToPlayer() <= 475))
                        {
                            R.Cast();
                        }
                        break;

                    case 1:
                        if (Me.CountEnemyHeroesInRange(R.Range) >= target.Value)
                        {
                            R.Cast();
                        }
                        break;

                    case 2:
                        foreach (var ene in GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.DistanceToPlayer() <= 475))
                        {
                            if (ene.Health + ene.AllShield <= RDamage(ene))
                            {
                                R.Cast();
                            }
                        }
                        break;
                }
            }
        }

        private static void Jungleclear()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob);
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;

            foreach (var qtarget in GameObjects.EnemyHeroes.Where(hero =>
                         hero.IsValidTarget(Q.Range) && !hero.HasBuff("JudicatorIntervention") &&
                         !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") &&
                         !hero.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (Q.IsReady() && ksQ)
                {
                    if (qtarget != null)
                    {
                        if (qtarget.DistanceToPlayer() <= Q.Range)
                        {
                            if (qtarget.Health + qtarget.AllShield + qtarget.HPRegenRate <= QDamage(qtarget))
                            {
                                Q.Cast(qtarget);
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
                if (E.IsReady() && ksE)
                {
                    if (etarget != null)
                    {
                        if (etarget.DistanceToPlayer() <= E.Range)
                        {
                            if (etarget.Health + etarget.AllShield + etarget.HPRegenRate <= EDamage(etarget))
                            {
                                E.Cast(etarget.Position);
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
                var qcolor = Config["Draw"].GetValue<MenuColor>("colorQ").Color;
                CircleRender.Draw(Me.Position, Q.Range, qcolor, 2);
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

            if (Config["Draw"].GetValue<MenuBool>("drawD").Enabled)
            {
                foreach (var Pos in Daggers.Where(x => !x.Dagger.IsDead && x.Dagger.IsValid).Select(x => x.Position))
                {
                    if (Pos != Vector3.Zero)
                    {
                        if (Pos.CountEnemyHeroesInRange(340) == 0)
                        {
                            CircleRender.Draw(Pos, 340f, Color.Red, 2);
                        }

                        if (Pos.CountEnemyHeroesInRange(340) >= 1)
                        {
                            CircleRender.Draw(Pos, 340f, Color.Green, 2);
                        }
                    }
                }
            }
        }

        private static void OnCreate(GameObject sender)
        {
            try
            {
                if (!sender.Name.Contains("Katarina_"))
                {
                    return;
                }

                switch (sender.Name)
                {
                    case "Katarina_Base_Q_Dagger_Land_Stone":
                    case "Katarina_Base_Q_Dagger_Land_Water":
                    case "Katarina_Base_Q_Dagger_Land_Grass":
                    case "Katarina_Base_Q_Dagger_Land_Dirt":
                    case "Katarina_Base_W_Indicator_Ally":
                    case "Katarina_Base_E_Beam":
                    case "Katarina_Base_Dagger_Ground_Indicator":
                        Daggers.Add(new MyDaggerManager(sender, sender.Position, Variables.GameTimeTickCount));
                        break;
                    case "Katarina_Base_Dagger_PickUp_Cas":
                    case "Katarina_Base_Dagger_PickUp_Tar":
                        var firstDagger = Daggers.OrderBy(x => x.Dagger.Position.Distance(sender.Position))
                            .FirstOrDefault();
                        Daggers.Remove(firstDagger);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnCreate." + ex);
            }
        }

        private static void OnDestroy(GameObject sender)
        {
            try
            {
                if (!sender.Name.Contains("Katarina"))
                {
                    return;
                }

                switch (sender.Name)
                {
                    case "Katarina_Base_Q_Dagger_Land_Stone":
                    case "Katarina_Base_Q_Dagger_Land_Water":
                    case "Katarina_Base_Q_Dagger_Land_Grass":
                    case "Katarina_Base_Q_Dagger_Land_Dirt":
                    case "Katarina_Base_W_Indicator_Ally":
                    case "Katarina_Base_E_Beam":
                    case "Katarina_Base_Dagger_Ground_Indicator":
                        Daggers.RemoveAll(x => x.Dagger.NetworkId == sender.NetworkId);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnCreate." + ex);
            }
        }

        private class MyDaggerManager
        {
            public GameObject Dagger { get; set; }
            public Vector3 Position { get; set; }
            public int CreateTime { get; set; }

            public MyDaggerManager(GameObject dagger, Vector3 position, int thetime)
            {
                this.Dagger = dagger;
                this.Position = position;
                this.CreateTime = thetime;
            }
        }

        private static readonly float[] QBaseDamage = { 0f, 80f, 110f, 140f, 170f, 200f, 200f };
        private static readonly float[] EBaseDamage = { 0f, 20f, 35f, 50f, 65f, 80f, 80f };
        private static readonly float[] RBaseDamage = { 0f, 375f, 562f, 750f, 750f };

        private static float QDamage(AIBaseClient target)
        {
            var qlevel = Q.Level;
            var qBaseDamage = QBaseDamage[qlevel] + .35f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, qBaseDamage);
        }

        private static float EDamage(AIBaseClient target)
        {
            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel] + (.4 * GameObjects.Player.TotalAttackDamage + .25f * GameObjects.Player.TotalMagicalDamage);
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Magical, eBaseDamage);
        }

        private static float RDamage(AIBaseClient target)
        {
            var rlevel = R.Level;
            var rBaseDamage = RBaseDamage[rlevel] + 2.85f * GameObjects.Player.TotalMagicalDamage;
            return (float)GameObjects.Player.CalculateDamage(target, DamageType.Physical, rBaseDamage);
        }
    }
}
