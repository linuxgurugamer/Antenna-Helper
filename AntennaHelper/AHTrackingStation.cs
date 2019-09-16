using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;
using ToolbarControl_NS;
using ClickThroughFix;

namespace AntennaHelper
{
	[KSPAddon (KSPAddon.Startup.TrackingStation, false)]
	public class AHTrackingStation : MonoBehaviour
	{
		private string targetPid;
		private Dictionary<string, Dictionary<string, string>> listShipTransmitter;
		private Dictionary<string, Dictionary<string, string>> listShipRelay;
		private Dictionary<string, Dictionary<string, List<GameObject>>> listMarkers;

		private float trackingStationLvl;
		private double dsnPower;

		private WaitForSeconds waitFor;

		// GUI
		private ToolbarControl toolbarControl;
		private Rect rectMainWindow, rectEditorShipWindow;
		private Vector2 scrollerEditorShipWindow;
		private GUICircleSelection circleTypeSelected;
		private bool mainWindowOn, editorShipWindowOn;
		private bool editorShipVab, editorShipRelay;
		private List<Dictionary<string, string>> guiListEditorShipDisplay;
		private List<Dictionary<string, string>> guiListEditorShipVabAll;
		private List<Dictionary<string, string>> guiListEditorShipSphAll;
		private List<Dictionary<string, string>> guiListEditorShipVabRelay;
		private List<Dictionary<string, string>> guiListEditorShipSphRelay;

		public void Start ()
		{
			if (HighLogic.CurrentGame.Mode == Game.Modes.MISSION_BUILDER)
			{
				Destroy (this);
			}

			if (!HighLogic.CurrentGame.Parameters.CustomParams<AntennaHelperSettings> ().enableInTrackingStation) {
				Destroy (this);
				return;
			}

			targetPid = "";

			trackingStationLvl = ScenarioUpgradeableFacilities.GetFacilityLevel (SpaceCenterFacility.TrackingStation);
			dsnPower = GameVariables.Instance.GetDSNRange (trackingStationLvl);

			waitFor = new WaitForSeconds (.1f);

			GameEvents.onPlanetariumTargetChanged.Add (NewTarget);
			GameEvents.OnMapFocusChange.Add (NewTarget);
			GameEvents.CommNet.OnCommStatusChange.Add (CommNetUpdate);

//			if (AHShipList.visitFlightOnce) {
//				GetListsShip ();
//				CreateMarkers ();
//			}

			// GUI
			rectMainWindow = new Rect (0, 0, 150, 245);
			rectMainWindow.position = AHSettings.posTrackingStationMainWindow;
			mainWindowOn = false;

			rectEditorShipWindow = new Rect (0, 0, 350, 200);
			rectEditorShipWindow.position = AHSettings.posTrackingStationShipWindow;
			editorShipWindowOn = false;

			circleTypeSelected = GUICircleSelection.ACTIVE;

			editorShipVab = true;
			editorShipRelay = false;

			guiListEditorShipVabAll = AHShipList.GetShipListAsList (true, false, "VAB");
			guiListEditorShipSphAll = AHShipList.GetShipListAsList (true, false, "SPH");
			guiListEditorShipVabRelay = AHShipList.GetShipListAsList (true, true, "VAB");
			guiListEditorShipSphRelay = AHShipList.GetShipListAsList (true, true, "SPH");

			guiListEditorShipDisplay = guiListEditorShipVabAll;

			GameEvents.onGUIApplicationLauncherReady.Add (AddToolbarButton);
			GameEvents.onGUIApplicationLauncherDestroyed.Add (RemoveToolbarButton);
		}

		public void OnDestroy ()
		{
			AHSettings.SavePosition ("tracking_station_main_window_position", rectMainWindow.position);
			AHSettings.SavePosition ("tracking_station_ship_window_position", rectEditorShipWindow.position);
			AHSettings.WriteSave ();

			if (listMarkers != null) {
				DestroyMarkers ();
			}


			GameEvents.onPlanetariumTargetChanged.Remove (NewTarget);
			GameEvents.OnMapFocusChange.Remove (NewTarget);
			GameEvents.CommNet.OnCommStatusChange.Remove (CommNetUpdate);

			// GUI
			RemoveToolbarButton ();

			GameEvents.onGUIApplicationLauncherReady.Remove (AddToolbarButton);
			GameEvents.onGUIApplicationLauncherDestroyed.Remove (RemoveToolbarButton);
		}

		private void NewTarget (MapObject targetMapObject = null)
		{
			if (targetMapObject != null && targetMapObject.vessel != null) {
				targetPid = targetMapObject.vessel.id.ToString ();
				if (mainWindowOn) {
					ShowCircles ();
				}
			}
		}

		private void CommNetUpdate (Vessel v, bool b)
		{
//			Debug.Log ("[AH] CommNet Update fired");
			if (!inCoroutine) {
				StartCoroutine ("CommNetUpdateCoroutine");
			}
		}

		private bool inCoroutine = false;
		private IEnumerator CommNetUpdateCoroutine ()
		{
			inCoroutine = true;

			yield return waitFor;

			while (!AHShipList.shipListReady) {
				yield return waitFor;
			}

//			Debug.Log ("[AH] ship list is ready");

			if (listMarkers != null) {
				DestroyMarkers ();
			}

			AHShipList.ParseFlyingVessel (true);
			GetListsShip ();
			CreateMarkers ();

			inCoroutine = false;
		}

		private void GetListShipTransmitter ()
		{
			listShipTransmitter = new Dictionary<string, Dictionary<string, string>> (AHShipList.GetShipList (true, true));
		}

		private void GetListShipRelay ()
		{
			listShipRelay = new Dictionary<string, Dictionary<string, string>> ();
			foreach (KeyValuePair<string, Dictionary<string, string>> vesselPairInfo in listShipTransmitter) {
				if (vesselPairInfo.Value ["type"] != "VAB" && vesselPairInfo.Value ["type"] != "SPH") {
					if (vesselPairInfo.Value ["powerRelay"] != "0") {
						listShipRelay.Add (vesselPairInfo.Key, vesselPairInfo.Value);
					}
				}
			}
		}

		private void GetListsShip ()
		{
			
			GetListShipTransmitter ();
			GetListShipRelay ();
		}

		private void CreateMarkers ()
		{
			listMarkers = new Dictionary<string, Dictionary<string, List<GameObject>>> ();

			foreach (KeyValuePair<string, Dictionary<string, string>> vesselPairInfo in listShipTransmitter)
			{
//				Debug.Log ("[AH] creating marker for vessel : " + vesselPairInfo.Value ["name"]);

				listMarkers.Add (vesselPairInfo.Key, new Dictionary<string, List<GameObject>> ());

				double vesselPower, maxRange, realSignal;
				Vessel transmiter;
				bool editorShip;
				bool isHome;
				Transform relay;
				AHMapMarker marker;

				vesselPower = AHUtil.TruePower (Double.Parse (vesselPairInfo.Value ["powerTotal"]));
				if ((vesselPairInfo.Value ["type"] == "VAB") || (vesselPairInfo.Value ["type"] == "SPH")) {
					transmiter = null;
					editorShip = true;
				} else {
					transmiter = FlightGlobals.Vessels.Find (v => v.id.ToString () == vesselPairInfo.Key);
					editorShip = false;
				}
//				Debug.Log ("[AH] vessel power computed");

				// Active Connection :
				if (vesselPairInfo.Value ["connectedTo"] == "") {
					// the active connection is to the DSN or isn't set
					maxRange = AHUtil.GetRange (vesselPower, dsnPower);
					realSignal = 1d;
					isHome = true;
					relay = Planetarium.fetch.Home.MapObject.trf;
				} else {
					// active connection going trough a relay
					maxRange = AHUtil.GetRange (vesselPower, AHUtil.TruePower (Double.Parse (listShipRelay [vesselPairInfo.Value ["connectedTo"]] ["powerRelay"])));
					realSignal = Double.Parse (listShipRelay [vesselPairInfo.Value ["connectedTo"]] ["realSignal"]);
					isHome = false;
					relay = FlightGlobals.Vessels.Find (v => v.id.ToString () == vesselPairInfo.Value ["connectedTo"]).mapObject.trf;
				}
				maxRange = AHUtil.GetDistanceAt0 (maxRange);
				listMarkers [vesselPairInfo.Key].Add ("ACTIVE", new List<GameObject> ());
				listMarkers [vesselPairInfo.Key] ["ACTIVE"].Add (new GameObject ());
				marker = listMarkers [vesselPairInfo.Key] ["ACTIVE"] [0].AddComponent<AHMapMarker> ();
				marker.SetUp (maxRange, transmiter, relay, isHome, realSignal, editorShip);
//				Debug.Log ("[AH] active connection done");

				// DSN Connection :
				maxRange = AHUtil.GetRange (vesselPower, dsnPower);
				maxRange = AHUtil.GetDistanceAt0 (maxRange);
				realSignal = 1d;
				isHome = true;
				relay = Planetarium.fetch.Home.MapObject.trf;

				listMarkers [vesselPairInfo.Key].Add ("DSN", new List<GameObject> ());
				listMarkers [vesselPairInfo.Key] ["DSN"].Add (new GameObject ());
				marker = listMarkers [vesselPairInfo.Key] ["DSN"] [0].AddComponent<AHMapMarker> ();
				marker.SetUp (maxRange, transmiter, relay, isHome, realSignal, editorShip);
//				Debug.Log ("[AH] dsn connection done");

				// Relay(s) Connection :
				List<GameObject> listRelayMarkers = new List<GameObject> ();
				foreach (KeyValuePair<string, Dictionary<string, string>> relayPairInfo in listShipRelay) {
					if (relayPairInfo.Key == vesselPairInfo.Key) {
						continue;
					}
					maxRange = AHUtil.GetRange (vesselPower, AHUtil.TruePower (Double.Parse (relayPairInfo.Value ["powerRelay"])));
					maxRange = AHUtil.GetDistanceAt0 (maxRange);
					realSignal = Double.Parse (relayPairInfo.Value ["realSignal"]);
					isHome = false;
					relay = FlightGlobals.Vessels.Find (v => v.id.ToString () == relayPairInfo.Key).mapObject.trf;

					listRelayMarkers.Add (new GameObject ());
					marker = listRelayMarkers [listRelayMarkers.Count - 1].AddComponent<AHMapMarker> ();
					marker.SetUp (maxRange, transmiter, relay, isHome, realSignal, editorShip);
				}
				listMarkers [vesselPairInfo.Key].Add ("RELAY", listRelayMarkers);
//				Debug.Log ("[AH] relays connections done");
			}
		}

		private void UpdateMarkersScale ()
		{
			foreach (KeyValuePair<string, Dictionary<string, List<GameObject>>> vesselPairMarker in listMarkers) {
				foreach (KeyValuePair<string, List<GameObject>> connectPairMarker in vesselPairMarker.Value) {
					foreach (GameObject marker in connectPairMarker.Value) {
						double newRealSignal = 1d;
						if (listShipTransmitter [vesselPairMarker.Key] ["connectedTo"] != "") {
							newRealSignal = Double.Parse (listShipRelay [listShipTransmitter [vesselPairMarker.Key] ["connectedTo"]] ["realSignal"]);
						}
						marker.GetComponent<AHMapMarker> ().SetScale (newRealSignal);
					}
				}
			}
		}

		private void DestroyMarkers ()
		{
			foreach (KeyValuePair<string, Dictionary<string, List<GameObject>>> vesselPairMarker in listMarkers) {
				foreach (KeyValuePair<string, List<GameObject>> connectPairMarker in vesselPairMarker.Value) {
					foreach (GameObject marker in connectPairMarker.Value) {
						Destroy (marker);
					}
				}
			}
		}

		private void ShowCircles ()
		{
			HideCircles ();

			if (targetPid == "" || !listMarkers.ContainsKey (targetPid)) {
				return;
			}

			switch (circleTypeSelected) {
			case GUICircleSelection.ACTIVE:
				listMarkers [targetPid] ["ACTIVE"] [0].GetComponent<AHMapMarker> ().Show ();
				break;
			case GUICircleSelection.DSN:
				listMarkers [targetPid] ["DSN"] [0].GetComponent<AHMapMarker> ().Show ();
				break;
			case GUICircleSelection.RELAY:
				foreach (GameObject marker in listMarkers [targetPid] ["RELAY"]) {
					marker.GetComponent<AHMapMarker> ().Show ();
				}
				break;
			case GUICircleSelection.DSN_AND_RELAY:
			default:
				listMarkers [targetPid] ["DSN"] [0].GetComponent<AHMapMarker> ().Show ();
				foreach (GameObject marker in listMarkers [targetPid] ["RELAY"]) {
					marker.GetComponent<AHMapMarker> ().Show ();
				}
				break;
			}
		}

		private void HideCircles ()
		{
			foreach (KeyValuePair<string, Dictionary<string, List<GameObject>>> vesselPairMarker in listMarkers) {
				foreach (KeyValuePair<string, List<GameObject>> connectPairMarker in vesselPairMarker.Value) {
					foreach (GameObject marker in connectPairMarker.Value) {
						marker.GetComponent<AHMapMarker> ().Hide ();
					}
				}
			}
		}

		#region GUI
		public void OnGUI ()
		{
			if (mainWindowOn) {
				rectMainWindow = ClickThruBlocker.GUIWindow (889204, rectMainWindow, MainWindow, Localizer.Format ("#autoLOC_AH_0001"));
			}

			if (editorShipWindowOn) {
				rectEditorShipWindow = ClickThruBlocker.GUIWindow (524258, rectEditorShipWindow, EditorShipListWindow, Localizer.Format ("#autoLOC_AH_0044"));
			}

		}

		private void MainWindow (int id)
		{
			GUILayout.BeginVertical ();

			string transmitterName = "";
			if (targetPid != "" && listShipTransmitter.ContainsKey (targetPid)) {
				transmitterName = listShipTransmitter [targetPid] ["name"];
			}

			GUILayout.Label (Localizer.Format ("#autoLOC_AH_0050", new string[] { transmitterName } ));
			GUILayout.Label (Localizer.Format ("#autoLOC_AH_0051", new string[] { AHUtil.FormatCircleSelect (circleTypeSelected) } ));
			GUILayout.Space (3f);
			if (GUILayout.Button (Localizer.Format ("#autoLOC_AH_0045"))) {
				circleTypeSelected = GUICircleSelection.ACTIVE;
				ShowCircles ();
			}
			if (GUILayout.Button (Localizer.Format ("#autoLOC_AH_0046"))) {
				circleTypeSelected = GUICircleSelection.DSN;
				ShowCircles ();
			}
			if (GUILayout.Button (Localizer.Format ("#autoLOC_AH_0048"))) {
				circleTypeSelected = GUICircleSelection.RELAY;
				ShowCircles ();
			}
			if (GUILayout.Button (Localizer.Format ("#autoLOC_AH_0047"))) {
				circleTypeSelected = GUICircleSelection.DSN_AND_RELAY;
				ShowCircles ();
			}

			GUILayout.Space (10f);
			if (GUILayout.Button (Localizer.Format ("#autoLOC_AH_0044"))) {
				editorShipWindowOn = !editorShipWindowOn;
			}

			GUILayout.EndVertical ();

			GUI.DragWindow ();
		}

		private void EditorShipListWindow (int id)
		{
			GUIStyle guiStyleLabel;
			GUIStyle guiStyleLabelNorm = new GUIStyle (GUI.skin.GetStyle ("Label"));
			GUIStyle guiStyleLabelBold = new GUIStyle (GUI.skin.GetStyle ("Label"));
			guiStyleLabelBold.fontStyle = FontStyle.Bold;

			GUIStyle guiStyleButton;
			GUIStyle guiStyleButtonNorm = new GUIStyle (GUI.skin.GetStyle ("Button"));
			GUIStyle guiStyleButtonBold = new GUIStyle (GUI.skin.GetStyle ("Button"));
			guiStyleButtonBold.fontStyle = FontStyle.Bold;

			// Close Button
			if (GUI.Button (new Rect (rectEditorShipWindow.size.x - 22, 2, 20, 20), "X")) {
				editorShipWindowOn = false;
			}

			GUILayout.BeginVertical ();

			GUILayout.BeginHorizontal ();
			if (editorShipVab) {
				guiStyleButton = guiStyleButtonBold;
			} else {
				guiStyleButton = guiStyleButtonNorm;
			}
			if (GUILayout.Button (Localizer.Format ("#autoLOC_AH_0019"), guiStyleButton)) {
				editorShipVab = true;
			}

			if (editorShipVab) {
				guiStyleButton = guiStyleButtonNorm;
			} else {
				guiStyleButton = guiStyleButtonBold;
			}
			if (GUILayout.Button (Localizer.Format ("#autoLOC_AH_0020"), guiStyleButton)) {
				editorShipVab = false;
			}
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			GUILayout.Space (35f);
			if (editorShipRelay) {
				guiStyleButton = guiStyleButtonNorm;
			} else {
				guiStyleButton = guiStyleButtonBold;
			}
			if (GUILayout.Button (Localizer.Format ("#autoLOC_AH_0021"), guiStyleButton)) {
				editorShipRelay = false;
			}

			if (editorShipRelay) {
				guiStyleButton = guiStyleButtonBold;
			} else {
				guiStyleButton = guiStyleButtonNorm;
			}
			if (GUILayout.Button (Localizer.Format ("#autoLOC_AH_0003"), guiStyleButton)) {
				editorShipRelay = true;
			}
			GUILayout.Space (35f);
			GUILayout.EndHorizontal ();

			if (editorShipVab) {
				if (editorShipRelay) {
					guiListEditorShipDisplay = guiListEditorShipVabRelay;
				} else {
					guiListEditorShipDisplay = guiListEditorShipVabAll;
				}
			} else {
				if (editorShipRelay) {
					guiListEditorShipDisplay = guiListEditorShipSphRelay;
				} else {
					guiListEditorShipDisplay = guiListEditorShipSphAll;
				}
			}

			scrollerEditorShipWindow = GUILayout.BeginScrollView (scrollerEditorShipWindow);

			foreach (Dictionary <string, string> vesselInfo in guiListEditorShipDisplay) {
				GUILayout.BeginHorizontal ();
				if (GUILayout.Button (Localizer.Format ("#autoLOC_AH_0022"), GUILayout.Width (60f))) {
					targetPid = vesselInfo ["pid"];
					ShowCircles ();
				}

				if (targetPid == vesselInfo ["pid"]) {
					guiStyleLabel = guiStyleLabelBold;
				} else {
					guiStyleLabel = guiStyleLabelNorm;
				}
				string power;
				if (editorShipRelay) {
					power = AHUtil.TruePower (Double.Parse (vesselInfo ["powerRelay"])).ToString ();
				} else {
					power = AHUtil.TruePower (Double.Parse (vesselInfo ["powerTotal"])).ToString ();
				}
				GUILayout.Label ("(" + power + ")  " + vesselInfo ["name"], guiStyleLabel);

				GUILayout.EndHorizontal ();
			}

			GUILayout.EndScrollView ();
			GUILayout.EndVertical ();
			GUI.DragWindow ();
		}

        private void AddToolbarButton ()
		{
			toolbarControl = gameObject.AddComponent<ToolbarControl> ();

			toolbarControl.AddToAllToolbars (
				ToolbarButtonOnTrue,
				ToolbarButtonOnFalse,
				KSP.UI.Screens.ApplicationLauncher.AppScenes.TRACKSTATION,
                AHEditor.MODID,
				"421980",
				"AntennaHelper/Textures/icon_dish_on",
				"AntennaHelper/Textures/icon_off",
				"AntennaHelper/Textures/icon_dish_on_small",
				"AntennaHelper/Textures/icon_dish_off_small",
				Localizer.Format (AHEditor.MODNAME));
            
		}

		private void RemoveToolbarButton ()
		{
			if (toolbarControl != null) {
				toolbarControl.OnDestroy ();
				Destroy (toolbarControl);
			}
		}

//		public void Update ()
//		{
//			if (Input.GetKeyDown (KeyCode.Keypad5)) {
//				Debug.Log ("[AH] parsing ship list :");
//				foreach (KeyValuePair<string, Dictionary<string, string>> kvp in listShipTransmitter) {
//					Debug.Log ("[AH] info on ship : " + kvp.Key);
//					foreach (KeyValuePair<string, string> infos in kvp.Value) {
//						Debug.Log ("[AH] " + infos.Key + " : " + infos.Value);
//					}
//				}
//				return;
//			}
//		}

		private void ToolbarButtonOnTrue ()
		{
			if (listMarkers != null) {
				mainWindowOn = true;
				editorShipWindowOn = false;
				ShowCircles ();
			}
		}

		private void ToolbarButtonOnFalse ()
		{
			if (listMarkers != null) {
				mainWindowOn = false;
				editorShipWindowOn = false;
				HideCircles ();
			}

		}
		#endregion
	}
}

