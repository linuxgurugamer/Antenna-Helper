using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;

namespace AntennaHelper
{
	public class AHEditorWindows
	{
		private static string antennaTypeStr = /*Direct*/Localizer.Format ("#autoLOC_AH_0002");
		private static bool antennaTypeIsDirect = true;

		public static void MainWindow (int id)
		{
			// Close Button
			if (GUI.Button (new Rect (AHEditor.rectMainWindow.size.x - 22, 2, 20, 20), "X")) {
				AHEditor.CloseMainWindow ();
			}

			GUILayout.BeginVertical ();

			// Choose direct / relay antennas
			GUILayout.Label (/*Selected type*/Localizer.Format ("#autoLOC_AH_0004") + " : " + antennaTypeStr);
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button (
				/*Direct*/Localizer.Format ("#autoLOC_AH_0002") 
				+ " (" + /*All Antennas*/Localizer.Format ("#autoLOC_AH_0005") + ")"))
			{
				antennaTypeStr = /*Direct*/Localizer.Format ("#autoLOC_AH_0002");
				antennaTypeIsDirect = true;
			}
			if (GUILayout.Button (/*Relay*/Localizer.Format ("#autoLOC_AH_0003"))) {
				antennaTypeStr = /*Relay*/Localizer.Format ("#autoLOC_AH_0003");
				antennaTypeIsDirect = false;
			}
			GUILayout.EndHorizontal ();

			// Pick a target :
			GUILayout.Label (
				/*Current target*/Localizer.Format ("#autoLOC_AH_0006")
				+ " : " + AHEditor.targetName
				+ "  (" + AHEditor.targetPower.ToString ("N0") + ")");
			if (GUILayout.Button (/*Pick A Target*/Localizer.Format ("#autoLOC_AH_0007"))) {
				if (AHEditor.showTargetWindow) {
					AHEditor.CloseTargetWindow ();
				} else {
					AHEditor.showTargetWindow = true;
				}
			}

			// Number display :
			float width = AHEditor.rectMainWindow.width * .4f;
			GUILayout.BeginHorizontal ();
			GUILayout.Label(/*Status*/Localizer.Format("#autoLOC_AH_0008") + " : ", GUILayout.Width(width));
			GUILayout.Label(antennaTypeIsDirect ? AHEditor.statusStringDirect : AHEditor.statusStringRelay);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label(/*Power*/Localizer.Format("#autoLOC_AH_0009") + " : ", GUILayout.Width(width));
			GUILayout.Label(antennaTypeIsDirect ? AHEditor.directBetterPower.ToString("N0"): AHEditor.relayBetterPower.ToString("N0"));
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label(/*Max Range*/Localizer.Format("#autoLOC_AH_0010") + " : ", GUILayout.Width(width));
			GUILayout.Label(antennaTypeIsDirect ? AHEditor.directBetterRange.ToString("N0") + "m" : AHEditor.relayBetterRange.ToString("N0") + "m");
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label(/*Max Distance At 100%*/Localizer.Format("#autoLOC_AH_0011") + " : ", GUILayout.Width(width));
			GUILayout.Label(antennaTypeIsDirect ? AHEditor.directDistanceAt100.ToString("N0") + "m": AHEditor.relayDistanceAt100.ToString("N0") + "m");
			GUILayout.EndHorizontal();

#if fale
			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical ();
			GUILayout.Label (/*Status*/Localizer.Format ("#autoLOC_AH_0008") + " : ");
			GUILayout.Label (/*Power*/Localizer.Format ("#autoLOC_AH_0009") + " : ");
			GUILayout.Label (/*Max Range*/Localizer.Format ("#autoLOC_AH_0010") + " : ");
			GUILayout.Label (/*Max Distance At 100%*/Localizer.Format ("#autoLOC_AH_0011") + " : ");
			GUILayout.EndVertical ();
			GUILayout.BeginVertical ();
			if (antennaTypeIsDirect) {
				GUILayout.Label (AHEditor.statusStringDirect);
				GUILayout.Label (AHEditor.directBetterPower.ToString ("N0"));
				GUILayout.Label (AHEditor.directBetterRange.ToString ("N0") + "m");
				GUILayout.Label (AHEditor.directDistanceAt100.ToString ("N0") + "m");
			} else {
				GUILayout.Label (AHEditor.statusStringRelay);
				GUILayout.Label (AHEditor.relayBetterPower.ToString ("N0"));
				GUILayout.Label (AHEditor.relayBetterRange.ToString ("N0") + "m");
				GUILayout.Label (AHEditor.relayDistanceAt100.ToString ("N0") + "m");
			}
			GUILayout.EndVertical ();
			GUILayout.EndHorizontal ();
#endif
			GUILayout.Space (16f);
			GUIStyle guiStyleCenter = new GUIStyle (GUI.skin.GetStyle ("Label"));
			guiStyleCenter.alignment = TextAnchor.MiddleCenter;

			GUILayout.BeginHorizontal ();
			if (antennaTypeIsDirect) {
				GUILayout.Label (AHEditor.directDistanceAt75.ToString ("N0") + "m", guiStyleCenter);
				GUILayout.Label (AHEditor.directDistanceAt25.ToString ("N0") + "m", guiStyleCenter);
			} else {
				GUILayout.Label (AHEditor.relayDistanceAt75.ToString ("N0") + "m", guiStyleCenter);
				GUILayout.Label (AHEditor.relayDistanceAt25.ToString ("N0") + "m", guiStyleCenter);
			}
			GUILayout.EndHorizontal ();

			GUILayout.Label (AHUtil.signalPerDistanceTex);

			if (antennaTypeIsDirect) {
				GUILayout.Label (AHEditor.directDistanceAt50.ToString ("N0") + "m", guiStyleCenter);
			} else {
				GUILayout.Label (AHEditor.relayDistanceAt50.ToString ("N0") + "m", guiStyleCenter);
			}

			// Planet view button :
			if (GUILayout.Button (/*Signal Strength / Distance*/Localizer.Format ("#autoLOC_AH_0060") 
				+ " / " + Localizer.Format ("#autoLOC_AH_0059")))
			{
				if (AHEditor.showPlanetWindow) {
					AHEditor.ClosePlanetWindow ();
				} else {
					AHEditor.showPlanetWindow = true;
				}
			}

			if (GUILayout.Button (/*Add Ship to the Target List*/Localizer.Format ("#autoLOC_AH_0013"))) {
				AHEditor.AddShipToShipList ();
			}

			GUILayout.EndVertical ();
			GUI.DragWindow ();
		}

		public static void TargetWindow (int id)
		{
			// Close Button
			if (GUI.Button (new Rect (AHEditor.rectTargetWindow.size.x - 20, 2, 18, 18), "X")) {
				AHEditor.CloseTargetWindow ();
			}

			GUIStyle guiStyle;
			GUIStyle guiStyleNorm = new GUIStyle (GUI.skin.GetStyle ("Button"));
			GUIStyle guiStyleBold = new GUIStyle (GUI.skin.GetStyle ("Button"));
			guiStyleBold.fontStyle = FontStyle.Bold;

			GUILayout.BeginVertical ();

			for (int i = 0 ; i < 3 ; i++) {
				
				string dsnStr;
				if (i / 2f == AHEditor.trackingStationLevel) {
					dsnStr = "** " + /*DSN Level*/Localizer.Format ("#autoLOC_AH_0015") + " " + (i + 1) + "  (" + GameVariables.Instance.GetDSNRange (i / 2f).ToString ("N0") + ") **";
				} else {
					dsnStr = /*DSN Level*/Localizer.Format ("#autoLOC_AH_0015") + " " + (i + 1) + "  (" + GameVariables.Instance.GetDSNRange (i / 2f).ToString ("N0") + ")";
				}

				if ((AHEditor.targetType == AHEditorTargetType.DSN) 
					&& (AHEditor.targetName == /*DSN Level*/Localizer.Format ("#autoLOC_AH_0015") + " " + (i + 1).ToString ())) {

					guiStyle = guiStyleBold;
				} else {
					guiStyle = guiStyleNorm;
				}

				if (GUILayout.Button (dsnStr, guiStyle)) {
					AHEditor.SetTarget (i / 2f);
				}
			}

			GUILayout.BeginHorizontal ();
			if (AHEditor.targetType == AHEditorTargetType.FLIGHT) {
				guiStyle = guiStyleBold;
			} else {
				guiStyle = guiStyleNorm;
			}
			if (GUILayout.Button (/*In-Flight Ships*/Localizer.Format ("#autoLOC_AH_0016"), guiStyle)) {
				if (HighLogic.CurrentGame.Mode != Game.Modes.MISSION_BUILDER) {
					AHEditor.CloseTargetShipEditorWindow ();
					AHEditor.CloseTargetPartWindow ();
					AHEditor.showTargetShipFlightWindow = true;
				}
			}

			if (AHEditor.targetType == AHEditorTargetType.EDITOR) {
				guiStyle = guiStyleBold;
			} else {
				guiStyle = guiStyleNorm;
			}
			if (GUILayout.Button (/*Editor Ships*/Localizer.Format ("#autoLOC_AH_0017"), guiStyle)) {
				AHEditor.CloseTargetShipFlightWindow ();
				AHEditor.CloseTargetPartWindow ();
				AHEditor.showTargetShipEditorWindow = true;
			}

			if (AHEditor.targetType == AHEditorTargetType.PART) {
				guiStyle = guiStyleBold;
			} else {
				guiStyle = guiStyleNorm;
			}
			if (GUILayout.Button (/*Antenna Parts*/Localizer.Format ("#autoLOC_AH_0018"), guiStyle)) {
				AHEditor.CloseTargetShipEditorWindow ();
				AHEditor.CloseTargetShipFlightWindow ();
				AHEditor.showTargetPartWindow = true;
			}
			GUILayout.EndHorizontal ();

			GUILayout.EndVertical ();
			GUI.DragWindow ();
		}

		private static bool vab = true;
		private static bool relay = false;
		private static Vector2 scrollVectorEditor;
		private static List<Dictionary<string, string>> displayList;
		public static void TargetWindowShipEditor (int id)
		{
			GUIStyle guiStyleLabel;
			GUIStyle guiStyleLabelNorm = new GUIStyle (GUI.skin.GetStyle ("Label"));
			GUIStyle guiStyleLabelBold = new GUIStyle (GUI.skin.GetStyle ("Label"));
			guiStyleLabelBold.fontStyle = FontStyle.Bold;

			GUIStyle guiStyleButton;
			GUIStyle guiStyleButtonNorm = new GUIStyle (GUI.skin.GetStyle ("Button"));
			GUIStyle guiStyleButtonBold = new GUIStyle (GUI.skin.GetStyle ("Button"));
			guiStyleButtonBold.fontStyle = FontStyle.Bold;

			GUIStyle guiStyleButtonRed = new GUIStyle (GUI.skin.GetStyle ("Button"));
			guiStyleButtonRed.fontStyle = FontStyle.Bold;
			guiStyleButtonRed.normal.textColor = Color.red;
			guiStyleButtonRed.hover.textColor = Color.red;

			// Close Button
			if (GUI.Button (new Rect (AHEditor.rectTargetShipEditorWindow.size.x - 20, 2, 18, 18), "X")) {
				AHEditor.CloseTargetShipEditorWindow ();
			}

			GUILayout.BeginVertical ();

			GUILayout.BeginHorizontal ();
			if (vab) {
				guiStyleButton = guiStyleButtonBold;
			} else {
				guiStyleButton = guiStyleButtonNorm;
			}
			if (GUILayout.Button (/*VAB*/Localizer.Format ("#autoLOC_AH_0019"), guiStyleButton)) {
				vab = true;
			}

			if (vab) {
				guiStyleButton = guiStyleButtonNorm;
			} else {
				guiStyleButton = guiStyleButtonBold;
			}
			if (GUILayout.Button (/*SPH*/Localizer.Format ("#autoLOC_AH_0020"), guiStyleButton)) {
				vab = false;
			}
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			GUILayout.Space (35f);
			if (relay) {
				guiStyleButton = guiStyleButtonNorm;
			} else {
				guiStyleButton = guiStyleButtonBold;
			}
			if (GUILayout.Button (/*All*/Localizer.Format ("#autoLOC_AH_0021"), guiStyleButton)) {
				relay = false;
			}

			if (relay) {
				guiStyleButton = guiStyleButtonBold;
			} else {
				guiStyleButton = guiStyleButtonNorm;
			}
			if (GUILayout.Button (/*Relay*/Localizer.Format ("#autoLOC_AH_0003"), guiStyleButton)) {
				relay = true;
			}
			GUILayout.Space (35f);
			GUILayout.EndHorizontal ();

			scrollVectorEditor = GUILayout.BeginScrollView (scrollVectorEditor);
			if (vab) {
				if (relay) {
					displayList = AHEditor.guiExternListShipEditorVabRelay;
				} else {
					displayList = AHEditor.guiExternListShipEditorVabAll;
				}
			} else {
				if (relay) {
					displayList = AHEditor.guiExternListShipEditorSphRelay;
				} else {
					displayList = AHEditor.guiExternListShipEditorSphAll;
				}
			}

			foreach (Dictionary <string, string> vesselInfo in displayList) {
				if ((vab && (vesselInfo ["type"] != "VAB")) || (!vab && (vesselInfo ["type"] != "SPH"))) {
					continue;
				}

				GUILayout.BeginHorizontal ();
				if (GUILayout.Button (Localizer.Format ("#autoLOC_AH_0022"), GUILayout.Width (60f))) {
					AHEditor.SetTarget (vesselInfo ["pid"]);
				}

				if (AHEditor.targetPid == vesselInfo ["pid"]) {
					guiStyleLabel = guiStyleLabelBold;
				} else {
					guiStyleLabel = guiStyleLabelNorm;
				}
				GUILayout.Label (
					"("
					+ AHUtil.TruePower (Double.Parse (vesselInfo ["powerRelay"])).ToString ("N0")
					+ ")  "
					+ vesselInfo ["name"], guiStyleLabel);
				if (GUILayout.Button ("X", guiStyleButtonRed, GUILayout.Width (22f))) {
					AHEditor.RemoveShipFromShipList (vesselInfo ["pid"]);
				}
				GUILayout.EndHorizontal ();
			}
			GUILayout.EndScrollView ();

			GUILayout.EndVertical ();
		}

		private static Vector2 scrollVectorFlight;
		public static void TargetWindowShipFlight (int id)
		{
			GUIStyle guiStyleLabel;
			GUIStyle guiStyleLabelNorm = new GUIStyle (GUI.skin.GetStyle ("Label"));
			GUIStyle guiStyleLabelBold = new GUIStyle (GUI.skin.GetStyle ("Label"));
			guiStyleLabelBold.fontStyle = FontStyle.Bold;

			// Close Button
			if (GUI.Button (new Rect (AHEditor.rectTargetShipFlightWindow.size.x - 20, 2, 18, 18), "X")) {
				AHEditor.CloseTargetShipFlightWindow ();
			}

			GUILayout.BeginVertical ();

			scrollVectorFlight = GUILayout.BeginScrollView (scrollVectorFlight);
			foreach (Dictionary <string, string> vesselInfo in AHEditor.guiExternListShipFlight) {
				if (vesselInfo ["type"] != "Relay") {
					continue;
				}

				GUILayout.BeginHorizontal ();
				if (GUILayout.Button (/*Select*/Localizer.Format ("#autoLOC_AH_0022"), GUILayout.Width (60f))) {
					AHEditor.SetTarget (vesselInfo ["pid"]);
				}

				if (AHEditor.targetPid == vesselInfo ["pid"]) {
					guiStyleLabel = guiStyleLabelBold;
				} else {
					guiStyleLabel = guiStyleLabelNorm;
				}
				GUILayout.Label (
					"("
					+ AHUtil.TruePower (Double.Parse (vesselInfo ["powerRelay"])).ToString ("N0")
					+ ")  "
					+ vesselInfo ["name"], guiStyleLabel);
				GUILayout.EndHorizontal ();
			}
			GUILayout.EndScrollView ();

			GUILayout.EndVertical ();
		}

		private static Vector2 scrollVectorPart;
		public static void TargetWindowPart (int id)
		{
			GUIStyle guiStyleLabel;
			GUIStyle guiStyleLabelNorm = new GUIStyle (GUI.skin.GetStyle ("Label"));
			GUIStyle guiStyleLabelBold = new GUIStyle (GUI.skin.GetStyle ("Label"));
			guiStyleLabelBold.fontStyle = FontStyle.Bold;

			GUIStyle guiStyleButtonBold = new GUIStyle (GUI.skin.GetStyle ("Button"));
			guiStyleButtonBold.fontStyle = FontStyle.Bold;

			// Close Button
			if (GUI.Button (new Rect (AHEditor.rectTargetPartWindow.size.x - 20, 2, 18, 18), "X")) {
				AHEditor.CloseTargetPartWindow ();
			}

			GUILayout.BeginVertical ();
			scrollVectorPart = GUILayout.BeginScrollView (scrollVectorPart);

			foreach (ModuleDataTransmitter antenna in AHShipList.listAntennaPart) {
				
				if (antenna.antennaType != AntennaType.RELAY) {
					continue;
				}

				if (AHEditor.listAntennaPart [antenna] > 0) {
					guiStyleLabel = guiStyleLabelBold;
				} else {
					guiStyleLabel = guiStyleLabelNorm;
				}

				GUILayout.BeginHorizontal ();

				GUILayout.Label (AHEditor.listAntennaPart [antenna].ToString (), guiStyleLabel, GUILayout.Width (15f));

				if (GUILayout.Button ("+", guiStyleButtonBold, GUILayout.Width (20f))) {
					AHEditor.listAntennaPart [antenna]++;
					AHEditor.UpdateTargetPartPower ();
				}
				if (GUILayout.Button ("-", guiStyleButtonBold, GUILayout.Width (20f))) {
					AHEditor.listAntennaPart [antenna]--;
					AHEditor.UpdateTargetPartPower ();
				}

				GUILayout.Label (
					"(" + AHUtil.TruePower (antenna.antennaPower).ToString ("N0") + ")  " 
					+ antenna.part.partInfo.title, guiStyleLabel);

				GUILayout.EndHorizontal ();
			}
			GUILayout.EndScrollView ();

			GUILayout.Space (10f);

			GUILayout.BeginHorizontal ();
			GUILayout.Label (/*Power*/Localizer.Format ("#autoLOC_AH_0009") + " : " + AHEditor.targetPartPower.ToString ("N0"));
			if (GUILayout.Button (/*Set As Target*/Localizer.Format ("#autoLOC_AH_0023"))) {
				AHEditor.SetTargetAsPart ();
			}
			GUILayout.EndHorizontal ();

			GUILayout.EndVertical ();
		}

		public static void PlanetWindow (int id)
		{
			// Close Button
			if (GUI.Button (new Rect (AHEditor.rectPlanetWindow.size.x - 22, 2, 20, 20), "X")) {
				AHEditor.ClosePlanetWindow ();
			}

			GUILayout.BeginVertical ();
			GUILayout.BeginHorizontal ();
			GUILayout.BeginVertical ();
			// Planet name
			GUILayout.Label (/*Planet / Moon*/Localizer.Format ("#autoLOC_AH_0024"));
			foreach (MyTuple planet in AHUtil.signalPlanetList) {
				GUILayout.Label (
					new GUIContent (
						planet.item1, 
						/*Min*/Localizer.Format ("#autoLOC_AH_0025") + " = " + planet.item2.ToString ("N0") + "m | " 
					+ /*Max*/Localizer.Format ("#autoLOC_AH_0026") + " = " + planet.item3.ToString ("N0") + "m"));
//				GUI.Label (new Rect (Mouse.screenPos.x, Mouse.screenPos.y, 50, 20), GUI.tooltip);
				GUILayout.BeginArea (new Rect 
					(Mouse.screenPos.x - AHEditor.rectPlanetWindow.position.x, 
						Mouse.screenPos.y - AHEditor.rectPlanetWindow.position.y - 15, 450, 30));
				GUILayout.Label (GUI.tooltip);
				GUILayout.EndArea ();
			}
			GUILayout.EndVertical ();
			GUILayout.BeginVertical ();
			// Min distance
			GUILayout.Label (/*Signal at Min Distance*/Localizer.Format ("#autoLOC_AH_0027"));
			if (antennaTypeIsDirect) {
				foreach (double signal in AHEditor.signalMinDirect) {
					GUILayout.Label (signal.ToString ("0.00%"));
				}
			} else {
				foreach (double signal in AHEditor.signalMinRelay) {
					GUILayout.Label (signal.ToString ("0.00%"));
				}
			}

			GUILayout.EndVertical ();
			GUILayout.BeginVertical ();
			// Max distance
			GUILayout.Label (/*Signal at Max Distance*/Localizer.Format ("#autoLOC_AH_0028"));
			if (antennaTypeIsDirect) {
				foreach (double signal in AHEditor.signalMaxDirect) {
					GUILayout.Label (signal.ToString ("0.00%"));
				}
			} else {
				foreach (double signal in AHEditor.signalMaxRelay) {
					GUILayout.Label (signal.ToString ("0.00%"));
				}
			}
			GUILayout.EndVertical ();
			GUILayout.EndHorizontal ();

			// Custom distance
			GUILayout.Label (/*Check the Signal Strength at a given distance*/Localizer.Format ("#autoLOC_AH_0029") + " :");
			GUILayout.BeginHorizontal ();
			GUILayout.BeginVertical ();
			AHEditor.customDistance = GUILayout.TextField (AHEditor.customDistance);
			GUILayout.EndVertical ();
			GUILayout.BeginVertical ();
			if (antennaTypeIsDirect) {
				GUILayout.Label (AHEditor.signalCustomDistanceDirect.ToString ("0.00%"));
			} else {
				GUILayout.Label (AHEditor.signalCustomDistanceRelay.ToString ("0.00%"));
			}
			GUILayout.EndVertical ();
			GUILayout.BeginVertical ();
			if (GUILayout.Button (/*Math !*/Localizer.Format ("#autoLOC_AH_0030"))) {
				AHEditor.CalcCustomDistance ();
			}
			GUILayout.EndVertical ();
			GUILayout.EndHorizontal ();
			GUILayout.EndVertical ();
			GUI.DragWindow ();
		}
	}
}

