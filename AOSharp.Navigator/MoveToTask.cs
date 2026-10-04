using AOSharp.Common.GameData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Navigator
{
    public class MoveToTask : NavigatorTask
    {
        public readonly Vector3 Destination;

        internal MoveToTask(PlayfieldId id, Vector3 dst) : base(id)
        {
            Destination = dst;
        }
    }
}
