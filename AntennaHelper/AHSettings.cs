using System;
using UnityEngine;

namespace AntennaHelper
{
	public static class AHSettings
	{
		private static ConfigNode settingsNode;

		private static ConfigNode nodePosWindows;

		// Editor
		public static Vector2 posMainWindow = new Vector2 (Screen.width / 2f, Screen.height / 2f);
		public static Vector2 posTargetWindow = new Vector2 (Screen.width / 2f - 400f, Screen.height / 2f);
		public static Vector2 posPlanetWindow = new Vector2 (Screen.width / 2f + 400f, Screen.height / 2f);

		// Flight
		public static Vector2 posFlightMainWindow = new Vector2 (Screen.width / 2f, Screen.height / 2f);
		public static Vector2 posFlightMapViewWindow = new Vector2 ((Screen.width / 2f + 300f), (Screen.height / 2f));

		// Tracking Station
		public static Vector2 posTrackingStationMainWindow = new Vector2 (Screen.width - 150f, Screen.height - 285f);
		public static Vector2 posTrackingStationShipWindow = new Vector2 (Screen.width - 500f, Screen.height - 285f);

		static AHSettings ()
		{
			Load ();

			WriteSave ();
		}

		public static void Load ()
		{
			// Check for the settings file
			settingsNode = ConfigNode.Load (RegisterToolbar.ApplicationRootPath + "GameData/AntennaHelper/PluginData/Settings.cfg");
			if (settingsNode == null) {
				settingsNode = new ConfigNode ();
			}

			// Check for nodes in settings file
			if (! settingsNode.HasNode("Windows_Position")) {
				settingsNode.AddNode ("Windows_Position");
			}
			nodePosWindows = settingsNode.GetNode ("Windows_Position");

			// Check for value in nodes
			// Editor window position
			if (nodePosWindows.HasValue ("editor_main_window_position")) {
				posMainWindow = ConfigNode.ParseVector2 (nodePosWindows.GetValue ("editor_main_window_position"));
			}
			nodePosWindows.SetValue ("editor_main_window_position", posMainWindow, true);

			if (nodePosWindows.HasValue ("editor_target_window_position")) {
				posTargetWindow = ConfigNode.ParseVector2 (nodePosWindows.GetValue ("editor_target_window_position"));
			}
			nodePosWindows.SetValue ("editor_target_window_position", posTargetWindow, true);

			if (nodePosWindows.HasValue ("editor_signal_strenght_per_planet_window_position")) {
				posPlanetWindow = ConfigNode.ParseVector2 (nodePosWindows.GetValue ("editor_signal_strenght_per_planet_window_position"));
			}
			nodePosWindows.SetValue ("editor_signal_strenght_per_planet_window_position", posPlanetWindow, true);

			// Flight window position
			if (nodePosWindows.HasValue ("flight_main_window_position")) {
				posFlightMainWindow = ConfigNode.ParseVector2 (nodePosWindows.GetValue ("flight_main_window_position"));
			}
			nodePosWindows.SetValue ("flight_main_window_position", posFlightMainWindow, true);

			if (nodePosWindows.HasValue ("flight_map_view_window_position")) {
				posFlightMapViewWindow = ConfigNode.ParseVector2 (nodePosWindows.GetValue ("flight_map_view_window_position"));
			}
			nodePosWindows.SetValue ("flight_map_view_window_position", posFlightMapViewWindow, true);

			// Tracking Station window position
			if (nodePosWindows.HasValue ("tracking_station_main_window_position")) {
				posTrackingStationMainWindow = ConfigNode.ParseVector2 (nodePosWindows.GetValue ("tracking_station_main_window_position"));
			}
			nodePosWindows.SetValue ("tracking_station_main_window_position", posTrackingStationMainWindow, true);

			if (nodePosWindows.HasValue ("tracking_station_ship_window_position")) {
				posTrackingStationShipWindow = ConfigNode.ParseVector2 (nodePosWindows.GetValue ("tracking_station_ship_window_position"));
			}
			nodePosWindows.SetValue ("tracking_station_ship_window_position", posTrackingStationShipWindow, true);
		}

		public static void SavePosition (string windowName, Vector2 position)
		{
			nodePosWindows.SetValue (windowName, position, true);
		}

		public static void WriteSave ()
		{
			settingsNode.Save (RegisterToolbar.ApplicationRootPath + "GameData/AntennaHelper/PluginData/Settings.cfg");
			Load ();
		}
	}
}

