﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk;
using CorruptCore;
using NetCore;
using Newtonsoft.Json;
using RTCV.CorruptCore;
using RTCV.NetCore;
using RTCV.Vanguard;

namespace RTC
{
	public static class RTC_EmuCore
	{
		public static string[] args;

		internal static DialogResult ShowErrorDialog(Exception exception, bool canContinue = false)
		{
			return new RTCV.NetCore.CloudDebug(exception, canContinue).Start();


		}


		/// <summary>
		/// Global exceptions in Non User Interfarce(other thread) antipicated error
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		internal static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Exception ex = (Exception)e.ExceptionObject;
			Form error = new RTCV.NetCore.CloudDebug(ex);
			var result = error.ShowDialog();

		}

		/// <summary>
		/// Global exceptions in User Interfarce antipicated error
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		internal static void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
		{
			Exception ex = e.Exception;
			Form error = new RTCV.NetCore.CloudDebug(ex);
			var result = error.ShowDialog();

			Form loaderObject = (sender as Form);

			if (result == DialogResult.Abort)
			{
				if (loaderObject != null)
					NetCore.SyncObjectSingleton.SyncObjectExecute(loaderObject, (o, ea) =>
					{
						loaderObject.Close();
					});
			}
		}

		public static bool attached = false;

		public static string System
		{
			get => (string)EmuSpec[VSPEC.SYSTEM.ToString()];
			set => EmuSpec.Update(VSPEC.SYSTEM.ToString(), value);
		}
		public static string GameName
		{
			get => (string)EmuSpec[VSPEC.GAMENAME.ToString()];
			set => EmuSpec.Update(VSPEC.GAMENAME.ToString(), value);
		}
		public static string SystemPrefix
		{
			get => (string)EmuSpec[VSPEC.SYSTEMPREFIX.ToString()];
			set => EmuSpec.Update(VSPEC.SYSTEMPREFIX.ToString(), value);
		}
		public static string SystemCore
		{
			get => (string)EmuSpec[VSPEC.SYSTEMCORE.ToString()];
			set => EmuSpec.Update(VSPEC.SYSTEMCORE.ToString(), value);
		}
		public static string SyncSettings
		{
			get => (string)EmuSpec[VSPEC.SYNCSETTINGS.ToString()];
			set => EmuSpec.Update(VSPEC.SYNCSETTINGS.ToString(), value);
		}
		public static string OpenRomFilename
		{
			get => (string)EmuSpec[VSPEC.OPENROMFILENAME.ToString()];
			set => EmuSpec.Update(VSPEC.OPENROMFILENAME.ToString(), value);
		}
		public static int LastLoaderRom
		{
			get => (int)EmuSpec[VSPEC.CORE_LASTLOADERROM.ToString()];
			set => EmuSpec.Update(VSPEC.CORE_LASTLOADERROM.ToString(), value);
		}
		public static string[] BlacklistedDomains
		{
			get => (string[])EmuSpec[VSPEC.MEMORYDOMAINS_BLACKLISTEDDOMAINS.ToString()];
			set => EmuSpec.Update(VSPEC.MEMORYDOMAINS_BLACKLISTEDDOMAINS.ToString(), value);
		}
		public static MemoryDomainProxy[] MemoryInterfacees
		{
			get => (MemoryDomainProxy[])EmuSpec[VSPEC.MEMORYDOMAINS_INTERFACES.ToString()];
			set => EmuSpec.Update(VSPEC.MEMORYDOMAINS_INTERFACES.ToString(), value);
		}

		public static PartialSpec getDefaultPartial()
		{
			var partial = new PartialSpec("RTCSpec");

			partial[VSPEC.SYSTEM.ToString()] = String.Empty;
			partial[VSPEC.GAMENAME.ToString()] = String.Empty;
			partial[VSPEC.SYSTEMPREFIX.ToString()] = String.Empty;
			partial[VSPEC.OPENROMFILENAME.ToString()] = String.Empty;
			partial[VSPEC.SYNCSETTINGS.ToString()] = String.Empty;
			partial[VSPEC.OPENROMFILENAME.ToString()] = String.Empty;
			partial[VSPEC.MEMORYDOMAINS_BLACKLISTEDDOMAINS.ToString()] = new string[] { };
			partial[VSPEC.MEMORYDOMAINS_INTERFACES.ToString()] = new MemoryDomainProxy[] { };
			partial[VSPEC.CORE_LASTLOADERROM.ToString()] = -1;

			return partial;
		}

		public static volatile FullSpec EmuSpec;


		public static void RegisterEmuhawkSpec()
		{
			PartialSpec emuSpecTemplate = new PartialSpec("EmuSpec");

			emuSpecTemplate.Insert(RTC_EmuCore.getDefaultPartial());

			EmuSpec = new FullSpec(emuSpecTemplate, !RTC_CorruptCore.Attached); //You have to feed a partial spec as a template

			if (RTC_EmuCore.attached)
				RTCV.Vanguard.VanguardConnector.PushVanguardSpecRef(RTC_EmuCore.EmuSpec);

			LocalNetCoreRouter.Route(NetcoreCommands.CORRUPTCORE, NetcoreCommands.REMOTE_PUSHEMUSPEC, emuSpecTemplate, true);
			LocalNetCoreRouter.Route(NetcoreCommands.UI, NetcoreCommands.REMOTE_PUSHEMUSPEC, emuSpecTemplate, true);


			EmuSpec.SpecUpdated += (o, e) =>
			{
				PartialSpec partial = e.partialSpec;

				if(!RTC_EmuCore.attached)
					RTCV.NetCore.AllSpec.VanguardSpec = EmuSpec;

				LocalNetCoreRouter.Route(NetcoreCommands.CORRUPTCORE, NetcoreCommands.REMOTE_PUSHEMUSPECUPDATE, partial, true);
				LocalNetCoreRouter.Route(NetcoreCommands.UI, NetcoreCommands.REMOTE_PUSHEMUSPECUPDATE, partial, true);
			};
		}

		//This is the entry point of RTC. Without this method, nothing will load.
		public static void Start(RTC_Standalone_Form _standaloneForm = null)
		{
			//Grab an object on the main thread to use for netcore invokes
			SyncObjectSingleton.SyncObject = GlobalWin.MainForm;

			//Start everything
			VanguardImplementation.StartClient();
			RTC_EmuCore.RegisterEmuhawkSpec();
			RTC_CorruptCore.StartEmuSide();

			//Refocus on Bizhawk
			RTC_Hooks.BIZHAWK_MAINFORM_FOCUS();

			//Force create bizhawk config file if it doesn't exist
			if (!File.Exists(RTC_CorruptCore.bizhawkDir + Path.DirectorySeparatorChar + "config.ini"))
				RTC_Hooks.BIZHAWK_MAINFORM_SAVECONFIG();

			//If it's attached, lie to vanguard
			if (RTC_EmuCore.attached)
				VanguardConnector.ImplyClientConnected();
		}


		public static void StartSound()
		{
			RTC_Hooks.BIZHAWK_STARTSOUND();
		}

		public static void StopSound()
		{
			RTC_Hooks.BIZHAWK_STOPSOUND();
		}


		public static string EmuFolderCheck(string SystemDisplayName)
		{
			//Workaround for Bizhawk's folder name quirk

			if (SystemDisplayName.Contains("(INTERIM)"))
			{
				char[] delimiters = { '(', ' ', ')' };

				string temp = SystemDisplayName.Split(delimiters)[0];
				SystemDisplayName = temp + "_INTERIM";
			}
			switch (SystemDisplayName)
			{
				case "Playstation":
					return "PSX";
				case "GG":
					return "Game Gear";
				case "Commodore 64":
					return "C64";
				case "SG":
					return "SG-1000";
				default:
					return SystemDisplayName;
			}
		}
		/// <summary>
		/// Loads a NES-based title screen.
		/// Can be overriden by putting a file named "overridedefault.nes" in the ASSETS folder
		/// </summary>
		public static void LoadDefaultRom()
		{
			int lastLoaderRom = RTC_EmuCore.LastLoaderRom;
			int newNumber = RTC_EmuCore.LastLoaderRom;

			while (newNumber == lastLoaderRom)
			{
				int nbNesFiles = Directory.GetFiles(RTC_CorruptCore.assetsDir, "*.nes").Length;

				newNumber = RTC_CorruptCore.RND.Next(1, nbNesFiles + 1);

				if (newNumber != lastLoaderRom)
				{
					if (File.Exists(RTC_CorruptCore.assetsDir + "overridedefault.nes"))
						LoadRom_NET(RTC_CorruptCore.assetsDir + "overridedefault.nes");
					//Please ignore
					else if (RTC_CorruptCore.RND.Next(0, 420) == 7)
						LoadRom_NET(RTC_CorruptCore.assetsDir + "gd.fds");
					else
						LoadRom_NET(RTC_CorruptCore.assetsDir + newNumber.ToString() + "default.nes");

					lastLoaderRom = newNumber;
					break;
				}
			}
		}

		/// <summary>
		/// Loads a rom within Bizhawk. To be called from within Bizhawk only
		/// </summary>
		/// <param name="RomFile"></param>
		public static void LoadRom_NET(string RomFile)
		{
			var loadRomWatch = Stopwatch.StartNew();

			StopSound();

			if (RomFile == null)
				RomFile = RTC_Hooks.BIZHAWK_GET_CURRENTLYOPENEDROM(); ;


			//Stop capturing rewind while we load
			RTC_Hooks.AllowCaptureRewindState = false;
			RTC_Hooks.BIZHAWK_LOADROM(RomFile);
			RTC_Hooks.AllowCaptureRewindState = true;

			StartSound();
			loadRomWatch.Stop();
			Console.WriteLine($"Time taken for LoadRom_NET: {0}ms", loadRomWatch.ElapsedMilliseconds);
		}

		/// <summary>
		/// Creates a savestate using a key as the filename and returns the path.
		/// Bizhawk process only.
		/// </summary>
		/// <param name="Key"></param>
		/// <param name="threadSave"></param>
		/// <returns></returns>
		public static string SaveSavestate_NET(string Key, bool threadSave = false)
		{
			//Don't state if we don't have a core
			if (RTC_Hooks.BIZHAWK_ISNULLEMULATORCORE())
				return null;

			//Build the shortname
			string quickSlotName = Key + ".timejump";

			//Get the prefix for the state
			string prefix = RTC_Hooks.BIZHAWK_GET_SAVESTATEPREFIX();
			prefix = prefix.Substring(prefix.LastIndexOf('\\') + 1);

			//Build up our path
			var path = RTC_CorruptCore.workingDir + Path.DirectorySeparatorChar + "SESSION" + Path.DirectorySeparatorChar + prefix + "." + quickSlotName + ".State";

			//If the path doesn't exist, make it
			var file = new FileInfo(path);
			if (file.Directory != null && file.Directory.Exists == false)
				file.Directory.Create();

			//Savestates on a new thread. Doesn't work properly as Bizhawk doesn't support threaded states
			if (threadSave)
			{
				(new Thread(() =>
				{
					try
					{
						RTC_Hooks.BIZHAWK_SAVESTATE(path, quickSlotName);
					}
					catch (Exception ex)
					{
						Console.WriteLine("Thread collision ->\n" + ex.ToString());
					}
				})).Start();
			}
			else
				RTC_Hooks.BIZHAWK_SAVESTATE(path, quickSlotName); //savestate

			return path;
		}

		/// <summary>
		/// Loads a savestate from a path. 
		/// </summary>
		/// <param name="path">The path of the state</param>
		/// <param name="stateLocation">Where the state is located in a stashkey (used for errors, not required)</param>
		/// <returns></returns>
		public static bool LoadSavestate_NET(string path, StashKeySavestateLocation stateLocation = StashKeySavestateLocation.DEFAULTVALUE)
		{
			try
			{
				//If we don't have a core just exit out
				if (RTC_Hooks.BIZHAWK_ISNULLEMULATORCORE())
					return false;

				//If we can't find the file, throw a message
				if (File.Exists(path) == false)
				{
					RTC_Hooks.BIZHAWK_OSDMESSAGE("Unable to load " + Path.GetFileName(path) + " from " + stateLocation);
					return false;
				}

				RTC_Hooks.BIZHAWK_LOADSTATE(path);

				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
				return false;
			}
		}

		/// <summary>
		/// Loads the window size/position from a param
		/// </summary>
		public static void LoadBizhawkWindowState()
		{
			if (RTCV.NetCore.Params.IsParamSet("BIZHAWK_SIZE"))
			{
				string[] size = RTCV.NetCore.Params.ReadParam("BIZHAWK_SIZE").Split(',');
				RTC_Hooks.BIZHAWK_GETSET_MAINFORMSIZE = new Size(Convert.ToInt32(size[0]), Convert.ToInt32(size[1]));
				string[] location = RTCV.NetCore.Params.ReadParam("BIZHAWK_LOCATION").Split(',');
				RTC_Hooks.BIZHAWK_GETSET_MAINFORMLOCATION = new Point(Convert.ToInt32(location[0]), Convert.ToInt32(location[1]));
			}
		}
		/// <summary>
		/// Saves the window size/position to a param
		/// </summary>
		public static void SaveBizhawkWindowState()
		{
			var size = RTC_Hooks.BIZHAWK_GETSET_MAINFORMSIZE;
			var location = RTC_Hooks.BIZHAWK_GETSET_MAINFORMLOCATION;

			RTCV.NetCore.Params.SetParam("BIZHAWK_SIZE", $"{size.Width},{size.Height}");
			RTCV.NetCore.Params.SetParam("BIZHAWK_LOCATION", $"{location.X},{location.Y}");
		}

		/// <summary>
		/// Loads the default rom and shows bizhawk
		/// </summary>
		public static void LoadDefaultAndShowBizhawkForm()
		{

			RTC_EmuCore.LoadDefaultRom();
			RTC_EmuCore.LoadBizhawkWindowState();
			GlobalWin.MainForm.Focus();

			//Yell at the user if they're using audio throttle as it's buggy
			if (Global.Config.SoundThrottle)
			{
				MessageBox.Show("Sound throttle is buggy and can result in crashes.\nSwapping to clock throttle.");
				Global.Config.SoundThrottle = false;
				Global.Config.ClockThrottle = true;
				RTC_Hooks.BIZHAWK_MAINFORM_SAVECONFIG();
			}
		}


		/// <summary>
		/// Returns the list of domains that are blacklisted from being auto-selected
		/// </summary>
		/// <param name="systemName"></param>
		/// <returns></returns>
		public static string[] GetBlacklistedDomains(string systemName)
		{
			// Returns the list of Domains that can't be rewinded and/or are just not good to use

			List<string> domainBlacklist = new List<string>();
			switch (systemName)
			{
				case "NES":     //Nintendo Entertainment system

					domainBlacklist.Add("System Bus");
					domainBlacklist.Add("PRG ROM");
					domainBlacklist.Add("PALRAM"); //Color Memory (Useless and disgusting)
					domainBlacklist.Add("CHR VROM"); //Cartridge
					domainBlacklist.Add("Battery RAM"); //Cartridge Save Data
					domainBlacklist.Add("FDS Side"); //ROM data for the FDS. Sadly uncorruptable.
					break;

				case "GB":      //Gameboy
				case "GBC":     //Gameboy Color
					domainBlacklist.Add("ROM"); //Cartridge
					domainBlacklist.Add("System Bus");
					domainBlacklist.Add("OBP"); //SGB dummy domain doesn't do anything in sameboy
					domainBlacklist.Add("BGP");  //SGB dummy domain doesn't do anything in sameboy
					domainBlacklist.Add("BOOTROM"); //Sameboy SGB Bootrom
					break;

				case "SNES":    //Super Nintendo

					domainBlacklist.Add("CARTROM"); //Cartridge
					domainBlacklist.Add("APURAM"); //SPC700 memory
					domainBlacklist.Add("CGRAM"); //Color Memory (Useless and disgusting)
					domainBlacklist.Add("System Bus"); // maxvalue is not representative of chip (goes ridiculously high)
					domainBlacklist.Add("SGB CARTROM"); // Supergameboy cartridge

					if (RTC_MemoryDomains.MemoryInterfaces.ContainsKey("SGB CARTROM"))
					{
						domainBlacklist.Add("VRAM");
						domainBlacklist.Add("WRAM");
						domainBlacklist.Add("CARTROM");
					}

					break;

				case "N64":     //Nintendo 64
					domainBlacklist.Add("System Bus");
					domainBlacklist.Add("PI Register");
					domainBlacklist.Add("EEPROM");
					domainBlacklist.Add("ROM");
					domainBlacklist.Add("SI Register");
					domainBlacklist.Add("VI Register");
					domainBlacklist.Add("RI Register");
					domainBlacklist.Add("AI Register");
					break;

				case "PCE":     //PC Engine / Turbo Grafx
				case "SGX":     //Super Grafx
					domainBlacklist.Add("ROM");
					domainBlacklist.Add("System Bus"); //BAD THINGS HAPPEN WITH THIS DOMAIN
					domainBlacklist.Add("System Bus (21 bit)");
					break;

				case "GBA":     //Gameboy Advance
					domainBlacklist.Add("OAM");
					domainBlacklist.Add("BIOS");
					domainBlacklist.Add("PALRAM");
					domainBlacklist.Add("ROM");
					domainBlacklist.Add("System Bus");
					break;

				case "SMS":     //Sega Master System
					domainBlacklist.Add("System Bus"); // the game cartridge appears to be on the system bus
					domainBlacklist.Add("ROM");
					break;

				case "GG":      //Sega GameGear
					domainBlacklist.Add("System Bus"); // the game cartridge appears to be on the system bus
					domainBlacklist.Add("ROM");
					break;

				case "SG":      //Sega SG-1000
					domainBlacklist.Add("System Bus");
					domainBlacklist.Add("ROM");
					break;

				case "32X_INTERIM":
				case "GEN":     //Sega Genesis and CD
					domainBlacklist.Add("MD CART");
					domainBlacklist.Add("CRAM"); //Color Ram
					domainBlacklist.Add("VSRAM"); //Vertical scroll ram. Do you like glitched scrolling? Have a dedicated domain...
					domainBlacklist.Add("SRAM"); //Save Ram
					domainBlacklist.Add("BOOT ROM"); //Genesis Boot Rom
					domainBlacklist.Add("32X FB"); //32X Sprinkles
					domainBlacklist.Add("CD BOOT ROM"); //Sega CD boot rom
					domainBlacklist.Add("S68K BUS");
					domainBlacklist.Add("M68K BUS");
					break;

				case "PSX":     //Sony Playstation 1
					domainBlacklist.Add("BiosROM");
					domainBlacklist.Add("PIOMem");
					break;

				case "A26":     //Atari 2600
					domainBlacklist.Add("System Bus");
					break;

				case "A78":     //Atari 7800
					domainBlacklist.Add("System Bus");
					break;

				case "LYNX":    //Atari Lynx
					domainBlacklist.Add("Save RAM");
					domainBlacklist.Add("Cart B");
					domainBlacklist.Add("Cart A");
					break;

				case "WSWAN":   //Wonderswan
					domainBlacklist.Add("ROM");
					break;

				case "Coleco":  //Colecovision
					domainBlacklist.Add("System Bus");
					break;

				case "VB":      //Virtualboy
					domainBlacklist.Add("ROM");
					break;

				case "SAT":     //Sega Saturn
					domainBlacklist.Add("Backup RAM");
					domainBlacklist.Add("Boot Rom");
					domainBlacklist.Add("Backup Cart");
					domainBlacklist.Add("VDP1 Framebuffer"); //Sprinkles
					domainBlacklist.Add("VDP2 CRam"); //VDP 2 color ram (pallettes)
					domainBlacklist.Add("Sound Ram"); //90% chance of killing the audio
					break;

				case "INTV": //Intellivision
					domainBlacklist.Add("Graphics ROM");
					domainBlacklist.Add("System ROM");
					domainBlacklist.Add("Executive Rom"); //??????
					break;

				case "APPLEII": //Apple II
					domainBlacklist.Add("System Bus");
					break;

				case "C64":     //Commodore 64
					domainBlacklist.Add("System Bus");
					domainBlacklist.Add("1541 Bus");
					break;

				case "PCECD":   //PC-Engine CD / Turbo Grafx CD
				case "TI83":    //Ti-83 Calculator
				case "SGB":     //Super Gameboy
				case "DGB":
					break;

					//TODO: Add more domains for cores like gamegear, atari, turbo graphx
			}

			return domainBlacklist.ToArray();;
		}

	}
}