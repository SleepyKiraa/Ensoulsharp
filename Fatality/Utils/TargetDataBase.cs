using EnsoulSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp.SDK;

namespace Fatality.Utils
{
    public enum Skilltype
    {
        Unknown = 0,
        Line = 1,
        Circle = 2,
        Cone = 3,
        Unit = 4
    }

    public class TargetDataBAse
    {
        public string HeroName { get; set; }
        public string SpellMenuName { get; set; }
        public SpellSlot Slot { get; set; }
        public Skilltype Type { get; set; }
        public float Delay { get; set; }
        public string SDataName { get; set; }
        public static List<TargetDataBAse> GDLIST = new List<TargetDataBAse>();
        public static List<TargetDataBAse> GDLIstInstant = new List<TargetDataBAse>();

        static TargetDataBAse()
        {
            GDLIST.Add(
                new TargetDataBAse
                {
                    HeroName = "Alistar",
                    SpellMenuName = "Headbutt",
                    Slot = SpellSlot.W,
                    Type = Skilltype.Unit,
                    Delay = 0.25f,
                    SDataName = "Headbutt",
                });
            GDLIstInstant.Add(
                new TargetDataBAse
                {
                    HeroName = "Blitzcrank",
                    SpellMenuName = "Power Fist",
                    Slot = SpellSlot.E,
                    Type = Skilltype.Unit,
                    Delay = 0.25f,
                    SDataName = "PowerFistAttack",
                });
            GDLIstInstant.Add(
                new TargetDataBAse
                {
                    HeroName = "Chogath",
                    SpellMenuName = "Feast",
                    Slot = SpellSlot.R,
                    Type = Skilltype.Unit,
                    Delay = 0.25f,
                    SDataName = "Feast",
                });
            GDLIstInstant.Add(
                new TargetDataBAse
                {
                    HeroName = "Darius",
                    SpellMenuName = "Noxian Guillotine",
                    Slot = SpellSlot.R,
                    Type = Skilltype.Unit,
                    Delay = 0.25f,
                    SDataName = "DariusExecute"
                });
            GDLIST.Add(
                new TargetDataBAse
                {
                    HeroName = "Ekko",
                    SpellMenuName = "Phase Dive",
                    Slot = SpellSlot.E,
                    Type = Skilltype.Unit,
                    Delay = 0f,
                    SDataName = "EkkoE"
                });
            GDLIST.Add(
                new TargetDataBAse
                {
                    HeroName = "Fiddlesticks",
                    SpellMenuName = "Terrify",
                    Slot = SpellSlot.Q,
                    Type = Skilltype.Unit,
                    Delay = 0.25f,
                    SDataName = "FiddleSticksQ"
                });
            GDLIstInstant.Add(
                new TargetDataBAse
                {
                    HeroName = "Garen",
                    SpellMenuName = "Demacian Justice",
                    Slot = SpellSlot.R,
                    Type = Skilltype.Unit,
                    Delay = 0.25f,
                    SDataName = "GarenR",
                });
            GDLIstInstant.Add(
                new TargetDataBAse
                {
                    HeroName = "Hecarim",
                    SpellMenuName = "Devastating Charge",
                    Slot = SpellSlot.E,
                    Type = Skilltype.Unit,
                    Delay = 0f,
                    SDataName = "HecarimRamp"
                });
            GDLIstInstant.Add(
                new TargetDataBAse
                {
                    HeroName = "JarvanIV",
                    SpellMenuName = "Cataclysm",
                    Slot = SpellSlot.R,
                    Type = Skilltype.Unit,
                    Delay = 0f,
                    SDataName = "JarvanIVCataclysm"
                });
            GDLIstInstant.Add(
                new TargetDataBAse
                {
                    HeroName = "Khazix",
                    SpellMenuName = "Taste Their Fear",
                    Slot = SpellSlot.Q,
                    Type = Skilltype.Unit,
                    Delay = 0.25f,
                    SDataName = "KhazixQ"
                });
            GDLIST.Add(
            new TargetDataBAse
            {
                HeroName = "Kindred",
                SpellMenuName = "Mounting Dread",
                Slot = SpellSlot.E,
                Type = Skilltype.Unit,
                Delay = 0.25f,
                SDataName = "KindredEWrapper"
            });
            GDLIstInstant.Add(
                new TargetDataBAse
                {
                    HeroName = "Leesin",
                    SpellMenuName = "Dragons Rage",
                    Slot = SpellSlot.R,
                    Type = Skilltype.Unit,
                    Delay = 0.25f,
                    SDataName = "BlindMonkRKick"
                });
            GDLIstInstant.Add(
                new TargetDataBAse
                {
                    HeroName = "Leona",
                    SpellMenuName = "Shield of Daybreak",
                    Slot = SpellSlot.Q,
                    Type = Skilltype.Unit,
                    Delay = 0f,
                    SDataName = "LeonaShieldOfDaybreakAttack"
                });
            GDLIstInstant.Add(
                new TargetDataBAse
                {
                    HeroName = "Lissandra",
                    SpellMenuName = "Frozen Tomb",
                    Slot = SpellSlot.R,
                    Type = Skilltype.Unit,
                    Delay = 0.25f,
                    SDataName = "LissandraR"
                });
            GDLIST.Add(
                new TargetDataBAse
                {
                    HeroName = "Lulu",
                    SpellMenuName = "Whimsy",
                    Slot = SpellSlot.W,
                    Type = Skilltype.Unit,
                    Delay = 0.25f,
                    SDataName = "LuluWTwo"
                });
            GDLIST.Add(
                new TargetDataBAse
                {
                    HeroName = "Nautilus",
                    SpellMenuName = "Depth Charge",
                    Slot = SpellSlot.R,
                    Type = Skilltype.Unit,
                    Delay = 0.25f,
                    SDataName = "NautilusGrandLine"
                });
            GDLIST.Add(
                new TargetDataBAse
                {
                    HeroName = "Pantheon",
                    SpellMenuName = "Shield Vault",
                    Slot = SpellSlot.W,
                    Type = Skilltype.Unit,
                    Delay = 0f,
                    SDataName = "PantheonW"
                });
            GDLIST.Add(
                new TargetDataBAse
                {
                    HeroName = "Poppy",
                    SpellMenuName = "Heroic Charge",
                    Slot = SpellSlot.E,
                    Type = Skilltype.Unit,
                    Delay = 0f,
                    SDataName = "PoppyE"
                });
            GDLIstInstant.Add(
                new TargetDataBAse
                {
                    HeroName = "Renekton",
                    SpellMenuName = "Ruthless Predator",
                    Slot = SpellSlot.W,
                    Type = Skilltype.Unit,
                    Delay = 0f,
                    SDataName = "RenektonExecute"
                });
            GDLIST.Add(
                new TargetDataBAse
                {
                    HeroName = "Sejuani",
                    SpellMenuName = "Permafrost",
                    Slot = SpellSlot.W,
                    Type = Skilltype.Unit,
                    Delay = 0.25f,
                    SDataName = "SejuaniE2"
                });
            GDLIstInstant.Add(
                new TargetDataBAse
                {
                    HeroName = "Sett",
                    SpellMenuName = "The Show Stopper",
                    Slot = SpellSlot.R,
                    Type = Skilltype.Unit,
                    Delay = 0f,
                    SDataName = "SettR"
                });
            GDLIstInstant.Add(
                new TargetDataBAse
                {
                    HeroName = "Sett",
                    SpellMenuName = "The Show Stopper",
                    Slot = SpellSlot.R,
                    Type = Skilltype.Unit,
                    Delay = 0f,
                    SDataName = "SettR"
                });
            GDLIstInstant.Add(
                new TargetDataBAse
                {
                    HeroName = "Singed",
                    SpellMenuName = "Fling",
                    Slot = SpellSlot.E,
                    Type = Skilltype.Unit,
                    Delay = 0f,
                    SDataName = "Fling"
                });
            GDLIstInstant.Add(
                new TargetDataBAse
                {
                    HeroName = "Skarner",
                    SpellMenuName = "Impale",
                    Slot = SpellSlot.R,
                    Type = Skilltype.Unit,
                    Delay = 0f,
                    SDataName = "SkarnerImpale"
                });
            GDLIST.Add(
                new TargetDataBAse
                {
                    HeroName = "Syndra",
                    SpellMenuName = "Unleashed Power",
                    Slot = SpellSlot.R,
                    Type = Skilltype.Unit,
                    Delay = 0f,
                    SDataName = "SyndraR"
                });
            GDLIST.Add(
                new TargetDataBAse
                {
                    HeroName = "Teemo",
                    SpellMenuName = "Blinding Dart",
                    Slot = SpellSlot.Q,
                    Type = Skilltype.Unit,
                    Delay = 0f,
                    SDataName = "BlindingDart"
                });
            GDLIST.Add(
                new TargetDataBAse
                {
                    HeroName = "Tristana",
                    SpellMenuName = "Buster Shot",
                    Slot = SpellSlot.R,
                    Type = Skilltype.Unit,
                    Delay = 0f,
                    SDataName = "TristanaR"
                });
            GDLIST.Add(
                new TargetDataBAse
                {
                    HeroName = "Vayne",
                    SpellMenuName = "Condemn",
                    Slot = SpellSlot.E,
                    Type = Skilltype.Unit,
                    Delay = 0.25f,
                    SDataName = "VayneCondemn"
                });
            GDLIST.Add(
                new TargetDataBAse
                {
                    HeroName = "Veigar",
                    SpellMenuName = "Primordial Burst",
                    Slot = SpellSlot.R,
                    Type = Skilltype.Unit,
                    Delay = 0.25f,
                    SDataName = "VeigarR"
                });
            GDLIST.Add(
                new TargetDataBAse
                {
                    HeroName = "Vi",
                    SpellMenuName = "Cease and Desist",
                    Slot = SpellSlot.R,
                    Type = Skilltype.Unit,
                    Delay = 0.25f,
                    SDataName = "ViR"
                });
        }
    }
}