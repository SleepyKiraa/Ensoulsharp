using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using Color = System.Drawing.Color;
using EnsoulSharp.SDK.MenuUI;


namespace Fatality.Utils
{
    public class Developer
    {
        private static Menu Config, Dev;
        private static AIHeroClient Me = ObjectManager.Player;

        public static void onGameLoad()
        {
            Config = new Menu("Devtool", "[Fatality] Devtool", true);

            Dev = new Menu("Devv", "Dev Stuff");
            Dev.Add(new MenuBool("enable", "Enable Dev tool"));
            Dev.Add(new MenuBool("name", "Enable Name"));
            Dev.Add(new MenuBool("speed", "Enable speed"));
            Dev.Add(new MenuBool("width", "Enable width"));
            Dev.Add(new MenuBool("Range", "Enable Range"));
            Dev.Add(new MenuBool("char", "Enable char"));
            Dev.Add(new MenuBool("spells", "Enable spells name"));
            Dev.Add(new MenuBool("Particle", "Enable Particle Drawing"));
            Dev.Add(new MenuBool("Animation", "Enable Animation Name"));
            Config.Add(Dev);
            
            Config.Attach();

            AIBaseClient.OnProcessSpellCast += OnProcessSpellCastt;
            AIBaseClient.OnPlayAnimation += OnAnimation;
            Drawing.OnDraw += OnDraw2;

        }

        public static void OnProcessSpellCastt(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            var enabledd = Config["Devv"].GetValue<MenuBool>("enable");
            var namee = Config["Devv"].GetValue<MenuBool>("name");
            var speed = Config["Devv"].GetValue<MenuBool>("speed");
            var width = Config["Devv"].GetValue<MenuBool>("width");
            var range = Config["Devv"].GetValue<MenuBool>("Range");
            var charname = Config["Devv"].GetValue<MenuBool>("char");
            var spells = Config["Devv"].GetValue<MenuBool>("spells");
            
            if (sender.IsMe && enabledd.Enabled && namee.Enabled)
            {
                Game.Print("Detected Spell Name: " + args.SData.Name + " Issued By: " + sender.CharacterName);
            }

            if (sender is AITurretClient && sender.IsEnemy)
            {
                Game.Print("Detected Turret Shots " + args.SData.Name);
            }

            if (sender.IsMe && enabledd.Enabled && speed.Enabled)
            {
                Game.Print("Detected Spell Speed: " + args.SData.MissileSpeed + " Issued By: " + sender.CharacterName);
            }

            if (sender.IsMe && enabledd.Enabled && width.Enabled)
            {
                Game.Print("Detected Spell Width: " + args.SData.LineWidth + " Issued By: " + sender.CharacterName);
            }

            if (sender.IsMe && enabledd.Enabled && range.Enabled)
            {
                Game.Print("Detected Spell Range: " + args.SData.CastRange + " Issued By: " + sender.CharacterName);
            }

            if (sender.IsMe && enabledd.Enabled && charname.Enabled)
            {
                Game.Print("Char Name:" + ObjectManager.Player.CharacterName);
            }
            
            if (sender.IsMe && enabledd.Enabled && spells.Enabled)
            {
                Game.Print("Q Name:" + Me.GetSpell(SpellSlot.Q).Name);
                Game.Print("W Name:" + Me.GetSpell(SpellSlot.W).Name);
                Game.Print("E Name:" + Me.GetSpell(SpellSlot.E).Name);
                Game.Print("R Name:" + Me.GetSpell(SpellSlot.R).Name);
            }
        }

        private static void OnDraw2(EventArgs args)
        {
            foreach (var obj in ObjectManager.Get<GameObject>().Where(o =>
                o.Position.Distance(Game.CursorPos) < 100 && !(o is Turret) && o.Name != "missile" &&
                !(o is GrassObject) && !(o is DrawFX) && !(o is LevelPropSpawnerPoint) &&
                !o.Name.Contains("MoveTo") && !(o is EffectEmitter)))
            {
                var X = Drawing.WorldToScreen(obj.Position).X;
                var Y = Drawing.WorldToScreen(obj.Position).Y;
                Drawing.DrawText(X, Y, Color.DarkTurquoise,
                    (obj is AIHeroClient) ? ((AIHeroClient) obj).CharacterName :
                    (obj is AIMinionClient) ? (obj as AIMinionClient).CharacterName :
                    (obj is AITurretClient) ? (obj as AITurretClient).CharacterName : obj.Name);
                Drawing.DrawText(X, Y + 10, Color.DarkTurquoise, obj.Type.ToString());
                Drawing.DrawText(X, Y + 20, Color.DarkTurquoise, "NetworkID: " + obj.NetworkId);
                Drawing.DrawText(X, Y + 30, Color.DarkTurquoise, obj.Position.ToString());
                if (obj is AIBaseClient)
                {
                    var aiobj = obj as AIBaseClient;
                    Drawing.DrawText(X, Y + 40, Color.DarkTurquoise,
                        "Health: " + aiobj.Health + "/" + aiobj.MaxHealth + "(" + aiobj.HealthPercent + "%)");
                }

                if (obj is EffectEmitter && obj.Name.Contains("Lux"))
                {
                    if (Config["Devv"].GetValue<MenuBool>("Particle").Enabled)
                    {
                        Game.Print(obj.Name);
                    }
                }

                if (obj is AIHeroClient)
                {
                    var hero = obj as AIHeroClient;
                    Drawing.DrawText(X, Y + 50, Color.DarkTurquoise, "Spells:");
                    Drawing.DrawText(X, Y + 60, Color.DarkTurquoise, "(Q): " + hero.Spellbook.Spells[0].Name);
                    Drawing.DrawText(X, Y + 70, Color.DarkTurquoise, "(W): " + hero.Spellbook.Spells[1].Name);
                    Drawing.DrawText(X, Y + 80, Color.DarkTurquoise, "(E): " + hero.Spellbook.Spells[2].Name);
                    Drawing.DrawText(X, Y + 90, Color.DarkTurquoise, "(R): " + hero.Spellbook.Spells[3].Name);
                    Drawing.DrawText(X, Y + 100, Color.DarkTurquoise, "(D): " + hero.Spellbook.Spells[4].Name);
                    Drawing.DrawText(X, Y + 110, Color.DarkTurquoise, "(F): " + hero.Spellbook.Spells[5].Name);
                    var buffs = hero.Buffs;
                    if (buffs.Any())
                    {
                        Drawing.DrawText(X, Y + 120, Color.DarkTurquoise, "Buffs:");
                    }

                    for (var i = 0; i < buffs.Count() * 10; i += 10)
                    {
                        Drawing.DrawText(X, (Y + 130 + i), Color.DarkTurquoise,
                            buffs[i / 10].Count + "x " + buffs[i / 10].Name);
                    }

                }                

                if (obj is MissileClient && obj.Name != "missile")
                {
                    var missile = obj as MissileClient;
                    Drawing.DrawText(X, Y + 40, Color.DarkTurquoise, "Missile Speed: " + missile.SData.MissileSpeed);
                    Drawing.DrawText(X, Y + 50, Color.DarkTurquoise, "Cast Range: " + missile.SData.CastRange);
                }
            }
        }

        private static void OnAnimation(AIBaseClient sender, AIBaseClientPlayAnimationEventArgs args)
        {
            var animation = Config["Devv"].GetValue<MenuBool>("Animation");

            if (animation.Enabled)
            {
                if (sender is AITurretClient && sender.Name.Contains("Turret") && sender.IsEnemy)
                {
                    Game.Print("Animation Name: " + args.Animation);
                    Console.WriteLine("Animation Name: " + args.Animation);
                }
            }           
        }
        
        private static void OnDraw3(EventArgs args)
        {
            foreach (var obj in ObjectManager.Get<GameObject>().Where(o =>
                o.Position.Distance(Game.CursorPos) < 100 && !(o is Turret) && o.Name != "missile" &&
                !(o is GrassObject) && !(o is DrawFX) && !(o is LevelPropSpawnerPoint) && !(o is EffectEmitter) &&
                !o.Name.Contains("MoveTo")))
            {
                var X = Drawing.WorldToScreen(obj.Position).X;
                var Y = Drawing.WorldToScreen(obj.Position).Y;
                Drawing.DrawText(X, Y, Color.DarkTurquoise,
                    (obj is AIHeroClient) ? ((AIHeroClient) obj).CharacterName :
                    (obj is AIMinionClient) ? (obj as AIMinionClient).CharacterName :
                    (obj is AITurretClient) ? (obj as AITurretClient).CharacterName : obj.Name);
                Drawing.DrawText(X, Y + 10, Color.DarkTurquoise, obj.Type.ToString());
                Drawing.DrawText(X, Y + 20, Color.DarkTurquoise, "NetworkID: " + obj.NetworkId);
                Drawing.DrawText(X, Y + 30, Color.DarkTurquoise, obj.Position.ToString());
                if (obj is AIBaseClient)
                {
                    var aiobj = obj as AIBaseClient;
                    Drawing.DrawText(X, Y + 40, Color.DarkTurquoise,
                        "Health: " + aiobj.Health + "/" + aiobj.MaxHealth + "(" + aiobj.HealthPercent + "%)");
                }

                if (obj is AIHeroClient)
                {
                    var hero = obj as AIHeroClient;
                    Drawing.DrawText(X, Y + 50, Color.DarkTurquoise, "Spells:");
                    Drawing.DrawText(X, Y + 60, Color.DarkTurquoise, "(Q): " + hero.Spellbook.Spells[0].Name);
                    Drawing.DrawText(X, Y + 70, Color.DarkTurquoise, "(W): " + hero.Spellbook.Spells[1].Name);
                    Drawing.DrawText(X, Y + 80, Color.DarkTurquoise, "(E): " + hero.Spellbook.Spells[2].Name);
                    Drawing.DrawText(X, Y + 90, Color.DarkTurquoise, "(R): " + hero.Spellbook.Spells[3].Name);
                    Drawing.DrawText(X, Y + 100, Color.DarkTurquoise, "(D): " + hero.Spellbook.Spells[4].Name);
                    Drawing.DrawText(X, Y + 110, Color.DarkTurquoise, "(F): " + hero.Spellbook.Spells[5].Name);
                    var buffs = hero.Buffs;
                    if (buffs.Any())
                    {
                        Game.Print("Buffs:");
                    }

                    for (var i = 0; i < buffs.Count() * 10; i += 10)
                    {
                        Game.Print(buffs[i / 10].Count + "x " + buffs[i / 10].Name);
                    }

                }

                if (obj is MissileClient && obj.Name != "missile")
                {
                    var missile = obj as MissileClient;
                    Drawing.DrawText(X, Y + 40, Color.DarkTurquoise, "Missile Speed: " + missile.SData.MissileSpeed);
                    Drawing.DrawText(X, Y + 50, Color.DarkTurquoise, "Cast Range: " + missile.SData.CastRange);
                }
            }
        }
    }
}