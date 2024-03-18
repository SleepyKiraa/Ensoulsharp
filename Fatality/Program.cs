using System;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using Fatality.Champions.Ahri;
using Fatality.Champions.Akali;
using Fatality.Champions.Ashe;
using Fatality.Champions.Aurelion;
using Fatality.Champions.Blitzcrank;
using Fatality.Champions.Brand;
using Fatality.Champions.Caitlyn;
using Fatality.Champions.Cassiopeia;
using Fatality.Champions.Corki;
using Fatality.Champions.Diana;
using Fatality.Champions.Ezreal;
using Fatality.Champions.Jhin;
using Fatality.Champions.Jinx;
using Fatality.Champions.Kalista;
using Fatality.Champions.Karthus;
using Fatality.Champions.Katarina;
using Fatality.Champions.Kayn;
using Fatality.Champions.Kennen;
using Fatality.Champions.Khazix;
using Fatality.Champions.KogMaw;
using Fatality.Champions.Ksante;
using Fatality.Champions.Lucian;
using Fatality.Champions.Lux;
using Fatality.Champions.MasterYi;
using Fatality.Champions.Milio;
using Fatality.Champions.Neeko;
using Fatality.Champions.Nocturne;
using Fatality.Champions.Olaf;
using Fatality.Champions.Pyke;
using Fatality.Champions.Riven;
using Fatality.Champions.Samira;
using Fatality.Champions.Shyvana;
using Fatality.Champions.Soraka;
using Fatality.Champions.Sylas;
using Fatality.Champions.Tristana;
using Fatality.Champions.TwistedFate;
using Fatality.Champions.Twitch;
using Fatality.Champions.Vayne;
using Fatality.Champions.Xerath;
using Fatality.Champions.Yasuo;
using Fatality.Champions.Yone;
using Fatality.Champions.Zeri;
using Fatality.Utils;
using Menu = EnsoulSharp.SDK.MenuUI.Menu;

namespace Fatality
{
    public class Program
    {
        public static string DiscordLink = "Discord";
        public static void Main(string[] args)
        {
            /*ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            OnProgramStart.Initialize("FatalityAio", "448139", "95BtEnG8UEwYaSisJFIdVT0uvgDC0bmsfpJ", "1.0");
            */
            
            GameEvent.OnGameLoad += OnLoadingComplete;
            ProgramLoad();


            /* string pathh = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\FatalityAio.txt";
 
             if (!File.Exists(pathh))
             {
                 using (StreamWriter sw = File.CreateText(pathh))
                 {
                     
                 }
             }
 
             if (!ApplicationSettings.Freemode)
             {
                 StreamReader sr = new StreamReader(pathh);
                 string data = sr.ReadLine();
                 if (new FileInfo(pathh).Length == 0)
                 {
                     sr.Close();
                     Console.WriteLine("Key: ");
                     string key = Console.ReadLine();
                     StreamWriter sw = new StreamWriter(pathh);
                     sw.Write(key);
                     sw.Close();
                     if (API.AIO(key))
                     {
                         Console.WriteLine("Success");
                         GameEvent.OnGameLoad += OnLoadingCompleteBeta;
                         ProgramLoad();
                     }
                 }
 
                 if (new FileInfo(pathh).Length == 32)
                 {
                     Console.WriteLine("Key: ");
                     if (API.AIO(data))
                     {
                         Console.WriteLine("Success");
                         API.Log(User.Username, "Logged In");
                         GameEvent.OnGameLoad += OnLoadingCompleteBeta;
                     }
                 }
                 else
                 {
                     Console.WriteLine("Wrong Key!");
                 }
             }*/
        }

        private static void OnLoadingComplete()
        {

            if (Champion.Enabled)
            {
                if (ObjectManager.Player == null)
                    return;

                var Version = "<font color='#ff0000' size='25'> [Fatality 1.0.0.23]: </font>";
                var UpdateName = "<font color='#02d5e8' size='25'>Jhin Recode Added</font>";
                try
                {
                    switch (GameObjects.Player.CharacterName)
                    {
                        case "Khazix":
                            Khazix.OnGameload();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Khazix The Voidreaver Loaded!</font>");
                            break;

                        case "Sylas":
                            Sylas.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Sylas The Unshackled Loaded!</font>");
                            break;

                        case "Ezreal":
                            Ezreal.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Ezreal The Prodigal Explorer Loaded!</font>");
                            break;
                            

                        case "Varus":
                            if (LoadAD.Enabled)
                            {
                                Champions.Varus.ADVarus.OnGameLoad();
                                Game.Print("[Fatality] AD Varus Loaded!");
                                Game.Print(Version +
                                           UpdateName);
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                                Console.ResetColor();
                            }
                            else if (LoadAP.Enabled)
                            {
                                Champions.Varus.APVarus.OnGameLoad();
                                Game.Print("[Fatality] AP Varus Loaded!");
                                Game.Print(Version +
                                           UpdateName);
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                                Console.ResetColor();
                            }

                            break;

                        case "Tristana":
                            Tristana.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Tristana The Yordle Gunner Loaded!</font>");
                            break;

                        case "Xerath":
                            Xerath.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Xerath The Magus Ascendant Loaded!</font>");
                            break;

                        case "Ashe":
                            Ashe.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Ashe The Frost Archer Loaded!</font>");
                            break;

                        case "Kennen":
                            Kennen.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Kennen The Heart of The Tempest Loaded!</font>");
                            break;

                        case "Shyvana":
                            Shyvana.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Shyvana The Half Dragon Loaded!</font>");
                            break;

                        case "Pyke":
                            Pyke.OnGameLoad();

                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Pyke The Bloodharbor Ripper Loaded!</font>");
                            break;

                        case "Vayne":
                            Vayne.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Vayne The Night Hunter Loaded!</font>");
                            break;

                        case "Jhin":
                            Jhin.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Jhin The Virtuoso Loaded!</font>");
                            break;

                        case "Blitzcrank":
                            Blitzcrank.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Blitzcrank The Great Steam Golem Loaded!</font>");
                            break;

                        case "Akali":
                            Akali.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Akali The Rouge Assassin Loaded!</font>");
                            break;

                        case "Lucian":
                            Lucian.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Lucian The Purifier Loaded!</font>");
                            break;

                        case "Cassiopeia":
                            Cassiopeia.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Cassiopeia The Serpents Embrace Loaded!</font>");
                            break;

                        case "Diana":
                            Diana.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Diana Scorn of the Moon Loaded!</font>");
                            break;

                        case "Ahri":
                            Ahri.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Ahri The Nine Tailed Fox Loaded!</font>");
                            break;

                        case "KSante":
                            Ksante.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Ksante The Pride of Nazumah Loaded!</font>");
                            break;

                        case "Zeri":
                            Zeri.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Zeri The Spark of Zaun Loaded!</font>");
                            break;

                        case "Kalista":
                            Kalista.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Kalista The Spear of Vengeance Loaded!</font>");
                            break;

                        case "Corki":
                            Corki.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Corki The Daring Bombardier Loaded!</font>");
                            break;

                        case "Yasuo":
                            Yasuo.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Yasuo The Unforgiven Loaded!</font>");
                            break;

                        case "Twitch":
                            Twitch.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Twitch The Plague Rat Loaded!</font>");
                            break;
                        
                        case "Samira":
                            Samira.OnGameload();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Samira The Desert Rose Loaded!</font>");
                            break;
                        
                        case "Lux":
                            Lux.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Lux The Lady Of Luminosity Loaded!</font>");
                            break;
                        
                        case "Jinx":
                            Jinx.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Jinx The Loose Cannon Loaded!</font>");
                            break;
                        
                        case "Kayn":
                            Kayn.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Kayn The Shadow Reaper Loaded!</font>");
                            break;
                        
                        case "Caitlyn":
                            Caitlyn.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Caitlyn The Sheriff Of Piltover Loaded!</font>");
                            break;
                        
                        case "Karthus":
                            Karthus.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Karthus The Deathsinger Loaded!</font>");
                            break;

                        case "Katarina":
                            Katarina.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Katarina The Sinister Blade Loaded!</font>");
                            break;

                        case "Nocturne":
                            Nocturne.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Nocturne The Eternal Nightmare Loaded!</font>");
                            break;

                        case "Soraka":
                            Soraka.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Soraka The Starchild Loaded!</font>");
                            break;

                        case "TwistedFate":
                            TwistedFate.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Twisted Fate The Card Master Loaded!</font>");
                            break;

                        case "AurelionSol":
                            AurelionSol.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Aurelion Sol The Star Forger Loaded!</font>");
                            break;

                        case "Yone":
                            Yone.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Yone The Unforgotten Loaded!</font>");
                            break;

                        case "Olaf":
                            Olaf.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Olaf The Berserker Loaded!</font>");
                            break;
                        
                        case "KogMaw":
                            KogMaw.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] KogMaw The Mouth of The Abyss Loaded!</font>");
                            break;
                        
                        case "Riven":
                            Riven.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Riven The Exile Loaded!</font>");
                            break;
                        
                        case "Brand":
                            Brand.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Brand The Burning Vengeance Loaded!</font>");
                            break;

                        case "Neeko":
                            Neeko.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Neeko The Curious Chameleon Loaded!</font>");
                            break;

                        case "MasterYi":
                            MasterYi.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Master Yi The Wuju Bladesman Loaded!</font>");
                            break;
                        
                        case "Milio":
                            Milio.OnGameLoad();
                            Game.Print(Version +
                                       UpdateName);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Fatality] " + GameObjects.Player.CharacterName + " Loaded");
                            Console.ResetColor();
                            Game.Print("<font color='#00c003' size='25'>[Fatality] Milio The Gentle Flame Loaded!</font>");
                            break;

                        default:
                            Game.Print("<font color='#ff0000' size='25'>[Fatality] Does Not Support: " +
                                       ObjectManager.Player.CharacterName + "</font>");
                            Console.ForegroundColor= ConsoleColor.Red;
                            Console.WriteLine("[Fatality] Does Not Support " + ObjectManager.Player.CharacterName);
                            Console.ResetColor();
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                if (Activator.Enabled)
                {
                    Utils.Activator.OnGameLoad();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[Fatality] Activator Loaded!");
                    Console.ResetColor();
                    Game.Print("<font color='#00c003' size='25'>[Fatality] Activator Loaded!</font>");
                }

                if (Awareness.Enabled)
                {
                    Utils.FatalityAwareness.Awareness.OnGameLoad();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[Fatality] Awareness Loaded!");
                    Console.ResetColor();
                    Game.Print("<font color='#00c003' size='25'>[Fatality] Awareness Loaded!</font>");
                }

                if (Orb.Enabled)
                {
                    Orbwalker.GetOrbwalker("SDK").Dispose();
                    Orbwalker.AddOrbwalker("Orbwalker", new Fatality.Utils.NewOrbwalker());
                    Orbwalker.SetOrbwalker("Orbwalker");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[Fatality] Orbwalker Loaded!");
                    Console.ResetColor();
                    Game.Print("<font color='#00c003' size='25'>[Fatality] Orbwalker Loaded!</font>");
                }
            }
        }

        private static Menu menuu = null;
        private static MenuBool Champion = new MenuBool("Champs", "Load " + ObjectManager.Player.CharacterName);
        private static MenuBool Activator = new MenuBool("activator", "Load Activator", true);
        private static MenuBool Awareness = new MenuBool("awareness", "Load Awareness", true);
        private static MenuBool Orb = new MenuBool("orbwalker", "Load Orbwalker", true);
        private static MenuBool LoadAD = new MenuBool("ADVarus", "Load AD Varus", false);
        private static MenuBool LoadAP = new MenuBool("APVarus", "Load AP Varus", false);

        private static void ProgramLoad()
        {
            menuu = new Menu("Aio", "[Fatality] Core", true);
            menuu.SetLogo(SpriteRender.CreateLogo(Properties.Resource1.Core));
            menuu.Add(Champion);
            menuu.Add(Activator);
            menuu.Add(Awareness);
            menuu.Add(Orb);
            if (ObjectManager.Player.CharacterName == "Varus")
            {
                menuu.Add(LoadAD);
                menuu.Add(LoadAP);
            }

            menuu.Attach();
        }
    }
}