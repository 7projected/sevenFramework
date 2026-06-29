using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace sevenFramework
{
    internal record Tile(List<Polygon> polygons, Texture2D texture, bool cullable, string collisionLayer);
    internal record Chunk(Rectangle boundingBox, Dictionary<String, List<Polygon>> collisionLayers);

    internal class CollisionMap
    {
        public Dictionary<String, List<Polygon>> collisionLayers;
        public List<Chunk> chunkList;
        public int polygonCount;

        public CollisionMap()
        {
            collisionLayers = new();

            polygonCount = 0;
            collisionLayers.Add("collide", new());
        }

        public List<Chunk> GetIntersectingChunks(Rectangle boundingBox)
        {
            List<Chunk> ret = new();

            foreach (Chunk chunk in chunkList)
            {
                if (chunk.boundingBox.Intersects(boundingBox))
                {
                    ret.Add(chunk);
                }
            }

            return ret;
        }

        public void BakePolygonCount()
        {
            polygonCount = 0;

            foreach (KeyValuePair<String, List<Polygon>> kvp in collisionLayers)
            {
                polygonCount += kvp.Value.Count;
            }
        }

        public void LoadChunks(int mapSizeX, int mapSizeY, int chunkWidth, int chunkHeight, int tileWidth, int tileHeight)
        {
            // Clear any existing chunks
            chunkList = new List<Chunk>();

            int xChunks = (int)Math.Ceiling((double)mapSizeX / chunkWidth);
            int yChunks = (int)Math.Ceiling((double)mapSizeY / chunkHeight);

            int chunkPixelWidth = (chunkWidth * tileWidth);
            int chunkPixelHeight = (chunkHeight * tileHeight);

            // Create chunks
            for (int xChunk = 0; xChunk < xChunks; xChunk++)
            {
                for (int yChunk = 0; yChunk < yChunks; yChunk++)
                {
                    Rectangle boundingBox = new(new Point(xChunk * chunkPixelWidth, yChunk * chunkPixelHeight), new Point(chunkWidth * tileWidth, chunkHeight * tileHeight));
                    chunkList.Add(new(boundingBox, new()));
                }
            }

            foreach (Chunk chunk in chunkList)
            {
                Rectangle AABB = chunk.boundingBox;

                foreach (KeyValuePair<String, List<Polygon>> kvp in collisionLayers)
                {
                    foreach (Polygon poly in kvp.Value)
                    {
                        if (poly.GetBoundingRectangle().Intersects(AABB))
                        {
                            if (!chunk.collisionLayers.ContainsKey(kvp.Key)) chunk.collisionLayers.Add(kvp.Key, new());
                            chunk.collisionLayers[kvp.Key].Add(poly);
                        }
                    }
                }
            }

            BakePolygonCount();
        }

        public void AddLayer(String name, List<Polygon> polygonList)
        {
            if (collisionLayers.ContainsKey(name))
            {
                collisionLayers[name] = polygonList;
                return;
            }
            collisionLayers.Add(name, polygonList);
            BakePolygonCount();
        }

        public bool RemoveLayer(String name)
        {
            if (collisionLayers.ContainsKey(name))
            {
                collisionLayers.Remove(name);
                BakePolygonCount();

                return true;
            }
            return false;
        }

        public bool AddPolygonToLayer(String name, Polygon polygon)
        {
            if (collisionLayers.ContainsKey(name))
            {
                collisionLayers[name].Add(polygon);
                BakePolygonCount();

                return true;
            }
            return false;
        }

        public bool RemovePolygonFromLayer(String name, Polygon polygon)
        {
            if (collisionLayers.ContainsKey(name))
            {
                if (collisionLayers[name].Contains(polygon))
                {
                    collisionLayers[name].Remove(polygon);
                    BakePolygonCount();

                    return true;
                }
                return false;
            }
            return false;
        }

        public void SetLayer(String name, List<Polygon> polygons)
        {
            if (collisionLayers.ContainsKey(name))
            {
                collisionLayers[name].Clear();
                foreach (Polygon poly in polygons)
                {
                    collisionLayers[name].Add(poly);
                }
            }
            BakePolygonCount();
        }

        public List<Polygon> GetLayer(String name)
        {
            if (collisionLayers.ContainsKey(name))
            {
                return collisionLayers[name];
            }
            return new();
        }
    }

    internal class TileSet
    {
        public Polygon topLeftPolygon = new(new(0, 0), new(1, 0), new(0, 1), new(new(0, 0), new(1, 1), new(0, 0)));
        public Polygon topRightPolygon = new(new(0, 0), new(1, 0), new(1, 1), new(new(0, 0), new(1, 1), new(0, 0)));
        public Polygon bottomLeftPolygon = new(new(0, 0), new(0, 1), new(1, 1), new(new(0, 0), new(1, 1), new(0, 0)));
        public Polygon bottomRightPolygon = new(new(1, 1), new(1, 0), new(0, 1), new(new(0, 0), new(1, 1), new(0, 0)));

        public Dictionary<int, Tile> dict;

        public TileSet(Dictionary<int, Tile>? tileSet = null)
        {
            if (tileSet == null) dict = new();
            else dict = tileSet;
        }

        public void AddTile(int index, List<Polygon> polygons, Texture2D texture, bool cullable, string collisionLayer = "collide")
        {
            dict.Add(index, new(polygons, texture, cullable, collisionLayer));
        }

        public Tile GetTile(int index)
        {
            if (dict.ContainsKey(index))
            {
                return dict[index];
            }

            return dict[0];
        }
    }

    internal class TileMap
    {
        SceneManager sm;
        public TileSet tileset;
        public RenderTarget2D renderTarget;
        public bool baked;

        public Vector2 position;
        public Vector2 tileScale;
        public Color color;
        public Color backgroundColor;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int TileWidth { get; private set; }
        public int TileHeight { get; private set; }
        public List<TiledLayer> Layers { get; private set; }

        public CollisionMap collisionMap;

        public int chunkWidth;
        public int chunkHeight;

        public TileMap(SceneManager sm, int chunkWidth, int chunkHeight, string path, TileSet tileset, Vector2 position, Vector2 tileScale, Color color, Color backgroundColor)
        {
            this.sm = sm;
            string json = File.ReadAllText(path);
            this.baked = false;

            this.position = position;
            this.tileScale = tileScale;
            this.color = color;
            this.backgroundColor = backgroundColor;

            this.chunkWidth = chunkWidth;
            this.chunkHeight = chunkHeight;
            this.collisionMap = new();

            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true
            };

            TiledMapData mapData = JsonSerializer.Deserialize<TiledMapData>(json, options);
            Width = mapData.width;
            Height = mapData.height;
            TileWidth = (int)(mapData.tilewidth * tileScale.X);
            TileHeight = (int)(mapData.tileheight * tileScale.Y);
            Layers = mapData.layers;

            SetTileset(tileset);
            renderTarget = new(sm.graphicsDevice, Width * TileWidth, Height * TileHeight);

            LoadCollisionMap();
        }

        public void SetTileset(TileSet tileset)
        {
            this.tileset = tileset;
        }

        public int GetTile(int x, int y, int layerIndex = 0)
        {
            TiledLayer layer = Layers[layerIndex];

            return layer.data[y * layer.width + x];
        }

        public Vector2i FindTile(Tile tile, int index)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int ti = GetTile(x, y);
                    Tile t = tileset.GetTile(ti);

                    if (t == tile)
                    {
                        return new(x, y);
                    }
                }
            }
            return new(0, 0);
        }

        public Vector2i FindTileByIndex(int tile, int index)
        {
            for (int l = 0; l < Layers.Count; l++)
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        int ti = GetTile(x, y, l);

                        if (ti == tile)
                        {
                            return new(x, y);
                        }
                    }
                }
            }
            return new(0, 0);
        }



        public void BakeIfNeeded(SpriteBatch sb)
        {
            if (!baked) BakeAllLayers(sb);
            else return;
        }

        public void BakeAllLayers(SpriteBatch sb)
        {
            sm.graphicsDevice.SetRenderTarget(renderTarget);
            sb.GraphicsDevice.Clear(backgroundColor);
            sb.Begin(samplerState: SamplerState.PointClamp);

            for (int i = 0; i < Layers.Count; i++)
            {
                BakeRenderTarget(sb, i);
                Debug.Print($"Baking @{i} {Layers[i].name}");
            }

            baked = true;
            sb.End();
        }

        public void BakeRenderTarget(SpriteBatch sb, int layerIndex = 0)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int tile = GetTile(x, y, layerIndex);

                    if (tileset.dict.ContainsKey(tile))
                    {
                        if (tileset.dict[tile].texture != null)
                            sb.Draw(tileset.dict[tile].texture, new Rectangle((int)x * TileWidth, (int)y * TileHeight, TileWidth, TileHeight), Color.White);
                    }
                }
            }
        }


        public void LoadCollisionMap()
        {
            // Load layers into collisionmap
            collisionMap = new();

            for (int l = 0; l < Layers.Count; l++)
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        int tileInt = GetTile(x, y, l);
                        if (tileset.dict.ContainsKey(tileInt))
                        {
                            Tile tile = tileset.dict[tileInt];
                            collisionMap.AddLayer(tile.collisionLayer, new());
                        }
                    }
                }
            }
            // CHunks are now manages in collisionmap

            for (int l = 0; l < Layers.Count; l++)
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        int tileInt = GetTile(x, y, l);
                        if (tileset.dict.ContainsKey(tileInt))
                        {
                            Tile tile = tileset.dict[tileInt];

                            bool addCollision;

                            if (!tile.cullable)
                            {
                                addCollision = true;
                            }
                            else
                            {
                                int topTile = y > 0 ? GetTile(x, y - 1, l) : 0;
                                int bottomTile = y < Height - 1 ? GetTile(x, y + 1, l) : 0;
                                int leftTile = x > 0 ? GetTile(x - 1, y, l) : 0;
                                int rightTile = x < Width - 1 ? GetTile(x + 1, y, l) : 0;

                                bool topSolid =
                                    topTile != 0 &&
                                    tileset.dict.ContainsKey(topTile) &&
                                    tileset.dict[topTile].collisionLayer == tile.collisionLayer;

                                bool bottomSolid =
                                    bottomTile != 0 &&
                                    tileset.dict.ContainsKey(bottomTile) &&
                                    tileset.dict[bottomTile].collisionLayer == tile.collisionLayer;

                                bool leftSolid =
                                    leftTile != 0 &&
                                    tileset.dict.ContainsKey(leftTile) &&
                                    tileset.dict[leftTile].collisionLayer == tile.collisionLayer;

                                bool rightSolid =
                                    rightTile != 0 &&
                                    tileset.dict.ContainsKey(rightTile) &&
                                    tileset.dict[rightTile].collisionLayer == tile.collisionLayer;

                                bool fullySurrounded =
                                    topSolid &&
                                    bottomSolid &&
                                    leftSolid &&
                                    rightSolid;

                                addCollision = !fullySurrounded;
                            }

                            if (addCollision)
                            {
                                foreach (Polygon poly in tileset.dict[tileInt].polygons)
                                {
                                    List<Vector2> vertices = poly.GetVertices(); // Unscaled normalized size;

                                    for (int i = 0; i < vertices.Count; i++)
                                    {
                                        vertices[i] = new(vertices[i].X * TileWidth, vertices[i].Y * TileHeight);
                                    }

                                    //AddPolygon(new(vertices, new(new(x * TileWidth, y * TileHeight), new(TileWidth, TileHeight), new(0, 0))));
                                    collisionMap.AddPolygonToLayer(tile.collisionLayer,
                                        new(vertices, new(new(x * TileWidth, y * TileHeight), new(TileWidth, TileHeight), new(0, 0)))
                                        );
                                }
                            }
                        }
                    }
                }
            }

            collisionMap.LoadChunks(Width, Height, chunkWidth, chunkHeight, TileWidth, TileHeight);
        }


        public void Draw(SpriteBatch sb)
        {
            Vector2i mapSize = new(Width * TileWidth, Height * TileHeight);
            sb.Draw(renderTarget, new Rectangle(position.ToPoint(), mapSize.ToPoint()), color);
        }
    }

    internal class TiledMapData
    {
        public int width { get; set; }
        public int height { get; set; }

        public int tilewidth { get; set; }
        public int tileheight { get; set; }

        public List<TiledLayer> layers { get; set; }
    }

    internal class TiledLayer
    {
        public List<int> data { get; set; }

        public int width { get; set; }
        public int height { get; set; }

        public string name { get; set; }
        public string type { get; set; }
    }


    internal class Polygon
    {
        // LOCAL vertices (relative to origin)
        public List<Vector2> localVertices;
        public Transform transform;
        public List<Vector2> globalVertices
        {
            get
            {
                return GetVertices();
            }
        }

        public float Top => GetVertices().Min(v => v.Y);
        public float Bottom => GetVertices().Max(v => v.Y);
        public float Left => GetVertices().Min(v => v.X);
        public float Right => GetVertices().Max(v => v.X);

        public Polygon(List<Vector2> vertices, Transform transform)
        {
            this.localVertices = vertices;
            this.transform = transform;
        }

        public Polygon(Vector2 X, Vector2 Y, Vector2 Z, Transform transform)
        {
            localVertices = new() { X, Y, Z };
            this.transform = transform;
        }

        // WORLD vertices
        public List<Vector2> GetVertices()
        {
            List<Vector2> verts = new();

            float rot = transform.rotation.radians;

            float cos = MathF.Cos(rot);
            float sin = MathF.Sin(rot);

            foreach (Vector2 local in localVertices)
            {
                // Rotate
                Vector2 rotated = new(
                    local.X * cos - local.Y * sin,
                    local.X * sin + local.Y * cos
                );

                // Translate
                rotated += transform.position;

                verts.Add(rotated);
            }

            return verts;
        }

        public IEnumerable<(Vector2 a, Vector2 b)> GetEdges()
        {
            List<Vector2> vertices = GetVertices();

            for (int i = 0; i < vertices.Count; i++)
            {
                yield return (
                    vertices[i],
                    vertices[(i + 1) % vertices.Count]
                );
            }
        }

        public Rectangle GetBoundingRectangle()
        {
            List<Vector2> vertices = GetVertices();

            if (vertices.Count == 0)
                return Rectangle.Empty;

            float minX = vertices.Min(v => v.X);
            float minY = vertices.Min(v => v.Y);
            float maxX = vertices.Max(v => v.X);
            float maxY = vertices.Max(v => v.Y);

            return new Rectangle(
                (int)minX,
                (int)minY,
                (int)(maxX - minX),
                (int)(maxY - minY)
            );
        }

        public Vector2 GetCenter()
        {
            List<Vector2> vertices = GetVertices();

            Vector2 sum = Vector2.Zero;

            foreach (var v in vertices)
                sum += v;

            return sum / vertices.Count;
        }

        public List<Vector2> GetAxes()
        {
            var axes = new List<Vector2>();

            foreach (var edge in GetEdges())
            {
                Vector2 e = edge.b - edge.a;

                Vector2 axis = new(-e.Y, e.X);

                float len = axis.Length();

                if (len <= float.Epsilon)
                    continue;

                axis /= len;

                axes.Add(axis);
            }

            return axes;
        }

        public (float min, float max) ProjectOntoAxis(Vector2 axis)
        {
            List<Vector2> vertices = GetVertices();

            axis = Vector2.Normalize(axis);

            float min = Vector2.Dot(vertices[0], axis);
            float max = min;

            for (int i = 1; i < vertices.Count; i++)
            {
                float proj = Vector2.Dot(vertices[i], axis);

                if (proj < min) min = proj;
                if (proj > max) max = proj;
            }

            return (min, max);
        }

        public bool Intersects(Polygon other, out Vector2 mtv)
        {
            mtv = Vector2.Zero;

            List<Vector2> verticesA = GetVertices();
            List<Vector2> verticesB = other.GetVertices();

            if (verticesA.Count < 3 || verticesB.Count < 3)
                return false;

            float smallestOverlap = float.PositiveInfinity;
            Vector2 smallestAxis = Vector2.Zero;

            var axes = GetAxes();
            axes.AddRange(other.GetAxes());

            Vector2 centerA = GetCenter();
            Vector2 centerB = other.GetCenter();

            foreach (var axis in axes)
            {
                Vector2 normAxis = Vector2.Normalize(axis);

                var aProj = ProjectOntoAxis(normAxis);
                var bProj = other.ProjectOntoAxis(normAxis);

                float overlap =
                    Math.Min(aProj.max, bProj.max) -
                    Math.Max(aProj.min, bProj.min);

                if (overlap <= 0f)
                {
                    mtv = Vector2.Zero;
                    return false;
                }

                if (overlap < smallestOverlap)
                {
                    smallestOverlap = overlap;
                    smallestAxis = normAxis;

                    Vector2 dir = centerB - centerA;

                    if (Vector2.Dot(dir, smallestAxis) < 0f)
                    {
                        smallestAxis = -smallestAxis;
                    }
                }
            }

            mtv = smallestAxis * smallestOverlap;

            return true;
        }

        public bool Intersects(Polygon other)
        {
            return Intersects(other, out _);
        }
    }

    internal class SquarePrimitive
    {
        public List<Polygon> polygons;

        private Transform _transform;
        public Transform transform
        {
            get
            {
                return _transform;
            }
            set
            {
                foreach (Polygon polygon in polygons)
                {
                    polygon.transform = value;
                    _transform = value;
                }
            }
        }

        public float Top;
        public float Bottom;
        public float Left;
        public float Right;

        public float Width;
        public float Height;

        public SquarePrimitive(Transform transform)
        {
            Width = transform.size.X;
            Height = transform.size.Y;

            polygons = new();

            float zeroX = -(transform.size.X / 2);
            float zeroY = -(transform.size.Y / 2);
            float oneX = transform.size.X / 2;
            float oneY = transform.size.Y / 2;

            polygons = new();
            polygons.Add(
                new Polygon(
                    new List<Vector2>
                    {
                        new(zeroX, zeroY),
                        new(oneX, zeroY),
                        new(zeroX, oneY)
                    },
                    this.transform
                    )
                );

            polygons.Add(
                new Polygon(
                    new List<Vector2>
                    {
                        new(oneX, oneY),
                        new(zeroX,  oneY),
                        new(oneX, zeroY)
                    },
                    this.transform
                    )
                );


            this.transform = transform;
            GetEdges();
        }

        public Rectangle LoadBoundingBox()
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;

            float maxX = float.MinValue;
            float maxY = float.MinValue;

            foreach (Polygon poly in polygons)
            {
                Rectangle rect = poly.GetBoundingRectangle();

                if (rect.Left < minX) minX = rect.Left;
                if (rect.Top < minY) minY = rect.Top;

                if (rect.Right > maxX) maxX = rect.Right;
                if (rect.Bottom > maxY) maxY = rect.Bottom;
            }

            Rectangle boundingBox = new Rectangle(
                (int)minX,
                (int)minY,
                (int)(maxX - minX),
                (int)(maxY - minY)
            );

            return boundingBox;
        }

        private void GetEdges()
        {
            Top = polygons[0].Top;
            Bottom = polygons[1].Bottom;
            Left = polygons[0].Left;
            Right = polygons[1].Right;
        }

        public bool Intersects(Polygon polygon)
        {
            foreach (Polygon p in polygons)
            {
                if (p.Intersects(polygon)) return true;
            }
            return false;
        }
    }

    internal class BasicEntity
    {
        public Transform transform;
        public SquarePrimitive squarePrimitive;
        public Vector2 velocity;

        public Vector2i broadPhaseColliderTopLeftExtension = new(64, 64);
        public Vector2i broadPhaseColliderBottomRightExtension = new(64, 64);

        public BasicEntity(Transform transform)
        {
            this.transform = transform;
            this.squarePrimitive = new(transform);
            this.velocity = new(0, 0);
        }

        public void MoveAndCollide(List<Polygon> polygonList)
        {
            /* Broad Phase Collision:
             *  Instead of checking every single polygon against the entity,
             *  it uses AABB collision, which uses rectangles to check for intersections.
             *  this helps a ton with performance since polygons and SAT collison
             *  never intersects unless they are close enough for bounding boxes to tell.
             *  
             *  The only dowside ive found with using AABB then SAT is chunking,
             *  if the entity moves to another chunk in a frame and is moving fast,
             *  they can clip through the wall, but this is fixable easily
             *  using a larger bounding box to check for AABB collision.
             */
            List<Polygon> culled = BroadPhaseCollide(polygonList);

            /* Narrow Phase Collision:
             *  This is where the magic happens. Unlike broad phase, narrow phase is way more precise
             *  Here you can see the use of a SAT collision algorithm, 
             *  which uses projection of all normals of a polygon/shape
             *  and checks if the projections collide, its way more precise and malleable than AABB, 
             *  but it is way more expensive on the system.
             *  Thats the reason it is used sparingly compared to broad phase 
             */

            NarrowPhaseCollide(culled);
        }

        public List<Polygon> BroadPhaseCollide(List<Polygon> polygonList)
        {
            List<Polygon> ret = new();

            Rectangle currentBoundingBox = squarePrimitive.LoadBoundingBox();

            // You must expand the AABB bounding box of the entity since thats the only way to get proper culling around the entity.

            // expand top-left (negative direction)
            currentBoundingBox.X -= broadPhaseColliderTopLeftExtension.X;
            currentBoundingBox.Y -= broadPhaseColliderTopLeftExtension.Y;

            // expand size (positive direction on opposite side)
            currentBoundingBox.Width += broadPhaseColliderTopLeftExtension.X + broadPhaseColliderBottomRightExtension.X;
            currentBoundingBox.Height += broadPhaseColliderTopLeftExtension.Y + broadPhaseColliderBottomRightExtension.Y;

            foreach (Polygon polygon in polygonList)
            {
                if (polygon.GetBoundingRectangle().Intersects(currentBoundingBox))
                {
                    ret.Add(polygon);
                }
            }

            return ret;
        }

        private void NarrowPhaseCollide(List<Polygon> collisionList)
        {
            // Seperating the X and Y axis typically is better for collision.
            // It can help with shapes sliding against eachother instead of getting stuck on eachother

            XAxisNarrowPhase(collisionList);
            YAxisNarrowPhase(collisionList);
        }

        private void XAxisNarrowPhase(List<Polygon> collisionList)
        {
            Transform newTrans = new(new(transform.position.X + velocity.X, transform.position.Y), transform.size, transform.rotation);
            SquarePrimitive newPrim = new(newTrans);

            bool collided = false;

            foreach (Polygon polygon in collisionList)
            {
                if (newPrim.Intersects(polygon))
                {
                    collided = true;
                    break;
                }
            }

            if (!collided)
            {
                transform.position = newTrans.position;
                squarePrimitive = new(transform);
            }
        }

        private void YAxisNarrowPhase(List<Polygon> collisionList)
        {
            Transform newTrans = new(new(transform.position.X, transform.position.Y + velocity.Y), transform.size, transform.rotation);
            SquarePrimitive newPrim = new(newTrans);

            bool collided = false;

            foreach (Polygon polygon in collisionList)
            {
                if (newPrim.Intersects(polygon))
                {
                    collided = true;
                    break;
                }
            }

            if (!collided)
            {
                transform.position = newTrans.position;
                squarePrimitive = new(transform);
            }
        }
    }

    internal class BasicPlayer : BasicEntity
    {
        public int speed;

        public BasicPlayer(Transform transform, int speed) : base(transform)
        {
            this.speed = speed;
        }

        public void Update(List<Polygon> polygonList, float dt)
        {
            Vector2 dir = new();
            KeyboardState ks = Keyboard.GetState();

            if (ks.IsKeyDown(Keys.A)) dir.X -= 1;
            if (ks.IsKeyDown(Keys.D)) dir.X += 1;
            if (ks.IsKeyDown(Keys.W)) dir.Y -= 1;
            if (ks.IsKeyDown(Keys.S)) dir.Y += 1;

            velocity = dir * speed * dt;

            MoveAndCollide(polygonList);
        }
    }

    internal class Camera
    {
        public Vector2 localMousePosition
        {
            get
            {
                MouseState ms = Mouse.GetState();
                return ms.Position.ToVector2();
            }
        }
        public Vector2 globalMousePosition
        {
            get
            {
                return position + localMousePosition - (new Vector2(width / 2, height / 2));
            }
        }

        public RenderTarget2D renderTarget;
        private int width;
        private int height;
        private GraphicsDevice graphics;

        private Vector2 position;
        public Vector2 Position
        {
            get => position;
            set => position = value;
        }

        public float Zoom { get; set; } = 1f;
        public float Rotation { get; set; } = 0f;
        public Vector2 Origin { get; private set; }

        public int top;
        public int bottom;
        public int right;
        public int left;

        public Camera(GraphicsDevice graphics, int width, int height)
        {
            renderTarget = new(graphics, width, height);
            this.width = width;
            this.height = height;
            this.graphics = graphics;

            position = Vector2.Zero;
            Origin = new Vector2(width * 0.5f, height * 0.5f);
        }

        public Matrix Transform
        {
            get
            {
                // Move world by -position, then rotate/scale around origin, then move origin to screen center
                return
                    Matrix.CreateTranslation(new Vector3(-position, 0f)) *
                    Matrix.CreateRotationZ(Rotation) *
                    Matrix.CreateScale(Zoom, Zoom, 1f) *
                    Matrix.CreateTranslation(new Vector3(Origin, 0f));
            }
        }
        public void SetPosition(float x, float y) => Position = new Vector2(x, y);
        public void Move(Vector2 delta) => Position += delta;
        public void CenterOn(Vector2 worldPosition) => Position = worldPosition;
        public void DebugCamera(float dt, int speed)
        {
            Vector2 cameraDir = new(0, 0);
            KeyboardState ks = Keyboard.GetState();

            if (ks.IsKeyDown(Keys.W)) cameraDir.Y -= 1; if (ks.IsKeyDown(Keys.S)) cameraDir.Y += 1;
            if (ks.IsKeyDown(Keys.A)) cameraDir.X -= 1; if (ks.IsKeyDown(Keys.D)) cameraDir.X += 1;

            Move(cameraDir * dt * speed);
        }

        public Vector2 GetGlobalMousePosition()
        {
            Vector2 mp = Mouse.GetState().Position.ToVector2();
            return mp + (position - (new Vector2(width, height) / 2));
        }

        public void DrawToScreen(SpriteBatch sb)
        {
            sb.Draw(renderTarget, new Rectangle(0, 0, width, height), Color.White);
        }

        public void ClampToBounds()
        {
            float halfWidth = (width * 0.5f) / Zoom;
            float halfHeight = (height * 0.5f) / Zoom;

            float minX = left + halfWidth;
            float maxX = right - halfWidth;
            float minY = top + halfHeight;
            float maxY = bottom - halfHeight;

            position.X = Math.Clamp(position.X, minX, maxX);
            position.Y = Math.Clamp(position.Y, minY, maxY);
        }
    }


    internal class MathHelper
    {
        public MathHelper()
        {

        }

        public float DegToRad(float degrees) => (degrees * ((float)Math.PI / 180));
        public float RadToDeg(float radians) => (radians * (180 / (float)Math.PI));

        public Vector2 PerpendicularVector(Vector2 edge)
        {
            return new Vector2(-edge.Y, edge.X);
        }

        public List<Vector2> PointsBetween(Vector2 a, Vector2 b, int steps)
        {
            List<Vector2> points = new();

            for (int i = 0; i < steps; i++)
            {
                float step = (float)i / steps;

                Vector2 point = new(0, 0);
                point.X = a.X + (b.X - a.X) * step;
                point.Y = a.Y + (b.Y - a.Y) * step;

                points.Add(point);
            }

            return points;
        }
    }

    internal class Vector2i : IEquatable<Vector2i>
    {
        public int X;
        public int Y;

        public Vector2 ToVector2()
        {
            return new((float)X, (float)Y);
        }

        public Point ToPoint()
        {
            return new Point(X, Y);
        }

        public Vector2i(float x, float y)
        {
            this.X = (int)x;
            this.Y = (int)y;
        }

        public Vector2i(double x, double y)
        {
            this.X = (int)x;
            this.Y = (int)y;
        }

        public Vector2i(Vector2 vec2)
        {
            this.X = (int)vec2.X;
            this.Y = (int)vec2.Y;
        }

        public Vector2i(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        // Arithmetic operators
        public static Vector2i operator +(Vector2i a, Vector2i b) => new(a.X + b.X, a.Y + b.Y);
        public static Vector2i operator -(Vector2i a, Vector2i b) => new(a.X - b.X, a.Y - b.Y);
        public static Vector2i operator -(Vector2i a) => new(-a.X, -a.Y);

        // Scalar multiplication (int and float)
        public static Vector2i operator *(Vector2i v, int s) => new(v.X * s, v.Y * s);
        public static Vector2i operator *(Vector2i v, Vector2i s) => new(v.X * s.X, v.Y * s.Y);
        public static Vector2i operator *(int s, Vector2i v) => v * s;
        public static Vector2i operator *(Vector2i v, float s) => new((int)(v.X * s), (int)(v.Y * s));
        public static Vector2i operator *(float s, Vector2i v) => v * s;

        // Scalar division (int and float)
        public static Vector2i operator /(Vector2i v, int s)
        {
            if (s == 0) throw new DivideByZeroException();
            return new(v.X / s, v.Y / s);
        }

        public static Vector2i operator /(Vector2i v, float s)
        {
            if (s == 0f) throw new DivideByZeroException();
            return new((int)(v.X / s), (int)(v.Y / s));
        }

        // Equality
        public static bool operator ==(Vector2i a, Vector2i b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            return a.X == b.X && a.Y == b.Y;
        }

        public static bool operator !=(Vector2i a, Vector2i b) => !(a == b);

        public bool Equals(Vector2i? other)
        {
            if (other is null) return false;
            return this.X == other.X && this.Y == other.Y;
        }

        public override bool Equals(object? obj) => Equals(obj as Vector2i);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString() => $"Vector2i({X}, {Y})";
    }

    internal class Rotation
    {
        private float _degrees;
        private float _radians;

        public float degrees
        {
            get
            {
                return _degrees;
            }
            set
            {
                _degrees = value;
                _radians = value * ((float)Math.PI / 180);
            }
        }

        public float radians
        {
            get
            {
                return _radians;
            }
            set
            {
                _radians = value;
                _degrees = value * (180 / (float)Math.PI);
            }
        }

        public Rotation(float? degrees = null, float? radians = null)
        {
            if (degrees != null) this.degrees = (float)degrees;
            if (radians != null) this.radians = (float)radians;
        }
    }

    internal class Transform
    {
        public Vector2 position;
        public Vector2i size;
        public Rotation rotation;

        public float top
        {
            get
            {
                return position.Y - (size.Y / 2);
            }
            set
            {
                position.Y = value + (size.Y / 2);
            }
        }
        public float bottom
        {
            get
            {
                return position.Y + (size.Y / 2);
            }
            set
            {
                position.Y = value - (size.Y / 2);
            }
        }
        public float left
        {
            get
            {
                return position.X - (size.X / 2);
            }
            set
            {
                position.X = value + (size.X / 2);
            }
        }
        public float right
        {
            get
            {
                return position.X + (size.X / 2);
            }
            set
            {
                position.X = value - (size.X / 2);
            }
        }

        public Transform(Vector2 position, Vector2i size, Rotation rotation)
        {
            this.position = position;
            this.size = size;
            this.rotation = rotation;
        }
    }


    internal class Sprite
    {
        public Texture2D texture;
        public Transform transform;
        public Color color;
        public bool flipVertical = false;
        public bool flipHorizontal = false;

        public Sprite(Texture2D texture, Transform transform, Color color)
        {
            this.texture = texture ?? throw new ArgumentNullException(nameof(texture));
            this.transform = transform ?? throw new ArgumentNullException(nameof(transform));
            this.color = color;
        }

        public void Draw(SpriteBatch sb)
        {
            if (texture == null) return;

            int texW = Math.Max(1, texture.Width);
            int texH = Math.Max(1, texture.Height);

            Vector2 origin = new Vector2(texW / 2f, texH / 2f);

            Vector2 scale = new Vector2(
                transform.size.X / (float)texW,
                transform.size.Y / (float)texH
            );

            SpriteEffects effects = SpriteEffects.None;

            if (flipHorizontal)
                effects |= SpriteEffects.FlipHorizontally;

            if (flipVertical)
                effects |= SpriteEffects.FlipVertically;

            sb.Draw(
                texture,
                transform.position,
                null,
                color,
                transform.rotation.radians,
                origin,
                scale,
                effects,
                0f
            );
        }
    }

    internal class AnimationCycle
    {
        public float duration;
        public float currentTime = 0f;
        public bool loop = true;

        // ORDERED keyframes (critical fix)
        public SortedDictionary<float, Texture2D> textureKeyframes;

        public AnimationCycle(bool loop, float duration)
        {
            this.loop = loop;
            this.duration = duration;
            this.textureKeyframes = new SortedDictionary<float, Texture2D>();
        }

        public void Update(float dt)
        {
            currentTime += dt;

            if (loop)
            {
                // clean wrap (no pause at end)
                currentTime %= duration;
            }
            else
            {
                if (currentTime > duration)
                    currentTime = duration;
            }
        }

        public Texture2D GetTextureFrame()
        {
            Texture2D currentTX = default;

            foreach (var kvp in textureKeyframes)
            {
                if (kvp.Key > currentTime)
                    break;

                currentTX = kvp.Value;
            }

            return currentTX;
        }

        public void AddFrame(float time, Texture2D tx)
        {
            textureKeyframes.Add(time, tx);
        }
    }

    internal class AnimationManager
    {
        public Dictionary<String, AnimationCycle> animationCycles;
        public String currentAnimationName;

        public AnimationManager()
        {
            animationCycles = new();
            // name, loop, duration, dictionary<float, texture2d>
        }

        public AnimationCycle GetAnimationCycle()
        {
            return animationCycles[currentAnimationName];
        }

        public bool SetAnimation(String name)
        {
            currentAnimationName = name;

            if (animationCycles.ContainsKey(currentAnimationName)) return true;
            else return false;
        }

        public Texture2D GetCurrentAnimationFrame()
        {
            return animationCycles[currentAnimationName].GetTextureFrame();
        }


        public void AddAnimation(String name, bool loop, float duration, Dictionary<float, Texture2D> textureKeyframes)
        {
            animationCycles.Add(name, new(loop, duration));

            foreach (var kvp in textureKeyframes)
            {
                animationCycles[name].AddFrame(kvp.Key, kvp.Value);
            }
        }
        public void UpdateAnimation(float dt) { GetAnimationCycle().Update(dt); }
    }


    public sealed class FrameRateCounter
    {
        private int _frameCount;
        private int _framesPerSecond;
        private double _accumulatedSeconds;
        private TimeSpan _lastTotalTime;
        private bool _initialized;

        public int CurrentFramerate => _framesPerSecond;
        public void Reset()
        {
            _frameCount = 0;
            _framesPerSecond = 0;
            _accumulatedSeconds = 0;
            _lastTotalTime = TimeSpan.Zero;
            _initialized = false;
        }

        public void UpdateFromDraw(GameTime gameTime)
        {
            if (!_initialized)
            {
                // Initialize reference time on first call to avoid a large initial delta.
                _lastTotalTime = gameTime.TotalGameTime;
                _initialized = true;
                return;
            }

            var delta = (gameTime.TotalGameTime - _lastTotalTime).TotalSeconds;
            _lastTotalTime = gameTime.TotalGameTime;

            // Ignore pathological deltas (e.g. debugging pause or device lost)
            if (delta <= 0 || delta > 1.0)
            {
                return;
            }

            _accumulatedSeconds += delta;
            _frameCount++;

            if (_accumulatedSeconds >= 1.0)
            {
                _framesPerSecond = _frameCount;
                _frameCount = 0;
                _accumulatedSeconds -= 1.0;
            }
        }
    }

    internal interface IScene
    {
        public void Load(SceneManager sm);
        public void Update(float dt);
        public void Bake(SpriteBatch sb);
        public void Draw(SpriteBatch sb);
    }

    internal class SceneManager
    {
        public IScene scene;
        public Dictionary<String, Texture2D> textureDictionary;

        public FrameRateCounter frameCounter;
        public MathHelper mathHelper;
        public DebugManager debugManager;
        public ContentManager contentManager;
        public GraphicsDevice graphicsDevice;

        public float dt;
        public GameTime gameTime;

        public KeyboardState currentKeyboardState;
        public KeyboardState previousKeyboardState;

        public MouseState currentMouseState;
        public MouseState previousMouseState;

        public Func<GraphicsDevice, ContentManager, Dictionary<string, Texture2D>> LoadTextures;

        public SceneManager(ContentManager contentManager, GraphicsDevice graphicsDevice, IScene scene, SpriteFont debugFont,
            Func<GraphicsDevice, ContentManager, Dictionary<string, Texture2D>> loadTexturesFunction)
        {
            this.contentManager = contentManager;
            this.graphicsDevice = graphicsDevice;
            this.mathHelper = new();
            this.frameCounter = new();
            debugManager = new(this, debugFont);

            LoadTextures = loadTexturesFunction;
            textureDictionary = LoadTextures.Invoke(graphicsDevice, contentManager);
            LoadScene(scene);
        }

        public bool KeyJustPressed(Keys key)
        {
            if (currentKeyboardState.IsKeyDown(key) && !previousKeyboardState.IsKeyDown(key))
            {
                return true;
            }
            return false;
        }

        public bool KeyJustReleased(Keys key)
        {
            if (!currentKeyboardState.IsKeyDown(key) && previousKeyboardState.IsKeyDown(key))
            {
                return true;
            }
            return false;
        }



        public bool LMBJustPressed()
        {
            if (currentMouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
            {
                return true;
            }
            return false;
        }

        public bool LMBJustReleased()
        {
            if (currentMouseState.LeftButton == ButtonState.Released && previousMouseState.LeftButton == ButtonState.Pressed)
            {
                return true;
            }
            return false;
        }


        public bool RMBJustPressed()
        {
            if (currentMouseState.RightButton == ButtonState.Pressed && previousMouseState.RightButton == ButtonState.Released)
            {
                return true;
            }
            return false;
        }

        public bool RMBJustReleased()
        {
            if (currentMouseState.RightButton == ButtonState.Released && previousMouseState.RightButton == ButtonState.Pressed)
            {
                return true;
            }
            return false;
        }


        public void LoadScene(IScene scene)
        {
            this.scene = scene;
            scene.Load(this);
        }

        public void UpdateScene(float dt, GameTime gt)
        {
            currentKeyboardState = Keyboard.GetState();
            currentMouseState = Mouse.GetState();
            this.scene.Update(dt);
            previousKeyboardState = Keyboard.GetState();
            previousMouseState = Mouse.GetState();

            this.dt = dt;
            this.gameTime = gt;
        }

        public void BakeScene(SpriteBatch sb)
        {
            this.scene.Bake(sb);
        }

        public void DrawScene(SpriteBatch sb)
        {
            this.scene.Draw(sb);
            frameCounter.UpdateFromDraw(gameTime);
        }
    }

    internal record PolygonDebugPacket(Color color, Polygon polygon);
    internal record RectangleDebugPacket(Color color, Rectangle rectangle);
    internal record TextDebugPacket(Vector2 offset, Color color, String str);
    internal record PointDebugPacket(Vector2 center, int size, Color color);

    internal class DebugManager
    {
        private SceneManager sm;

        private List<TextDebugPacket> debugTextList = new();
        private List<RectangleDebugPacket> debugRectList = new();
        private List<PolygonDebugPacket> debugPolygonList = new();
        private List<PointDebugPacket> debugPointList = new();

        public SpriteFont font;
        public bool visible = true;

        public DebugManager(SceneManager sm, SpriteFont font)
        {
            this.sm = sm;
            this.font = font;
        }

        public void AddTextToScreen(Vector2 offset, Color color, String str)
        {
            this.debugTextList.Add(new(offset, color, str));
        }

        public void AddRectToScreen(Color color, Rectangle rect)
        {
            this.debugRectList.Add(new(color, rect));
        }

        public void AddPolygonToScreen(Color color, Polygon polygon)
        {
            this.debugPolygonList.Add(new(color, polygon));
        }

        public void AddPointToScreen(Color color, Vector2 center, int size)
        {
            debugPointList.Add(new(center, size, color));
        }


        public void DrawPoint(SpriteBatch sb, Color color, Vector2 center, int size)
        {
            sb.Draw(sm.textureDictionary["pixel"],
                new Rectangle(new Point((int)center.X - size / 2, (int)center.Y - size / 2), new Point(size, size)), color);
        }

        public void DrawAllPoints(SpriteBatch sb)
        {
            foreach (PointDebugPacket packet in debugPointList)
            {
                DrawPoint(sb, packet.color, packet.center, packet.size);
            }
            debugPointList.Clear();
        }

        public void DrawPolygon(SpriteBatch sb, Polygon polygon, int lineSteps, Color color)
        {
            MathHelper mh = sm.mathHelper;
            List<Vector2> vertices = polygon.globalVertices;
            Vector2 X = vertices[0];
            Vector2 Y = vertices[1];
            Vector2 Z = vertices[2];

            // Fallback to per-point drawing (original behavior) if bake failed
            List<Vector2> xy = mh.PointsBetween(X, Y, lineSteps);
            List<Vector2> xz = mh.PointsBetween(X, Z, lineSteps);
            List<Vector2> yz = mh.PointsBetween(Y, Z, lineSteps);

            List<Vector2> pts = new();
            foreach (Vector2 pt in xy) pts.Add(pt);
            foreach (Vector2 pt in xz) pts.Add(pt);
            foreach (Vector2 pt in yz) pts.Add(pt);

            foreach (Vector2 point in pts)
            {
                AddPointToScreen(color, point, 4);
            }

            AddPointToScreen(color, X, 4);
            AddPointToScreen(color, Y, 4);
            AddPointToScreen(color, Z, 4);
        }

        public void DrawAllPolygons(SpriteBatch sb, int lineSteps = 25)
        {
            foreach (PolygonDebugPacket packet in debugPolygonList)
            {
                DrawPolygon(sb, packet.polygon, lineSteps, packet.color);
            }
            debugPolygonList.Clear();
        }


        public void DrawRect(SpriteBatch sb, Rectangle rect, int outlineWidth, Color color)
        {
            // Top
            sb.Draw(sm.textureDictionary["pixel"], new Rectangle(rect.X, rect.Y, rect.Width, outlineWidth), color);
            // Bottom
            sb.Draw(sm.textureDictionary["pixel"], new Rectangle(rect.X, rect.Y + rect.Height - outlineWidth, rect.Width, outlineWidth), color);
            // Left
            sb.Draw(sm.textureDictionary["pixel"], new Rectangle(rect.X, rect.Y, outlineWidth, rect.Height), color);
            // Right
            sb.Draw(sm.textureDictionary["pixel"], new Rectangle(rect.X + rect.Width - outlineWidth, rect.Y, outlineWidth, rect.Height), color);
        }

        public void DrawAllRects(SpriteBatch sb)
        {
            foreach (RectangleDebugPacket packet in debugRectList.ToList())
            {
                DrawRect(sb, packet.rectangle, 2, packet.color);
            }
            debugRectList.Clear();
        }


        public void DrawAllShapes(SpriteBatch sb)
        {
            if (visible)
            {
                DrawAllPolygons(sb);
                DrawAllRects(sb);
                DrawAllPoints(sb);
            }
        }

        public void DrawAllText(SpriteBatch sb)
        {
            if (visible)
            {
                foreach (TextDebugPacket packet in debugTextList.ToList())
                {
                    sb.DrawString(font, packet.str, packet.offset, packet.color);
                }

                debugTextList.Clear();
            }
        }
    }
}