using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;
using ToolbarControl_NS;
using ClickThroughFix;

namespace AntennaHelper
{
	public partial class AHFlight
	{
		private bool guiIsUp = false;
		private bool guiHasStarted = false;

		private bool showActiveConnectWindow = false;
		private bool showSelectCircleTypeWindow = false;
		private bool showPotentialRelaysWindow = false;
		private bool showLinkDetailWindow = false;

		private Rect rectActiveConnectWindow = new Rect (0, 0, 300, 0);
		private Rect rectSelectCircleTypeWindow = new Rect (0, 0, 0, 0);
		private Rect rectNotStartedWindow = new Rect (0, 0, 150, 0);
		private Rect rectPotentialRelaysWindow = new Rect (0, 0, 300, 0);
		private Rect rectLinkDetailWindow = new Rect (0, 0, 300, 0);

		private GUICircleSelection currentCircleType = GUICircleSelection.ACTIVE;
		private Dictionary<string, string> selectedLink;
		private bool selectedLinkIsActive;
		private int selectedListLinkInt, selectedLinkInt;
		private Vector2 linkDetailPosOffset = Vector2.zero;

		private GUIStyle guiStyleButtonNorm, guiStyleButtonBold,
			guiStyleSelectActive, guiStyleSelectDSN, guiStyleSelectRelay, guiStyleSelectRelayAndDSN, guiStyleSelectNone,
			guiStyleSpace, guiStyleLabelBold;

		private ToolbarControl toolbarController;
		private bool toolbarButtonAdded = false;

		#region ButtonAndMapSwitch
		private void EnteringMap ()
		{
			inMapView = true;
			if (guiIsUp) {
				showSelectCircleTypeWindow = true;
				SelectCircleType ();
			}
		}

		private void ExitingMap ()
		{
			showSelectCircleTypeWindow = false;
			inMapView = false;
		}

        private void AddToolbarButton ()
		{
			KSP.UI.Screens.ApplicationLauncher.AppScenes scenes = 
				KSP.UI.Screens.ApplicationLauncher.AppScenes.FLIGHT 
				| KSP.UI.Screens.ApplicationLauncher.AppScenes.MAPVIEW;

			if (!HighLogic.CurrentGame.Parameters.CustomParams<AntennaHelperSettings> ().enableInFlight) {
				scenes = KSP.UI.Screens.ApplicationLauncher.AppScenes.MAPVIEW;
			}
			if (!HighLogic.CurrentGame.Parameters.CustomParams<AntennaHelperSettings> ().enableInMapView) {
				scenes = KSP.UI.Screens.ApplicationLauncher.AppScenes.FLIGHT;
			}

			if (!toolbarButtonAdded) {
				toolbarController = gameObject.AddComponent<ToolbarControl> ();

				toolbarController.AddToAllToolbars (
					ToolbarButtonOnTrue,
					ToolbarButtonOnFalse,
					scenes,
                    AHEditor.MODID,
					"368879",
					"AntennaHelper/Textures/icon_sat_on",
					"AntennaHelper/Textures/icon_off",
					"AntennaHelper/Textures/icon_dish_on_small",
					"AntennaHelper/Textures/icon_dish_off_small",
					Localizer.Format (AHEditor.MODNAME));

				toolbarButtonAdded = true;
			}
		}

		private void RemoveToolbarButton ()
		{
			if (toolbarButtonAdded) {
				toolbarController.OnDestroy ();
				Destroy (toolbarController);
				toolbarButtonAdded = false;
			}
		}

		private void ToolbarButtonOnTrue ()
		{
			doMath = true;
			StartCoroutine ("UpdateCommNet");
			guiIsUp = true;

			if (inMapView) {
				SelectCircleType ();
				showSelectCircleTypeWindow = true;
			}

			showActiveConnectWindow = true;
		}

		private void ToolbarButtonOnFalse ()
		{
			guiIsUp = false;
			doMath = false;
			hasStarted = false;

			ShowDSNCircles (false);
			ShowRelaysCircles (false);

			showActiveConnectWindow = false;
			showPotentialRelaysWindow = false;
			showLinkDetailWindow = false;
			showSelectCircleTypeWindow = false;
		}
		#endregion

		#region OnGUIWindow
		private void OnGUIStarter ()
		{
			guiStyleButtonNorm = new GUIStyle (GUI.skin.GetStyle ("Button"));
			guiStyleButtonBold = new GUIStyle (GUI.skin.GetStyle ("Button"));
			guiStyleButtonBold.fontStyle = FontStyle.Bold;

			guiStyleSpace = new GUIStyle (GUI.skin.GetStyle ("Label"));
			Texture2D spaceTex = new Texture2D (1, 1);
			spaceTex.SetPixel (0, 0, Color.grey);
			guiStyleSpace.normal.background = spaceTex;

			guiStyleLabelBold = new GUIStyle (GUI.skin.GetStyle ("Label"));
			guiStyleLabelBold.fontStyle = FontStyle.Bold;

			rectNotStartedWindow.position = new Vector2 ((Screen.width / 2f), (Screen.height / 2f));

			rectActiveConnectWindow.position = AHSettings.posFlightMainWindow;
			rectSelectCircleTypeWindow.position = AHSettings.posFlightMapViewWindow;

			guiHasStarted = true;
		}

		void OnGUI ()
		{
			if (!HighLogic.CurrentGame.Parameters.CustomParams<AntennaHelperSettings>().altSkin)
				GUI.skin = HighLogic.Skin;


			if (!guiHasStarted) {
				OnGUIStarter ();
			}
			if (!hasStarted && (showActiveConnectWindow || showSelectCircleTypeWindow)) {
				rectNotStartedWindow = ClickThruBlocker.GUILayoutWindow (485768, rectNotStartedWindow, NotStartedWindow, Localizer.Format ("#autoLOC_AH_0001"));
				return;
			}
			if (showActiveConnectWindow) {
				rectActiveConnectWindow = ClickThruBlocker.GUILayoutWindow (434324, rectActiveConnectWindow, ActiveConnectWindow, Localizer.Format ("#autoLOC_AH_0001"));
			}
			if (showSelectCircleTypeWindow) {
				rectSelectCircleTypeWindow = ClickThruBlocker.GUILayoutWindow (647886, rectSelectCircleTypeWindow, SelectCircleTypeWindow, Localizer.Format ("#autoLOC_AH_0052"));
			}
			if (showPotentialRelaysWindow) {
				rectPotentialRelaysWindow.position = new Vector2 (
					rectActiveConnectWindow.position.x, 
					rectActiveConnectWindow.position.y + rectActiveConnectWindow.size.y);
				
				rectPotentialRelaysWindow = ClickThruBlocker.GUILayoutWindow (307428, rectPotentialRelaysWindow, PotentialRelaysWindow, Localizer.Format ("#autoLOC_AH_0053"));
			}
			if (showLinkDetailWindow) {
				rectLinkDetailWindow.position = rectActiveConnectWindow.position - linkDetailPosOffset;
				rectLinkDetailWindow = ClickThruBlocker.GUILayoutWindow (675752, rectLinkDetailWindow, LinkDetailWindow, Localizer.Format ("#autoLOC_AH_0054", new string[] { selectedLink ["aName"], selectedLink ["bName"] } ));
			}
		}

		private void SetLinkDetailWindowPos ()
		{
			float posX = rectActiveConnectWindow.position.x - rectLinkDetailWindow.size.x;

			float posY = Mouse.screenPos.y - (rectLinkDetailWindow.size.y / 2f);
			if (posY < rectActiveConnectWindow.position.y) {
				posY = rectActiveConnectWindow.position.y;
			}

			rectLinkDetailWindow.position = new Vector2 (posX, posY);

			linkDetailPosOffset = rectActiveConnectWindow.position - rectLinkDetailWindow.position;
		}

		private void ActiveConnectWindow (int id)
		{
			
			if (GUI.Button (new Rect (rectActiveConnectWindow.size.x - 22, 2, 20, 20), "X")) {
				showActiveConnectWindow = false;
				showPotentialRelaysWindow = false;
				showLinkDetailWindow = false;
			}

			GUILayout.BeginVertical ();

			GUILayout.BeginHorizontal ();
			GUILayout.Label (detailsActiveConnectLinks [0] ["aName"], guiStyleLabelBold);
			GUILayout.Label ((" " + /*TO*/Localizer.Format ("#autoLOC_AH_0055") + " "), guiStyleLabelBold);
			GUILayout.Label (/*DSN*/Localizer.Format ("#autoLOC_AH_0014"), guiStyleLabelBold);
			GUILayout.Label (" : ", guiStyleLabelBold);
			GUILayout.Label (detailsActiveConnectLinks [0] ["activeSignalStrength"], guiStyleLabelBold);
			GUILayout.EndHorizontal ();
			
			foreach (Dictionary<string, string> activeLink in detailsActiveConnectLinks) {
				GUILayout.BeginHorizontal ();
				GUILayout.Space (10f);
				if (GUILayout.Button (
					activeLink ["aName"] 
					+ (" " + /*TO*/Localizer.Format ("#autoLOC_AH_0055") + " ") 
					+ activeLink ["bName"] 
					+ " : " 
					+ activeLink ["signalStrength"]))
				{
					if (selectedLink == activeLink) {
						showLinkDetailWindow = !showLinkDetailWindow;
					} else {
						showLinkDetailWindow = true;
					}
					selectedLink = activeLink;
					selectedLinkIsActive = true;
					selectedLinkInt = detailsActiveConnectLinks.IndexOf (activeLink);
					SetLinkDetailWindowPos ();
				}
				GUILayout.Space (10f);
				GUILayout.EndHorizontal ();
			}

			GUILayout.Label ("", guiStyleSpace, GUILayout.Height (3f));

			if (GUILayout.Button (/*Potential Relays*/Localizer.Format ("#autoLOC_AH_0056"))) {
				showPotentialRelaysWindow = !showPotentialRelaysWindow;
			}

			GUILayout.EndVertical ();

			GUI.DragWindow ();
		}

		private void PotentialRelaysWindow (int id)
		{
			GUILayout.BeginVertical ();
			foreach (List<Dictionary<string, string>> relay in detailsRelaysLinks) {
				GUILayout.BeginHorizontal ();
				GUILayout.Label (relay [0] ["aName"], guiStyleLabelBold);
				GUILayout.Label ((" " + Localizer.Format ("#autoLOC_AH_0055") + " "), guiStyleLabelBold);
				GUILayout.Label (Localizer.Format ("#autoLOC_AH_0014"), guiStyleLabelBold);
				GUILayout.Label (" : ", guiStyleLabelBold);
				GUILayout.Label (relay [0] ["endSignalStrength"], guiStyleLabelBold, GUILayout.ExpandWidth (false));
				GUILayout.EndHorizontal ();
				foreach (Dictionary<string, string> activeLink in relay) {

					GUILayout.BeginHorizontal ();
					GUILayout.Space (10f);
					if (GUILayout.Button (
						activeLink ["aName"] 
						+ (" " + Localizer.Format ("#autoLOC_AH_0055") + " ") 
						+ activeLink ["bName"] 
						+ " : " 
						+ activeLink ["signalStrength"]))
					{
						if (selectedLink == activeLink) {
							showLinkDetailWindow = !showLinkDetailWindow;
						} else {
							showLinkDetailWindow = true;
						}
						selectedLink = activeLink;
						selectedLinkIsActive = false;
						selectedListLinkInt = detailsRelaysLinks.IndexOf (relay);
						selectedLinkInt = relay.IndexOf (activeLink);
						SetLinkDetailWindowPos ();
					}
					GUILayout.Space (10f);
					GUILayout.EndHorizontal ();

				}
				GUILayout.Label ("", guiStyleSpace, GUILayout.Height (1f));
			}
			GUILayout.EndVertical ();
		}

		private void LinkDetailWindow (int id)
		{
			if (selectedLinkIsActive) {
				selectedLink = detailsActiveConnectLinks [selectedLinkInt];
			} else {
				selectedLink = detailsRelaysLinks [selectedListLinkInt] [selectedLinkInt];
			}

			GUILayout.BeginVertical ();

			GUILayout.BeginHorizontal ();
			GUILayout.Label (/*<<1>> \t Relay Power : */Localizer.Format ("#autoLOC_AH_0073", new string[] { selectedLink ["aName"] } ));
			GUILayout.Label (selectedLink ["aPowerRelay"], GUILayout.ExpandWidth (false));
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			GUILayout.Label (Localizer.Format (/*<<1>> \t Total Power : */"#autoLOC_AH_0074", new string[] { selectedLink ["aName"] } ));
			GUILayout.Label (selectedLink ["aPowerTotal"], GUILayout.ExpandWidth (false));
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			GUILayout.Label (/*<<1>> \t Relay Power : */Localizer.Format ("#autoLOC_AH_0073", new string[] { selectedLink ["bName"] } ));
			GUILayout.Label (selectedLink ["bPowerRelay"], GUILayout.ExpandWidth (false));
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			GUILayout.Label (/*<<1>> \t Total Power : */Localizer.Format ("#autoLOC_AH_0074", new string[] { selectedLink ["bName"] } ));
			GUILayout.Label (selectedLink ["bPowerTotal"], GUILayout.ExpandWidth (false));
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			GUILayout.Label (/*Max Range*/Localizer.Format ("#autoLOC_AH_0010") + " : ");
			GUILayout.Label (selectedLink ["maxRange"], GUILayout.ExpandWidth (false));
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			GUILayout.Label (/*Distance*/Localizer.Format ("#autoLOC_AH_0059") + " : ");
			GUILayout.Label (selectedLink ["distance"], GUILayout.ExpandWidth (false));
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			GUILayout.Label (/*Signal Strength*/Localizer.Format ("#autoLOC_AH_0060") + " : ");
			GUILayout.Label (selectedLink ["signalStrength"], GUILayout.ExpandWidth (false));
			GUILayout.EndHorizontal ();

			GUILayout.EndHorizontal ();
		}

		private void SelectCircleTypeWindow (int id)
		{
			GUILayout.BeginVertical ();

			if (GUILayout.Button (/*"Active"*/Localizer.Format ("#autoLOC_AH_0045"), guiStyleSelectActive)) {
				currentCircleType = GUICircleSelection.ACTIVE;
				SelectCircleType ();
			}
			if (GUILayout.Button (/*"DSN"*/Localizer.Format ("#autoLOC_AH_0046"), guiStyleSelectDSN)) {
				currentCircleType = GUICircleSelection.DSN;
				SelectCircleType ();
			}
			if (GUILayout.Button (/*"Relay(s)"*/Localizer.Format ("#autoLOC_AH_0048"), guiStyleSelectRelay)) {
				currentCircleType = GUICircleSelection.RELAY;
				SelectCircleType ();
			}
			if (GUILayout.Button (/*"DSN and Relay(s)"*/Localizer.Format ("#autoLOC_AH_0047"), guiStyleSelectRelayAndDSN)) {
				currentCircleType = GUICircleSelection.DSN_AND_RELAY;
				SelectCircleType ();
			}
			if (GUILayout.Button (/*"None"*/Localizer.Format ("#autoLOC_AH_0049"), guiStyleSelectNone)) {
				currentCircleType = GUICircleSelection.NONE;
				SelectCircleType ();
			}

			GUILayout.EndVertical ();

			GUI.DragWindow ();
		}

		private void NotStartedWindow (int id)
		{
			GUILayout.BeginVertical ();
			GUILayout.Label (/*"Antenna Helper isn't ready just yet"*/Localizer.Format ("#autoLOC_AH_0061"));
			GUILayout.Label (/*"Wait..."*/Localizer.Format ("#autoLOC_AH_0062"));
			GUILayout.EndVertical ();

			GUI.DragWindow ();
		}
		#endregion

		#region CircleHandling
		private void SelectCircleType ()
		{
			switch (currentCircleType) {
			case GUICircleSelection.DSN:
				ShowDSNCircles ();
				ShowRelaysCircles (false);
				guiStyleSelectDSN = guiStyleButtonBold;
				guiStyleSelectRelay = guiStyleButtonNorm;
				guiStyleSelectRelayAndDSN = guiStyleButtonNorm;
				guiStyleSelectActive = guiStyleButtonNorm;
				guiStyleSelectNone = guiStyleButtonNorm;
				break;
			case GUICircleSelection.RELAY:
				ShowDSNCircles (false);
				ShowRelaysCircles ();
				guiStyleSelectDSN = guiStyleButtonNorm;
				guiStyleSelectRelay = guiStyleButtonBold;
				guiStyleSelectRelayAndDSN = guiStyleButtonNorm;
				guiStyleSelectActive = guiStyleButtonNorm;
				guiStyleSelectNone = guiStyleButtonNorm;
				break;
			case GUICircleSelection.DSN_AND_RELAY:
				ShowDSNCircles ();
				ShowRelaysCircles ();
				guiStyleSelectDSN = guiStyleButtonNorm;
				guiStyleSelectRelay = guiStyleButtonNorm;
				guiStyleSelectRelayAndDSN = guiStyleButtonBold;
				guiStyleSelectActive = guiStyleButtonNorm;
				guiStyleSelectNone = guiStyleButtonNorm;
				break;
			case GUICircleSelection.ACTIVE:
				if (connectedToDSN) {
					ShowDSNCircles ();
					ShowRelaysCircles (false);
				} else if (connectedTo != null) {
					ShowDSNCircles (false);
					ShowRelaysCircles (false);
					markerObjectsRelay [connectedTo].GetComponent<AHMapMarker> ().Show ();
				}
				guiStyleSelectDSN = guiStyleButtonNorm;
				guiStyleSelectRelay = guiStyleButtonNorm;
				guiStyleSelectRelayAndDSN = guiStyleButtonNorm;
				guiStyleSelectActive = guiStyleButtonBold;
				guiStyleSelectNone = guiStyleButtonNorm;
				break;
			case GUICircleSelection.NONE:
			default:
				ShowDSNCircles (false);
				ShowRelaysCircles (false);
				guiStyleSelectDSN = guiStyleButtonNorm;
				guiStyleSelectRelay = guiStyleButtonNorm;
				guiStyleSelectRelayAndDSN = guiStyleButtonNorm;
				guiStyleSelectActive = guiStyleButtonNorm;
				guiStyleSelectNone = guiStyleButtonBold;
				break;
			}
		}

		private void ShowDSNCircles (bool show = true)
		{
			if (show) {
				markerObjectDSN.GetComponent<AHMapMarker> ().Show ();
			} else {
				markerObjectDSN.GetComponent<AHMapMarker> ().Hide ();
			}
		}

		private void ShowRelaysCircles (bool show = true)
		{
			if (show) {
				foreach (KeyValuePair<Vessel, GameObject> relayMarkerObjects in markerObjectsRelay) {
					relayMarkerObjects.Value.GetComponent<AHMapMarker> ().Show ();
				}
			} else {
				foreach (KeyValuePair<Vessel, GameObject> relayMarkerObjects in markerObjectsRelay) {
					relayMarkerObjects.Value.GetComponent<AHMapMarker> ().Hide ();
				}
			}
		}
		#endregion
	}
}

