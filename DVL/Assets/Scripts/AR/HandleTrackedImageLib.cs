using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class HandleTrackedImageLib : MonoBehaviour
{
	private ARTrackedImageManager manager;
	private GameObject boardPrefab;
	public GameObject tilePrefabParent;
	public static HandleTrackedImageLib CustomTrackingManagerInstance;

	private List<string> BoardTrackers = new List<string>();

	private void Awake()
	{
		CustomTrackingManagerInstance = this;
	}
	private void Start()
	{
		manager = this.GetComponent<ARTrackedImageManager>();
		manager.trackedImagesChanged += OnTrackedImagesChanged;
		boardPrefab = FindObjectOfType<BoardGrid>().gameObject;
		SetUpBoardTracker();
#if UNITY_STANDALONE || UNITY_EDITOR
		PCSetUP();
#endif
	}

	//Set Up a plane for PC play
	private void PCSetUP()
    {
		GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
		plane.transform.position = new Vector3(0, 0, 0);
		plane.transform.rotation = Quaternion.identity;
		plane.transform.localScale = new Vector3(10, 10, 10);
		plane.GetComponent<MeshRenderer>().enabled = false;
		plane.name = "DebugPlane";
		plane.layer = 9;
	}

    private void Update()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
		MouseController();
#endif
	}

	//Cast a ray to handle the tilePrefabParent with mouse
	private void MouseController()
    {
		if (Input.GetKey(KeyCode.LeftShift))
		{
			RaycastHit hit;
			Ray mousePos = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(mousePos.origin, mousePos.direction, out hit, 2, 1 << 9))
			{
				if (hit.transform.name == "DebugPlane")
				{
					tilePrefabParent.transform.position = hit.point;
				}	
			}
		}
	}

	//Includes all Multitrackernames to List
    private void SetUpBoardTracker()
    {
		BoardTrackers.AddMany("BottomLeft", "BottomRight", "TopLeft", "TopRight");
	}

	//Update function for the Image Tracker to get the right pos/rot
	private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
	{
		foreach (var trackedImage in eventArgs.added)
		{
			trackedImage.transform.localScale = new Vector3(1f, 1f, 1f);
		}

		List<ARTrackedImage> multiTrackList = new List<ARTrackedImage>();
		foreach (var trackedImage in eventArgs.updated)
		{
			if (BoardTrackers.Contains(trackedImage.referenceImage.name))
			{
				if(trackedImage.trackingState == TrackingState.Tracking)
					multiTrackList.Add(trackedImage);
			}
			else
			{
				if (trackedImage.trackingState == TrackingState.Tracking)
                {
					HandleSingleTracker(trackedImage);
                }				
			}
		}

		if(multiTrackList.Count>0)
			HandleMultiTracker(multiTrackList[0]);
	}

	//sets up the new Prefab that dropped out of the Grid 
	public void ChangeTrackedPrefab(GameObject droppedOutPrefab)
	{
		if (NetworkManager.instance.isDebug)
		{
			droppedOutPrefab.GetComponent<Tile>().isInFOW = false;
			droppedOutPrefab.GetComponent<Tile>().PrefabColor();
			var meshes = droppedOutPrefab.GetComponentsInChildren<MeshRenderer>();
			foreach (MeshRenderer mesh in meshes)
			{
				if (mesh.CompareTag("Fog"))
				{
					mesh.enabled = false;
				}
				else
				{
					mesh.enabled = true;
					mesh.material.color = Color.white;
				}
			}
		}


		tilePrefabParent.SetActive(false);

		droppedOutPrefab.transform.SetParent(tilePrefabParent.transform);
		droppedOutPrefab.transform.localPosition = Vector3.zero;
		droppedOutPrefab.transform.localRotation = Quaternion.identity;

		droppedOutPrefab.GetComponent<FindNearestGridSlot>().enabled = true;
		BoardGrid.instance.trackedTile = droppedOutPrefab.GetComponent<Tile>();

		Invoke("ToggleBackOn", 2);
	}

	//Reactivates the Prefab after time 
	private void ToggleBackOn()
	{
		tilePrefabParent.SetActive(true);
	}
	private void HandleMultiTracker(ARTrackedImage trackedImage)
    {
		boardPrefab.SetActive(true);
		if (trackedImage.trackingState != TrackingState.Tracking)
			return;

		boardPrefab.transform.localPosition = trackedImage.transform.localPosition - GetOffset(trackedImage);
		boardPrefab.transform.localEulerAngles = GetRotation(trackedImage);
	}

	private void HandleSingleTracker(ARTrackedImage trackedImage)
    {
		tilePrefabParent.SetActive(true);
		tilePrefabParent.transform.localPosition = trackedImage.transform.localPosition;
		tilePrefabParent.transform.localRotation = trackedImage.transform.localRotation;
	}

    #region calculate the right position and rotation
    private Vector3 GetOffset(ARTrackedImage image)
    {
        switch(image.referenceImage.name)
        {
			case "BottomLeft":
				return Vector3.zero;
			case "BottomRight":
				return boardPrefab.transform.right * 0.6f;
			case "TopLeft":
				return (boardPrefab.transform.right + boardPrefab.transform.forward) * 0.6f;
			case "TopRight":
				return boardPrefab.transform.forward * 0.6f;
			default:
				return Vector3.zero;
		}
    }
	private Vector3 GetRotation(ARTrackedImage image)
    {
		Vector3 gy = GyroModifyCamera().eulerAngles;
		Vector3 a = new Vector3(gy.x, image.transform.localEulerAngles.y, gy.z);
		return a;
	}
	Quaternion GyroModifyCamera()
	{
		return GyroToUnity(Input.gyro.attitude);
	}
	private static Quaternion GyroToUnity(Quaternion q)
	{
		return new Quaternion(q.x, q.y, -q.z, -q.w);
	}
    #endregion
}
