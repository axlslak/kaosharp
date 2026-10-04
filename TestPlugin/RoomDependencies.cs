using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;

namespace TestPlugin
{
    public enum TemplatePlayfields
    {
        MidTech = 320,
        HiTech = 321,
        Cave = 322,
        ClanStandard = 324,
    }

    public class PlayfieldACGInfo
    {
        public int Revision { get; set; }
        public short Version { get; set; }
        public short Width { get; set; }
        public short Height { get; set; }
        public short Unknown3 { get; set; }
        public TemplatePlayfields TemplateId { get; set; }
        public byte AmbientR { get; set; }
        public byte AmbientG { get; set; }
        public byte AmbientB { get; set; }


        public List<RoomInstance> Rooms { get; set; }
    }

    public class RoomInstance : RoomTemplate
    {
        //[JsonIgnore]
        //private Room _roomTemplate;
        //[JsonIgnore]
        //public Room RoomTemplate
        //{
        //    get
        //    {
        //        if (_roomTemplate != null)
        //            return _roomTemplate;

        //        var pfe = PlayfieldExtract.Cache[PlayfieldTemplate];

        //        var template = pfe.Rooms[RoomId];

        //        return _roomTemplate = template;
        //    }
        //}

        public short PlayfieldTemplate { get; set; }
        public short RoomId { get; set; }
        public byte Floor { get; set; }


        public RoomInstance()
        {
        }

        public RoomInstance(short playfieldTemplate, short roomId, byte floor, byte x, byte y, byte rotation) : base()
        {
            PlayfieldTemplate = playfieldTemplate;
            RoomId = roomId;
            Floor = floor;
            X = x;
            Y = y;
            Rotation = rotation;
        }
    }

    public class RoomTemplate 
    {
        // Do not change this, has to match with the client..
        public const float TileSize = 2.0f;

        public int X { get; set; }
        public int Y { get; set; }

        // This rotation either comes from RoomInstance or the RDB depending on if this is a room instance
        // Possibly this is not right what if a Template room has rotation too.. 
        public int Rotation { get; set; }
    }
}
