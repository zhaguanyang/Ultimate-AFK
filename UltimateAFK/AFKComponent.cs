using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MEC;
using Exiled.API.Features;
using Exiled.Loader;
using PlayableScps;
using System.Reflection;
using Exiled.API.Enums;
using Exiled.API.Features.Roles;
namespace UltimateAFK
{
    public class AFKComponent : MonoBehaviour
    {
        public MainClass plugin;

        public bool disabled;

        Player ply;

        public Vector3 AFKLastPosition;
        public Vector3 AFKLastAngle;

        public int AFKTime = 0;
        public int AFKCount = 0;
        private float timer = 0.0f;

        // Do not change this delay. It will screw up the detection
        public float delay = 1.0f;

        //private IEnumerable<Player> TryGet035() => Scp035.API.AllScp035;
        //private void TrySpawn035(Player player) => Scp035.API.Spawn035(player);

        // Expose replacing player for plugin support
        public Player PlayerToReplace;


        void Awake()
        {
            ply = Player.Get(gameObject);
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer > delay)
            {
                timer = 0f;
                if (!disabled)
                {
                    try
                    {
                        AFKChecker();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            }
        }

        // Called every 1 second according to the player's Update function. This is way more efficient than the old way of doing a forloop for every player.
        // Also, since the gameObject for the player is deleted when they disconnect, we don't need to worry about cleaning any variables :) 
        private void AFKChecker()
        {
            if (plugin.Config.EnableDebugLog)
                Log.Info($"AFK Time: {this.AFKTime} AFK Count: {this.AFKCount}");
            if (ply.Role.Team == Team.RIP || Player.List.Count() < plugin.Config.MinPlayers || (plugin.Config.IgnoreTut && ply.Role.Team == Team.TUT)) return;

            bool isScp079 = (ply.Role == RoleType.Scp079);
            bool scp096TryNotToCry = false;

            // When SCP096 is in the state "TryNotToCry" he cannot move or it will cancel,
            // therefore, we don't want to AFK check 096 while he's in this state.
            if (ply.Role == RoleType.Scp096)
            {
                PlayableScps.Scp096 scp096 = ply.ReferenceHub.scpsController.CurrentScp as PlayableScps.Scp096;
                scp096TryNotToCry = (scp096.PlayerState == Scp096PlayerState.TryNotToCry);
            }

            Vector3 CurrentPos = ply.Position;
            Vector3 CurrentAngle = new Vector3();
            if (ply.Role.Is(out Scp079Role scp079))
            {
                CurrentAngle = scp079.Camera.HeadPosition;
            }
            else
            {
                CurrentAngle = ply.Rotation;
            }

            if (CurrentPos != AFKLastPosition || CurrentAngle != AFKLastAngle || scp096TryNotToCry)
            {
                if (plugin.Config.EnableDebugLog)
                    Log.Info($"Pos reseting player. Line 85: {CurrentPos != AFKLastPosition} {CurrentAngle != AFKLastAngle} {scp096TryNotToCry}");
                AFKLastPosition = CurrentPos;
                AFKLastAngle = CurrentAngle;
                AFKTime = 0;
                PlayerToReplace = null;
                return;
            }

            if (plugin.Config.EnableDebugLog)
                Log.Info($"Updating AFKTime: {AFKTime}");
            // The player hasn't moved past this point.
            AFKTime++;

            // If the player hasn't reached the time yet don't continue.
            if (AFKTime < plugin.Config.AfkTime) return;

            // Check if we're still in the "grace" period
            int secondsuntilspec = (plugin.Config.AfkTime + plugin.Config.GraceTime) - AFKTime;
            if (secondsuntilspec > 0)
            {
                string warning = plugin.Config.MsgGrace;
                warning = warning.Replace("%timeleft%", secondsuntilspec.ToString());

                ply.ClearBroadcasts();
                ply.Broadcast(1, $"{plugin.Config.MsgPrefix} {warning}");
                return;
            }

            // The player is AFK and action will be taken.
            Log.Info($"{ply.Nickname} ({ply.UserId}) was detected as AFK!");
            AFKTime = 0;

            // Let's make sure they are still alive before doing any replacement.
            if (ply.Role.Team == Team.RIP) return;

            if (plugin.Config.TryReplace && !IsPastReplaceTime())
            {
                Assembly easyEvents = Loader.Plugins.FirstOrDefault(pl => pl.Name == "EasyEvents")?.Assembly;

                var roleEasyEvents = easyEvents?.GetType("EasyEvents.Util")?.GetMethod("GetRole")?.Invoke(null, new object[] { ply });

                // Credit: DCReplace :)
                // I mean at this point 90% of this has been rewritten lol...
                var inventory = ply.Items.ToList();

                RoleType role = ply.Role;
                Vector3 pos = ply.Position;
                float health = ply.Health;

                // New strange ammo system because the old one was fucked.
                var ammoHolder = ply.Ammo;

                // Stuff for 079
                byte Level079 = 0;
                float Exp079 = 0f, AP079 = 0f;
                if (ply.Role.Is(out Scp079Role scp0792))
                {
                    Level079 = scp0792.Level;
                    Exp079 = scp0792.Experience;
                    AP079 = scp0792.Energy;
                }

                PlayerToReplace = Player.List.FirstOrDefault(x => x.Role == RoleType.Spectator && x.UserId != string.Empty && !x.IsOverwatchEnabled && x != ply);
                if (PlayerToReplace != null)
                {
                    // Make the player a spectator first so other plugins can do things on player changing role with uAFK.
                    ply.ClearInventory(); // Clear their items to prevent dupes.
                    ply.SetRole(RoleType.Spectator);
                    ply.Broadcast(30, $"{plugin.Config.MsgPrefix} {plugin.Config.MsgFspec}");

                    PlayerToReplace.SetRole(role);

                    Timing.CallDelayed(0.3f, () =>
                    {
                        PlayerToReplace.Position = pos;

                        PlayerToReplace.ClearInventory();
                        PlayerToReplace.ResetInventory(inventory);

                        PlayerToReplace.Health = health;
                        
                        foreach (var ammoPair in ammoHolder)
                        {
                            PlayerToReplace.Ammo[ammoPair.Key] = ammoPair.Value;
                        }

                        if (ply.Role.Is(out Scp079Role scp079role))
                        {
                            Level079 = scp079role.Level;
                            Exp079 = scp079role.Experience;
                            AP079 = scp079role.Energy;
                        }

                        PlayerToReplace.Broadcast(10, $"{plugin.Config.MsgPrefix} {plugin.Config.MsgReplace}");
						if (roleEasyEvents != null) easyEvents?.GetType("EasyEvents.CustomRoles")?.GetMethod("ChangeRole")?.Invoke(null, new object[] { PlayerToReplace, roleEasyEvents });
                        PlayerToReplace = null;
                    });
                }
                else
                {
                    // Couldn't find a valid player to spawn, just ForceToSpec anyways.
                    ForceToSpec(ply);
                }
            }
            else
            {
                // Replacing is disabled, just ForceToSpec
                ForceToSpec(ply);
            }
            // If it's -1 we won't be kicking at all.
            if (plugin.Config.NumBeforeKick != -1)
            {
                // Increment AFK Count
                AFKCount++;
                if (AFKCount >= plugin.Config.NumBeforeKick)
                {
                    // Since this.AFKCount is greater than the config we're going to kick that player for being AFK too many times in one match.
                    ServerConsole.Disconnect(gameObject, plugin.Config.MsgKick);
                }
            }
        }

        private void ForceToSpec(Player hub)
        {
            hub.SetRole(RoleType.Spectator);
            hub.Broadcast(30, $"{plugin.Config.MsgPrefix} {plugin.Config.MsgFspec}");
        }

        private bool IsPastReplaceTime()
        {
            if (plugin.Config.MaxReplaceTime != -1)
            {
                if (Round.ElapsedTime.TotalSeconds > plugin.Config.MaxReplaceTime)
                {
                    Log.Info("Since we are past the allowed replace time, we will not look for replacement player.");
                    return true;
                }
            }
            return false;
        }
    }
}
