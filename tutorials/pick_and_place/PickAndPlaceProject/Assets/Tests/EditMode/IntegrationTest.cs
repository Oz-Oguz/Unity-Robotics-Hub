using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RosSharp;
using RosSharp.Control;
using RosSharp.Urdf.Editor;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.Robotics.PickAndPlace.Tests
{
    [TestFixture]
    // IMPORTANT: In order for this category of tests to run correctly, MessageGeneration must be run first and the
    //            INTEGRATION_TEST script define must be set
    [Category("Integration")]
    public class RosIntegrationTests
    {
        #region Parameters
        
        // Testing parameters
        const float k_TestTimeoutSeconds = 20f;
        
        const string k_NamePackageNiryoMoveIt = "niryo_moveit";
        const string k_NameNiryoOne = "niryo_one";
        const string k_NameBaseLink = "base_link";
       
        // Prefabs that get instantiated into scene
        const string k_NameTable = "Table";
        const string k_NameTarget = "Target";
        const string k_NameTargetPlacement = "TargetPlacement";
        
        // GameObjects that hold important components
        const string k_NameCamera = "Main Camera";
        const string k_NameRosConnect = "ROSConnect";
        const string k_NamePublisher = "Publisher";

        // Parameters for robot joint controller
        const float k_ControllerStiffness = 10000f;
        const float k_ControllerDamping = 100f;
        const float k_ControllerForceLimit = 1000f;
        const float k_ControllerSpeed = 30f;
        const float k_ControllerAcceleration = 10f;

        // Parameters for ROS connection
        const string k_IpAddressLoopback = "127.0.0.1";
        const int k_HostPort = 10000;
        const int k_UnityPort = 5005;
        const int k_NumAwaitDataRetries = 10;
        const int k_NumAwaitDataSleepSeconds = 1;

        const string k_PrefabSuffix = ".prefab";
        readonly string k_DirectoryPrefabs = Path.Combine("Assets", "Prefabs");
        readonly string k_PathUrdf = Path.Combine("URDF", "niryo_one", "niryo_one.urdf");
        readonly string k_PathTestScene = Path.Combine("Assets","Scenes","EmptyScene.unity");
        
        readonly Vector3 k_CameraPosition = new Vector3(0, 1.4f, -0.7f);
        readonly Quaternion k_CameraRotation = Quaternion.Euler(new Vector3(45, 0, 0));
        
        readonly ImportSettings k_UrdfImportSettings = new ImportSettings
        {
            choosenAxis = ImportSettings.axisType.yAxis,
            convexMethod = ImportSettings.convexDecomposer.unity
        };
        #endregion
        
        float m_TimeElapsedSeconds;
        [SerializeField, HideInInspector]
        ROSConnection m_RosConnection;
        [SerializeField, HideInInspector]
        TargetPlacement m_TargetPlacement;

        bool DidPlacementSucceed => m_TargetPlacement.CurrentState == TargetPlacement.PlacementState.InsidePlaced;


#if INTEGRATION_TEST
        [UnityTest]
        public IEnumerator TrajectoryPublisher_PickAndPlaceDemo_CompletesTask()
        {
            SetUpScene();
            // TODO: This test could be made a PlayMode test once ImportRobot can use the PlayMode URDF import
            ImportRobot();
            CreateRosConnection();
            CreateTrajectoryPlannerPublisher();
            yield return new EnterPlayMode();

            m_TargetPlacement = GameObject.Find(k_NameTargetPlacement).GetComponent<TargetPlacement>();
            Assert.IsNotNull(m_TargetPlacement, $"Unable to find {nameof(TargetPlacement)} attached to a " +
                $"GameObject called {k_NameTargetPlacement} in scene.");

            var publisher = GameObject.Find(k_NamePublisher).GetComponent<TrajectoryPlanner>();
            publisher.PublishJoints();

            while(!DidPlacementSucceed && m_TimeElapsedSeconds < k_TestTimeoutSeconds)
            {
                m_TimeElapsedSeconds += Time.deltaTime;
                yield return null;
            }
            
            Assert.IsTrue(DidPlacementSucceed, "Pick and Place did not complete before test timed out.");
            // TODO: Wait a reasonable amount of time and check if the cube is in the expected location
            
            yield return new ExitPlayMode();
        }

        void SetUpScene()
        {
            EditorSceneManager.OpenScene(k_PathTestScene);
            
            InstantiatePrefabFromName(k_NameTable);
            InstantiatePrefabFromName(k_NameTarget);
            InstantiatePrefabFromName(k_NameTargetPlacement);

            var camera = GameObject.Find(k_NameCamera);
            camera.transform.position = k_CameraPosition;
            camera.transform.rotation = k_CameraRotation;
        }

        void CreateTrajectoryPlannerPublisher()
        {
            var planner = new GameObject(k_NamePublisher).AddComponent<TrajectoryPlanner>();
            planner.rosServiceName = k_NamePackageNiryoMoveIt;
            planner.niryoOne = GameObject.Find(k_NameNiryoOne);
            planner.target = GameObject.Find(k_NameTarget);
            planner.targetPlacement = GameObject.Find(k_NameTargetPlacement);
        }

        GameObject InstantiatePrefabFromName(string name)
        {
            var filepath = Path.Combine(k_DirectoryPrefabs, $"{name}{k_PrefabSuffix}");
            var gameObject = (GameObject) PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(filepath));
            gameObject.name = name;
            return gameObject;
        }

        void CreateRosConnection()
        {
            m_RosConnection = new GameObject(k_NameRosConnect).AddComponent<ROSConnection>();
            m_RosConnection.rosIPAddress = k_IpAddressLoopback;
            m_RosConnection.rosPort = k_HostPort;
            m_RosConnection.overrideUnityIP = k_IpAddressLoopback;
            m_RosConnection.unityPort = k_UnityPort;
            m_RosConnection.awaitDataMaxRetries = k_NumAwaitDataRetries;
            m_RosConnection.awaitDataSleepSeconds = k_NumAwaitDataSleepSeconds;
        }

        void ImportRobot()
        {
            var urdfFullPath = Path.Combine(Application.dataPath, k_PathUrdf);
            var robotImporter = UrdfRobotExtensions.Create(urdfFullPath, k_UrdfImportSettings, false);
            // Create is a coroutine that would usually run only in EditMode, so we need to force its execution here
            while (robotImporter.MoveNext())
            {
            }

            // Ensure parameters are set to reasonable values
            var controller = GameObject.Find(k_NameNiryoOne).GetComponent<Controller>();
            controller.stiffness = k_ControllerStiffness;
            controller.damping = k_ControllerDamping;
            controller.forceLimit = k_ControllerForceLimit;
            controller.speed = k_ControllerSpeed;
            controller.acceleration = k_ControllerAcceleration;
            GameObject.Find(k_NameBaseLink).GetComponent<ArticulationBody>().immovable = true;
        }
#endif
    }
}
