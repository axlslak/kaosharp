using AOSharp.Common.GameData;
using AOSharp.Core;
using System;

namespace NavmeshMovementController
{
    public class PointNotOnNavMeshException : Exception
    {
        public PointNotOnNavMeshException(Vector3 pos) : base ($"Unable to find NavMeshPoint for ({pos})")
        {
        }
    }
    public class StartPositionNotOnNavMeshException : Exception
    {
        public StartPositionNotOnNavMeshException(Vector3 pos) : base($"Unable to find NavMeshPoint for Start Position ({pos})")
        {
        }
    }

    public class DestinationNotOnNavMeshException : Exception
    {
        public DestinationNotOnNavMeshException(Vector3 pos) : base($"Unable to find NavMeshPoint for Destination ({pos})")
        {
        }
    }
}
