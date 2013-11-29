﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BizHawk.Client.Common
{
	public static class HeaderKeys
	{
		public const string MovieVersion = "BizHawk v0.0.1";

		public const string EMULATIONVERSION = "emuVersion";
		public const string MOVIEVERSION = "MovieVersion";
		public const string PLATFORM = "Platform";
		public const string GAMENAME = "GameName";
		public const string AUTHOR = "Author";
		public const string RERECORDS = "rerecordCount";
		public const string GUID = "GUID";
		public const string STARTSFROMSAVESTATE = "StartsFromSavestate";
		public const string FOURSCORE = "FourScore";
		public const string SHA1 = "SHA1";
		public const string FIRMWARESHA1 = "FirmwareSHA1";
		public const string PAL = "PAL";
		public const string BOARDNAME = "BoardName";

		//Gameboy Settings that affect sync
		public const string GB_FORCEDMG = "Force_DMG_Mode";
		public const string GB_GBA_IN_CGB = "GBA_In_CGB";
		public const string SGB = "SGB"; //a snes movie will set this to indicate that it's actually SGB

		//BIO skipping setting (affects sync)
		public const string SKIPBIOS = "Skip_Bios";

		//Plugin Settings
		public const string VIDEOPLUGIN = "VideoPlugin";

		//Board properties
		public const string BOARDPROPERTIES = "BoardProperty";

		public static string NewGuid
		{
			get
			{
				return Guid.NewGuid().ToString();
			}
		}

		public static bool Contains(string val)
		{
			var keys = new List<string>();
			foreach (FieldInfo field in typeof(HeaderKeys).GetFields())
			{
				keys.Add(field.GetValue(null).ToString());
			}

			return keys.Contains(val);
		}
	}
}
