using UnityEngine;
using System.Collections;
using Holofunk.Core;
//using Windows.Kinect;


public class SkeletonOverlayer : MonoBehaviour 
{
//	[Tooltip("GUI-texture used to display the color camera feed on the scene background.")]
//	public GUITexture backgroundImage;

	[Tooltip("Camera that will be used to overlay the 3D-objects over the background.")]
	public Camera foregroundCamera;
	
	[Tooltip("Index of the player, tracked by this component. 0 means the 1st player, 1 - the 2nd one, 2 - the 3rd one, etc.")]
	public int playerIndex = 0;
	
	[Tooltip("Game object used to overlay the joints.")]
	public GameObject jointPrefab;

	[Tooltip("Line object used to overlay the bones.")]
	public LineRenderer linePrefab;
	//public float smoothFactor = 10f;

	//public UnityEngine.UI.Text debugText;
	
	private GameObject[] joints = null;
	private LineRenderer[] lines = null;

	private Quaternion initialRotation = Quaternion.identity;

	// background rectangle
	private Rect backgroundRect = Rect.zero;

	private Vector3Averager[] averagers = null;


	void Start()
	{
		KinectManager kinectManager = KinectManager.Instance;

		if(kinectManager && kinectManager.IsInitialized())
		{
			int jointsCount = kinectManager.GetJointCount();

			if(jointPrefab)
			{
				// array holding the skeleton joints
				joints = new GameObject[jointsCount];
				averagers = new Vector3Averager[jointsCount];
				
				for(int i = 0; i < joints.Length; i++)
				{
					joints[i] = Instantiate(jointPrefab) as GameObject;
					joints[i].transform.parent = transform;
					joints[i].name = ((KinectInterop.JointType)i).ToString();
					joints[i].SetActive(false);

					// we'll average over five joint positions... arbitrary but nice and smooth
					averagers[i] = new Vector3Averager(10);
				}
			}

			// array holding the skeleton lines
			lines = new LineRenderer[jointsCount];

//			if(linePrefab)
//			{
//				for(int i = 0; i < lines.Length; i++)
//				{
//					lines[i] = Instantiate(linePrefab) as LineRenderer;
//					lines[i].transform.parent = transform;
//					lines[i].gameObject.SetActive(false);
//				}
//			}
		}

		// always mirrored
		initialRotation = Quaternion.Euler(new Vector3(0f, 180f, 0f));

		if (!foregroundCamera) 
		{
			// by default - the main camera
			foregroundCamera = Camera.main;
		}
	}
	
	void Update () 
	{
		KinectManager manager = KinectManager.Instance;
		
		if(manager && manager.IsInitialized() && foregroundCamera)
		{
//			//backgroundImage.renderer.material.mainTexture = manager.GetUsersClrTex();
//			if(backgroundImage && (backgroundImage.texture == null))
//			{
//				backgroundImage.texture = manager.GetUsersClrTex();
//			}

			// get the background rectangle (use the portrait background, if available)
			backgroundRect = foregroundCamera.pixelRect;
			PortraitBackground portraitBack = PortraitBackground.Instance;
			
			if(portraitBack && portraitBack.enabled)
			{
				backgroundRect = portraitBack.GetBackgroundRect();
			}

			// overlay all joints in the skeleton
			if(manager.IsUserDetected(playerIndex))
			{
				long userId = manager.GetUserIdByIndex(playerIndex);
				int jointsCount = manager.GetJointCount();

				for(int i = 0; i < jointsCount; i++)
				{
					int joint = i;

					if(manager.IsJointTracked(userId, joint))
					{
						Vector3 rawPosJoint = manager.GetJointPosColorOverlay(userId, joint, foregroundCamera, backgroundRect);
						averagers[i].Update(rawPosJoint);
						Vector3 posJoint = averagers[i].Average;
						//Vector3 posJoint = manager.GetJointPosition(userId, joint);

						if(joints != null)
						{
							// overlay the joint, only on the hands
							if (posJoint != Vector3.zero
								&& (joint == (int)KinectInterop.JointType.HandLeft 
									|| joint == (int)KinectInterop.JointType.HandRight))
							{
//								if(debugText && joint == 0)
//								{
//									debugText.text = string.Format("{0} - {1}\nRealPos: {2}", 
//									                                       (KinectInterop.JointType)joint, posJoint,
//									                                       manager.GetJointPosition(userId, joint));
//								}
								
								joints[i].SetActive(true);
								averagers[i].Update(posJoint);
								joints[i].transform.position = posJoint;

								Quaternion rotJoint = manager.GetJointOrientation(userId, joint, false);
								rotJoint = initialRotation * rotJoint;
								// we don't try to average joint rotation because we don't even care (Holofunk, that is)
								joints[i].transform.rotation = rotJoint;
							}
							else
							{
								joints[i].SetActive(false);
							}
						}

						if(lines[i] == null && linePrefab != null)
						{
							lines[i] = Instantiate(linePrefab) as LineRenderer;
							lines[i].transform.parent = transform;
							lines[i].gameObject.SetActive(false);
						}

						if(lines[i] != null)
						{
							// overlay the line to the parent joint
							int jointParent = (int)manager.GetParentJoint((KinectInterop.JointType)joint);
							// hopefully the parent got updated first lol
							//Vector3 posParent = manager.GetJointPosColorOverlay(userId, jointParent, foregroundCamera, backgroundRect);
							Vector3 posParent = averagers[jointParent].Average;

							if(posJoint != Vector3.zero && posParent != Vector3.zero)
							{
								lines[i].gameObject.SetActive(true);
								
								//lines[i].SetVertexCount(2);
								lines[i].SetPosition(0, posParent);
								lines[i].SetPosition(1, posJoint);
							}
							else
							{
								lines[i].gameObject.SetActive(false);
							}
						}
						
					}
					else
					{
						if(joints != null)
						{
							joints[i].SetActive(false);
						}
						
						if(lines[i] != null)
						{
							lines[i].gameObject.SetActive(false);
						}
					}
				}

			}
		}
	}

	// returns body joint position
	public Vector3 GetJointPosition(long userId, int joint)
	{
		KinectManager kinectManager = KinectManager.Instance;

		Vector3 posJoint = Vector3.zero;

		if (foregroundCamera)
		{
			posJoint = kinectManager.GetJointPosColorOverlay(userId, joint, foregroundCamera, backgroundRect);
		}
		/* vestigial Azure Kinect code:
		else if (sensorTransform)
		{
			posJoint = kinectManager.GetJointKinectPosition(userId, joint, true);
		}
		*/
		else
		{
			posJoint = kinectManager.GetJointPosition(userId, joint);
		}

		return posJoint;
	}
}
