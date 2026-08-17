using System;
using System.IO;
using Bpsim.Serialization;
using UnityEngine;

namespace Bpsim
{
	[Serializable]
	public class UserSettings
	{
		public Version Version { get; set; }

		public SimulationSettings SimulationSettings { get; set; }

		public ButtonSettings ButtonSettings { get; set; }

		public static UserSettings Default { get; private set; }

		public static UserSettings Instance { get; private set; }

		public UserSettings(Version version)
		{
			Version = version;
			SimulationSettings = new SimulationSettings();
			ButtonSettings = new ButtonSettings();
		}

		public void Update(UserSettings settings)
		{
			SimulationSettings.Update(settings.SimulationSettings);
			ButtonSettings.Update(settings.ButtonSettings);
		}

		public void Reset()
		{
			Update(Default);
		}

		public static void Load()
		{
			Version version = Version.Parse(Application.version);
			Default = new UserSettings(version);
			Instance = new UserSettings(version);
			string path = Application.persistentDataPath + "/UserSettings.json";
			try
			{
				if (!File.Exists(path))
				{
					return;
				}
				using FileStream stream = File.OpenRead(path);
				UserSettings userSettings = Json.Deserialize<UserSettings>(stream);
				if (userSettings != null && userSettings.Version != null && userSettings.Version >= new Version(2023, 0, 0))
				{
					Instance.Update(userSettings);
				}
			}
			catch
			{
			}
		}

		public static void Save()
		{
			string persistentDataPath = Application.persistentDataPath;
			string path = persistentDataPath + "/UserSettings.json";
			try
			{
				if (!Directory.Exists(persistentDataPath))
				{
					Directory.CreateDirectory(persistentDataPath);
				}
				using FileStream stream = File.OpenWrite(path);
				Json.Serialize(stream, Instance);
			}
			catch
			{
			}
		}
	}
}
