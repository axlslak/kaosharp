namespace AOSharp.Pathfinding
{
    public static class Vector3Extensions
    {
        public static org.critterai.Vector3 ToCAIVector3(this AOSharp.Common.GameData.Vector3 v)
        {
            return new org.critterai.Vector3(v.X, v.Y, v.Z);
        }
    }
}
