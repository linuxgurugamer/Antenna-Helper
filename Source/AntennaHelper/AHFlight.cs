﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AntennaHelper
{
	[KSPAddon (KSPAddon.Startup.Flight, false)]
	public class AHFlight : MonoBehaviour
	{
		// UI Stuff :
		public static GUICircleSelection guiCircle;
		public static Rect windowRect = new Rect (Vector2.zero, new Vector2 (138, 154));

		private static AHFlight instance;

		private float timeAtStart;
		private Vessel vessel;

		// GameObjects saves :
		private List<GameObject> allRelay = new List<GameObject> ();
		private GameObject activeConnect;
		private GameObject DSNConnect;

		// UI stuff :
		private bool isToolbarOn = false;
		private KSP.UI.Screens.ApplicationLauncherButton toolbarButton;


		public void Start ()
		{
			instance = this;
			vessel = FlightGlobals.ActiveVessel;
			guiCircle = GUICircleSelection.ACTIVE;
			timeAtStart = Time.time;

			GameEvents.onGUIApplicationLauncherReady.Add (AddToolbarButton);
			GameEvents.onGUIApplicationLauncherDestroyed.Add (RemoveToolbarButton);

			GameEvents.onVesselSwitching.Add (VesselSwitch);

			// for the map view gui :
			GameEvents.OnMapEntered.Add (MapEnter);
			GameEvents.OnMapExited.Add (MapExit);

			StartCoroutine ("WaitAtStart");
		}

		public void OnDestroy ()
		{
			DestroyMarker ();
			// Toolbar button
			RemoveToolbarButton ();
			GameEvents.onGUIApplicationLauncherReady.Remove (AddToolbarButton);
			GameEvents.onGUIApplicationLauncherDestroyed.Remove (RemoveToolbarButton);

			GameEvents.onVesselSwitching.Remove (VesselSwitch);

			// for the map view gui :
			GameEvents.OnMapEntered.Remove (MapEnter);
			GameEvents.OnMapExited.Remove (MapExit);
		}
		private bool inMapView = false;
		private void MapEnter ()
		{
			inMapView = true;
		}

		private void MapExit ()
		{
			inMapView = false;
		}

		private void DestroyMarker ()
		{
			Destroy (activeConnect);
			Destroy (DSNConnect);
			foreach (GameObject gO in allRelay) {
				Destroy (gO);
			}
		}

		private void VesselSwitch (Vessel fromVessel, Vessel toVessel)
		{
			StopCoroutine ("WaitAtStart");
			Destroy (this);
		}

		private IEnumerator WaitAtStart ()
		{
			while (Time.time < timeAtStart + .5f) {
				yield return new WaitForSeconds (.1f);
			}
			SetMapMarker ();
		}

		private void SetMapMarker ()
		{
//			Debug.Log ("[AH] DSN level = " + ScenarioUpgradeableFacilities.GetFacilityLevel (SpaceCenterFacility.TrackingStation));
//			Debug.Log ("[AH] DSN level int = " + AHUtil.DSNLevel);
//			Debug.Log ("[AH] DSN power = " + AHUtil.DSNLevelList [AHUtil.DSNLevel]);
//			Debug.Log ("[AH] vessel transmit power = " + FlightGlobals.ActiveVessel.Connection.Comm.antennaTransmit.power);
//			Debug.Log ("[AH] vessel relay power = " + FlightGlobals.ActiveVessel.Connection.Comm.antennaRelay.power);
//			Debug.Log ("[AH] vessel total power = " + (FlightGlobals.ActiveVessel.Connection.Comm.antennaTransmit.power + FlightGlobals.ActiveVessel.Connection.Comm.antennaRelay.power));
//			Debug.Log ("[AH] max range = " + AHUtil.GetRange (FlightGlobals.ActiveVessel.Connection.Comm.antennaTransmit.power, AHUtil.DSNLevelList [AHUtil.DSNLevel]));

			////
			/// New idea : 
			/// 3 types of display : 
			///  * only the active relay/DSN
			///  * all the relay
			///  * the DSN
			/// 
			/// for this I need 3 list of relay, only one will have more than 1 entry, but I like list
			/// an UI in the map view with 3 checkbox, one for each type.
			/// If the "only active" is selected the other 2 should be disabled
			/// 
			/// Those list should be reset at every vessel change but also when the active vessel change,
			/// crash, (de)coupling. AND when the antennas are extend/retract, I don't think there is an
			/// event for this, will it be better to write my own ? If not the coroutine will have to keep
			/// the speed vs time-wrap
			/// 
			/// Question : should offline relay be listed as well ? Don't think one way is more intuitive
			/// than the other. For now all will be listed. SEE BELOW :
			/// Actually there is a limitation to this : the fact that I draw relay circle based on their
			/// real signal strength mean I can't draw circle for a relay with 0 signal, the circle will
			/// have no color.
			/// So : only online relay are accounted for.
			/// 
			/// Question a : should vessel with relay capacity but not of the RELAY type be listed ?
			/// for now nope, only vessel designated as RELAY will be listed.
			/// 
			/// Question a1 : what about an active connection through a vessel with relay capacity but not
			/// of the relay type ?
			/// For now the 3 lists are created separetly, so such vessel will be in the active connection
			/// list but not in the relay list, probably not intuitive. Need to think more about it.
			/// 

			//  Get the Active vessel power
			double vesselPower;
			//// 
			/// Here is the thing when trying to get the transmit power from a vessel : 
			/// (Vessel.Connection.Comm.antennaTransmit.power)
			/// if only an internal antenna + relay = good
			/// if only direct antenna + relay = guess it's good, not possible in stock, every command module 
			///   got an internal antenna
			/// if internal + direct (extended) + relay = good
			/// if internal + direct (retract) + relay = only the internal is taking into account, the relay
			///   should also, I guess it is because the internal can't be combine so if left out of the 
			///   calcul it would show a relult without any transmitter antenna, does that even make sense ?
			/// Anyway, best will be to manually do the math with info directly from the part
			/// 

			double powerBestAntenna = 0;
			ModuleDataTransmitter bestAntenna;
			List<ModuleDataTransmitter> antennaList = new List<ModuleDataTransmitter> ();
			List<ModuleDataTransmitter> antennaListCanCombine = new List<ModuleDataTransmitter> ();

			foreach (Part part in vessel.parts) {
				antennaList.AddRange (part.FindModulesImplementing<ModuleDataTransmitter> ());
			}

			foreach (ModuleDataTransmitter antenna in antennaList) {
				if (antenna.antennaCombinable) {antennaListCanCombine.Add (antenna);}
				if (antenna.antennaPower > powerBestAntenna) {
					powerBestAntenna = antenna.antennaPower;
					bestAntenna = antenna;
				}
			}

			powerBestAntenna = AHUtil.TruePower (powerBestAntenna);
			double combinePower = AHUtil.GetVesselPower (antennaListCanCombine);
			if (combinePower > powerBestAntenna) {
				vesselPower = combinePower;
			} else {
				vesselPower = powerBestAntenna;
			}

			// list of all the relay in-flight :
			int i = 0;
			foreach (Vessel v in FlightGlobals.Vessels) {
				if (v != vessel) {
					// make sure the active vessel do not end up in the list
					if (v.vesselType == VesselType.Relay) {
						// check that the vesselType is Relay
						if (v.Connection.IsConnected) {
							// make sure the relay is online, need to double check this, it may not work 
							//  as expected.

							// need to get its real signal strength now :
							double realSignal = GetRealSignal (v.Connection.ControlPath);

							// math the max range :
							double range = AHUtil.GetRange (vesselPower, v.Connection.Comm.antennaRelay.power);
							// get real maxRange :
							range = AHUtil.GetDistanceAt0 (range);

							allRelay.Add (new GameObject ());
							allRelay [i].AddComponent<AHMapMarker> ();
//							allRelay [i].GetComponent<AHMapMarker> ().Start ();
							allRelay [i].GetComponent<AHMapMarker> ().SetUp (range, vessel, v.mapObject.trf, false, realSignal);

							i++;
						}
					}
				}
			}
//			Debug.Log ("[AH] relay marker done");

			// Active Connection :
			double rangeAC;
			Transform relay;
			double activeSignal;
			bool isHome;
			if (!vessel.Connection.IsConnected || vessel.Connection.ControlPath [0].b.isHome) {
				rangeAC =  AHUtil.GetRange (vesselPower, AHUtil.DSNLevelList [AHUtil.DSNLevel]);
				relay = FlightGlobals.GetHomeBody ().MapObject.trf;
				activeSignal = 1d;
				isHome = true;
			} else {
				rangeAC = AHUtil.GetRange (vesselPower, vessel.Connection.ControlPath[0].b.antennaRelay.power);
				relay = vessel.Connection.ControlPath [0].b.transform.GetComponent<Vessel> ().mapObject.trf;
				activeSignal = GetRealSignal (vessel.Connection.ControlPath);
				isHome = false;
			}
			// get real maxRange :
			rangeAC = AHUtil.GetDistanceAt0 (rangeAC);

			activeConnect = new GameObject ();
			activeConnect.AddComponent<AHMapMarker> ();
//			activeConnect.GetComponent<AHMapMarker> ().Start ();
			activeConnect.GetComponent<AHMapMarker> ().SetUp (rangeAC, vessel, relay, isHome, activeSignal);
//			Debug.Log ("[AH] active marker done");

			// DSN Connection :
			double rangeDSN = AHUtil.GetRange (vesselPower, AHUtil.DSNLevelList [AHUtil.DSNLevel]);
			// get real maxRange
			rangeDSN = AHUtil.GetDistanceAt0 (rangeDSN);
			DSNConnect = new GameObject ();
			AHMapMarker markerDSN = DSNConnect.AddComponent<AHMapMarker> ();
//			markerDSN.Start ();
			markerDSN.SetUp (rangeDSN, vessel, FlightGlobals.GetHomeBody ().MapObject.trf, true, 1d);

//			if (FlightGlobals.ActiveVessel.Connection != null) {
//				Debug.Log ("[AH] active vessel CommnetVessel found");
//				if (FlightGlobals.ActiveVessel.Connection.ControlPath != null) {
//					Debug.Log ("[AH] active vessel ControlPath found");
//					if (FlightGlobals.ActiveVessel.Connection.ControlPath.First != null) {
//						Debug.Log ("[AH] active vessel ControlPath.First found");
//						Debug.Log ("[AH] ControlPath.First : " + FlightGlobals.ActiveVessel.Connection.ControlPath.First.ToString ());
//
//					} else {
//						Debug.Log ("[AH] active vessel ControlPath.First not found");
//					}
//				} else {
//					Debug.Log ("[AH] active vessel ControlPath not found");
//				}
//			} else {
//				Debug.Log ("[AH] active vessel CommnetVessel not found");
//			}
		}

		private double GetRealSignal (CommNet.CommPath path)
		{
			double signal = 1d;
			foreach (CommNet.CommLink link in path) {
				if (link.a.transform.GetComponent<Vessel> () != vessel) {
					signal *= link.signalStrength;
				}
			}
			return signal;
		}

		#region GUI
		public void OnGUI ()
		{
			if (isToolbarOn && inMapView) {
				GUILayout.BeginArea (windowRect);
				windowRect = GUILayout.Window (806641, windowRect, AHMapViewWindow.AntennaSelectWindow, "Antenna Helper");
				GUILayout.EndArea ();
			}
		}

		public static void GUISelectCircle ()
		{
			switch (guiCircle) {
			case GUICircleSelection.ACTIVE:
				instance.activeConnect.GetComponent<AHMapMarker> ().Show ();
				instance.DSNConnect.GetComponent<AHMapMarker> ().Hide ();
				foreach (GameObject gO in instance.allRelay) {
					gO.GetComponent<AHMapMarker> ().Hide ();
				}
				break;
			case GUICircleSelection.DSN:
				instance.activeConnect.GetComponent<AHMapMarker> ().Hide ();
				instance.DSNConnect.GetComponent<AHMapMarker> ().Show ();
				foreach (GameObject gO in instance.allRelay) {
					gO.GetComponent<AHMapMarker> ().Hide ();
				}
				break;
			case GUICircleSelection.RELAY:
				instance.activeConnect.GetComponent<AHMapMarker> ().Hide ();
				instance.DSNConnect.GetComponent<AHMapMarker> ().Hide ();
				foreach (GameObject gO in instance.allRelay) {
					gO.GetComponent<AHMapMarker> ().Show ();
				}
				break;
			case GUICircleSelection.DSN_AND_RELAY:
				instance.activeConnect.GetComponent<AHMapMarker> ().Hide ();
				instance.DSNConnect.GetComponent<AHMapMarker> ().Show ();
				foreach (GameObject gO in instance.allRelay) {
					gO.GetComponent<AHMapMarker> ().Show ();
				}
				break;
				default:
				instance.activeConnect.GetComponent<AHMapMarker> ().Show ();
				instance.DSNConnect.GetComponent<AHMapMarker> ().Hide ();
				foreach (GameObject gO in instance.allRelay) {
					gO.GetComponent<AHMapMarker> ().Hide ();
				}
				break;
			}
		}
		private void SetWindowPos ()
		{
			float posX = toolbarButton.transform.position.x * 2f - windowRect.width + 38f;
			if (posX + windowRect.width > Screen.width) {
				posX = Screen.width - windowRect.width;
			} else if (posX < 0) {
				posX = 0;
			}

			float posY = Screen.height / 2f - toolbarButton.transform.position.y - windowRect.height / 3f;
			if (posY + windowRect.height > Screen.height) {
				posY = Screen.height - windowRect.height;
			} else if (posY < 0) {
				posY = 0;
			}

			windowRect.position = new Vector2 (posX, posY);

		}
		#endregion

		#region ToolbarButton
		private void AddToolbarButton ()
		{
			toolbarButton = KSP.UI.Screens.ApplicationLauncher.Instance.AddModApplication (
				ToolbarButtonOnTrue, 
				ToolbarButtonOnFalse, 
				AHUtil.DummyVoid, 
				AHUtil.DummyVoid, 
				AHUtil.DummyVoid, 
				AHUtil.DummyVoid,
				KSP.UI.Screens.ApplicationLauncher.AppScenes.MAPVIEW,
				AHUtil.toolbarButtonTex);
		}

		private void RemoveToolbarButton ()
		{
			KSP.UI.Screens.ApplicationLauncher.Instance.RemoveModApplication (toolbarButton);
		}

		private void ToolbarButtonOnTrue ()
		{
			if (activeConnect != null) {
				GUISelectCircle ();
				// Reset window position each time it is clicked, I can't predict where the button will be
				SetWindowPos ();
				isToolbarOn = true;
			}
		}

		private void ToolbarButtonOnFalse ()
		{
			if (activeConnect != null) {
				activeConnect.GetComponent<AHMapMarker> ().Hide ();
				DSNConnect.GetComponent<AHMapMarker> ().Hide ();
				foreach (GameObject gO in allRelay) {
					gO.GetComponent<AHMapMarker> ().Hide ();
				}
				isToolbarOn = false;
			}
		}
		#endregion
	}
}

