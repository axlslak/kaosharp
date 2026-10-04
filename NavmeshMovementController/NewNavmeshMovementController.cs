using AOSharp.Core;
using AOSharp.Core.Movement;
using AOSharp.Common.GameData;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using Path = AOSharp.Core.Movement.Path;
using oVector3 = org.critterai.Vector3;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using org.critterai.nav;
using AOSharp.Core.UI;
using NavmeshMovementController;
using System.Runtime.Serialization.Formatters.Binary;

namespace AOSharp.Pathfinding
{
    public class NewNavmeshMovementController : MovementController
    {
        private const float PathUpdateInterval = 1f;

        public NewPathfinder Pathfinder = null;
        protected readonly string _navMeshFolderPath;
        private double _nextPathUpdate = 0;

        public delegate Navmesh NavmeshResolveDelegate(NewNavmeshMovementController movementController);
        public event NavmeshResolveDelegate NavmeshResolve;

        public NewNavmeshMovementController(bool drawPath = false) : base(drawPath)
        {
        }

        public override void Update()
        {
            if (IsNavigating && _paths.Peek() is NewNavMeshPath path && path.Initialized && Time.NormalTime > _nextPathUpdate)
            {
                path.UpdatePath();
                _nextPathUpdate = Time.NormalTime + PathUpdateInterval;
            }

            base.Update();
        }

        public void Test()
        {
            if (NavUtil.Failed(Pathfinder.GetNavMeshPoint(new Vector3(149.2292, 0.4225993, 206.8754), new oVector3(0.3f, 2, 0.3f), out NavmeshPoint origin)) || origin.point == new oVector3())
                Chat.WriteLine("Failed to find origin");
            else
                Chat.WriteLine($"Found origin at {origin.point}");
        }

        public bool GetDistance(Vector3 destination, out float distance)
        {
            distance = 0;

            try
            {
                List<Vector3> path = Pathfinder.GeneratePath(DynelManager.LocalPlayer.Position, destination);

                for(int i = 0; i < path.Count - 1; i++)
                    distance += Vector3.Distance(path[i], path[i+1]);

                return true;
            }
            catch (PointNotOnNavMeshException e)
            {
                return false;
            }
        }

        public void SetNavMeshDestination(Vector3 destination)
        {
            SetNavMeshDestination(destination, out _);
        }

        public void AppendNavMeshDestination(Vector3 destination)
        {
            AppendNavMeshDestination(destination, out _);
        }

        public bool SetNavMeshDestination(Vector3 destination, out NewNavMeshPath path)
        {
            _paths.Clear();
            return AppendNavMeshDestination(destination, out path);
        }

        public bool AppendNavMeshDestination(Vector3 destination, out NewNavMeshPath path)
        {
            path = new NewNavMeshPath(Pathfinder, destination);

            if (Pathfinder == null)
                return false;

            if (IsNavigating && _paths.Peek() is NewNavMeshPath navPath && navPath.Destination == destination)
                return false;

            base.AppendPath(path);
            return true;
        }

        public void SetNavmesh(Navmesh navmesh)
        {
            if (navmesh == null)
                return;

            Pathfinder = new NewPathfinder(navmesh);
        }

        public bool LoadNavmesh(string filePath)
        {
            if(LoadNavmesh(filePath, out Navmesh navmesh))
            {
                Pathfinder = new NewPathfinder(navmesh);
                return true;
            }

            return false;
        }

        public bool LoadNavmesh(string filePath, out Navmesh navmesh)
        {
            bool succeeded = false;
            navmesh = null;
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            FileStream fileStream = null;

            if (!File.Exists(filePath))
                return false;

            try
            {
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                succeeded = Navmesh.Create((byte[])binaryFormatter.Deserialize(fileStream), out navmesh) == NavStatus.Sucess;
            }
            finally
            {
                fileStream?.Close();
            }

            return succeeded && navmesh != null;
        }
    }

    public class NewNavMeshPath : Path
    {
        private NewPathfinder _pathfinder;
        public readonly Vector3 Destination;

        public NewNavMeshPath(NewPathfinder pathfinder, Vector3 dstPos) : base(new List<Vector3>())
        {
            _pathfinder = pathfinder;
            Destination = dstPos;
        }

        public override void OnPathStart()
        {
            UpdatePath();

            base.OnPathStart();
        }

        public override void OnPathFinished()
        {
            base.OnPathFinished();
        }

        internal void UpdatePath()
        {
            try
            {
                base.SetWaypoints(_pathfinder.GeneratePath(DynelManager.LocalPlayer.Position, Destination));
            }
            catch(PointNotOnNavMeshException e)
            {
                Chat.WriteLine(e.Message);
                base.SetWaypoints(new List<Vector3>());
            }
        }
    }
}
