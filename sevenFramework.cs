using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace sevenFramework
{
    internal class Polygon
    {
        public List<Vector2> vertices;

        public Vector2 X
        {
            get
            {
                if (vertices == null || vertices.Count < 1) throw new InvalidOperationException("Polygon has no vertices");
                return vertices[0];
            }
        }
        public Vector2 Y
        {
            get
            {
                if (vertices == null || vertices.Count < 2) throw new InvalidOperationException("Polygon does not have a Y vertex");
                return vertices[1];
            }
        }
        public Vector2 Z
        {
            get
            {
                if (vertices == null || vertices.Count < 3) throw new InvalidOperationException("Polygon does not have a Z vertex");
                return vertices[2];
            }
        }

        // Generic polygon constructor
        public Polygon(Vector2 X, Vector2 Y, Vector2 Z)
        {
            vertices = new() { X, Y, Z };
        }

        public void SetVertices(Vector2 X, Vector2 Y, Vector2 Z)
        {
            vertices = new() { X, Y, Z };
        }

        public void SetVertices(List<Vector2> vertices)
        {
            this.vertices = vertices;
        }


        public IEnumerable<(Vector2 a, Vector2 b)> GetEdges()
        {
            if (vertices == null) yield break;
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
            if (vertices == null || vertices.Count == 0)
                return Rectangle.Empty;

            float minX = vertices.Min(v => v.X);
            float minY = vertices.Min(v => v.Y);
            float maxX = vertices.Max(v => v.X);
            float maxY = vertices.Max(v => v.Y);

            int x = (int)Math.Floor(minX);
            int y = (int)Math.Floor(minY);
            int width = (int)Math.Ceiling(maxX - minX);
            int height = (int)Math.Ceiling(maxY - minY);

            // Ensure non-negative dimensions
            width = Math.Max(0, width);
            height = Math.Max(0, height);

            return new Rectangle(x, y, width, height);
        }

        // Temporary
        public void Move(Vector2 offs)
        {
            List<Vector2> newVerts = new();

            for (int i = 0; i < vertices.Count; i++)
            {
                newVerts.Add(vertices[i] + offs);
            }

            SetVertices(newVerts);
        }

        // Compute polygon centroid (average of vertices). Good enough for SAT direction decisions.
        public Vector2 GetCenter()
        {
            if (vertices == null || vertices.Count == 0) return Vector2.Zero;
            Vector2 sum = Vector2.Zero;
            foreach (var v in vertices) sum += v;
            return sum / vertices.Count;
        }

        // Build normalized axes (per-edge normals) for SAT.
        public List<Vector2> GetAxes()
        {
            var axes = new List<Vector2>();
            if (vertices == null || vertices.Count < 2) return axes;

            foreach (var edge in GetEdges())
            {
                Vector2 e = edge.b - edge.a;
                // perpendicular vector
                Vector2 axis = new Vector2(-e.Y, e.X);

                float len = axis.Length();
                if (len <= float.Epsilon) continue; // skip degenerate edges
                axis /= len; // normalize
                axes.Add(axis);
            }

            return axes;
        }

        // Project polygon onto axis (axis should be normalized for meaningful overlap values)
        public (float min, float max) ProjectOntoAxis(Vector2 axis)
        {
            if (vertices == null || vertices.Count == 0) return (0f, 0f);

            // Normalize axis in-case caller didn't
            float axisLen = axis.Length();
            if (axisLen <= float.Epsilon) return (0f, 0f);
            axis /= axisLen;

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

        // SAT polygon-vs-polygon test.
        // Returns true if polygons intersect. If true, 'mtv' will contain the minimum translation vector
        // to move 'this' polygon out of collision (direction points away from 'other').
        public bool Intersects(Polygon other, out Vector2 mtv)
        {
            mtv = Vector2.Zero;
            if (other == null) return false;
            if (vertices == null || vertices.Count < 3 || other.vertices == null || other.vertices.Count < 3) return false;

            float smallestOverlap = float.PositiveInfinity;
            Vector2 smallestAxis = Vector2.Zero;

            // Collect axes from both polygons
            var axes = GetAxes();
            axes.AddRange(other.GetAxes());

            Vector2 centerA = GetCenter();
            Vector2 centerB = other.GetCenter();

            foreach (var axis in axes)
            {
                // Use normalized axis
                Vector2 normAxis = axis;
                float len = normAxis.Length();
                if (len <= float.Epsilon) continue;
                normAxis /= len;

                var aProj = ProjectOntoAxis(normAxis);
                var bProj = other.ProjectOntoAxis(normAxis);

                // overlap length on this axis
                float overlap = Math.Min(aProj.max, bProj.max) - Math.Max(aProj.min, bProj.min);

                // If no overlap -> separating axis found
                if (overlap <= 0f)
                {
                    mtv = Vector2.Zero;
                    return false;
                }

                // track smallest overlap for MTV
                if (overlap < smallestOverlap)
                {
                    smallestOverlap = overlap;
                    smallestAxis = normAxis;

                    // make axis point from A to B so MTV pushes A away from B
                    Vector2 dir = centerB - centerA;
                    if (Vector2.Dot(dir, smallestAxis) < 0f)
                    {
                        smallestAxis = -smallestAxis;
                    }
                }
            }

            // If all axes overlapped, polygons intersect. MTV is axis * overlap.
            mtv = smallestAxis * smallestOverlap;
            return true;
        }

        // Convenience overload if caller does not need MTV
        public bool Intersects(Polygon other)
        {
            return Intersects(other, out _);
        }
    }

    internal class Camera
    {
        private RenderTarget2D renderTarget;
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

        public Sprite(Texture2D texture, Transform transform)
        {
            this.texture = texture ?? throw new ArgumentNullException(nameof(texture));
            this.transform = transform ?? throw new ArgumentNullException(nameof(transform));
        }

        public void Draw(SpriteBatch sb)
        {
            if (texture == null) return; // defensive; ctor already throws, but keep this safe in case of future mutation
                                         // Protect against zero-sized textures to avoid divide-by-zero
            int texW = Math.Max(1, texture.Width);
            int texH = Math.Max(1, texture.Height);

            Vector2 origin = new Vector2(texW / 2f, texH / 2f);
            Vector2 scale = new Vector2(transform.size.X / (float)texW, transform.size.Y / (float)texH);

            sb.Draw(
                texture,
                transform.position,
                null,
                Color.White,
                transform.rotation.radians,
                origin,
                scale,
                SpriteEffects.None,
                0f
            );
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
        public float fps;

        public MathHelper mathHelper;
        public DebugManager debugManager;
        public ContentManager contentManager;
        public GraphicsDevice graphicsDevice;

        public SceneManager(ContentManager contentManager, GraphicsDevice graphicsDevice, IScene scene, SpriteFont debugFont)
        {
            this.contentManager = contentManager;
            this.graphicsDevice = graphicsDevice;
            this.mathHelper = new();
            debugManager = new(this, debugFont);

            LoadTextures(contentManager);
            LoadScene(scene);
        }

        public void LoadTextures(ContentManager cm)
        {
            textureDictionary = new();

            textureDictionary.Add("pixel", cm.Load<Texture2D>("pixel"));
            textureDictionary.Add("kenny", cm.Load<Texture2D>("kney"));
        }

        public void LoadScene(IScene scene)
        {
            this.scene = scene;
            scene.Load(this);
        }

        public void UpdateScene(float dt, GameTime gametime)
        {
            fps = (int)(1f / gametime.ElapsedGameTime.TotalSeconds);
            this.scene.Update(dt);
        }

        public void BakeScene(SpriteBatch sb)
        {
            this.scene.Bake(sb);
        }

        public void DrawScene(SpriteBatch sb)
        {
            this.scene.Draw(sb);
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
            foreach(PointDebugPacket packet in debugPointList)
            {
                DrawPoint(sb, packet.color, packet.center, packet.size);
            }
            debugPointList.Clear();
        }

        public void DrawPolygon(SpriteBatch sb, Polygon polygon, int lineSteps, Color color)
        {
            MathHelper mh = sm.mathHelper;
            Vector2 X = polygon.X;
            Vector2 Y = polygon.Y;
            Vector2 Z = polygon.Z;

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
            foreach(PolygonDebugPacket packet in debugPolygonList)
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
                DrawRect(sb, packet.rectangle , 2, packet.color);
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
