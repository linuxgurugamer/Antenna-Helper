using System;
using UnityEngine;

namespace AntennaHelper
{
	public class AHMapMarker : MonoBehaviour
	{
		private Transform parent, target;
		private GameObject marker, markerMirror, circleGreen, circleYellow, circleOrange, circleRed;
		private float scaleGreen, scaleYellow, scaleOrange, scaleRed;
		private bool isEnabled;
		private Orbit transmitOrbit;
		private bool connectedToHome;
		private bool cameraIsClose;
		private bool forTrackingStation;
		private double maxRange;

		public double scale;

		void Start ()
		{
			TimingManager.LateUpdateAdd (TimingManager.TimingStage.BetterLateThanNever, DoUpdate);

			isEnabled = false;
			cameraIsClose = true;
			GameEvents.OnMapEntered.Add (MapEnter);
			GameEvents.OnMapExited.Add (MapExit);
		}

		public void SetUp (double maximumRange, Vessel vesselTransmitter, Transform mapObjectRelay, bool relayIsHome = false, double sS = Double.NaN, bool forTrackingStationParam = false)
		{
			parent = mapObjectRelay;

			forTrackingStation = forTrackingStationParam;

			if (!forTrackingStation) {
				target = vesselTransmitter.mapObject.trf;

				transmitOrbit = vesselTransmitter.orbit;
			}



			maxRange = maximumRange;
			if (relayIsHome) {
				maxRange += Planetarium.fetch.Home.Radius;
			}
			connectedToHome = relayIsHome;

			SetScale (sS);
//			scaleGreen = 0;
//			scaleYellow = 0;
//			scaleOrange = 0;
//			scaleRed = AHUtil.GetMapScale (maxRange);
//			if (sS >= .25d) {
//				// draw orange circle
//				scaleOrange = AHUtil.GetMapScale (AHUtil.GetDistanceFor (.25d * (1d / sS), maxRange));
//			}
//			if (sS >= .5d) {
//				// draw yellow circle
//				scaleYellow = AHUtil.GetMapScale (AHUtil.GetDistanceFor (.5d * (1d / sS), maxRange));
//			}
//			if (sS >= .75d) {
//				// draw green circle
//				scaleGreen = AHUtil.GetMapScale (AHUtil.GetDistanceFor (.75d * (1d / sS), maxRange));
//			}
//			if (sS == 1d) {
//				scaleGreen = AHUtil.GetMapScale (AHUtil.GetDistanceFor (75, maxRange));
//				scaleYellow = AHUtil.GetMapScale (AHUtil.GetDistanceFor (50, maxRange));
//				scaleOrange = AHUtil.GetMapScale (AHUtil.GetDistanceFor (25, maxRange));
//			}


			// Creating circles :
			circleGreen = GameObject.CreatePrimitive (PrimitiveType.Quad);
			circleGreen.layer = 10;
			Destroy (circleGreen.GetComponent<MeshCollider> ());
			circleGreen.GetComponent<MeshRenderer> ().material = new Material (Shader.Find ("Unlit/Transparent"));
			circleGreen.GetComponent<MeshRenderer> ().material.mainTexture = AHUtil.circleGreenTex;
			circleGreen.transform.localScale = new Vector3 (scaleGreen, scaleGreen, 1f);

			circleYellow = GameObject.CreatePrimitive (PrimitiveType.Quad);
			circleYellow.layer = 10;
			Destroy (circleYellow.GetComponent<MeshCollider> ());
			circleYellow.GetComponent<MeshRenderer> ().material = new Material (Shader.Find ("Unlit/Transparent"));
			circleYellow.GetComponent<MeshRenderer> ().material.mainTexture = AHUtil.circleYellowTex;
			circleYellow.transform.localScale = new Vector3 (scaleYellow, scaleYellow, 1f);

			circleOrange = GameObject.CreatePrimitive (PrimitiveType.Quad);
			circleOrange.layer = 10;
			Destroy (circleOrange.GetComponent<MeshCollider> ());
			circleOrange.GetComponent<MeshRenderer> ().material = new Material (Shader.Find ("Unlit/Transparent"));
			circleOrange.GetComponent<MeshRenderer> ().material.mainTexture = AHUtil.circleOrangeTex;
			circleOrange.transform.localScale = new Vector3 (scaleOrange, scaleOrange, 1f);

			circleRed = GameObject.CreatePrimitive (PrimitiveType.Quad);
			circleRed.layer = 10;
			Destroy (circleRed.GetComponent<MeshCollider> ());
			circleRed.GetComponent<MeshRenderer> ().material = new Material (Shader.Find ("Unlit/Transparent"));
			circleRed.GetComponent<MeshRenderer> ().material.mainTexture = AHUtil.circleRedTex;
			circleRed.transform.localScale = new Vector3 (scaleRed, scaleRed, 1f);

			// set position and parenting :
			marker = this.gameObject;
			marker.layer = 10;
			marker.transform.localPosition = Vector3.zero;
			marker.transform.position = parent.position;

			circleGreen.transform.localPosition = Vector3.zero;
			circleGreen.transform.SetParent (marker.transform);
			circleGreen.transform.position = marker.transform.position;
			circleGreen.transform.localRotation = Quaternion.Euler (Vector3.zero);
			circleGreen.transform.eulerAngles = new Vector3 (90f, 0, 0);

			circleYellow.transform.localPosition = Vector3.zero;
			circleYellow.transform.SetParent (marker.transform);
			circleYellow.transform.position = marker.transform.position;
			circleYellow.transform.localRotation = Quaternion.Euler (Vector3.zero);
			circleYellow.transform.eulerAngles = new Vector3 (90f, 0, 0);

			circleOrange.transform.localPosition = Vector3.zero;
			circleOrange.transform.SetParent (marker.transform);
			circleOrange.transform.position = marker.transform.position;
			circleOrange.transform.localRotation = Quaternion.Euler (Vector3.zero);
			circleOrange.transform.eulerAngles = new Vector3 (90f, 0, 0);

			circleRed.transform.localPosition = Vector3.zero;
			circleRed.transform.SetParent (marker.transform);
			circleRed.transform.position = marker.transform.position;
			circleRed.transform.localRotation = Quaternion.Euler (Vector3.zero);
			circleRed.transform.eulerAngles = new Vector3 (90f, 0, 0);

			//Set a mirror of the circles
			markerMirror = (GameObject)Instantiate (marker, marker.transform);
			Destroy (markerMirror.GetComponent<AHMapMarker> ());
			markerMirror.transform.eulerAngles = new Vector3 (180, 0, 0);
			foreach (MeshCollider collider in markerMirror.GetComponentsInChildren<MeshCollider> ()) {
				Destroy (collider);
				////
				/// For whatever reason when duplicating a gameObject the collider destroyed on
				/// the original gets added back to the clone...
				//
			}
			marker.SetActive (false);
		}

		public void OnDestroy ()
		{
			TimingManager.LateUpdateRemove (TimingManager.TimingStage.BetterLateThanNever, DoUpdate);
			GameEvents.OnMapEntered.Remove (MapEnter);
			GameEvents.OnMapExited.Remove (MapExit);
		}

		public void SetScale (double sS)
		{
			scaleGreen = 0;
			scaleYellow = 0;
			scaleOrange = 0;
			scaleRed = AHUtil.GetMapScale (maxRange);
			if (sS >= .25d) {
				// draw orange circle
				scaleOrange = AHUtil.GetMapScale (AHUtil.GetDistanceFor (.25d * (1d / sS), maxRange));
			}
			if (sS >= .5d) {
				// draw yellow circle
				scaleYellow = AHUtil.GetMapScale (AHUtil.GetDistanceFor (.5d * (1d / sS), maxRange));
			}
			if (sS >= .75d) {
				// draw green circle
				scaleGreen = AHUtil.GetMapScale (AHUtil.GetDistanceFor (.75d * (1d / sS), maxRange));
			}
			if (sS == 1d) {
				scaleGreen = AHUtil.GetMapScale (AHUtil.GetDistanceFor (75, maxRange));
				scaleYellow = AHUtil.GetMapScale (AHUtil.GetDistanceFor (50, maxRange));
				scaleOrange = AHUtil.GetMapScale (AHUtil.GetDistanceFor (25, maxRange));
			}

			scale = sS;
		}

		#region Hide/Show
		private void MapEnter ()
		{
			if (isEnabled) {
				marker.SetActive (true);
			}
		}

		private void MapExit ()
		{
			if (marker != null) {
				marker.SetActive (false);
			}
		}

		public void Hide ()
		{
			marker.SetActive (false);
			isEnabled = false;
		}

		public void Show ()
		{
			marker.SetActive (true);
			isEnabled = true;
		}
		#endregion
		public void DoUpdate ()
		{
			if (!this.isActiveAndEnabled) {
				return;
			}

			marker.transform.position = parent.position;
			marker.transform.rotation = Planetarium.Rotation;

			if (forTrackingStation) {
				marker.transform.LookAt (Planetarium.fetch.Sun.MapObject.trf);
				return;
			}

			if (connectedToHome && transmitOrbit.referenceBody == Planetarium.fetch.Home) {
				// This is the best method for a transmitter orbiting a DSN
				marker.transform.eulerAngles = new Vector3 
					(marker.transform.eulerAngles.x, marker.transform.eulerAngles.y - (float)transmitOrbit.LAN, marker.transform.eulerAngles.z);
				marker.transform.eulerAngles = new Vector3 
					(marker.transform.eulerAngles.x - (float)transmitOrbit.inclination, marker.transform.eulerAngles.y, marker.transform.eulerAngles.z);
			} else {
				// All in one method :
				marker.transform.LookAt (target);
			}

			CheckCameraDistance ();
		}

		private void CheckCameraDistance ()
		{
			float distance = Vector3.Distance (marker.transform.position, CameraManager.GetCurrentCamera ().transform.position);
			if (distance > 3E+07) {
				if (cameraIsClose) {
					marker.SetLayerRecursive (24);
					cameraIsClose = false;
				}
			} else if (!cameraIsClose) {
				marker.SetLayerRecursive (10);
				cameraIsClose = true;
			}
		}
	}
}

