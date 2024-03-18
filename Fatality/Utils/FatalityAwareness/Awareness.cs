using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Core;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Newtonsoft.Json;
using Fatality.Utils.FatalityAwareness;
using SharpDX;
using System.Drawing.Text;
using System.Timers;
using EnsoulSharp.SDK.Rendering.Caches;
using EnsoulSharp.SDK.Utility;

namespace Fatality.Utils.FatalityAwareness
{
    public class Awareness
    {
        private static Menu Config, clone, turret, Skin, ene, minion, tracker;
        private static AIHeroClient Me = ObjectManager.Player;

        public static void OnGameLoad()
        {
            Config = new Menu("Awareness", "[Fatality] Awareness", true);
            Config.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Awareness));

            clone = new Menu("Clonetracker", "Clone Tracker");
            clone.Add(new MenuBool("useC", "Enable Clone Tracker"));
            Config.Add(clone);

            Skin = new Menu("Skin", "Skin Changer");
            Skin.Add(new MenuBool("Enable", "Enable Skin Changer"));
            var names = ChampionSkinData.Skins[ObjectManager.Player.CharacterName].Keys.ToArray();
            Skin.Add(new MenuList("skins", "Skins", names));
            Config.Add(Skin);

            var skinList = Config["Skin"].GetValue<MenuList>("skins");

            if (Config["Skin"].GetValue<MenuList>("skins").Index >= skinList.Items.Length)
                Config["Skin"].GetValue<MenuList>("skins").Index = 0;


            turret = new Menu("Turret", "Turret Attack Range");
            turret.Add(new MenuBool("enabled", "Enabled Turret Range Drawings", true));
            turret.Add(new MenuBool("ally", "Draw Ally Turret Attack Ranges", false));
            turret.Add(new MenuBool("enemy", "Draw Enemy Turret Attack Range", true));
            turret.Add(new MenuColor("allycolor", "Ally Turret Attack Range Color", Color.Green));
            turret.Add(new MenuColor("enemycolor", "Turret Attack Range Color", Color.Red));
            Config.Add(turret);

            ene = new Menu("Enemys", "Enemy Drawing");
            ene.Add(new MenuBool("Zhonyas", "Draw Zhonyas Time"));
            Config.Add(ene);

            tracker = new Menu("tracker", "Baron & Dragon Tracker");
            tracker.Add(new MenuBool("barontracker", "Enable Baron Tracker"));
            tracker.Add(new MenuBool("heraldtracker", "Enable Herald Tracker"));
            tracker.Add(new MenuBool("dragontracker", "Enable Dragon Tracker"));
            Config.Add(tracker);

            minion = new Menu("Min", "Drawings");
            minion.Add(new MenuBool("minionkill", "Last Hit Helper"));
            minion.Add(new MenuBool("targets", "Draw Current Target"));
            Config.Add(minion);

            Config.Attach();

            Render.OnDraw += OnDraw;
            AIBaseClient.OnPlayAnimation += JungleTracker;
            Render.OnRenderMouseOvers += OnMouse;
            skinList.ValueChanged += SkinListOnValueChanged;
            Game.OnNotify += OnNotify;
            Me.SetSkin(ChampionSkinData.Skins[Me.CharacterName][skinList.SelectedValue]);
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void OnNotify(GameNotifyEventArgs args)
        {
            switch (args.EventId)
            {
                case GameEventId.OnReincarnate:
                case GameEventId.OnResetChampion:
                    setSkinId();
                    break;
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (Config["Clonetracker"].GetValue<MenuBool>("useC").Enabled)
            {
                foreach (AIHeroClient hero in GameObjects.Get<AIHeroClient>())
                {
                    if (hero.IsEnemy && !hero.IsDead && hero.IsVisible)
                    {
                        if (hero.CharacterName == "Shaco" ||
                            hero.CharacterName == "Leblanc" ||
                            hero.CharacterName == "MonkeyKing" ||
                            hero.CharacterName == "Neeko")
                        {
                            CircleRender.Draw(hero.Position, hero.BoundingRadius + 50, Color.LawnGreen, 3);
                        }
                    }
                }

                foreach (AIMinionClient clones in GameObjects.Get<AIMinionClient>())
                {
                    if (clones.IsEnemy && !clones.IsDead && clones.IsVisible)
                    {
                        if (clones.CharacterName == "Shaco" ||
                            clones.CharacterName == "Leblanc" ||
                            clones.CharacterName == "MonkeyKing" ||
                            clones.CharacterName == "Neeko")
                        {
                            CircleRender.Draw(clones.Position, clones.BoundingRadius + 50, Color.Red, 3);
                        }
                    }
                }
            }

            if (Config["Turret"].GetValue<MenuBool>("enabled").Enabled)
            {
                var Turretcolor = Config["Turret"].GetValue<MenuColor>("enemycolor").Color;
                var Alllycolor = Config["Turret"].GetValue<MenuColor>("allycolor").Color;
                if (Config["Turret"].GetValue<MenuBool>("enemy").Enabled)
                {
                    foreach (var enemyturret in GameObjects.EnemyTurrets.Where(x => x.IsVisibleOnScreen && x.IsValid && !x.IsDead).Select(x => x.Position))
                    {
                        if (enemyturret != Vector3.Zero)
                        {
                            CircleRender.Draw(enemyturret, 850, Turretcolor, 2);
                        }
                    }
                }

                if (Config["Turret"].GetValue<MenuBool>("ally").Enabled)
                {
                    foreach (var allyturret in GameObjects.AllyTurrets.Where(x => x.IsVisibleOnScreen && x.IsValid && !x.IsDead).Select(x => x.Position))
                    {
                        if (allyturret != Vector3.Zero)
                        {
                            CircleRender.Draw(allyturret, 850, Alllycolor, 2);
                        }
                    }
                }

                if (Config["Enemys"].GetValue<MenuBool>("Zhonyas").Enabled)
                {
                    foreach (var Enemys in GameObjects.EnemyHeroes.Where(x => x.IsVisible && !x.IsDead))
                    {
                        if (!Enemys.HasBuff("ZhonyasRingShield"))
                        {
                            return;
                        }

                        if (Enemys != null)
                        {
                            var timer = Enemys.Buffs.Find(buff => buff.Name == "ZhonyasRingShield").EndTime - Game.Time;
                            FatalityRenderRing.DrawText2($"{timer:N1}", Drawing.WorldToScreen(Enemys.Position - Drawing.Height * .06f), SharpDX.Color.White);
                        }
                    }
                }

            }
        }

        private static void OnMouse(EventArgs args)
        {
            if (Config["Min"].GetValue<MenuBool>("minionkill").Enabled)
            {
                foreach (var minionn in GameObjects.EnemyMinions.Where(x => x.IsVisibleOnScreen && !x.IsDead && x.IsMinion() && x.IsEnemy))
                {
                    if (minionn.Health <= Me.GetAutoAttackDamage(minionn))
                    {
                        minionn.Glow(System.Drawing.Color.LawnGreen, 5, 1);
                    }

                }
            }

            if (Config["Min"].GetValue<MenuBool>("targets").Enabled)
            {
                var target = Orbwalker.GetTarget();
                if (target != null)
                {
                    target.Glow(System.Drawing.Color.MediumPurple, 5, 1);
                }
            }
        }

        private static void SkinListOnValueChanged(object sender, EventArgs e) => setSkinId();
        private static void setSkinId()
        {
            if (!Config["Skin"].GetValue<MenuBool>("Enable").Enabled)
            {
                return;
            }

            var skinlist = Config["Skin"].GetValue<MenuList>("skins");
            var selectedskin = skinlist.SelectedValue;
            var skinidtoset = ChampionSkinData.Skins[Me.CharacterName][selectedskin];

            Me.SetSkin(skinidtoset);
        }


        private static void JungleTracker(AIBaseClient sender, AIBaseClientPlayAnimationEventArgs args)
        {
            var barontrack = Config["tracker"].GetValue<MenuBool>("barontracker");
            var dragontrack = Config["tracker"].GetValue<MenuBool>("dragontracker");
            var heraldtracker = Config["tracker"].GetValue<MenuBool>("heraldtracker");

            if (barontrack.Enabled)
            {
                if (sender is AIMinionClient)
                {
                    if (sender.Name.Contains("Baron"))
                    {
                        if (args.Animation == "Spell2" || args.Animation == "Spell5" || args.Animation == "Attack1" || args.Animation == "Spell3" || args.Animation == "Spell4" || args.Animation == "Spell6" || args.Animation == "Spell1" || args.Animation == "Spell1_windup" || args.Animation == "Spell2_windup" || args.Animation == "Spell3_windup" || args.Animation == "Spell4_windup" || args.Animation == "Spell5_windup" || args.Animation == "Spell6_windup" && args.Animation != "Idle1")
                        {
                            Game.Print("<font color='#ff0000' size='25'>[Fatality Beta] Baron under Attack!</font>");
                        }
                    }
                }
            }

            if (dragontrack.Enabled)
            {
                if (sender is AIMinionClient)
                {
                    if (sender.Name.Contains("Dragon"))
                    {
                        if (args.Animation == "Spell1" || args.Animation == "Spell3")
                        {
                            Game.Print("<font color='#ff0000' size='25'>[Fatality Beta] Dragon under Attack!</font>");
                        }
                    }
                }
            }

            if (heraldtracker.Enabled)
            {
                if (sender is AIMinionClient)
                {
                    if (sender.Name.Contains("Herald"))
                    {
                        if (args.Animation == "Attack1" || args.Animation == "Attack2" || args.Animation == "Attack3" || args.Animation == "eye_open" || args.Animation == "dash_windup")
                        {
                            Game.Print("<font color='#ff0000' size='25'>[Fatality Beta] Herald under Attack!</font>");
                        }
                    }
                }
            }
        }
    }
}