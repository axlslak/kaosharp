using org.critterai.nav;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.critterai;
using AOSharp.Core.UI;
using AOSharp.Core;
using AOVector3 = AOSharp.Common.GameData.Vector3;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace AOSharp.Recast
{
    public static class Extensions
    {
        public static AOVector3 ToAOVector3(this Vector3 vec)
        {
            return new AOVector3(vec.x, vec.y, vec.z);
        }

        public static Vector3 ToCAIVector3(this AOVector3 vec)
        {
            return new Vector3(vec.X, vec.Y, vec.Z);
        }

        public static void Draw(this Navmesh mesh, float maxDrawDist)
        {
            int count = mesh.GetMaxTiles();

            for (int i = 0; i < count; i++)
                Draw(mesh.GetTile(i), maxDrawDist);
        }

        private static void Draw(NavmeshTile tile, float maxDrawDist)
        {
            NavmeshTileHeader header = tile.GetHeader();

            // Keep this check.  Less trouble for clients.
            if (header.polyCount < 1)
                return;

            uint polyBase = tile.GetBasePolyRef();

            NavmeshPoly[] polys = new NavmeshPoly[header.polyCount];
            tile.GetPolys(polys);

            Vector3[] verts = new Vector3[header.vertCount];
            tile.GetVerts(verts);

            NavmeshDetailMesh[] meshes =
                new NavmeshDetailMesh[header.detailMeshCount];
            tile.GetDetailMeshes(meshes);

            byte[] detailTris = new byte[header.detailTriCount * 4];
            tile.GetDetailTris(detailTris);

            Vector3[] detailVerts = new Vector3[header.detailVertCount];
            tile.GetDetailVerts(detailVerts);

            for (int i = 0; i < header.polyCount; i++)
            {
                NavmeshPoly poly = polys[i];

                if (poly.Type == NavmeshPolyType.OffMeshConnection)
                    continue;

                NavmeshDetailMesh mesh = meshes[i];

                for (int j = 0; j < mesh.triCount; j++)
                {
                    AOVector3[] desu = new AOVector3[3];
                    int pTri = (int)(mesh.triBase + j) * 4;

                    for (int k = 0; k < 3; k++)
                    {
                        // Note: iVert and pVert refer to different
                        // arrays.
                        int iVert = detailTris[pTri + k];
                        if (iVert < poly.vertCount)
                        {
                            // Get the vertex from the main vertices.
                            int pVert = poly.indices[iVert];
                            desu[k] = verts[pVert].ToAOVector3();
                        }
                        else
                        {
                            // Get the vertex from the detail vertices.
                            int pVert = (int)
                                (mesh.vertBase + iVert - poly.vertCount);
                            desu[k] = detailVerts[pVert].ToAOVector3();
                        }
                    }

                    if (desu.Any(x => AOVector3.Distance(x, DynelManager.LocalPlayer.Position) > maxDrawDist))
                        continue;
                        
                    DrawTriangle(desu[0], desu[1], desu[2], DebuggingColor.Green);
                }
            }
        }

        public static void Save(this Navmesh navmesh, string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            BinaryFormatter formatter = new BinaryFormatter();

            using (var inflateStream = new FileStream(filePath, FileMode.CreateNew))
                formatter.Serialize(inflateStream, navmesh.GetSerializedMesh());
        }

        private static void DrawTriangle(AOVector3 vert1, AOVector3 vert2, AOVector3 vert3, AOVector3 color)
        {
            Debug.DrawLine(vert1, vert2, color);
            Debug.DrawLine(vert2, vert3, color);
            Debug.DrawLine(vert3, vert1, color);
        }
    }
}
