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

namespace AOSharp.Pathfinding
{
    public class NavMeshMovementController : MovementController
    {
        public bool LocalPlayerOnNavmesh => IsPosOnNavmesh(DynelManager.LocalPlayer.Position);

        public bool IsPosOnNavmesh(Vector3 pos)
        {
            return !NavUtil.Failed(_pathfinder.GetNavMeshPoint(pos, new oVector3(0.5f, 2, 0.5f), out NavmeshPoint _));
        }

        private const float PathUpdateInterval = 1f;

        private Pathfinder _pathfinder = null;
        protected readonly string _navMeshFolderPath;
        private double _nextPathUpdate = 0;

        public NavMeshMovementController(string navMeshFolderPath, bool drawPath = false) : base(drawPath)
        {
            _navMeshFolderPath = navMeshFolderPath;
            LoadPather();

            Game.PlayfieldInit += OnPlayfieldInit;
        }

        public override void Update()
        {
            if (IsNavigating && _paths.Peek() is NavMeshPath path && path.Initialized && Time.NormalTime > _nextPathUpdate)
            {
                path.UpdatePath();
                _nextPathUpdate = Time.NormalTime + PathUpdateInterval;
            }

            base.Update();
        }

        public void Test()
        {
            if (NavUtil.Failed(_pathfinder.GetNavMeshPoint(new Vector3(149.2292, 0.4225993, 206.8754), new oVector3(0.3f, 2, 0.3f), out NavmeshPoint origin)) || origin.point == new oVector3())
                Chat.WriteLine("Failed to find origin");
            else
                Chat.WriteLine($"Found origin at {origin.point}");
        }

        public void SetNavMeshDestination(Vector3 destination)
        {
            SetNavMeshDestination(destination, out _);
        }

        public void AppendNavMeshDestination(Vector3 destination)
        {
            AppendNavMeshDestination(destination, out _);
        }

        public bool SetNavMeshDestination(Vector3 destination, out NavMeshPath path)
        {
            _paths.Clear();
            return AppendNavMeshDestination(destination, out path);
        }

        public bool AppendNavMeshDestination(Vector3 destination, out NavMeshPath path)
        {
            path = new NavMeshPath(_pathfinder, destination);

            if (_pathfinder == null)
                return false;

            if (IsNavigating && _paths.Peek() is NavMeshPath navPath && navPath.Destination == destination)
                return false;

            base.AppendPath(path);
            return true;
        }

        private void OnPlayfieldInit(object s, uint id)
        {
            LoadPather();
        }

        protected virtual bool LoadPather()
        {
            string navFile = $"{_navMeshFolderPath}\\{Playfield.ModelIdentity.Instance}.navmesh";

            if (!File.Exists(navFile))
                return false;

            try
            {
                _pathfinder = Pathfinder.Create(navFile);
            } 
            catch(Exception e)
            {
                Chat.WriteLine(e.Message);
            }

            return true;
        }
    }

    public class NavMeshPath : Path
    {
        private Pathfinder _pathfinder;
        private PathCorridor _pathCorridor;
        public readonly Vector3 Destination;

        public NavMeshPath(Pathfinder pathfinder, Vector3 dstPos) : base(new List<Vector3>())
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
            catch (StartPositionNotOnNavMeshException e)
            {
                Chat.WriteLine(e.Message);
            }
            catch (DestinationNotOnNavMeshException e)
            {
                Chat.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
                base.SetWaypoints(new List<Vector3>());
            }
        }
    }
}
