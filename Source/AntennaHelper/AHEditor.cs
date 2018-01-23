﻿using System;
using System.Collections.Generic;
using UnityEngine;
using ToolbarControl_NS;

namespace AntennaHelper
{
	[KSPAddon (KSPAddon.Startup.EditorAny, false)]
	public class AHEditor : MonoBehaviour
	{
		private static AHEditor instance;
		public static float trackingStationLevel;

		public void Start ()
		{
			instance = this;

			trackingStationLevel = ScenarioUpgradeableFacilities.GetFacilityLevel (SpaceCenterFacility.TrackingStation);
			targetPower = GameVariables.Instance.GetDSNRange (trackingStationLevel);
			targetName = "DSN Level " + (int)(trackingStationLevel * 2 + 1);

			GetShipList ();

			GameEvents.onGUIApplicationLauncherReady.Add (AddToolbarButton);
			GameEvents.onGUIApplicationLauncherDestroyed.Add (RemoveToolbarButton);

			GameEvents.onEditorLoad.Add (VesselLoad);
			GameEvents.onEditorPartEvent.Add (PartEvent);
			GameEvents.onEditorPodPicked.Add (PodPicked);
			GameEvents.onEditorPodDeleted.Add (PodDeleted);

			GameEvents.onGameSceneSwitchRequested.Add (QuitEditor);
		}

		public void OnDestroy ()
		{
			GameEvents.onGUIApplicationLauncherReady.Remove (AddToolbarButton);
			GameEvents.onGUIApplicationLauncherDestroyed.Remove (RemoveToolbarButton);
			RemoveToolbarButton ();

			GameEvents.onEditorLoad.Remove (VesselLoad);
			GameEvents.onEditorPartEvent.Remove (PartEvent);
			GameEvents.onEditorPodPicked.Remove (PodPicked);
			GameEvents.onEditorPodDeleted.Remove (PodDeleted);

			GameEvents.onGameSceneSwitchRequested.Remove (QuitEditor);
		}

		public void QuitEditor (GameEvents.FromToAction<GameScenes, GameScenes> eData)
		{
			AHSettings.WriteSave ();
		}

		public void VesselLoad (ShipConstruct ship, KSP.UI.Screens.CraftBrowserDialog.LoadType screenType)
		{
			if (showMainWindow || showPlanetWindow || showTargetWindow) {
				CreateAntennaList ();
				DoTheMath ();
			}
		}

		public void PartEvent (ConstructionEventType eventType, Part part)
		{
			if (showMainWindow || showPlanetWindow || showTargetWindow) {
				if (eventType == ConstructionEventType.PartAttached) {
					AntennaListAddItem (part);

					// Symmetry counterparts
					foreach (Part symPart in part.symmetryCounterparts) {
						AntennaListAddItem (symPart);
					}

					// Child part
					foreach (Part childPart in part.children) {
						AntennaListAddItem (childPart);
					}

					DoTheMath ();

				} else if (eventType == ConstructionEventType.PartDetached) {
					AntennaListRemoveItem (part);
					List<ModuleDataTransmitter> remAntenna = new List<ModuleDataTransmitter> ();
					foreach (ModuleDataTransmitter antennaSym in directAntennaList) {
						if (antennaSym.part.isSymmetryCounterPart (part)) {
							remAntenna.Add (antennaSym);
						}
					}

					// Child part
					foreach (Part childPart in part.children) {
						AntennaListRemoveItem (childPart);
					}

					foreach (ModuleDataTransmitter remA in remAntenna) {
						AntennaListRemoveItem (remA);
					}
					DoTheMath ();
				}
			}
		}

		public void PodDeleted ()
		{
			CreateAntennaList ();
			DoTheMath ();
		}

		public void PodPicked (Part part = null)
		{
			CreateAntennaList ();
			DoTheMath ();
		}

		#region Logic
		public List<ModuleDataTransmitter> directAntennaList = new List<ModuleDataTransmitter> ();// Main list
		public List<ModuleDataTransmitter> relayAntennaList = new List<ModuleDataTransmitter> ();

		public List<ModuleDataTransmitter> directCombAntennaList = new List<ModuleDataTransmitter> ();
		public List<ModuleDataTransmitter> relayCombAntennaList = new List<ModuleDataTransmitter> ();

		public static int nbDirectAntenna = 0;
		public static int nbDirectCombAntenna = 0;

		public static int nbRelayAntenna = 0;
		public static int nbRelayCombAntenna = 0;

		public static double directPower;
		public static double directCombPower;
		private double rawDirectPower;
		private double rawDirectCombPower;

		public static double relayPower;
		public static double relayCombPower;
		private double rawRelayPower;
		private double rawRelayCombPower;

		public static double directRange;
		public static double directCombRange;

		public static double relayRange;
		public static double relayCombRange;

		public static string directAntennaName = "";
		public static string relayAntennaName = "";

		public static List<MyTuple> relaySignalPerPlanet;
		public static List<MyTuple> directSignalPerPlanet;

		public static double directDistanceAt100;
		public static double directDistanceAt75;
		public static double directDistanceAt50;
		public static double directDistanceAt25;

		public static double relayDistanceAt100;
		public static double relayDistanceAt75;
		public static double relayDistanceAt50;
		public static double relayDistanceAt25;

		public void DoTheMath ()
		{
			// Direct antenna (not-relay)
			nbDirectAntenna = directAntennaList.Count;
			nbDirectCombAntenna = directCombAntennaList.Count;

			// Direct combinable :
			if (nbDirectCombAntenna > 0) {
				rawDirectCombPower = AHUtil.GetVesselPower (directCombAntennaList, false);
				directCombPower = AHUtil.TruePower (rawDirectCombPower);//AHUtil.GetVesselPower (directCombAntennaList);
				directCombRange = AHUtil.GetRange (directCombPower, targetPower);
			} else {
				rawDirectCombPower = 0;
				directCombPower = 0;
				directCombRange = 0;
			}


			// Direct straight :
			if (nbDirectAntenna > 0) {
				ModuleDataTransmitter bigDirect = null;
				foreach (ModuleDataTransmitter antenna in directAntennaList) {
					if (bigDirect == null || bigDirect.antennaPower < antenna.antennaPower) {
						bigDirect = antenna;
					}
				}
				rawDirectPower = bigDirect.antennaPower;
				directPower = AHUtil.TruePower (rawDirectPower);//AHUtil.TruePower (bigDirect.antennaPower);
				directRange = AHUtil.GetRange (directPower, targetPower);
				directAntennaName = bigDirect.part.partInfo.title;
			} else {
				rawDirectPower = 0;
				directPower = 0;
				directRange = 0;
				directAntennaName = "No Antenna";
			}


			// Relay antenna :
			nbRelayAntenna = relayAntennaList.Count;
			nbRelayCombAntenna = relayCombAntennaList.Count;

			// Relay combinable :
			if (nbRelayCombAntenna > 0) {
				rawRelayCombPower = AHUtil.GetVesselPower (relayCombAntennaList, false);
				relayCombPower = AHUtil.TruePower (rawRelayCombPower);//AHUtil.GetVesselPower (relayCombAntennaList);
				relayCombRange = AHUtil.GetRange (relayCombPower, targetPower);
			} else {
				rawRelayCombPower = 0;
				relayCombPower = 0;
				relayCombRange = 0;
			}


			// Relay straight :
			if (nbRelayAntenna > 0) {
				ModuleDataTransmitter bigRelay = null;
				foreach (ModuleDataTransmitter antenna in relayAntennaList) {
					if (bigRelay == null || bigRelay.antennaPower < antenna.antennaPower) {
						bigRelay = antenna;
					}
				}
				rawRelayPower = bigRelay.antennaPower;
				relayPower = AHUtil.TruePower (rawRelayPower);//AHUtil.TruePower (bigRelay.antennaPower);
				relayRange = AHUtil.GetRange (relayPower, targetPower);
				relayAntennaName = bigRelay.part.partInfo.title;
			} else {
				rawRelayPower = 0;
				relayPower = 0;
				relayRange = 0;
				relayAntennaName = "No Antenna";
			}

			FetchBetterAntennas ();
			FetchAntennaStatus ();
			SetPerPlanetList ();

			directDistanceAt100 = AHUtil.GetDistanceAt100 (directBetterRange);
			directDistanceAt75 = AHUtil.GetDistanceAt75 (directBetterRange);
			directDistanceAt50 = AHUtil.GetDistanceAt50 (directBetterRange);
			directDistanceAt25 = AHUtil.GetDistanceAt25 (directBetterRange);

			relayDistanceAt100 = AHUtil.GetDistanceAt100 (relayBetterRange);
			relayDistanceAt75 = AHUtil.GetDistanceAt75 (relayBetterRange);
			relayDistanceAt50 = AHUtil.GetDistanceAt50 (relayBetterRange);
			relayDistanceAt25 = AHUtil.GetDistanceAt25 (relayBetterRange);
		}

		public static double directBetterPower;
		public static double directBetterRange;
		private double rawDirectBetterPower;

		public static double relayBetterPower;
		public static double relayBetterRange;
		private double rawRelayBetterPower;

		private void FetchBetterAntennas ()
		{
			if (directRange > directCombRange || directPower > directCombPower) {
				rawDirectBetterPower = rawDirectPower;
				directBetterPower = directPower;
				directBetterRange = directRange;
			} else {
				rawDirectBetterPower = rawDirectCombPower;
				directBetterPower = directCombPower;
				directBetterRange = directCombRange;
			}

			if (relayRange > relayCombRange || relayPower > relayCombPower) {
				rawRelayBetterPower = rawRelayPower;
				relayBetterPower = relayPower;
				relayBetterRange = relayRange;
			} else {
				rawRelayBetterPower = rawRelayCombPower;
				relayBetterPower = relayCombPower;
				relayBetterRange = relayCombRange;
			}
		}

		public static string statusStringDirect;
		public static string statusStringRelay;

		private void FetchAntennaStatus ()
		{
			// DIRECT
			if (nbDirectAntenna == 0) {
				statusStringDirect = "No antenna";
			} else if (nbDirectAntenna == 1) {
				statusStringDirect = "One antenna : " + directAntennaName;
			} else {
				if (nbDirectCombAntenna < 2) {
					statusStringDirect = nbDirectAntenna + " antennas, not combinable, "
					+ directAntennaName + " is the most powerfull";
				} else {
					statusStringDirect = nbDirectCombAntenna + " of " + nbDirectAntenna 
					+ " antennas are combinable";
				}
			}

			// RELAY
			if (nbRelayAntenna == 0) {
				statusStringRelay = "No antenna";
			} else if (nbRelayAntenna == 1) {
				statusStringRelay = "One antenna : " + relayAntennaName;
			} else {
				if (nbRelayCombAntenna < 2) {
					statusStringRelay = nbRelayAntenna + " antennas, not combinable, "
						+ relayAntennaName + " is the most powerfull";
				} else {
					statusStringRelay = nbRelayCombAntenna + " of " + nbRelayAntenna 
					+ " antennas are combinable";
				}
			}
		}

		public static List<double> signalMinDirect;
		public static List<double> signalMaxDirect;
		public static List<double> signalMinRelay;
		public static List<double> signalMaxRelay;

		private void SetPerPlanetList ()
		{
			signalMinDirect = new List<double> ();
			signalMaxDirect = new List<double> ();
			signalMinRelay = new List<double> ();
			signalMaxRelay = new List<double> ();

			foreach (MyTuple planet in AHUtil.signalPlanetList) {
				signalMinDirect.Add (AHUtil.GetSignalStrength (directBetterRange, planet.item2));
				signalMaxDirect.Add (AHUtil.GetSignalStrength (directBetterRange, planet.item3));
				signalMinRelay.Add (AHUtil.GetSignalStrength (relayBetterRange, planet.item2));
				signalMaxRelay.Add (AHUtil.GetSignalStrength (relayBetterRange, planet.item3));
			}
		}

		public static double signalCustomDistanceDirect = 0;
		public static double signalCustomDistanceRelay = 0;
		public static string customDistance = "";

		public static void CalcCustomDistance ()
		{
			signalCustomDistanceDirect = AHUtil.GetSignalStrength (directBetterRange, Double.Parse (customDistance));
			signalCustomDistanceRelay = AHUtil.GetSignalStrength (relayBetterRange, Double.Parse (customDistance));

		}

		public static double targetPower = 0;
		public static string targetName = "";

		public static void SetTarget (float dsnL)
		{
			targetPower = GameVariables.Instance.GetDSNRange (dsnL);
			targetName = "DSN Level " + (int)((dsnL * 2) + 1);
			instance.DoTheMath ();
		}

		public static void SetTarget (KeyValuePair<string, Dictionary <string, string>> relay)
		{
			targetPower = AHUtil.TruePower (Double.Parse (relay.Value ["powerRelay"]));
			targetName = "Vessel : " + relay.Value ["name"];
			instance.DoTheMath ();
		}

		public void CreateAntennaList ()
		{
			directAntennaList = new List<ModuleDataTransmitter> ();
			directCombAntennaList = new List<ModuleDataTransmitter> ();
			relayAntennaList = new List<ModuleDataTransmitter> ();
			relayCombAntennaList = new List<ModuleDataTransmitter> ();

			foreach (Part part in EditorLogic.fetch.ship.Parts) {
				foreach (ModuleDataTransmitter antenna in part.Modules.GetModules<ModuleDataTransmitter> ()) {
					directAntennaList.Add (antenna);
					if (antenna.antennaCombinable) {
						directCombAntennaList.Add (antenna);
					}
					if (antenna.antennaType == AntennaType.RELAY) {
						relayAntennaList.Add (antenna);
						if (antenna.antennaCombinable) {
							relayCombAntennaList.Add (antenna);
						}
					}
				}
			}
		}

		public void AntennaListAddItem (ModuleDataTransmitter antenna)
		{
			directAntennaList.Add (antenna);
			if (antenna.antennaCombinable) {
				directCombAntennaList.Add (antenna);
			}
			if (antenna.antennaType == AntennaType.RELAY) {
				relayAntennaList.Add (antenna);
				if (antenna.antennaCombinable) {
					relayCombAntennaList.Add (antenna);
				}
			}
		}

		public void AntennaListAddItem (Part part)
		{
			if (part.Modules.Contains<ModuleDataTransmitter> ()) {
				foreach (ModuleDataTransmitter antenna in part.Modules.GetModules<ModuleDataTransmitter> ()) {
					AntennaListAddItem (antenna);
				}
			}
		}

		public void AntennaListRemoveItem (ModuleDataTransmitter antenna)
		{
			if (directAntennaList.Contains (antenna)) {
				directAntennaList.Remove (antenna);
			}
			if (directCombAntennaList.Contains (antenna)) {
				directCombAntennaList.Remove (antenna);
			}
			if (relayAntennaList.Contains (antenna)) {
				relayAntennaList.Remove (antenna);
			}
			if (relayCombAntennaList.Contains (antenna)) {
				relayCombAntennaList.Remove (antenna);
			}
		}

		public void AntennaListRemoveItem (Part part)
		{
			if (part.Modules.Contains<ModuleDataTransmitter> ()) {
				foreach (ModuleDataTransmitter antenna in part.Modules.GetModules<ModuleDataTransmitter> ()) {
					AntennaListRemoveItem (antenna);
				}
			}
		}
		#endregion

		#region ExternalShipList
		public static Dictionary<string, Dictionary <string, string>> externListShipEditor;
		public static Dictionary<string, Dictionary <string, string>> externListShipFlight;
		public static void AddShipToShipList ()
		{
			string type;
			if (EditorDriver.editorFacility == EditorFacility.SPH) {
				type = "SPH";
			} else {
				type = "VAB";
			}
			AHShipList.SaveShip (EditorLogic.fetch.ship.shipName, type, instance.rawDirectBetterPower.ToString(), instance.rawRelayBetterPower.ToString ());
			instance.GetShipList ();

		}

		private void GetShipList ()
		{
			externListShipEditor = AHShipList.GetShipList (true, false);
			externListShipFlight = AHShipList.GetShipList (false, true);
			Debug.Log ("[AH] there is " + externListShipEditor.Count + " ships in the ship list");
		}
		#endregion

		#region GUI
		public static bool showMainWindow = false;
		public static Rect rectMainWindow = new Rect (AHSettings.posMainWindow, new Vector2 (400, 200));
		public static void CloseMainWindow ()
		{
			if (showMainWindow) {
				AHSettings.SavePosition ("main_window_position", rectMainWindow.position);
			}
			showMainWindow = false;
		}

		public static bool showTargetWindow = false;
		public static Rect rectTargetWindow = new Rect (AHSettings.posTargetWindow, new Vector2 (400, 80));
		public static void CloseTargetWindow ()
		{
			if (showTargetWindow) {
				AHSettings.SavePosition ("target_window_position", rectTargetWindow.position);
			}
			showTargetWindow = false;
			CloseTargetShipEditorWindow ();
			CloseTargetShipFlightWindow ();
		}

		public static bool showTargetShipEditorWindow = false;
		public static Rect rectTargetShipEditorWindow = 
			new Rect (new Vector2 (rectTargetWindow.position.x, rectTargetWindow.position.y + rectTargetWindow.height)
				, new Vector2 (400, 80));
		public static void CloseTargetShipEditorWindow ()
		{
//			if (showTargetWindow) {
//				AHSettings.SavePosition ("target_window_position", rectTargetWindow.position);
//			}
			showTargetShipEditorWindow = false;
		}

		public static bool showTargetShipFlightWindow = false;
		public static Rect rectTargetShipFlightWindow = new Rect (rectTargetShipEditorWindow);
		public static void CloseTargetShipFlightWindow ()
		{
//			if (showTargetWindow) {
//				AHSettings.SavePosition ("target_window_position", rectTargetWindow.position);
//			}
			showTargetShipFlightWindow = false;
		}

		public static bool showPlanetWindow = false;
		public static Rect rectPlanetWindow = new Rect (AHSettings.posPlanetWindow, new Vector2 (450, 240));
		public static void ClosePlanetWindow ()
		{
			if (showPlanetWindow) {
				AHSettings.SavePosition ("signal_strenght_per_planet_window_position", rectPlanetWindow.position);
			}
			showPlanetWindow = false;
		}

		private Vector2 ExtendWindowPos (Rect originalWindow)
		{
			float yPos;
			if (originalWindow.position.y + originalWindow.height * 2 > Screen.height) {
				yPos = originalWindow.position.y - originalWindow.height;
			} else {
				yPos = originalWindow.position.y + originalWindow.height;
			}
			return new Vector2 (originalWindow.position.x, yPos);
		}

		public void OnGUI ()
		{
			if (showMainWindow) {
				GUILayout.BeginArea (rectMainWindow);
				rectMainWindow = GUILayout.Window (835298, rectMainWindow, AHEditorWindows.MainWindow, "Antenna Helper");
				GUILayout.EndArea ();
			}
			if (showTargetWindow) {
				GUILayout.BeginArea (rectTargetWindow);
				rectTargetWindow = GUILayout.Window (419256, rectTargetWindow, AHEditorWindows.TargetWindow, "Pick A Target");
				GUILayout.EndArea ();
			}
			if (showTargetShipEditorWindow) {
//				showTargetShipFlightWindow = false;
//				CloseTargetShipFlightWindow ();
				rectTargetShipEditorWindow.position = ExtendWindowPos (rectTargetWindow);
				GUILayout.BeginArea (rectTargetShipEditorWindow);
				rectTargetShipEditorWindow = GUILayout.Window (415014, rectTargetShipEditorWindow, AHEditorWindows.TargetWindowShipEditor, "Editor Ship", GUILayout.MinHeight (rectTargetWindow.height));
				GUILayout.EndArea ();
			}
			if (showTargetShipFlightWindow) {
//				showTargetShipEditorWindow = false;
//				CloseTargetShipEditorWindow ();
				rectTargetShipFlightWindow.position = ExtendWindowPos (rectTargetWindow);
				GUILayout.BeginArea (rectTargetShipFlightWindow);
				rectTargetShipFlightWindow = GUILayout.Window (892715, rectTargetShipFlightWindow, AHEditorWindows.TargetWindowShipFlight, "In-Flight Ship", GUILayout.MinHeight (rectTargetWindow.height));
				GUILayout.EndArea ();
			}
			if (showPlanetWindow) {
				GUILayout.BeginArea (rectPlanetWindow);
				rectPlanetWindow = GUILayout.Window (332980, rectPlanetWindow, AHEditorWindows.PlanetWindow, "Signal Strength / Distance");
				GUILayout.EndArea ();
			}
		}
		#endregion

		#region ToolbarButton
		private ToolbarControl toolbarControl;

		private void AddToolbarButton ()
		{
			toolbarControl = gameObject.AddComponent<ToolbarControl> ();

			toolbarControl.AddToAllToolbars (
				ToolbarButtonOnTrue,
				ToolbarButtonOnFalse,
				KSP.UI.Screens.ApplicationLauncher.AppScenes.VAB | KSP.UI.Screens.ApplicationLauncher.AppScenes.SPH,
				"AntennaHelper",
				"823779",
				"AntennaHelper/Textures/icon_dish_on",
				"AntennaHelper/Textures/icon_off",
				"AntennaHelper/Textures/icon_dish_on_small",
				"AntennaHelper/Textures/icon_dish_off_small",
				"Antenna Helper");
			
			toolbarControl.UseBlizzy (AHSettings.useBlizzyToolbar);
		}

		private void RemoveToolbarButton ()
		{
			CloseMainWindow ();
			CloseTargetWindow ();
			ClosePlanetWindow ();
			CloseTargetShipEditorWindow ();
			CloseTargetShipFlightWindow ();

			toolbarControl.OnDestroy ();
			Destroy (toolbarControl);
		}

		private void ToolbarButtonOnTrue ()
		{
			showMainWindow = true;

			CloseTargetWindow ();
			ClosePlanetWindow ();
			CloseTargetShipEditorWindow ();
			CloseTargetShipFlightWindow ();
		}

		private void ToolbarButtonOnFalse ()
		{
			CloseMainWindow ();
			CloseTargetWindow ();
			ClosePlanetWindow ();
			CloseTargetShipEditorWindow ();
			CloseTargetShipFlightWindow ();
		}

		private void ToggleWindows ()
		{
			CreateAntennaList ();
			DoTheMath ();

			if (showMainWindow 
				|| showTargetWindow 
				|| showPlanetWindow 
				|| showTargetShipEditorWindow || showTargetShipFlightWindow) {

				CloseMainWindow ();
				CloseTargetWindow ();
				ClosePlanetWindow ();
				CloseTargetShipEditorWindow ();
				CloseTargetShipFlightWindow ();
			} else {
				showMainWindow = true;
			}
		}
		#endregion
	}
}

