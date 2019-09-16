using System;
using System.Collections.Generic;
using KSP.Localization;

namespace AntennaHelper
{
	public class LinkPath
	{
		public List<Link> linkList;
		public double endRelayPower;
		public double endRelaySignalStrength { get { return _endRelaySignalStrength (); } }

		private Vessel activeVessel;

		public LinkPath (Vessel v)
		{
			// in-flight vessel
			activeVessel = v;

			SetLinks ();

			endRelayPower = linkList [0].relayA.relayPower;
		}

		private double _endRelaySignalStrength ()
		{
			SetLinks ();
			double signal = 1d;
			foreach (Link link in linkList) {
				signal *= link.signalStrength;
			}
			return signal;
		}

		private void SetLinks ()
		{
			linkList = new List<Link> ();

			foreach (CommNet.CommLink link in activeVessel.Connection.ControlPath) {
				Relay relayB;
				if (link.b.isHome) {
					relayB = Relay.DSN;
				} else {
					relayB = Relay.GetRelayVessel (link.b.transform.GetComponent<Vessel> ());
				}
				linkList.Add (new Link (new RelayVessel (link.a.transform.GetComponent<Vessel> ()), relayB));
			}
		}
	}

	public class Link
	{
		public Relay relayA, relayB;
		public double maxRange;
		public double signalStrength { get { return _signalStrength (); } }
		public double distance { get { return _distance (); } }

		public Link (Relay transmitter, Relay relay)
		{
			relayA = transmitter;
			relayB = relay;

			maxRange = AHUtil.GetRange (relayA.relayPower, relayB.relayPower);
		}

		private double _signalStrength ()
		{
			return AHUtil.GetSignalStrength (maxRange, distance);
		}

		private double _distance ()
		{
			return ((Vector3d.Distance (relayA.position, relayB.position)) - relayB.distanceOffset);
		}
	}

	public class Relay
	{
		public string name;
		public double relayPower, directPower;
		//			public bool isConnected;
		//			public Relay isConnectedTo;
		public Vector3d position { get { return _position (); } }
		public bool isDSN = false;
		public double distanceOffset = 0;

		private static CelestialBody home;
		public static Relay DSN { get { return _dsn; } }
		private static Relay _dsn;

		static List<RelayVessel> potentialRelays;

		static Relay ()
		{
			home = FlightGlobals.GetHomeBody ();
			_dsn = new Relay ();
			_dsn.relayPower = GameVariables.Instance.GetDSNRange 
				(ScenarioUpgradeableFacilities.GetFacilityLevel 
					(SpaceCenterFacility.TrackingStation));
			_dsn.directPower = _dsn.relayPower;
			_dsn.isDSN = true;
			_dsn.distanceOffset = home.Radius;
			_dsn.name = /*"DSN"*/Localizer.Format ("#autoLOC_AH_0014");

			//				Debug.Log ("[AH] dsn distance offset : " + _dsn.distanceOffset);
			//				Debug.Log ("[AH] static dsn relay constructed");

			UpdateRelayVessels ();
		}

		public static void UpdateRelayVessels ()
		{
			potentialRelays = new List<RelayVessel> ();
			foreach (Vessel v in FlightGlobals.Vessels.FindAll (v => (v.Connection != null))) {
				double relayPower = AHUtil.GetActualVesselPower (v, true);
				if (relayPower > 0) {
					potentialRelays.Add (new RelayVessel (v));
				}
			}
		}

		public static RelayVessel GetRelayVessel (Vessel v)
		{
			return potentialRelays.Find (relay => (relay.vessel == v));
		}


		protected virtual Vector3d _position ()
		{
			return home.position;
		}
	}

	public class RelayVessel : Relay
	{
		public Vessel vessel;

		public RelayVessel (Vessel v)
		{
			vessel = v;
			relayPower = AHUtil.GetActualVesselPower (v, true);
			directPower = AHUtil.GetActualVesselPower (v);
			//				isConnected = vessel.Connection.IsConnected;
			name = vessel.GetName ();
		}

		protected override Vector3d _position ()
		{
			return vessel.GetTransform ().position;
		}
	}


}

