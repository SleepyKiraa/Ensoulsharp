using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Utils.Oktw;
using SharpDX;
using HealthPrediction = EnsoulSharp.SDK.HealthPrediction;

namespace Fatality.Utils
{
    public class Activator
    {
        private static Menu Config, OffensiveItems, DefensiveItems, Summoners, Inting;
        private static AIHeroClient Me = ObjectManager.Player;
        private static Items.Item Goredrinker, GaleForce, EverFrost, Ironspike, Stride, Claw, Qss, Mercurial, Rocketbelt, Healthpotion, RefillPotion, CorruptingPoition, Zhonyas;
        private static SpellSlot  ignite, cleanse, exhaust, smite, heal, barrier;
        private static readonly Dictionary<float, float> IncDamage = new Dictionary<float, float>();
        private static readonly Dictionary<float, float> InstDamage = new Dictionary<float, float>();

        public static void OnGameLoad()
        {
            Ironspike = new Items.Item(6029, 450f);
            Goredrinker = new Items.Item(6630, 450f);
            EverFrost = new Items.Item(6656, 900f);
            GaleForce = new Items.Item(6671, 425f);
            Stride = new Items.Item(6631, 450f);
            Claw = new Items.Item(6693, 500f);
            Qss = new Items.Item(3140, 0f);
            Mercurial = new Items.Item(3139, 0f);
            Rocketbelt = new Items.Item(3152, 1000f);
            Healthpotion = new Items.Item(2003, 0f);
            RefillPotion = new Items.Item(2031, 0f);
            CorruptingPoition = new Items.Item(2033, 0f);
            Zhonyas = new Items.Item(3157, 0f);

            ignite = Me.GetSpellSlot("SummonerDot");
            cleanse = Me.GetSpellSlot("SummonerBoost");
            exhaust = Me.GetSpellSlot("SummonerExhaust");
            heal = Me.GetSpellSlot("SummonerHeal");
            barrier = Me.GetSpellSlot("SummonerBarrier");
            smite = Me.GetSpellSlot("SummonerSmite");

            if (Me.HasBuff("SmiteDamageTrackerStalker"))
            {
                smite = Me.GetSpellSlot("S5_SummonerSmitePlayerGanker");
            }
            else if (Me.HasBuff("SmiteDamageTrackerAvatar"))
            {
                smite = Me.GetSpellSlot("SummonerSmiteAvatarOffensive");
            }

            var smiteslot = Me.Spellbook.Spells.FirstOrDefault(b => b.Name.ToLower().Contains("smite"));

            Config = new Menu("Activator", "[Fatality] Activator", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Activator));

            OffensiveItems = new Menu("OffensiveItems", "Offensive Items");
            OffensiveItems.Add(new MenuSeparator("Ever", "Everfrost"));
            OffensiveItems.Add(new MenuBool("useever", "Enable Everfrost"));
            OffensiveItems.Add(new MenuSeparator("force", "Galeforce"));
            OffensiveItems.Add(new MenuBool("usegale", "Enable Galeforce"));
            OffensiveItems.Add(new MenuSlider("galepercent", "Hp % to use Galeforce", 50, 1, 100));
            OffensiveItems.Add(new MenuSeparator("drinker", "Goredrinker"));
            OffensiveItems.Add(new MenuBool("usewhip", "Enable Ironspike Whip"));
            OffensiveItems.Add(new MenuBool("useDrinker", "Enable Goredrinker"));
            OffensiveItems.Add(new MenuSeparator("claw", "Prowlers Claw"));
            OffensiveItems.Add(new MenuBool("useclaw", "Enable Prowlers Claw"));
            OffensiveItems.Add(new MenuBool("clawsafe", "Enable Prowlers Claw Safe Mode"));
            OffensiveItems.Add(new MenuSeparator("breaker", "Stridebreaker"));
            OffensiveItems.Add(new MenuBool("usebreaker", " Enable Stridebreaker"));
            OffensiveItems.Add(new MenuSeparator("belt", "Hextech Rocketbelt"));
            OffensiveItems.Add(new MenuBool("useBelt", "Enable Hextech Rocketbelt"));
            Config.Add(OffensiveItems);
            
            DefensiveItems = new Menu("DefensiveItems", "Defensive Items");
            DefensiveItems.Add(new MenuSeparator("Qss", "Qss"));
            DefensiveItems.Add(new MenuBool("useqss", "Enable Qss"));
            DefensiveItems.Add(new MenuBool("qssStun", "Use Qss For Stun"));
            DefensiveItems.Add(new MenuBool("qssSnare", "Use Qss For Snare"));
            DefensiveItems.Add(new MenuBool("qssCharm", "Use Qss For Charm"));
            DefensiveItems.Add(new MenuBool("qssTaunt", "Use Qss for Taunt"));
            DefensiveItems.Add(new MenuSeparator("pp", "Potions"));
            DefensiveItems.Add(new MenuBool("HealthPot", "Use Normal Health Potion"));
            DefensiveItems.Add(new MenuBool("RefillablePot", "Use Refill Potion"));
            DefensiveItems.Add(new MenuBool("CorruptionPot", "Use Corrupting Potion"));
            DefensiveItems.Add(new MenuBool("base", "Dont use Potions on Fountain"));
            DefensiveItems.Add(new MenuSlider("PotHp", "Health % To use Potions", 50, 1, 100));
            DefensiveItems.Add(new MenuSeparator("zz", "Zhonya's "));
            DefensiveItems.Add(new MenuBool("useZH", "Enable Auto Zhonya"));
            DefensiveItems.Add(new MenuSlider("damage", "Min Incomming Damage to Auto Zhonya", 500, 100, 1500));
            DefensiveItems.Add(new MenuSeparator("zzzz", "Zhonyas Dangerous Spells Block"));
            foreach (var Enemys in GameObjects.EnemyHeroes.Where(x => x.IsEnemy))
            {
                if (Enemys.CharacterName == "Darius")
                {
                    DefensiveItems.Add(new MenuBool("DariusR", "Block Darius R"));
                }

                if (Enemys.CharacterName == "Garen")
                {
                    DefensiveItems.Add(new MenuBool("GarenR", "Block Garen R"));
                }

                if (Enemys.CharacterName == "Leesin")
                {
                    DefensiveItems.Add(new MenuBool("LeesinR", "Block Leesin R"));
                }
                
                if (Enemys.CharacterName == "Tristana")
                {
                    DefensiveItems.Add(new MenuBool("TristanaR", "Block Tristana R"));
                }
                
                if (Enemys.CharacterName == "Veigar")
                {
                    DefensiveItems.Add(new MenuBool("VeigarR", "Block Veigar R"));
                }
                
            }
            Config.Add(DefensiveItems);
            
            Summoners = new Menu("Summoners", "Summoner Settings");
            if (ignite != SpellSlot.Unknown)
            {
                Summoners.Add(new MenuSeparator("Ignite", "Ignite"));
                Summoners.Add(new MenuBool("useignite", "use Ignite To Kill Target"));
            }

            if (cleanse != SpellSlot.Unknown)
            {
                Summoners.Add(new MenuSeparator("Cleanse", "Cleanse"));
                Summoners.Add(new MenuBool("usecleanse", "Use Cleanse for CC"));
            }

            if (exhaust != SpellSlot.Unknown)
            {
                Summoners.Add(new MenuSeparator("Exhaust", "Exhaust"));
                Summoners.Add(new MenuBool("useExhaust", "use Exhaust", true));
                Summoners.Add(new MenuKeyBind("keyy", "Semi Exhaust Key", Keys.G, KeyBindType.Press));
            }

            if (heal != SpellSlot.Unknown)
            {
                Summoners.Add(new MenuSeparator("Heal", "Heal"));
                Summoners.Add(new MenuBool("useHeal", "Use Heal"));
                Summoners.Add(new MenuSlider("HealHP", "HP To use Heal", 200, 100, 1000));
            }

            if (barrier != SpellSlot.Unknown)
            {
                Summoners.Add(new MenuSeparator("Barrier", "Barrier"));
                Summoners.Add(new MenuBool("useBarrier", "Use Barrier"));
                Summoners.Add(new MenuSlider("BarrierHP", "HP To use Barrier", 200, 100, 1000));
            }

            if (smite != SpellSlot.Unknown)
            {
                Summoners.Add(new MenuSeparator("Smite", "Smite Settings"));
                Summoners.Add(new MenuKeyBind("usesmite", "Enable Smite", Keys.H, KeyBindType.Toggle)).AddPermashow();
                Summoners.Add(new MenuBool("Baron", "Smite Baron"));
                Summoners.Add(new MenuBool("Herald", "Smite Herald"));
                Summoners.Add(new MenuBool("Dragon", "Smite Dragons"));
                Summoners.Add(new MenuBool("Red", "Smite Red Buff"));
                Summoners.Add(new MenuBool("Blue", "Smite Blue Buff"));
                Summoners.Add(new MenuBool("Gromp", "Smite Gromp Buff"));
                Summoners.Add(new MenuBool("Wolves", "Smite Wolves Buff"));
                Summoners.Add(new MenuBool("Raptors", "Smite Raptors Buff"));
                Summoners.Add(new MenuBool("Krugs", "Smite Krugs Buff"));
                Summoners.Add(new MenuBool("Crab", "Smite Crab"));
            }
            Config.Add(Summoners);

            Inting = new Menu("Inting", "Auto Inting");
            Inting.Add(new MenuBool("Down", "Enable To Auto Running It Down Mid", false));
            Inting.Add(new MenuList("Mode", "Inting Mode",
                new string[] {"Base", "Ally (Only Enable 1 Ally)"}, 1));
            Inting.Add(new MenuSeparator("allylist", "Allys"));
            foreach (var ally in GameObjects.AllyHeroes.Where(x => x.IsAlly && !x.IsMe))
            {
                Inting.Add(new MenuBool(ally.CharacterName, ally.CharacterName, false));
            }
            Config.Add(Inting);

            Config.Attach();
            AIBaseClient.OnProcessSpellCast += OnProcessSpellcast;
            GameEvent.OnGameTick += OnUpdate;
            Game.OnUpdate += ontick;
            AIBaseClient.OnDoCast += SpellBlock;

        }

        private static void ontick(EventArgs args)
        {
            Qsss();
            Zhonyass();
            ignitekill();
            Smite();
            Heal();
            Barrier();
            AutoPot();

            if (exhaust != SpellSlot.Unknown)
            {
                if (Config["Summoners"].GetValue<MenuKeyBind>("keyy").Active)
                {
                    Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                    exhaustt();
                }
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
                IronspikeWhip();
                Goredrinkerr();
                GaleForcee();
                HextechBelt();
                StrideBreakerr();
                Everfrostt();
                Claww();
            }           
            cleanseCC();
            L9();
        }
        
        private static void IronspikeWhip()
        {
            var IronSpikee = TargetSelector.GetTarget(445f, DamageType.Physical);
            var useWhip = Config["OffensiveItems"].GetValue<MenuBool>("usewhip").Enabled;

            if (useWhip && Ironspike.IsReady)
            {
                if (IronSpikee != null)
                {
                    Ironspike.Cast();
                }
            }
        }
        
        private static void Goredrinkerr()
        {
            var gordetarget = TargetSelector.GetTarget(445f, DamageType.Physical);
            var usedrinkerr = Config["OffensiveItems"].GetValue<MenuBool>("useDrinker").Enabled;

            if (usedrinkerr && Goredrinker.IsReady)
            {
                if (gordetarget != null)
                {
                    Goredrinker.Cast();
                }
            }
        }

        private static void GaleForcee()
        {
            var galetarget = TargetSelector.GetTarget(750f, DamageType.Magical);
            var usegalee = Config["OffensiveItems"].GetValue<MenuBool>("usegale").Enabled;
            var galehp = Config["OffensiveItems"].GetValue<MenuSlider>("galepercent").Value;

            if (usegalee && GaleForce.IsReady)
            {
                if (galetarget != null)
                {
                    if (galetarget.HealthPercent <= galehp)
                    {
                        GaleForce.Cast(galetarget.Position);
                    }
                }
            }
        }

        private static void HextechBelt()
        {
            var belttarget = TargetSelector.GetTarget(995f, DamageType.Magical);
            var usebelt = Config["OffensiveItems"].GetValue<MenuBool>("useBelt").Enabled;

            if (usebelt && Rocketbelt.IsReady)
            {
                if (belttarget != null)
                {
                    if (belttarget.HealthPercent <= 15)
                    {
                        Rocketbelt.Cast(belttarget.Position);
                    }
                }
            }
        }

        private static void StrideBreakerr()
        {
            var stridetarget = TargetSelector.GetTarget(445f, DamageType.Physical);
            var usestride = Config["OffensiveItems"].GetValue<MenuBool>("usebreaker").Enabled;

            if (usestride && Stride.IsReady)
            {
                if (stridetarget != null)
                {
                    Stride.Cast(stridetarget.Position);
                }
            }
        }
        
        private static void Claww()
        {
            var clawtarget = TargetSelector.GetTarget(500f, DamageType.Physical);
            var useclaw = Config["OffensiveItems"].GetValue<MenuBool>("useclaw");
            var safe = Config["OffensiveItems"].GetValue<MenuBool>("clawsafe");
            if (clawtarget == null) return;

            if (clawtarget.IsValidTarget(500f) && useclaw.Enabled && !safe.Enabled)
            {
                Claw.Cast(clawtarget);
            }
            else if (clawtarget.IsValidTarget(500f) && useclaw.Enabled && safe.Enabled)
            {
                if (Me.HealthPercent >= clawtarget.HealthPercent)
                {
                    Claw.Cast(clawtarget);
                }
            }
        }
        
        private static void Everfrostt()
        {
            var evertarget = TargetSelector.GetTarget(850f, DamageType.Magical);
            var useever = Config["OffensiveItems"].GetValue<MenuBool>("useever");
            if (evertarget == null) return;

            if (evertarget.IsValidTarget(850f) && useever.Enabled)
            {
                if (!evertarget.HasBuffOfType(BuffType.Berserk) || !evertarget.HasBuffOfType(BuffType.SpellImmunity) ||
                    !evertarget.HasBuffOfType(BuffType.SpellShield) || !evertarget.HasBuffOfType(BuffType.Stun) ||
                    !evertarget.HasBuffOfType(BuffType.Snare))
                {
                    EverFrost.Cast(evertarget.Position);
                }
            }
        }
        
        private static void Qsss()
        {
            var useqss = Config["DefensiveItems"].GetValue<MenuBool>("useqss");
            var useqssstun = Config["DefensiveItems"].GetValue<MenuBool>("qssStun");
            var useqssSnare = Config["DefensiveItems"].GetValue<MenuBool>("qssSnare");
            var useqssCharm = Config["DefensiveItems"].GetValue<MenuBool>("qssCharm");
            var useqssTaunt = Config["DefensiveItems"].GetValue<MenuBool>("qssTaunt");

            if (cleanse.IsReady())
            {
                return;
            }

            if (useqss.Enabled && Me.HasBuffOfType(BuffType.Charm) && useqssCharm.Enabled)
            {
                Qss.Cast();
                Mercurial.Cast();
            }
            
            if (useqss.Enabled && Me.HasBuffOfType(BuffType.Stun) && useqssstun.Enabled)
            {
                Qss.Cast();
                Mercurial.Cast();
            }
            
            if (useqss.Enabled && Me.HasBuffOfType(BuffType.Snare) && useqssSnare.Enabled)
            {
                Qss.Cast();
                Mercurial.Cast();
            }
            
            if (useqssTaunt.Enabled && Me.HasBuffOfType(BuffType.Taunt))
            {
                Qss.Cast();
                Mercurial.Cast();
            }
        }
        
        private static void ignitekill()
        {
            if (ignite == SpellSlot.Unknown)
            {
                return;
            }
            
            var useIgnite = Config["Summoners"].GetValue<MenuBool>("useignite");
            var hero = GameObjects.EnemyHeroes.FirstOrDefault(x => x.IsValidTarget(600f));
            var ignitedamage = Me.GetSummonerSpellDamage(hero, SummonerSpell.Ignite);
            if (hero == null) return;

            if (useIgnite.Enabled && hero.IsValidTarget(600f) && ignitedamage >= hero.Health + hero.AllShield && ignite.IsReady())
            {
                Me.Spellbook.CastSpell(ignite, hero);
            }
        }
        
        private static void cleanseCC()
        {
            if (cleanse == SpellSlot.Unknown)
            {
                return;
            }
            
            var useCleanse = Config["Summoners"].GetValue<MenuBool>("usecleanse");

            if (useCleanse.Enabled && cleanse.IsReady())
            {
                if (Me.HasBuffOfType(BuffType.Knockback) || Me.HasBuffOfType(BuffType.Knockup))
                {
                    return;
                }

                if (Me.HasBuffOfType(BuffType.Charm) || Me.HasBuffOfType(BuffType.Fear) ||
                    Me.HasBuffOfType(BuffType.Snare) || Me.HasBuffOfType(BuffType.Stun) ||
                    Me.HasBuffOfType(BuffType.Suppression) || Me.HasBuffOfType(BuffType.Taunt))
                {                        
                    Me.Spellbook.CastSpell(cleanse);
                }
            }
        }

        private static void exhaustt()
        {
            if (exhaust == SpellSlot.Unknown)
            {
                return;
            }
            
            var useexhaust = Config["Summoners"].GetValue<MenuBool>("useExhaust").Enabled;
            var exhaustTarget = TargetSelector.GetTarget(650, DamageType.Physical);

            if (exhaust.IsReady() && useexhaust)
            {
                if (exhaustTarget != null)
                {
                    Me.Spellbook.CastSpell(exhaust, exhaustTarget);
                }
            }
        }

        private static void Heal()
        {
            if (heal == SpellSlot.Unknown)
            {
                return;
            }

            var heall = Config["Summoners"].GetValue<MenuBool>("useHeal");
            var hpvalue = Config["Summoners"].GetValue<MenuSlider>("HealHP");

            if (heall.Enabled && heal.IsReady())
            {
                if (HealthPrediction.GetPrediction(Me, 0) <= hpvalue.Value)
                {
                    Me.Spellbook.CastSpell(heal);
                }
            }

        }
        
        private static void Barrier()
        {
            if (barrier == SpellSlot.Unknown)
            {
                return;
            }

            var barrierr = Config["Summoners"].GetValue<MenuBool>("useBarrier");
            var hpvalue = Config["Summoners"].GetValue<MenuSlider>("BarrierHP");

            if (barrierr.Enabled && barrier.IsReady())
            {
                if (HealthPrediction.GetPrediction(Me, 0) <= hpvalue.Value)
                {
                    Me.Spellbook.CastSpell(barrier);
                }
            }

        }

        private static void Zhonyass()
        {
            var enable = Config["DefensiveItems"].GetValue<MenuBool>("useZH");
            var incom = Config["DefensiveItems"].GetValue<MenuSlider>("damage");

            if (enable.Enabled && Zhonyas.IsReady)
            {
                if (IncomingDamage >= incom.Value)
                {
                    Zhonyas.Cast();
                }
            }
        }

        private static void AutoPot()
        {
            var normalpot = Config["DefensiveItems"].GetValue<MenuBool>("HealthPot");
            var refipot = Config["DefensiveItems"].GetValue<MenuBool>("RefillablePot");
            var corruptpot = Config["DefensiveItems"].GetValue<MenuBool>("CorruptionPot");
            var fountain = Config["DefensiveItems"].GetValue<MenuBool>("base");
            var health = Config["DefensiveItems"].GetValue<MenuSlider>("PotHp");

            if (fountain.Enabled && Me.InFountain())
            {
                return;
            }

            if (normalpot.Enabled && Healthpotion.IsReady)
            {
                if (!Me.HasBuff("Item2003") && Me.HealthPercent <= health.Value)
                {
                    Healthpotion.Cast(Me);
                }
            }
            
            if (refipot.Enabled && RefillPotion.IsReady)
            {
                if (!Me.HasBuff("ItemCrystalFlask") && Me.HealthPercent <= health.Value)
                {
                    RefillPotion.Cast(Me);
                }
            }
            
            if (corruptpot.Enabled && CorruptingPoition.IsReady)
            {
                if (!Me.HasBuff("ItemDarkCrystalFlask") && Me.HealthPercent <= health.Value)
                {
                    CorruptingPoition.Cast(Me);
                }
            }
        }

        private static void Smite()
        {
            if (smite == SpellSlot.Unknown)
            {
                return;
            }
            
            var Baron = Config["Summoners"].GetValue<MenuBool>("Baron");
            var Herold = Config["Summoners"].GetValue<MenuBool>("Herald");
            var Dragon = Config["Summoners"].GetValue<MenuBool>("Dragon");
            var Red = Config["Summoners"].GetValue<MenuBool>("Red");
            var Blue = Config["Summoners"].GetValue<MenuBool>("Blue");
            var Crab = Config["Summoners"].GetValue<MenuBool>("Crab");
            var Gromp = Config["Summoners"].GetValue<MenuBool>("Gromp");
            var Ratpors = Config["Summoners"].GetValue<MenuBool>("Raptors");
            var Wolves = Config["Summoners"].GetValue<MenuBool>("Wolves");
            var Krugs = Config["Summoners"].GetValue<MenuBool>("Krugs");
            
            if (Config["Summoners"].GetValue<MenuKeyBind>("usesmite").Active)
            {
                foreach (var minion in GameObjects.Jungle.Where(x => x.IsValidTarget(750f) && IsLegendaryBaron(x)))
                {
                    if (Baron.Enabled)
                    {
                        if (Me.HasBuff("SmiteDamageTracker"))
                        {
                            if (minion.Health <= 600)
                            {
                                Me.Spellbook.CastSpell(smite, minion);
                            }
                        }
                        else if (Me.HasBuff("SmiteDamageTrackerStalker"))
                        {
                            if (minion.Health <= 900)
                            {
                                Me.Spellbook.CastSpell(smite, minion);
                            }
                        }
                        else if (Me.HasBuff("SmiteDamageTrackerAvatar"))
                        {
                            if (minion.Health <= SmiteDamage())
                            {
                                Me.Spellbook.CastSpell(smite, minion);
                            }
                        }
                    }
                }

                foreach (var minion2 in GameObjects.Jungle.Where(x => x.IsValidTarget(750f) && IsLegendaryDragon(x)))
                {
                    if (Dragon.Enabled)
                    {
                        if (Me.HasBuff("SmiteDamageTracker"))
                        {
                            if (minion2.Health <= 600)
                            {
                                Me.Spellbook.CastSpell(smite, minion2);
                            }
                        }
                        else if (Me.HasBuff("SmiteDamageTrackerStalker"))
                        {
                            if (minion2.Health <= 900)
                            {
                                Me.Spellbook.CastSpell(smite, minion2);
                            }
                        }
                        else if (Me.HasBuff("SmiteDamageTrackerAvatar"))
                        {
                            if (minion2.Health <= SmiteDamage())
                            {
                                Me.Spellbook.CastSpell(smite, minion2);
                            }
                        }
                    }
                }

                foreach (var minion3 in GameObjects.Jungle.Where(x => x.IsValidTarget(750f) && IsLegendaryHerald(x)))
                {
                    if (Herold.Enabled)
                    {
                        if (Me.HasBuff("SmiteDamageTracker"))
                        {
                            if (minion3.Health <= 600)
                            {
                                Me.Spellbook.CastSpell(smite, minion3);
                            }
                        }
                        else if (Me.HasBuff("SmiteDamageTrackerStalker"))
                        {
                            if (minion3.Health <= 900)
                            {
                                Me.Spellbook.CastSpell(smite, minion3);
                            }
                        }
                        else if (Me.HasBuff("SmiteDamageTrackerAvatar"))
                        {
                            if (minion3.Health <= SmiteDamage())
                            {
                                Me.Spellbook.CastSpell(smite, minion3);
                            }
                        }
                    }
                }

                foreach (var minion4 in GameObjects.Jungle.Where(x => x.IsValidTarget(750f) && IsRedBuff(x)))
                {
                    if (Red.Enabled)
                    {
                        if (Me.HasBuff("SmiteDamageTracker"))
                        {
                            if (minion4.Health <= 600)
                            {
                                Me.Spellbook.CastSpell(smite, minion4);
                            }
                        }
                        else if (Me.HasBuff("SmiteDamageTrackerStalker"))
                        {
                            if (minion4.Health <= 900)
                            {
                                Me.Spellbook.CastSpell(smite, minion4);
                            }
                        }
                        else if (Me.HasBuff("SmiteDamageTrackerAvatar"))
                        {
                            if (minion4.Health <= SmiteDamage())
                            {
                                Me.Spellbook.CastSpell(smite, minion4);
                            }
                        }
                    }
                }
                
                foreach (var minion5 in GameObjects.Jungle.Where(x => x.IsValidTarget(750f) && IsBlueBuff(x)))
                {
                    if (Blue.Enabled)
                    {
                        if (Me.HasBuff("SmiteDamageTracker"))
                        {
                            if (minion5.Health <= 600)
                            {
                                Me.Spellbook.CastSpell(smite, minion5);
                            }
                        }
                        else if (Me.HasBuff("SmiteDamageTrackerStalker"))
                        {
                            if (minion5.Health <= 900)
                            {
                                Me.Spellbook.CastSpell(smite, minion5);
                            }
                        }
                        else if (Me.HasBuff("SmiteDamageTrackerAvatar"))
                        {
                            if (minion5.Health <= SmiteDamage())
                            {
                                Me.Spellbook.CastSpell(smite, minion5);
                            }
                        }
                    }
                }
                
                foreach (var minion6 in GameObjects.Jungle.Where(x => x.IsValidTarget(750f) && IsCrab(x)))
                {
                    if (Crab.Enabled)
                    {
                        if (Me.HasBuff("SmiteDamageTracker"))
                        {
                            if (minion6.Health <= 600)
                            {
                                Me.Spellbook.CastSpell(smite, minion6);
                            }
                        }
                        else if (Me.HasBuff("SmiteDamageTrackerStalker"))
                        {
                            if (minion6.Health <= 900)
                            {
                                Me.Spellbook.CastSpell(smite, minion6);
                            }
                        }
                        else if (Me.HasBuff("SmiteDamageTrackerAvatar"))
                        {
                            if (minion6.Health <= SmiteDamage())
                            {
                                Me.Spellbook.CastSpell(smite, minion6);
                            }
                        }
                    }
                }
                
                foreach (var minion7 in GameObjects.Jungle.Where(x => x.IsValidTarget(750f) && IsGromp(x)))
                {
                    if (Gromp.Enabled)
                    {
                        if (Me.HasBuff("SmiteDamageTracker"))
                        {
                            if (minion7.Health <= 600)
                            {
                                Me.Spellbook.CastSpell(smite, minion7);
                            }
                        }
                        else if (Me.HasBuff("SmiteDamageTrackerStalker"))
                        {
                            if (minion7.Health <= 900)
                            {
                                Me.Spellbook.CastSpell(smite, minion7);
                            }
                        }
                        else if (Me.HasBuff("SmiteDamageTrackerAvatar"))
                        {
                            if (minion7.Health <= SmiteDamage())
                            {
                                Me.Spellbook.CastSpell(smite, minion7);
                            }
                        }
                    }
                }
                
                foreach (var minion8 in MinionManager.GetMinions(750f, MinionManager.MinionTypes.All, MinionManager.MinionTeam.Neutral).Where(m => !m.Name.Contains("Mini") && m.Name.Contains("Murkwolf") && m.IsValid))
                {
                    if (Wolves.Enabled)
                    {
                        if (Me.HasBuff("SmiteDamageTracker"))
                        {
                            if (minion8.Health <= 600)
                            {
                                Me.Spellbook.CastSpell(smite, minion8);
                            }
                        }
                        else if (Me.HasBuff("SmiteDamageTrackerStalker"))
                        {
                            if (minion8.Health <= 900)
                            {
                                Me.Spellbook.CastSpell(smite, minion8);
                            }
                        }
                        else if (Me.HasBuff("SmiteDamageTrackerAvatar"))
                        {
                            if (minion8.Health <= SmiteDamage())
                            {
                                Me.Spellbook.CastSpell(smite, minion8);
                            }
                        }
                    }
                }
                
                foreach (var minion9 in MinionManager.GetMinions(750f, MinionManager.MinionTypes.All, MinionManager.MinionTeam.Neutral).Where(m => !m.Name.Contains("Mini") && m.Name.Contains("Krug") && m.IsValid))
                {
                    if (Krugs.Enabled)
                    {
                        if (Me.HasBuff("SmiteDamageTracker"))
                        {
                            if (minion9.Health <= 600)
                            {
                                Me.Spellbook.CastSpell(smite, minion9);
                            }
                        }
                        else if (Me.HasBuff("SmiteDamageTrackerStalker"))
                        {
                            if (minion9.Health <= 900)
                            {
                                Me.Spellbook.CastSpell(smite, minion9);
                            }
                        }
                        else if (Me.HasBuff("SmiteDamageTrackerAvatar"))
                        {
                            if (minion9.Health <= SmiteDamage())
                            {
                                Me.Spellbook.CastSpell(smite, minion9);
                            }
                        }
                    }
                }
                
                foreach (var minion10 in MinionManager.GetMinions(750f, MinionManager.MinionTypes.All, MinionManager.MinionTeam.Neutral).Where(m => !m.Name.Contains("Mini") && m.Name.Contains("Razorbeak") && m.IsValid))
                {
                    if (Ratpors.Enabled)
                    {
                        if (Me.HasBuff("SmiteDamageTracker"))
                        {
                            if (minion10.Health <= 600)
                            {
                                Me.Spellbook.CastSpell(smite, minion10);
                            }
                        }
                        else if (Me.HasBuff("SmiteDamageTrackerStalker"))
                        {
                            if (minion10.Health <= 900)
                            {
                                Me.Spellbook.CastSpell(smite, minion10);
                            }
                        }
                        else if (Me.HasBuff("SmiteDamageTrackerAvatar"))
                        {
                            if (minion10.Health <= SmiteDamage())
                            {
                                Me.Spellbook.CastSpell(smite, minion10);
                            }
                        }
                    }
                }
            }
        }

        private static double SmiteDamage()
        {
            var Damage = 0;
            
            if (!Me.HasBuff("SmiteDamageTrackerAvatar"))
            {
                return 0;
            }


            if (Me.HasBuff("SmiteDamageTrackerAvatar"))
            {
                Damage += 1200;
            }

            return Damage;
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void L9()
        {
            switch (comb(Inting, "Mode"))
            {
                case 0:
                    if (Config["Inting"].GetValue<MenuBool>("Down").Enabled)
                    {
                        for (int i = 0; i < Intpos.Count; i++)
                        {
                            if (Me.Team == GameObjectTeam.Order)
                            {
                                Me.IssueOrder(GameObjectOrder.MoveTo, Intpos[i]);
                            }
                        }

                        for (int i = 0; i < Intpos2.Count; i++)
                        {
                            if (Me.Team == GameObjectTeam.Chaos)
                            {
                                Me.IssueOrder(GameObjectOrder.MoveTo, Intpos2[i]);
                            }
                        }

                    }
                    break;

                case 1:
                    if (Config["Inting"].GetValue<MenuBool>("Down").Enabled)
                    {
                        foreach (var allys in GameObjects.AllyHeroes.Where(x => x.IsAlly && !x.IsDead && !x.IsMe))
                        {
                            if (Config["Inting"].GetValue<MenuBool>(allys.CharacterName).Enabled)
                            {
                                if (allys != null)
                                {
                                    Me.IssueOrder(GameObjectOrder.MoveTo, allys.ServerPosition);
                                }
                            }
                        }
                    }
                    break;
            }           
        }

        private static void OnProcessSpellcast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
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

        private static List<Vector3> Intpos = new List<Vector3>()
        {
            new Vector3(14288f, 14280f, 171.9777f)         
        };

        private static List<Vector3> Intpos2 = new List<Vector3>()
        {
            new Vector3(394f, 402f, 1325f)
        };

        private static bool IsLegendaryDragon(AIMinionClient client)
        {
            return client.Name.Contains("Dragon");
        }
        
        private static bool IsLegendaryBaron(AIMinionClient client)
        {
            return client.Name.Contains("Baron");
        }
        
        private static bool IsLegendaryHerald(AIMinionClient client)
        {
            return client.Name.Contains("Herald");
        }

        private static bool IsRedBuff(AIMinionClient client)
        {
            return client.Name.Contains("Red");
        }
        
        private static bool IsBlueBuff(AIMinionClient client)
        {
            return client.Name.Contains("Blue");
        }
        
        private static bool IsCrab(AIMinionClient client)
        {
            return client.Name.Contains("Crab");
        }
        
        private static bool IsGromp(AIMinionClient client)
        {
            return client.Name.Contains("Gromp");
        }

        private static float IncomingDamage
        {
            get { return IncDamage.Sum(e => e.Value) + InstDamage.Sum(e => e.Value); }
        }

        private static void SpellBlock(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            var auto = Config["DefensiveItems"].GetValue<MenuBool>("useZH");

            if (!auto.Enabled || !Zhonyas.IsReady || sender == null || sender.IsAlly)
            {
                return;
            }

            foreach (var enemy in GameObjects.EnemyHeroes.Where(x => x.IsEnemy))
            {
                if (enemy.CharacterName == "Darius")
                {
                    if (Config["DefensiveItems"].GetValue<MenuBool>("DariusR").Enabled)
                    {
                        if (sender.CharacterName == "Darius" && args.Slot == SpellSlot.R)
                        {
                            if (args.Target.IsMe)
                            {
                                Zhonyas.Cast();
                            }
                        }
                    }
                }
                
                if (enemy.CharacterName == "Garen")
                {
                    if (Config["DefensiveItems"].GetValue<MenuBool>("GarenR").Enabled)
                    {
                        if (sender.CharacterName == "Garen" && args.Slot == SpellSlot.R)
                        {
                            if (args.Target.IsMe)
                            {
                                Zhonyas.Cast();
                            }
                        }
                    }
                }
                
                if (enemy.CharacterName == "Leesin")
                {
                    if (Config["DefensiveItems"].GetValue<MenuBool>("LeesinR").Enabled)
                    {
                        if (sender.CharacterName == "Leesin" && args.Slot == SpellSlot.R)
                        {
                            if (args.Target.IsMe)
                            {
                                Zhonyas.Cast();
                            }
                        }
                    }
                }
                
                if (enemy.CharacterName == "Tristana")
                {
                    if (Config["DefensiveItems"].GetValue<MenuBool>("TristanaR").Enabled)
                    {
                        if (sender.CharacterName == "Tristana" && args.Slot == SpellSlot.R)
                        {
                            if (args.Target.IsMe)
                            {
                                Zhonyas.Cast();
                            }
                        }
                    }
                }
                
                if (enemy.CharacterName == "Veigar")
                {
                    if (Config["DefensiveItems"].GetValue<MenuBool>("VeigarR").Enabled)
                    {
                        if (sender.CharacterName == "Veigar" && args.Slot == SpellSlot.R)
                        {
                            if (args.Target.IsMe)
                            {
                                Zhonyas.Cast();
                            }
                        }
                    }
                }
            }
        }
    }
}