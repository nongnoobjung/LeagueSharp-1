﻿/*
This shit is copied from Maufeat and blm95 and it comes with no warranty that
it will work as intended, or that it work at all.
I also suck at disclaimers.
hf spending your free IP ^_^
*/
using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace SimpleYetSoSharp
{
    internal class Program
    {
		//edit this if you feel like it
        private static string[] shityousaywhenyoudead = { "lulz", "lol", "omg", "noooooob", "help me", "fkin nooooobs", "TEAM WHERE YOU AT???", "WILL YOU HELP?", "HEEEEEEEEEEEELP", "OMG REPORT MY NOOB TEAM", "OMG MY TEAM SHOULD UNINSTALL", "MATCHMAKING SO UNFAIR", "gg", "I just want this game to end fking noobs" };
        private static Spell Q;
        private static Spell W;
        private static Spell E;
        private static Spell R;
        private static TargetSelector ts;
        private static Menu menu;
		private static int deathcounter = 0;
        private static double timedead;
        private static List<Obj_AI_Hero> allies;
        private static int i = 0;
        private static bool boughtbots, boughtaegis, boughtzekes = false;
        static Obj_AI_Hero player = ObjectManager.Player;
        static int qOff, wOff, eOff, rOff = 0;
        static int[] abilityOrder = { 1, 2, 3, 2, 2, 4, 2, 1, 2, 1, 4, 1, 1, 3, 3, 4, 3, 3, }; //spell level order
        public static bool lowhealth = false;
        public static int timeInFountain = 0;


        //list of known adcs to follow
        private static readonly string[] ad =
        {
            "Ashe", "Caitlyn", "Corki", "Draven", "Ezreal", "Graves", "KogMaw",
            "MissFortune", "Quinn", "Sivir", "Tristana", "Twitch", "Varus", "Vayne", "Jinx", "Lucian", "Kalista", "Teemo", "Urgot"
        };

        private static readonly string[] ap =
        {
            "Ahri", "Akali", "Anivia", "Annie", "Brand", "Cassiopeia", "Diana",
            "FiddleSticks", "Fizz", "Gragas", "Heimerdinger", "Karthus", "Kassadin", "Katarina", "Kayle", "Kennen",
            "Leblanc", "Lissandra", "Lux", "Malzahar", "Mordekaiser", "Morgana", "Nidalee", "Orianna", "Ryze", "Sion",
            "Swain", "Syndra", "Teemo", "TwistedFate", "Veigar", "Viktor", "Vladimir", "Xerath", "Ziggs", "Zyra",
            "Velkoz"
        };

        private static Vector3 followpos;
        private static Obj_AI_Hero follow;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
             if (player.Level == 1)
            {
                SpellSlot abilitySlot;
                if (abilityOrder[0] == 1)
                {
                    abilitySlot = SpellSlot.Q;
                }
                else if (abilityOrder[0] == 2)
                {
                    abilitySlot = SpellSlot.W;
                }
                else if (abilityOrder[0] == 3)
                {
                    abilitySlot = SpellSlot.E;
                }
                else if (abilityOrder[0] == 4)
                {
                    abilitySlot = SpellSlot.R;
                }
                else
                {
                    abilitySlot = SpellSlot.Q;
                }
                ObjectManager.Player.Spellbook.LevelUpSpell(abilitySlot);
            }
            CustomEvents.Unit.OnLevelUp += OnLevelUp;

            allies = new List<Obj_AI_Hero>();
            if (ObjectManager.Player.ChampionName == "Annie")
            {
                Q = new Spell(SpellSlot.Q, 650);
                W = new Spell(SpellSlot.W, 625);
                E = new Spell(SpellSlot.E);
                R = new Spell(SpellSlot.R, 600);
            }
            else
            {

                Q = new Spell(SpellSlot.Q);
                W = new Spell(SpellSlot.W);
                E = new Spell(SpellSlot.E);
                R = new Spell(SpellSlot.R);
            }
            ts = new TargetSelector(1025, TargetSelector.TargetingMode.AutoPriority);
            menu = new Menu("AutoPlay Bot", "syssb", true);
            menu.AddItem(new MenuItem("on", "Activate it!").SetValue(new KeyBind(32, KeyBindType.Toggle)));
            menu.AddSubMenu(new Menu("Follow:", "follower"));
            foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsAlly && !x.IsMe))
            {
                allies.Add(ally);
                if (ad.Contains(ally.ChampionName))
                    menu.SubMenu("follower").AddItem(new MenuItem(ally.ChampionName, ally.ChampionName).SetValue(true));
                else
                {
                    menu.SubMenu("follower").AddItem(new MenuItem(ally.ChampionName, ally.ChampionName).SetValue(false));
                }
            }

            menu.AddToMainMenu();
            Game.OnGameProcessPacket += Game_OnGameProcessPacket;
            Game.OnGameUpdate += Game_OnGameUpdate;
			Game.OnGameEnd += OnGameEnd;
            BuyItems();
        }



        private static void Game_OnGameProcessPacket(GamePacketEventArgs args)
        {
            GamePacket p = new GamePacket(args.PacketData);
            if (p.Header != Packet.S2C.TowerAggro.Header) return;
            if (Packet.S2C.TowerAggro.Decoded(args.PacketData).TargetNetworkId != ObjectManager.Player.NetworkId)
                return;
            

        }







        private static void Game_OnGameUpdate(EventArgs args)
        {
            follow =
                ObjectManager.Get<Obj_AI_Hero>()
                    .First(x => !x.IsMe && x.IsAlly && menu.Item(x.ChampionName).GetValue<bool>()) ??
                ObjectManager.Get<Obj_AI_Hero>().First(x => !x.IsMe && x.IsAlly && ap.Contains(x.ChampionName)) ??
                ObjectManager.Get<Obj_AI_Hero>().First(x => x.IsAlly && !x.IsMe);
            if (follow == null)
            {
                follow = ObjectManager.Get<Obj_AI_Hero>().First(x => !x.IsMe && x.IsAlly);
            }
            followpos = follow.Position;
            if (deathcounter == 14)
                deathcounter = 0;
            BuyItems();
            doFollow();

  

            if (ObjectManager.Player.IsDead && Game.Time - timedead > 80)
            {
                Game.Say(shityousaywhenyoudead[deathcounter]);
                deathcounter++;
                timedead = Game.Time;
            }


            if (follow.IsDead)
            {
                follow = ObjectManager.Get<Obj_AI_Hero>().First(x => !x.IsMe && x.Distance(ObjectManager.Player) < 1300);
            }

            Console.WriteLine(follow.IsDead);

 


        }
        public static void OnLevelUp(Obj_AI_Base sender, CustomEvents.Unit.OnLevelUpEventArgs args)
        {
            if ((qOff + wOff + eOff + rOff) < player.Level)
            {
                int i = player.Level - 1;
                SpellSlot abilitySlot;
                if (abilityOrder[i] == 1)
                {
                    abilitySlot = SpellSlot.Q;
                }
                else if (abilityOrder[i] == 2)
                {
                    abilitySlot = SpellSlot.W;
                }
                else if (abilityOrder[i] == 3)
                {
                    abilitySlot = SpellSlot.E;
                }
                else if (abilityOrder[i] == 4)
                {
                    abilitySlot = SpellSlot.R;
                }
                else
                {
                    abilitySlot = SpellSlot.Q;
                }
                ObjectManager.Player.Spellbook.LevelUpSpell(abilitySlot);
            }
        }




        public static void doFollow()
        {
           while (Utility.InFountain())
           {

               do
               {
                   timeInFountain++;
               }
               while (timeInFountain < 60000);
               if (timeInFountain >= 60000)
               {
                   follow = ObjectManager.Get<Obj_AI_Hero>().First(x => !x.IsMe && x.IsAlly);
                   timeInFountain = 0;
               }

           }
		if (!Utility.InFountain())
		{
			timeInFountain = 0;
		}
        if (follow.Distance(ObjectManager.Player.Position) > 700)
            {
                ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, followpos);
            }

            //if spells available, cast them.
            
        if (ObjectManager.Player.ChampionName == "Annie")
        {
            if (ts.Target.Distance(ObjectManager.Player) < Q.Range && Q.IsReady() && !Utility.UnderTurret(ObjectManager.Player, true))
            {
                Q.Cast(ts.Target);
            }

            if (ts.Target.Distance(ObjectManager.Player) < W.Range && W.IsReady() && !Utility.UnderTurret(ObjectManager.Player, true))
            {
                W.Cast(ts.Target);
            }

            if (ts.Target.Distance(ObjectManager.Player) < R.Range && R.IsReady() && !Utility.UnderTurret(ObjectManager.Player, true))
            {
                R.Cast(ts.Target);
            }
            if (E.IsReady())
            {
                E.Cast();
            }
        }
        else 
        {
            if (ts.Target.Distance(follow.Position) < 600 && follow.Distance(ObjectManager.Player.Position) < 700 && Q.IsReady() && !Utility.UnderTurret(ObjectManager.Player, true))
            {
                Q.Cast(ts.Target);
            }

            if (ts.Target.Distance(follow.Position) < 600 && follow.Distance(ObjectManager.Player.Position) < 700 && W.IsReady() && !Utility.UnderTurret(ObjectManager.Player, true))
            {
                W.Cast(ts.Target);
            }

            if (ts.Target.Distance(follow.Position) < 600 && follow.Distance(ObjectManager.Player.Position) < 700 &&  R.IsReady() && !Utility.UnderTurret(ObjectManager.Player, true))
            {
                R.Cast(ts.Target);
            }
            if (ts.Target.Distance(follow.Position) < 600 && follow.Distance(ObjectManager.Player.Position) < 700 && E.IsReady() && !Utility.UnderTurret(ObjectManager.Player, true))
            {
                E.Cast(ts.Target);
            }
        } 
        }

        public static void BuyItems()
        {
            
            if (Utility.InFountain() && ObjectManager.Player.Gold == 475 && !boughtbots)
            {
                Packet.C2S.BuyItem.Encoded(new Packet.C2S.BuyItem.Struct(1001)).Send();
                Game.PrintChat("BOUGHT BOTS");
                boughtbots = true;
            }
            if (Utility.InShopRange() && ObjectManager.Player.Gold > 1900 && ObjectManager.Player.Gold < 2550 && !boughtaegis)
            {
                Packet.C2S.BuyItem.Encoded(new Packet.C2S.BuyItem.Struct(3105)).Send();
                Game.PrintChat("BOUGHT AEGIS");
                boughtaegis = true;
            }
            else if (Utility.InShopRange() && ObjectManager.Player.Gold > 2550 && !boughtzekes)
            {
                Packet.C2S.BuyItem.Encoded(new Packet.C2S.BuyItem.Struct(3050)).Send();
                Game.PrintChat("BOUGHT ZEKES");
                boughtzekes = true;
            }

        }
		private static void OnGameEnd(EventArgs args)
		{
		Game.Say("gg");
		}
    }
}