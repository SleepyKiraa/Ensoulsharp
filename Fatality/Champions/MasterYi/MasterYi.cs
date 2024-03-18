using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Documents;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using EnsoulSharp.SDK.Utility;
using Fatality.Utils;
using SharpDX;
using SharpDX.Direct3D11;
using SPredictionMash1;

namespace Fatality.Champions.MasterYi
{
    public class MasterYi
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuL, menuK, menuD, menuM;
        private static AIHeroClient Me = ObjectManager.Player;
        private static int colorindex = 0;
        private static readonly Dictionary<float, float> IncDamage = new Dictionary<float, float>();
        private static readonly Dictionary<float, float> InstDamage = new Dictionary<float, float>();

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "MasterYi")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 600f);
            Q.SetTargetted(0f, float.MaxValue);

            W = new Spell(SpellSlot.W);

            E = new Spell(SpellSlot.E);

            R = new Spell(SpellSlot.R);

            Config = new Menu("MasterYi", "[Fatality] Master Yi", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.MasterYi));

            menuQ = new Menu("Qsettings", "Q Settings");
            menuQ.Add(new MenuBool("useQ", "Use Q in Combo"));
            menuQ.Add(new MenuList("Qmode", "Q Mode",
                new string[] { "Always", "Killable", "No AA Range" }, 2));
            menuQ.Add(new MenuKeyBind("Tower", "Enable Q on Targets under Turret", Keys.G, KeyBindType.Toggle))
                .AddPermashow();
            menuQ.Add(new MenuKeyBind("dash", "Follow Dashes", Keys.T, KeyBindType.Toggle)).AddPermashow();
            menuQ.Add(new MenuKeyBind("flash", "Follow Flash", Keys.T, KeyBindType.Toggle)).AddPermashow();
            menuQ.Add(new MenuSeparator("bb", "Q Evade Settings"));
            menuQ.Add(new MenuBool("masterswitch", "Enable Q Target Spells Evade"));
            menuQ.Add(new MenuBool("turrets", "Evade Turret Shots"));
            menuQ.Add(new MenuSlider("turretdogehp", "HP % To Q Evade Turret Shots", 50, 1, 100));
            foreach (var ene in ObjectManager.Get<AIHeroClient>().Where(x => x.Team != Me.Team))
            {
                foreach (var lib in TargetDataBAse.GDLIST.Where(x => x.HeroName == ene.CharacterName))
                {
                    menuQ.Add(new MenuBool(lib.SDataName, lib.SpellMenuName + " from " + ene.CharacterName));
                }

                foreach (var lib in TargetDataBAse.GDLIstInstant.Where(x => x.HeroName == ene.CharacterName))
                {
                    menuQ.Add(new MenuBool(lib.SDataName, lib.SpellMenuName + " from " + ene.CharacterName));
                }
            }

            menuQ.Add(new MenuSeparator("semiii", "Semi Q Settings"));
            menuQ.Add(new MenuKeyBind("SemiQ", "Semi Q", Keys.H, KeyBindType.Toggle)).AddPermashow();
            menuQ.Add(new MenuSeparator("semiqwhgite", "Semi Q White List"));
            foreach (var targetsss in GameObjects.EnemyHeroes.Where(x => x.IsEnemy))
            {
                menuQ.Add(new MenuBool(targetsss.CharacterName, "Use Semi Q on " + targetsss.CharacterName));
            }
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W Settings");
            menuW.Add(new MenuKeyBind("WAA", "Use W To AA Reset", Keys.J, KeyBindType.Toggle)).AddPermashow();
            menuW.Add(new MenuBool("WBlock", "Block Incomming Damage"));
            menuW.Add(new MenuSlider("MinDamage", "Min Incomming Damage to Use W", 300, 100, 1500));
            menuW.Add(new MenuSeparator("www", "W Target Spell Block"));
            menuW.Add(new MenuBool("masterswitch", "Enable W Target Spells Evade"));
            foreach (var ene in ObjectManager.Get<AIHeroClient>().Where(x => x.Team != Me.Team))
            {
                foreach (var lib in TargetDataBAse.GDLIST.Where(x => x.HeroName == ene.CharacterName))
                {
                    menuW.Add(new MenuBool(lib.SDataName, lib.SpellMenuName + " from " + ene.CharacterName));
                }

                foreach (var lib in TargetDataBAse.GDLIstInstant.Where(x => x.HeroName == ene.CharacterName))
                {
                    menuW.Add(new MenuBool(lib.SDataName, lib.SpellMenuName + " from " + ene.CharacterName));
                }
            }
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("useE", "Use E in Combo"));
            menuE.Add(new MenuSeparator("whitelistt", "Target Whitelist"));
            foreach (var targets in GameObjects.EnemyHeroes.Where(x => x.IsEnemy))
            {
                menuE.Add(new MenuBool(targets.CharacterName, "Use E on " + targets.CharacterName));
            }

            Config.Add(menuE);

            menuK = new Menu("Killsteal", "Killsteal Settings");
            menuK.Add(new MenuBool("KsQ", "Enable Q Killsteal"));
            Config.Add(menuK);

            menuL = new Menu("Clear", "Clear Settings");
            menuL.Add(new MenuSeparator("JungleClear", "Jungle Clear"));
            menuL.Add(new MenuBool("JcQ", "Use Q to Jungle Clear", true));
            menuL.Add(new MenuKeyBind("JcW", "Use W to Jungle Clear", Keys.J, KeyBindType.Toggle)).AddPermashow();
            Config.Add(menuL);

            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuBool("AG", "AntiGapcloser", true));
            Config.Add(menuM);

            menuD = new Menu("Draw", "Draw Settings");
            menuD.Add(new MenuSeparator("mm", "Main Draw Settings"));
            menuD.Add(new MenuList("mode", "Draw Mode",
                new string[] { "static", "Rainbow" }, 0));
            menuD.Add(new MenuSlider("speed", "Change Rainbow Speed", 1000, 500, 1500));
            menuD.Add(new MenuSeparator("qq", "Q Draw Settings"));
            menuD.Add(new MenuBool("drawQ", "Draw Q Range", true));
            menuD.Add(new MenuColor("colorQ", "Change Q Draw Color", Color.White));
            Config.Add(menuD);

            Config.Attach();

            Game.OnUpdate += OnTick;
            GameEvent.OnGameTick += OnUpdate;
            Orbwalker.OnAfterAttack += OnAfterAA;
            Orbwalker.OnBeforeAttack += OnBeforeAA;
            AntiGapcloser.OnGapcloser += Gap;
            AIBaseClient.OnProcessSpellCast += OnProcess;
            AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
            AIBaseClient.OnProcessSpellCast += OnProcessSpellCastW;
            AIBaseClient.OnDoCast += OnProcessSpellCastInstant;
            AIBaseClient.OnDoCast += OnProcessSpellCastInstantW;
            Spellbook.OnCastSpell += OncastSpell;
            AIBaseClient.OnProcessSpellCast += OnProcessSpelllcast;
            Render.OnDraw += OnDraw;
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void OnTick(EventArgs args)
        {
            LogicWBlock();
            Killsteal();

            if (Config["Qsettings"].GetValue<MenuKeyBind>("SemiQ").Active)
            {
                SemiQ();
            }

            if (Config["Qsettings"].GetValue<MenuKeyBind>("dash").Active)
            {
                OnDash();
            }
            
            if (Config["Qsettings"].GetValue<MenuKeyBind>("flash").Active)
            {
                OnFlash();
            }

            var target = TargetSelector.GetTarget(Me.GetRealAutoAttackRange() - 15, DamageType.Physical);
            if (Me.HasBuff("Meditate") && target.IsValidTarget() && target != null)
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Me.ServerPosition.Extend(Game.CursorPos, 25));
            }

            foreach (var entry in IncDamage.Where(entry => entry.Key < Game.Time).ToArray())
            {
                IncDamage.Remove(entry.Key);
            }
            
            foreach (var entry in InstDamage.Where(entry => entry.Key < Game.Time).ToArray())
            {
                InstDamage.Remove(entry.Key);
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicQ();
            }
        }

        private static void LogicQ()
        {
            var useQ = Config["Qsettings"].GetValue<MenuBool>("useQ");
            var turret = Config["Qsettings"].GetValue<MenuKeyBind>("Tower");
            var qtarget = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

            if (!turret.Active && qtarget != null && qtarget.IsUnderEnemyTurret())
            {
                return;
            }

            if (useQ.Enabled && Q.IsReady())
            {
                if (qtarget != null && qtarget.IsValidTarget())
                {
                    switch (comb(menuQ, "Qmode"))
                    {
                        case 0:
                            if (!Me.IsWindingUp)
                            {
                                Q.Cast(qtarget);
                            }

                            break;

                        case 1:
                            if (QDamage(qtarget) + Me.GetAutoAttackDamage(qtarget) * 2 >= qtarget.Health)
                            {
                                Q.Cast(qtarget);
                            }

                            break;

                        case 2:
                            if (!qtarget.InRange(Me.GetRealAutoAttackRange()))
                            {
                                Q.Cast(qtarget);
                            }

                            break;
                    }
                }
            }
        }

        private static void LogicW()
        {
            var aareset = Config["Wsettings"].GetValue<MenuKeyBind>("WAA");

            if (W.IsReady() && aareset.Active)
            {
                var wtarget = TargetSelector.GetTarget(Me.GetRealAutoAttackRange() - 15, DamageType.Physical);
                if (wtarget != null && wtarget.IsValidTarget())
                {
                    W.Cast();
                }
            }
        }

        private static void LogicWBlock()
        {
            var block = Config["Wsettings"].GetValue<MenuBool>("WBlock").Enabled;
            var blockhp = Config["Wsettings"].GetValue<MenuSlider>("MinDamage").Value;

            if (W.IsReady() && block)
            {
                if (IncomingDamage >= blockhp)
                {
                    W.Cast();
                }
            }
        }

        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("useE");

            if (E.IsReady() && useE.Enabled)
            {
                var etarget = TargetSelector.GetTarget(Me.GetRealAutoAttackRange(), DamageType.Physical);
                if (etarget.IsValidTarget() && etarget != null)
                {
                    if (Config["Esettings"].GetValue<MenuBool>(etarget.CharacterName).Enabled)
                    {
                        E.Cast();
                    }
                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
            var rainspeed = Config["Draw"].GetValue<MenuSlider>("speed");

            colorindex++;
            if (colorindex >= rainspeed.Value)
                colorindex = 0;

            switch (comb(menuD, "mode"))
            {
                case 0:
                    if (Config["Draw"].GetValue<MenuBool>("drawQ").Enabled && Q.Level > 0)
                    {
                        var colorQ = Config["Draw"].GetValue<MenuColor>("colorQ").Color;
                        CircleRender.Draw(Me.Position, Q.Range, colorQ, 2);
                    }
                    break;

                case 1:
                    if (Config["Draw"].GetValue<MenuBool>("drawQ").Enabled && Q.Level > 0)
                    {
                        var colorQ = Base.PlusRender.GetFullColorList(rainspeed.Value);
                        CircleRender.Draw(Me.Position, Q.Range, colorQ[colorindex], 2);
                    }
                    break;
            }
        }

        private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (!Q.IsReady() || !sender.IsEnemy || !Config["Qsettings"].GetValue<MenuBool>("masterswitch").Enabled)
            {
                return;
            }

            var attacker = ObjectManager.Get<AIHeroClient>().FirstOrDefault(x => x.NetworkId == sender.NetworkId);
            if (attacker != null)
            {
                foreach (var libary in TargetDataBAse.GDLIST.Where(x => x.HeroName == attacker.CharacterName && x.Slot == attacker.GetSpellSlot(args.SData.Name)))
                {
                    if (libary.Type == Skilltype.Unit && args.Target.IsMe)
                    {
                        if (Config["Qsettings"].GetValue<MenuBool>(libary.SDataName).Enabled)
                        {
                            Console.WriteLine(libary.SDataName + " Detected");
                            if (sender.InRange(Q.Range))
                            {
                                {
                                    Q.Cast(sender);
                                    Console.WriteLine("Casting Q on: " + sender.CharacterName);
                                }
                            }
                            else if (!sender.InRange(Q.Range))
                            {
                                foreach (var validtargets in GameObjects.EnemyHeroes.Where(x => x.IsEnemy))
                                {
                                    Q.Cast(validtargets);
                                    Console.WriteLine("Casting Q on: " + validtargets.CharacterName);
                                }
                            }
                            else if (Me.CountEnemyHeroesInRange(Q.Range) == 0)
                            {
                                foreach (var minions in GameObjects.EnemyMinions.Where(x => x.IsMinion() && x.IsEnemy))
                                {
                                    if (minions.InRange(Q.Range))
                                    {
                                        Q.Cast(minions);
                                        Console.WriteLine("Casting Q on Minion!");
                                    }
                                }
                            }
                        }
                    }
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
                if (Q.IsReady() && ksQ)
                {
                    if (Qtarget != null)
                    {
                        if (Qtarget.DistanceToPlayer() <= Q.Range)
                        {
                            if (Qtarget.Health + Qtarget.AllShield + Qtarget.HPRegenRate <= QDamage(Qtarget))
                            {
                                Q.Cast(Qtarget);
                            }
                        }
                    }
                }
            }
        }

        private static void Jungleclear()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ").Enabled;
            var JcWw = Config["Clear"].GetValue<MenuKeyBind>("JcW").Active;
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(W.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq && Q.IsReady() && Me.Distance(mob.Position) < Me.GetRealAutoAttackRange()) Q.Cast(mob);
                if (JcWw && W.IsReady() && Me.Distance(mob.Position) < Me.GetRealAutoAttackRange() && !Me.IsWindingUp) W.Cast();
            }
        }

        private static void OnProcessSpellCastInstant(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (!Q.IsReady() || !sender.IsEnemy || !Config["Qsettings"].GetValue<MenuBool>("masterswitch").Enabled)
            {
                return;
            }

            var attacker = ObjectManager.Get<AIHeroClient>().FirstOrDefault(x => x.NetworkId == sender.NetworkId);
            if (attacker != null)
            {
                foreach (var libary in TargetDataBAse.GDLIstInstant.Where(x => x.HeroName == attacker.CharacterName && x.Slot == attacker.GetSpellSlot(args.SData.Name)))
                {
                    if (libary.Type == Skilltype.Unit && args.Target.IsMe)
                    {
                        if (Config["Qsettings"].GetValue<MenuBool>(libary.SDataName).Enabled)
                        {
                            Console.WriteLine(libary.SDataName + " Detected");
                            if (sender.InRange(Q.Range))
                            {
                                DelayAction.Add(150, () =>
                                {
                                    Q.Cast(sender);
                                    Console.WriteLine("Casting Q on: " + sender.CharacterName);
                                });
                            }
                            else if (!sender.InRange(Q.Range))
                            {
                                foreach (var validtargets in GameObjects.EnemyHeroes.Where(x => x.IsEnemy))
                                {
                                    Q.Cast(validtargets);
                                    Console.WriteLine("Casting Q on: " + validtargets.CharacterName);
                                }
                            }
                            else if (Me.CountEnemyHeroesInRange(Q.Range) == 0)
                            {
                                foreach (var minions in GameObjects.EnemyMinions.Where(x => x.IsMinion() && x.IsEnemy))
                                {
                                    if (minions.InRange(Q.Range))
                                    {
                                        Q.Cast(minions);
                                        Console.WriteLine("Casting Q on Minion!");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void OnProcessSpellCastInstantW(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (Q.IsReady() || !W.IsReady() || !sender.IsEnemy || !Config["Wsettings"].GetValue<MenuBool>("masterswitch").Enabled)
            {
                return;
            }

            var attacker = ObjectManager.Get<AIHeroClient>().FirstOrDefault(x => x.NetworkId == sender.NetworkId);
            if (attacker != null)
            {
                foreach (var libary in TargetDataBAse.GDLIstInstant.Where(x => x.HeroName == attacker.CharacterName && x.Slot == attacker.GetSpellSlot(args.SData.Name)))
                {
                    if (libary.Type == Skilltype.Unit && args.Target.IsMe)
                    {
                        if (Config["Wsettings"].GetValue<MenuBool>(libary.SDataName).Enabled)
                        {
                            Console.WriteLine(libary.SDataName + " Detected");
                            Console.WriteLine("Casting W");
                            W.Cast();
                        }
                    }
                }
            }
        }

        private static void OnProcessSpellCastW(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (Q.IsReady() || !W.IsReady() || !sender.IsEnemy || !Config["Wsettings"].GetValue<MenuBool>("masterswitch").Enabled)
            {
                return;
            }

            var attacker = ObjectManager.Get<AIHeroClient>().FirstOrDefault(x => x.NetworkId == sender.NetworkId);
            if (attacker != null)
            {
                foreach (var libary in TargetDataBAse.GDLIST.Where(x => x.HeroName == attacker.CharacterName && x.Slot == attacker.GetSpellSlot(args.SData.Name)))
                {
                    if (libary.Type == Skilltype.Unit && args.Target.IsMe)
                    {
                        if (Config["Wsettings"].GetValue<MenuBool>(libary.SDataName).Enabled)
                        {
                            Console.WriteLine(libary.SDataName + " Detected");
                            Console.WriteLine("Casting W");
                            W.Cast();
                        }
                    }
                }
            }
        }

        private static readonly float[] QBaseDamage = { 0f, 52.5f, 105f, 157.5f, 210f, 262.5f, 262.5f };

        private static float QDamage(AIBaseClient target)
        {
            var qlevel = Q.Level;
            var qBaseDamage = QBaseDamage[qlevel] + .87f * Me.TotalAttackDamage;
            return (float)Me.CalculateDamage(target, DamageType.Physical, qBaseDamage);
        }

        private static void OncastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot == SpellSlot.W)
            {
                Orbwalker.ResetAutoAttackTimer();
                Console.WriteLine("Resetting AA");
            }
        }

        private static void Gap(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("AG").Enabled)
            {
                if (Q.IsReady())
                {
                    if (sender.IsValid && sender.IsEnemy)
                    {
                        if (args.EndPosition.DistanceToPlayer() < 350)
                        {
                            Q.Cast(sender);
                        }
                    }
                }
            }
        }

        private static void OnAfterAA(object sender, AfterAttackEventArgs args)
        {
            LogicW();

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Jungleclear();
            }
        }

        private static void OnBeforeAA(object sender, BeforeAttackEventArgs args)
        {
            LogicE();
        }

        private static void OnDash()
        {
            if (Q.IsReady())
            {
                foreach (var Enemies in GameObjects.EnemyHeroes.Where(x => x.IsEnemy && x.IsDashing() && x.InRange(Q.Range)))
                {
                    Q.Cast(Enemies);
                }
            }
        }

        private static void OnFlash()
        {
            var QFlash = Config["Qsettings"].GetValue<MenuKeyBind>("flash");
            
            if (Q.IsReady() && QFlash.Active)
            {
                var qtarget = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                if (qtarget != null && qtarget.IsValidTarget())
                {
                    if (qtarget.GetLastCastedSpell().Name == "SummonerFlash")
                    {
                        if (qtarget.InRange(Q.Range))
                        {
                            Q.Cast(qtarget);
                        }
                    }
                }
            }
        }

        private static void OnProcessSpelllcast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsEnemy)
            {
                if (Orbwalker.IsAutoAttack(args.SData.Name) && args.Target != null &&
                    args.Target.NetworkId == Me.NetworkId)
                {
                    IncDamage[Me.ServerPosition.Distance(sender.ServerPosition) / args.SData.MissileSpeed + Game.Time] =
                        (float)sender.GetAutoAttackDamage(Me);
                }
                else
                {
                    var attacker = sender as AIHeroClient;
                    if (attacker != null)
                    {
                        var slot = attacker.GetSpellSlotFromName(args.SData.Name);

                        if (slot != SpellSlot.Unknown)
                        {
                            if (slot == attacker.GetSpellSlotFromName("SummonerDot") && args.Target != null &&
                                args.Target.NetworkId == Me.NetworkId)
                            {
                                // Ingite damage (dangerous)
                                InstDamage[Game.Time + 2] =
                                    (float)attacker.GetSummonerSpellDamage(Me, SummonerSpell.Ignite);
                            }
                            else
                            {
                                switch (slot)
                                {
                                    case SpellSlot.Q:
                                    case SpellSlot.W:
                                    case SpellSlot.E:
                                    case SpellSlot.R:
                                        if ((args.Target != null && args.Target.NetworkId == Me.NetworkId) ||
                                            args.End.Distance(Me.ServerPosition) <
                                            Math.Pow(args.SData.LineWidth, 1))
                                        {
                                            // Instant damage to target
                                            InstDamage[Game.Time + 2] = (float)attacker.GetSpellDamage(Me, slot);
                                        }

                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static float IncomingDamage
        {
            get { return IncDamage.Sum(e => e.Value) + InstDamage.Sum(e => e.Value); }
        }

        private static void SemiQ()
        {
            if (Q.IsReady())
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                if (target != null && target.IsValidTarget())
                {
                    if (Config["Qsettings"].GetValue<MenuBool>(target.CharacterName).Enabled)
                    {
                        Q.Cast(target);
                    }
                }
            }
        }

        private static void OnProcess(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            var turret = Config["Qsettings"].GetValue<MenuBool>("turrets");
            var hp = Config["Qsettings"].GetValue<MenuSlider>("turretdogehp");

            if (Q.IsReady() && turret.Enabled)
            {
                if (sender is AITurretClient && sender.IsEnemy)
                {
                    if (args.Target.IsMe)
                    {
                        if (Me.CountEnemyHeroesInRange(Q.Range) == 0)
                        {
                            foreach (var minion in GameObjects.EnemyMinions.Where(x => x.IsEnemy && x.InRange(Q.Range)))
                            {
                                if (Me.HealthPercent <= hp.Value)
                                {
                                    DelayAction.Add(150, () =>
                                    {
                                        Q.Cast(minion);
                                        Console.WriteLine("Tower Shot Found");
                                    });
                                }
                            }
                        }

                        if (Me.CountEnemyHeroesInRange(Q.Range) > 0)
                        {
                            foreach (var enemys in GameObjects.EnemyHeroes.Where(x => x.IsEnemy && x.InRange(Q.Range)))
                            {
                                if (Me.HealthPercent <= hp.Value)
                                {
                                    DelayAction.Add(150, () =>
                                    {
                                        Q.Cast(enemys);
                                        Console.WriteLine("Tower Shot Found");
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}