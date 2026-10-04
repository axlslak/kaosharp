using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using AOSharp.Common.GameData;
using org.critterai.nav;
using oVector3 = org.critterai.Vector3;
using AOSharp.Core;
using AOSharp.Core.UI;
using NavmeshMovementController;

namespace AOSharp.Pathfinding
{
    public class Pathfinder
    {
        private const float WalkableStep = 0.3f;
        private const float WalkableHeight = 3f;
        private const float WalkableRadius = 0.5f;
        private const float WalkableSlope = 75f;
        private const float XzCellSize = 0.1f;
        private const float YCellSize = 0.07f;
        private const float MinRegionArea = 500f;

        private Navmesh _navMesh;
        private NavmeshQuery _query;
        private NavmeshQueryFilter _filter;
        private PathCorridor _pathCorridor;

        
        public Pathfinder(Navmesh navMesh)
        {
            _navMesh = navMesh;
            _filter = new NavmeshQueryFilter();

            if (NavUtil.Failed(NavmeshQuery.Create(_navMesh, 1000, out _query)))
                throw new Exception("NavQuery failed");

            _pathCorridor = new PathCorridor(1000, 1000, _query, _filter);
        }


        public List<Vector3> GeneratePath(Vector3 start, Vector3 end)
        {
            List<Vector3> finalPath = new List<Vector3>();

            if (NavUtil.Failed(GetNavMeshPoint(start, new oVector3(0.5f, 2, 0.5f), out NavmeshPoint origin)) || origin.point == new oVector3())
                throw new StartPositionNotOnNavMeshException(start);

            if (NavUtil.Failed(GetNavMeshPoint(end, new oVector3(0.5f, 2, 0.5f), out NavmeshPoint destination)) || destination.point == new oVector3())
                throw new DestinationNotOnNavMeshException(end);

            uint[] path = new uint[500];
            int pathCount;

            if (origin.polyRef == destination.polyRef)
            {
                path[0] = origin.polyRef;
                pathCount = 1;
            }
            else
            {
                NavStatus status = _query.FindPath(origin, destination, _filter, path, out pathCount);

                if (NavUtil.Failed(status) || pathCount == 0)
                {
                    Chat.WriteLine("FindPath failed?");
                    throw new Exception("FindPath failed: " + status);
                }
                else if (destination.polyRef != path[pathCount - 1])
                {
                    //Chat.WriteLine("Unable to generate full path? " + status);
                    //throw new Exception("Unable to generate full path: " + status);
                }
            }

            oVector3[] straightPath = StraightenPath(start.ToCAIVector3(), end.ToCAIVector3(), path, pathCount);

            finalPath.AddRange(straightPath.Select(node => new Vector3(node.x, node.y, node.z)));

            return finalPath;
        }

        public PathCorridor GeneratePathCorridor(Vector3 start, Vector3 end)
        {
            if (NavUtil.Failed(GetNavMeshPoint(start, new oVector3(0.3f, 2, 0.3f), out NavmeshPoint origin)) || origin.point == new oVector3())
                return null;

            if (NavUtil.Failed(GetNavMeshPoint(end, new oVector3(0.3f, 2, 0.3f), out NavmeshPoint destination)) || destination.point == new oVector3())
                return null;

            uint[] path = new uint[250];
            int pathCount;

            if (origin.polyRef == destination.polyRef)
            {
                pathCount = 1;
                path[0] = origin.polyRef;
            }
            else
            {
                NavStatus status = _query.FindPath(origin, destination, _filter, path, out pathCount);

                if (NavUtil.Failed(status) || pathCount == 0)
                {
                    // Handle pathfinding failure.
                    throw new Exception("path failed: " + status);
                }
                else if (destination.polyRef != path[pathCount - 1])
                {
                    //Chat.WriteLine("Unable to generate full path: " + status);
                    //throw new Exception("Unable to generate full path: " + status);
                    //return null;
                }
            }

            _pathCorridor.SetCorridor(end.ToCAIVector3(), path, pathCount);
            _pathCorridor.Move(start.ToCAIVector3(), end.ToCAIVector3());

            return _pathCorridor;
        }

        /*
        public float GetPathDistance(Vector3 start, Vector3 end)
        {
            List<Vector3> path = GeneratePath(start, end).;
            float distance = 0;

            for (int i = 0; i < path.Count - 1; i++)
            {
                distance += path[i].DistanceFrom(path[i + 1]);
            }

            return distance;
        }
        */

        private oVector3[] StraightenPath(oVector3 start, oVector3 end, uint[] path, int pathCount)
        {
            oVector3[] straightPath = new oVector3[200];
            int count = 0;
            if (NavUtil.Failed(_query.GetStraightPath(start, end, path, 0, pathCount, straightPath, null, null, out count)))
                throw new Exception("Failed to straighten path.");
            return straightPath.Take(count).ToArray();
        }

        public NavStatus GetNavMeshPoint(Vector3 position, oVector3 extents, out NavmeshPoint point)
        {
            return _query.GetNearestPoint(position.ToCAIVector3(), extents, _filter, out point);
        }

        public static Pathfinder Create(string filePath)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = null;

            if (!File.Exists(filePath))
                return null;

            try
            {
                stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                NavStatus status = Navmesh.Create((byte[])formatter.Deserialize(stream), out Navmesh navMesh);
                if (status == NavStatus.Sucess)
                    return new Pathfinder(navMesh);
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            return null;
        }
    }
}