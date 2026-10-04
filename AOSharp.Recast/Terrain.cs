using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.DbObjects;
using AOSharp.Core;
using AOSharp.Core.UI;

namespace AOSharp.Recast
{
    public class Terrain
    {
        public static List<Mesh> CreateFromCurrentPlayfield()
        {
            if (Playfield.IsDungeon)
                throw new Exception("Must be outdoors!");

            List<Mesh> chunkMeshes = new List<Mesh>();
            OutdoorRDBTilemap tilemap = Playfield.RDBTilemap as OutdoorRDBTilemap;

            foreach (OutdoorRDBTilemap.Chunk chunk in tilemap.Chunks)
                chunkMeshes.Add(CreateMesh(chunk, tilemap.TileSize, tilemap.HeightmapScale));

            return chunkMeshes;
        }

        public static List<Mesh> CreateFromTilemap(OutdoorRDBTilemap tilemap)
        {
            List<Mesh> chunkMeshes = new List<Mesh>();

            foreach (OutdoorRDBTilemap.Chunk chunk in tilemap.Chunks)
                chunkMeshes.Add(CreateMesh(chunk, tilemap.TileSize, tilemap.HeightmapScale));

            return chunkMeshes;
        }

        private static Mesh CreateMesh(OutdoorRDBTilemap.Chunk chunk, float tileSize, float heightMapScale)
        {
            var index = 0;
            var triIdx = 0;

            var vertices = new Vector3[chunk.Size * chunk.Size];

            for (var y = 0; y < chunk.Size; y++)
            {
                for (var x = 0; x < chunk.Size; x++)
                {
                    vertices[index] = new Vector3(
                        (x) * tileSize,
                        (float)chunk.Heightmap[x, y] * heightMapScale,
                        (y) * tileSize);

                    index++;
                }
            }

            var indices = new List<int>();
            for (int y = 0; y < chunk.Size - 1; y++)
            {
                for (int x = 0; x < chunk.Size - 1; x++)
                {
                    indices.Add((y * chunk.Size) + x + 1);
                    indices.Add((y * chunk.Size) + x);
                    indices.Add(((y + 1) * chunk.Size) + x);

                    indices.Add((y * chunk.Size) + x + 1);
                    indices.Add(((y + 1) * chunk.Size) + x);
                    indices.Add(((y + 1) * chunk.Size) + x + 1);

                    triIdx += 6;
                }
            }

            return new Mesh
            {
                Triangles = indices,
                Vertices = vertices.ToList(),
                Position = new Vector3((float)(chunk.X * (chunk.Size - 1)) * tileSize, 0, (float)(chunk.Y * (chunk.Size - 1)) * tileSize),
                Rotation = Quaternion.Identity,
                Scale = new Vector3(1, 1, 1)
            };
        }
    }

    public class DungeonTerrain
    {
        public static List<Mesh> CreateFromCurrentPlayfield()
        {
            if (!Playfield.IsDungeon)
                throw new Exception("Must be in a dungeon!");

            List<Mesh> roomGroundMeshes = new List<Mesh>();
            DungeonRDBTilemap tilemap = Playfield.RDBTilemap as DungeonRDBTilemap;

            foreach (Room room in Playfield.Rooms)
                roomGroundMeshes.Add(CreateMesh(room, tilemap));

            return roomGroundMeshes;
        }

        private static Mesh CreateMesh(Room room, DungeonRDBTilemap tilemap)
        {
            int numWidthTiles = (int)room.LocalRect.MaxX - (int)room.LocalRect.MinX;
            int numHeightTiles = (int)room.LocalRect.MaxY - (int)room.LocalRect.MinY;
            float width = numWidthTiles * tilemap.TileSize;
            float length = numHeightTiles * tilemap.TileSize;
            int hCount = numWidthTiles + 1;
            int vCount = numHeightTiles + 1;
            int numTriangles = numWidthTiles * numHeightTiles * 6;
            int numVertices = hCount * vCount;

            Vector3[] vertices = new Vector3[numVertices];
            List<int> indices = new List<int>();

            Vector3 anchorOffset = new Vector3(1, 0, 1);
            anchorOffset.X += (room.Center.X - (float)numWidthTiles / 2) * tilemap.TileSize;
            anchorOffset.Z += (room.Center.Z - (float)numHeightTiles / 2) * tilemap.TileSize;

            int idx = 0;
            for (int z = 0; z < vCount; z++)
            {
                for (int x = 0; x < hCount; x++)
                {
                    byte height = tilemap.Heightmap[x + (int)room.LocalRect.MinX - 1, z + (int)room.LocalRect.MinY - 1];

                    Vector3 vertex = new Vector3
                    {
                        X = x * tilemap.TileSize - width / 2f,
                        Y = (float)height * tilemap.HeightmapScale,
                        Z = z * tilemap.TileSize - length / 2f
                    };
                    vertex.X -= anchorOffset.X;
                    vertex.Z -= anchorOffset.Z;
                    vertices[idx] = vertex;
                    idx++;
                }
            }

            idx = 0;
            for (int z = 0; z < numHeightTiles; z++)
            {
                for (int x = 0; x < numWidthTiles; x++)
                {
                    byte tileCollisionData = tilemap.CollisionData[x + (int)room.LocalRect.MinX, z + (int)room.LocalRect.MinY];

                    if (tileCollisionData > 0 && tileCollisionData != 0x80)
                    {
                        indices.Add((z * hCount) + x);
                        indices.Add(((z + 1) * hCount) + x);
                        indices.Add((z * hCount) + x + 1);

                        indices.Add(((z + 1) * hCount) + x);
                        indices.Add(((z + 1) * hCount) + x + 1);
                        indices.Add((z * hCount) + x + 1);
                    }
                    idx += 6;
                }
            }


            List<Vector3> optimizedVerts = vertices.ToList();
            List<int> optimizedIndices = indices;

            int testVertex = 0;

            while (testVertex < optimizedVerts.Count)
            {
                if (optimizedIndices.Contains(testVertex))
                {
                    testVertex++;
                }
                else
                {
                    optimizedVerts.RemoveAt(testVertex);

                    for (int i = 0; i < optimizedIndices.Count; i++)
                    {
                        if (optimizedIndices[i] > testVertex)
                            optimizedIndices[i]--;
                    }
                }
            }

            return new Mesh
            {
                Triangles = optimizedIndices,
                Vertices = optimizedVerts,
                Position = room.Position - new Vector3(0, room.YOffset, 0),
                Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, room.Rotation * (Math.PI / 180)),
                Scale = new Vector3(1, 1, 1)
            };
        }
    }
}