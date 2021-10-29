using System;
using System.Reflection;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using Exiled.Permissions.Extensions;
using Exiled.Loader;
using System.Linq;

namespace UltimateAFK
{
    public class PlayerEvents
    {
		public MainClass plugin;

		public PlayerEvents(MainClass plugin)
		{
			this.plugin = plugin;
		}

		public void OnPlayerSpawned(SpawningEventArgs ev)
		{
			if (ev.Player.IPAddress == "127.0.0.1" && !plugin.Config.IgnorePermissionsAndIP || 
			    ev.Player.GameObject.gameObject.GetComponent<AFKComponent>() != null || 
			    ev.Player.CheckPermission("uafk.ignore") && !plugin.Config.IgnorePermissionsAndIP || 
			    IsGhost(ev.Player)
			) return;
			
			// Add a component to the player to check AFK status.
			AFKComponent afkComponent = ev.Player.GameObject.gameObject.AddComponent<AFKComponent>();
			afkComponent.plugin = plugin;
		}

		/*
		 * The following events are only here as additional AFK checks for some very basic player interactions
		 * I can add more interactions, but this seems good for now.
		 */
		public void OnDoorInteract(InteractingDoorEventArgs ev)
		{
			try
			{
				ResetAFKTime(ev.Player);
			}
			catch (Exception e)
			{
				Log.Error($"ERROR In OnDoorInteract(): {e}");
			}
		}

		public void OnPlayerShoot(ShootingEventArgs ev)
		{
			try
			{
				ResetAFKTime(ev.Shooter);
			}
			catch (Exception e)
			{
				Log.Error($"ERROR In ResetAFKTime(): {e}");
			}
		}
		public void On914Activate(ActivatingEventArgs ev)
		{
			try
			{
				ResetAFKTime(ev.Player);
			}
			catch (Exception e)
			{
				Log.Error($"ERROR In On914Activate(): {e}");
			}
		}
		public void On914Change(ChangingKnobSettingEventArgs ev)
		{
			try
			{
				ResetAFKTime(ev.Player);
			}
			catch (Exception e)
			{
				Log.Error($"ERROR In OnLockerInteract(): {e}");
			}
		}

		public void OnLockerInteract(InteractingLockerEventArgs ev)
		{
			try
			{
				ResetAFKTime(ev.Player);
			}
			catch (Exception e)
			{
				Log.Error($"ERROR In OnLockerInteract(): {e}");
			}
		}
		public void OnDropItem(DroppingItemEventArgs ev)
		{
			try
			{
				ResetAFKTime(ev.Player);
			}
			catch (Exception e)
			{
				Log.Error($"ERROR In OnDropItem(): {e}");
			}
		}

		public void OnSCP079Exp(GainingExperienceEventArgs ev)
		{
			try
			{
				ResetAFKTime(ev.Player);
			}
			catch (Exception e)
			{
				Log.Error($"ERROR In OnSCP079Exp(): {e}");
			}
		}

		/// <summary>
		/// Reset the AFK time of a player.
		/// Thanks iopietro!
		/// </summary>
		/// <param name="player"></param>
		public void ResetAFKTime(Player player)
		{
			try
			{
				if (player == null) return;

				AFKComponent afkComponent = player.GameObject.gameObject.GetComponent<AFKComponent>();

				if (afkComponent != null)
					afkComponent.AFKTime = 0;
				
			}
			catch (Exception e)
			{
				Log.Error($"ERROR In ResetAFKTime(): {e}");
			}
		}

		/// <summary>
		/// Checks if a player is a "ghost" using GhostSpectator's API.
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public static bool IsGhost(Player player)
		{
			Assembly assembly = Loader.Plugins.FirstOrDefault(pl => pl.Name == "GhostSpectator")?.Assembly;
			if (assembly == null) return false;
			return ((bool)assembly.GetType("GhostSpectator.API")?.GetMethod("IsGhost")?.Invoke(null, new object[] { player })) == true;
		}
	}
}
