using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;

namespace AntennaHelper
{
	[KSPAddon (KSPAddon.Startup.Flight, false)]
	public partial class AHFlight : MonoBehaviour
	{
		private Vessel vessel;
		private bool connectedToDSN;
		private Vessel connectedTo;
		private double vesselPower, vesselPowerRelay;
		private Dictionary<ModuleDeployableAntenna, bool> deployableAntennas;

		private CelestialBody dsnBody;
		private double dsnPower;
		private double dsnRange;

		private double targetPower;
		private double targetMaxRange;
		private double targetDistance;
		private double sSToTarget;
		private double sSTargetToDSN;
		private double sSToDSN;

		Dictionary<Vessel, LinkPath> relays;

		Dictionary<Vessel, GameObject> markerObjectsRelay;
		GameObject markerObjectDSN;

		private float timeAtStart;
		private bool hasStarted = false;
		private List<WaitForSeconds> timers;
		private bool doUpdate = true;
		private bool inMapView = false;
		private bool doMath = false;


		#region Starters
		void Start ()
		{
			if ((!HighLogic.CurrentGame.Parameters.CustomParams<AntennaHelperSettings> ().enableInFlight) 
				&& !HighLogic.CurrentGame.Parameters.CustomParams<AntennaHelperSettings> ().enableInMapView) {
				Destroy (this);
				return;
			}

			timeAtStart = Time.time;
			timers = new List<WaitForSeconds> ();
			timers.Add (new WaitForSeconds (.1f));
			double delay = HighLogic.CurrentGame.Parameters.CustomParams<AntennaHelperSettings> ().delayFlightUI;
			if (delay == 0) {
				doUpdate = false;
			}
			timers.Add (new WaitForSeconds ((float)delay));

			dsnBody = FlightGlobals.GetHomeBody ();
			dsnPower = GameVariables.Instance.GetDSNRange 
				(ScenarioUpgradeableFacilities.GetFacilityLevel 
					(SpaceCenterFacility.TrackingStation));

			GameEvents.onGUIApplicationLauncherDestroyed.Add (RemoveToolbarButton);

			GameEvents.onVesselWasModified.Add (VesselModified);
			GameEvents.onVesselSwitching.Add (VesselSwitch);
			GameEvents.onVesselDestroy.Add (VesselDestroy);

			GameEvents.OnMapEntered.Add (EnteringMap);
			GameEvents.OnMapExited.Add (ExitingMap);

			StartCoroutine ("StartSecond");
		}

		private IEnumerator StartSecond ()
		{
			while (Time.time < timeAtStart + HighLogic.CurrentGame.Parameters.CustomParams<AntennaHelperSettings> ().startDelay/*1f*/) {
				yield return timers [0];
			}

			vessel = FlightGlobals.ActiveVessel;

			// possible fix for AH not always working, see forum around 16.05.2018
			if (vessel == null)
			{
				Debug.Log ("[AH] active vessel is null, shouldn't happen...");
				ReStartSecond ();
			}

			vesselPower = AHUtil.GetActualVesselPower (vessel);
			vesselPowerRelay = AHUtil.GetActualVesselPower (vessel, true);
			dsnRange = AHUtil.GetRange (vesselPower, dsnPower);
			SetDeployableAntennaList ();

			SetRelayList ();
			SetActiveConnect ();
			yield return SetMarkerList ();
			AddToolbarButton ();
			StartCoroutine ("UpdateCommNet");
		}

		private void ReStartSecond ()
		{
			StopCoroutine ("StartSecond");
			timeAtStart = Time.time;
			StartCoroutine ("StartSecond");
		}

		private void SetActiveConnect ()
		{
			if (vessel.Connection == null || !vessel.Connection.IsConnected) {
				connectedToDSN = false;
				connectedTo = null;
				targetPower = 0;
				targetMaxRange = 0;
				targetDistance = 0;
				sSToTarget = 0;
				sSTargetToDSN = 0;
				sSToDSN = 0;
			} else if (vessel.Connection.ControlPath [0].b.isHome) {
				connectedToDSN = true;
				connectedTo = null;
				targetPower = dsnPower;
				targetMaxRange = dsnRange;
				targetDistance = Vector3.Distance (vessel.GetWorldPos3D (), Relay.DSN.position) - Relay.DSN.distanceOffset;
				targetDistance = (targetDistance < 0) ? 0 : targetDistance;
				sSToTarget = AHUtil.GetSignalStrength (targetMaxRange, targetDistance);
				sSTargetToDSN = 0;
				sSToDSN = sSToTarget;
			} else {
				connectedToDSN = false;
				connectedTo = vessel.Connection.ControlPath [0].b.transform.GetComponent<Vessel> ();
				targetPower = relays [connectedTo].linkList [0].relayA.relayPower;
				targetMaxRange = AHUtil.GetRange (vesselPower, relays [connectedTo].endRelayPower);
				targetDistance = Vector3.Distance (vessel.vesselTransform.position, connectedTo.vesselTransform.position);
				sSToTarget = AHUtil.GetSignalStrength (targetMaxRange, targetDistance);
				sSTargetToDSN = relays [connectedTo].endRelaySignalStrength;
				sSToDSN = sSToTarget * sSTargetToDSN;
			}
		}

		private void SetRelayList ()
		{
			Relay.UpdateRelayVessels ();

			relays = new Dictionary<Vessel, LinkPath> ();

			foreach (Vessel relay in FlightGlobals.Vessels.FindAll 
				(v => (v.vesselType == VesselType.Relay) && (v.Connection.IsConnected) && (v != vessel)))
			{
				relays.Add (relay, new LinkPath (relay));
			}
		}

		private void SetDeployableAntennaList ()
		{
			deployableAntennas = new Dictionary<ModuleDeployableAntenna, bool> ();

			foreach (Part part in vessel.Parts.FindAll (
				p => (p.HasModuleImplementing<ModuleDataTransmitter> ()) && (p.HasModuleImplementing<ModuleDeployableAntenna> ())))
			{
				bool extended = true;
				ModuleDeployableAntenna deployable = part.FindModuleImplementing<ModuleDeployableAntenna> ();

				if (deployable.deployState != ModuleDeployablePart.DeployState.EXTENDED) {
					extended = false;
				}
				deployableAntennas.Add (deployable, extended);
			}
		}

		private IEnumerator SetMarkerList ()
		{
			markerObjectDSN = CreateMapMarkerDSN ();

			markerObjectsRelay = new Dictionary<Vessel, GameObject> ();

			foreach (KeyValuePair<Vessel, LinkPath> relay in relays) {
				yield return timers [0];
				markerObjectsRelay.Add (relay.Key, CreateMapMarkerRelay (relay.Key));
			}

		}

		private GameObject CreateMapMarkerRelay (Vessel relay)
		{
			double realSignal = relays [relay].endRelaySignalStrength;//AHUtil.GetREALSignal (relay.Connection.ControlPath);
			double range = AHUtil.GetDistanceAt0 
							(AHUtil.GetRange (vesselPower, relays [relay].endRelayPower));
			GameObject markerObject = new GameObject ();
			AHMapMarker marker = markerObject.AddComponent<AHMapMarker> ();
			marker.SetUp (range, vessel, relay.mapObject.trf, false, realSignal);


			return markerObject;
		}

		private GameObject CreateMapMarkerDSN ()
		{
			GameObject markerObject = new GameObject ();
			AHMapMarker marker = markerObject.AddComponent<AHMapMarker> ();
			marker.SetUp (dsnRange, vessel, dsnBody.MapObject.trf, true, 1d);
			return markerObject;
		}
		#endregion

		#region Destroyers
		void OnDestroy ()
		{
			// Save windows position :
			AHSettings.SavePosition ("flight_main_window_position", rectActiveConnectWindow.position);
			AHSettings.SavePosition ("flight_map_view_window_position", rectSelectCircleTypeWindow.position);
			AHSettings.WriteSave ();

			DestroyMarkers ();
			RemoveToolbarButton ();

			GameEvents.onGUIApplicationLauncherDestroyed.Remove (RemoveToolbarButton);

			GameEvents.onVesselWasModified.Remove (VesselModified);
			GameEvents.onVesselSwitching.Remove (VesselSwitch);
			GameEvents.onVesselDestroy.Remove (VesselDestroy);

			GameEvents.OnMapEntered.Remove (EnteringMap);
			GameEvents.OnMapExited.Remove (ExitingMap);
		}

		private void DestroyMarkers ()
		{
			if (markerObjectDSN != null) {
				Destroy (markerObjectDSN);
			}

			if (markerObjectsRelay != null) {
				foreach (KeyValuePair<Vessel, GameObject> markerObject in markerObjectsRelay) {
					if (markerObject.Value != null) {
						Destroy (markerObject.Value);
					}

				}
			}
		}
		#endregion

		#region AntennaLookOut

		private void VesselSwitch (Vessel fromVessel, Vessel toVessel)
		{
			StopAllCoroutines ();
			Destroy (this);
		}

		private void VesselDestroy (Vessel v)
		{
			if (v == null) {
				Debug.Log ("[AH] a null vessel is destroyed");
				return;
			}

			if ((relays != null) && (relays.ContainsKey (v))) {
				Debug.Log ("[AH] a relay vessel is destroyed, named : " + v.GetName ());
				relays.Remove (v);
			}
			if ((markerObjectsRelay != null) && (markerObjectsRelay.ContainsKey (v))) {
				Debug.Log ("[AH] a vessel with its AH map marker is destroyed, named : " + v.GetName ());
				Destroy (markerObjectsRelay [v]);
				markerObjectsRelay.Remove (v);
			}

			if (vessel == null)
			{
				Debug.Log ("[AH] active vessel not set, vessel destroyed : " + v.GetName ());
				return;
			}

			if (v == vessel) {
				Debug.Log ("[AH] the active vessel is destroyed");
				StopAllCoroutines ();
				Destroy (this);
				return;
			}
		}

		private void VesselModified (Vessel v = null)
		{
			if (v != vessel)
			{
				return;
			}

			double actualPower = AHUtil.GetActualVesselPower (FlightGlobals.ActiveVessel);

			if (actualPower != vesselPower) {
				hasStarted = false;
				StopAllCoroutines ();
				DestroyMarkers ();
				timeAtStart = Time.time;
				StartCoroutine ("StartSecond");
			}
		}

		private IEnumerator UpdateCommNet ()
		{
			List<Vessel> relayList = FlightGlobals.Vessels.FindAll (
				                         v => ((v.vesselType == VesselType.Relay) 
				                         && (v.Connection.IsConnected) 
				                         && (v != vessel)));

			while (true) {
				// Check relay state
				foreach (Vessel relay in relayList) 
				{
//					relaySS = AHUtil.GetREALSignal (relay.Connection.ControlPath);

					AHMapMarker marker = markerObjectsRelay [relay].GetComponent<AHMapMarker> ();
					if ((relays [relay].endRelaySignalStrength > marker.scale + .01d) 
						|| (relays [relay].endRelaySignalStrength < marker.scale - .01d))
					{
						marker.SetScale (relays [relay].endRelaySignalStrength);
					}
					yield return timers [0];
				}
				// Check Active Connect state
				SetActiveConnect ();

				// Chech Deployable Antenna state
				foreach(KeyValuePair<ModuleDeployableAntenna, bool> kvp in deployableAntennas) {
					if (((kvp.Key.deployState == ModuleDeployablePart.DeployState.EXTENDED) 
							&& (kvp.Value != true)) 
						|| ((kvp.Key.deployState != ModuleDeployablePart.DeployState.EXTENDED) 
							&& (kvp.Value == true))) {
						VesselModified ();
					}
				}

				UpdateActiveDetails ();
				if (showPotentialRelaysWindow || !hasStarted) {
					UpdateRelaysDetails ();
				}

				hasStarted = true;
				if (!doMath || !doUpdate) {
					yield break;
				}
				yield return timers[1];
			}
		}

		List<Dictionary<string, string>> detailsActiveConnectLinks;
		List<List<Dictionary<string, string>>> detailsRelaysLinks;

		private void UpdateActiveDetails ()
		{
			detailsActiveConnectLinks = new List<Dictionary<string, string>> ();
			detailsActiveConnectLinks.Add (new Dictionary<string, string> ());

			// Always the same :
			detailsActiveConnectLinks [0].Add ("aName", vessel.GetName ());
			detailsActiveConnectLinks [0].Add ("aPowerTotal", vesselPower.ToString ("N0"));
			detailsActiveConnectLinks [0].Add ("aPowerRelay", vesselPowerRelay.ToString ("N0"));
			detailsActiveConnectLinks [0].Add ("activeSignalStrength", sSToDSN.ToString ("0.00%"));
			detailsActiveConnectLinks [0].Add ("distance", targetDistance.ToString ("N0") + "m");

			if (connectedToDSN) {
				detailsActiveConnectLinks [0].Add ("bName", /*"DSN"*/Localizer.Format ("#autoLOC_AH_0014"));
				detailsActiveConnectLinks [0].Add ("bPowerTotal", dsnPower.ToString ("N0"));
				detailsActiveConnectLinks [0].Add ("bPowerRelay", dsnPower.ToString ("N0"));
				detailsActiveConnectLinks [0].Add ("signalStrength", sSToDSN.ToString ("0.00%"));
				detailsActiveConnectLinks [0].Add ("maxRange", dsnRange.ToString ("N0") + "m");
			} else if (connectedTo != null){
				detailsActiveConnectLinks [0].Add ("bName", relays [connectedTo].linkList [0].relayA.name);
				detailsActiveConnectLinks [0].Add ("bPowerTotal", relays [connectedTo].linkList [0].relayA.directPower.ToString ("N0"));
				detailsActiveConnectLinks [0].Add ("bPowerRelay", relays [connectedTo].linkList [0].relayA.relayPower.ToString ("N0"));
				detailsActiveConnectLinks [0].Add ("signalStrength", sSToTarget.ToString ("0.00%"));
				detailsActiveConnectLinks [0].Add ("maxRange", targetMaxRange.ToString ("N0") + "m");

				int i = 1;
				foreach (Link link in relays [connectedTo].linkList) {
					detailsActiveConnectLinks.Add (new Dictionary<string, string> ());
					detailsActiveConnectLinks [i].Add ("aName", link.relayA.name);
					detailsActiveConnectLinks [i].Add ("bName", link.relayB.name);
					detailsActiveConnectLinks [i].Add ("aPowerTotal", link.relayA.directPower.ToString ("N0"));
					detailsActiveConnectLinks [i].Add ("bPowerTotal", link.relayB.directPower.ToString ("N0"));
					detailsActiveConnectLinks [i].Add ("aPowerRelay", link.relayA.relayPower.ToString ("N0"));
					detailsActiveConnectLinks [i].Add ("bPowerRelay", link.relayB.relayPower.ToString ("N0"));
					detailsActiveConnectLinks [i].Add ("signalStrength", link.signalStrength.ToString ("0.00%"));
					detailsActiveConnectLinks [i].Add ("maxRange", link.maxRange.ToString ("N0") + "m");
					detailsActiveConnectLinks [i].Add ("distance", link.distance.ToString ("N0") + "m");
					i++;
				}
			} else {
				detailsActiveConnectLinks [0].Add ("bName", /*"None"*/Localizer.Format ("#autoLOC_AH_0063"));
				detailsActiveConnectLinks [0].Add ("bPowerTotal", "0");
				detailsActiveConnectLinks [0].Add ("bPowerRelay", "0");
				detailsActiveConnectLinks [0].Add ("signalStrength", "0");
				detailsActiveConnectLinks [0].Add ("maxRange", "0m");
			}
		}

		private void UpdateRelaysDetails ()
		{
			detailsRelaysLinks = new List<List<Dictionary<string, string>>> ();

			int relayIt = 0;
			foreach (KeyValuePair<Vessel, LinkPath> relay in relays) {
				detailsRelaysLinks.Add (new List<Dictionary<string, string>> ());
				int linkIt = 0;
				foreach (Link link in relay.Value.linkList) {
					detailsRelaysLinks [relayIt].Add (new Dictionary<string, string> ());
					detailsRelaysLinks [relayIt] [linkIt].Add ("aName", link.relayA.name);
					detailsRelaysLinks [relayIt] [linkIt].Add ("bName", link.relayB.name);
					detailsRelaysLinks [relayIt] [linkIt].Add ("aPowerTotal", link.relayA.directPower.ToString ("N0"));
					detailsRelaysLinks [relayIt] [linkIt].Add ("bPowerTotal", link.relayB.directPower.ToString ("N0"));
					detailsRelaysLinks [relayIt] [linkIt].Add ("aPowerRelay", link.relayA.relayPower.ToString ("N0"));
					detailsRelaysLinks [relayIt] [linkIt].Add ("bPowerRelay", link.relayB.relayPower.ToString ("N0"));
					detailsRelaysLinks [relayIt] [linkIt].Add ("signalStrength", link.signalStrength.ToString ("0.00%"));
					detailsRelaysLinks [relayIt] [linkIt].Add ("maxRange", link.maxRange.ToString ("N0") + "m");
					detailsRelaysLinks [relayIt] [linkIt].Add ("endSignalStrength", relay.Value.endRelaySignalStrength.ToString ("0.00%"));
					detailsRelaysLinks [relayIt] [linkIt].Add ("distance", link.distance.ToString ("N0") + "m");
					linkIt++;
				}
				relayIt++;
			}
		}
		#endregion

		// nested class
//		class LinkPath
//		{
//			public List<Link> linkList;
//			public double endRelayPower;
//			public double endRelaySignalStrength { get { return _endRelaySignalStrength (); } }
//
//			private Vessel activeVessel;
//
//			public LinkPath (Vessel v)
//			{
//				activeVessel = v;
//
//				SetLinks ();
//
//				endRelayPower = linkList [0].relayA.relayPower;
//			}
//
//			private double _endRelaySignalStrength ()
//			{
//				SetLinks ();
//				double signal = 1d;
//				foreach (Link link in linkList) {
//					signal *= link.signalStrength;
//				}
//				return signal;
//			}
//
//			private void SetLinks ()
//			{
//				linkList = new List<Link> ();
//
//				foreach (CommNet.CommLink link in activeVessel.Connection.ControlPath) {
//					Relay relayB;
//					if (link.b.isHome) {
//						relayB = Relay.DSN;
//					} else {
//						relayB = Relay.GetRelayVessel (link.b.transform.GetComponent<Vessel> ());
//					}
//					linkList.Add (new Link (new RelayVessel (link.a.transform.GetComponent<Vessel> ()), relayB));
//				}
//			}
//		}
//
//		class Link
//		{
//			public Relay relayA, relayB;
//			public double maxRange;
//			public double signalStrength { get { return _signalStrength (); } }
//			public double distance { get { return _distance (); } }
//
//			public Link (Relay transmitter, Relay relay)
//			{
//				relayA = transmitter;
//				relayB = relay;
//
//				maxRange = AHUtil.GetRange (relayA.relayPower, relayB.relayPower);
//			}
//
//			private double _signalStrength ()
//			{
//				return AHUtil.GetSignalStrength (maxRange, distance);
//			}
//
//			private double _distance ()
//			{
//				return ((Vector3d.Distance (relayA.position, relayB.position)) - relayB.distanceOffset);
//			}
//		}
//
//		class Relay
//		{
//			public string name;
//			public double relayPower, directPower;
////			public bool isConnected;
////			public Relay isConnectedTo;
//			public Vector3d position { get { return _position (); } }
//			public bool isDSN = false;
//			public double distanceOffset = 0;
//
//			private static CelestialBody home;
//			public static Relay DSN { get { return _dsn; } }
//			private static Relay _dsn;
//
//			static List<RelayVessel> potentialRelays;
//
//			static Relay ()
//			{
//				home = FlightGlobals.GetHomeBody ();
//				_dsn = new Relay ();
//				_dsn.relayPower = GameVariables.Instance.GetDSNRange 
//					(ScenarioUpgradeableFacilities.GetFacilityLevel 
//						(SpaceCenterFacility.TrackingStation));
//				_dsn.directPower = _dsn.relayPower;
//				_dsn.isDSN = true;
//				_dsn.distanceOffset = home.Radius;
//				_dsn.name = /*"DSN"*/Localizer.Format ("#autoLOC_AH_0014");
//
//				potentialRelays = new List<RelayVessel> ();
//				foreach (Vessel v in FlightGlobals.Vessels.FindAll (v => (v.Connection != null) && (v != FlightGlobals.ActiveVessel))) {
//					double relayPower = AHUtil.GetActualVesselPower (v, true);
//					if (relayPower > 0) {
//						potentialRelays.Add (new RelayVessel (v));
//					}
//				}
////				Debug.Log ("[AH] dsn distance offset : " + _dsn.distanceOffset);
////				Debug.Log ("[AH] static dsn relay constructed");
//			}
//
//			public static RelayVessel GetRelayVessel (Vessel v)
//			{
//				return potentialRelays.Find (relay => (relay.vessel == v));
//			}
//
//
//			protected virtual Vector3d _position ()
//			{
//				return home.position;
//			}
//
//			/*
//			public static bool operator== (Relay relayA, Relay relayB)
//			{
//				if ((relayA.relayPower == relayB.relayPower) && (relayA.directPower == relayB.directPower)) {
//					return true;
//				} else {
//					return false;
//				}
//			}
//
//			public static bool operator!= (Relay relayA, Relay relayB)
//			{
//				return !(relayA == relayB);
//			}
//
//			public override bool Equals (object obj)
//			{
//				if (obj == null) {
//					if (this == null) {
//						return true;
//					} else {
//						return false;
//					}
//				}
//
//				if (obj.GetType () == this.GetType ()) {
//					if ((Relay)obj == this) {
//						return true;
//					} else {
//						return false;
//					}
//				} else {
//					return false;
//				}
//			}
//
//			public override int GetHashCode ()
//			{
//				int hash = relayPower.GetHashCode ();
//				hash = (hash * 7) + directPower.GetHashCode ();
//				return hash;
//			}*/
//		}
//
//		class RelayVessel : Relay
//		{
//			public Vessel vessel;
//
//			public RelayVessel (Vessel v)
//			{
//				vessel = v;
//				relayPower = AHUtil.GetActualVesselPower (v, true);
//				directPower = AHUtil.GetActualVesselPower (v);
////				isConnected = vessel.Connection.IsConnected;
//				name = vessel.GetName ();
//			}
//
//			protected override Vector3d _position ()
//			{
//				return vessel.GetTransform ().position;
//			}
//		}
//
//
	}
		
}

