using AOSharp.Common.GameData;
using System.Collections.Generic;
using System.Linq;

namespace AOSharp.Navigator
{
    public class PlayfieldNode
    {
        public List<PlayfieldLink> Links;

        internal bool TryGetLink(PlayfieldId dstPlayfieldId, out PlayfieldLink link)
        {
            return (link = Links.FirstOrDefault(x => x.DstId == dstPlayfieldId)) != null;
        }
    }
}
