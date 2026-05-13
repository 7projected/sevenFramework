using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace sevenFramework
{
    internal class MathHelper
    {
        public MathHelper()
        {

        }

        public float DegToRad(float degrees) => (degrees * ((float)Math.PI / 180));
        public float RadToDeg(float radians) => (radians * (180 / (float)Math.PI));
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


    internal interface IScene
    {
        public void Load(SceneManager sm);
        public void Update(float dt);
        public void Draw(SpriteBatch sb);
    }

    internal class SceneManager
    {
        public IScene scene;
        public Dictionary<String, Texture2D> textureDictionary;
        public float fps;
        public DebugManager debugManager;

        public SceneManager(IScene scene, SpriteFont debugFont)
        {
            textureDictionary = new();
            LoadScene(scene);
            debugManager = new(this, debugFont, Color.White);
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

        public void DrawScene(SpriteBatch sb)
        {
            this.scene.Draw(sb);
            this.debugManager.DrawAllMethods(sb);
        }
    }

    internal class DebugRect
    {
        public Color color;
        public Rectangle rect;

        public DebugRect(Color color, Rectangle rect)
        {
            this.color = color;
            this.rect = rect;
        }
    }

    internal class DebugManager
    {
        private SceneManager sm;
        private List<String> debugTextList = new();
        private List<DebugRect> debugRectList = new();

        public SpriteFont font;
        public bool visible = true;
        public Color fontColor;

        public DebugManager(SceneManager sm, SpriteFont font, Color color = default)
        {
            this.sm = sm;
            this.font = font;
            if (color == default) fontColor = Color.White;
            else fontColor = color;
        }

        public void AddTextToScreen(String str)
        {
            this.debugTextList.Add(str);
        }

        public void AddRectToScreen(DebugRect rect)
        {
            this.debugRectList.Add(rect);
        }

        public void DrawText(SpriteBatch sb)
        {
            foreach (String str in debugTextList.ToList())
            {
                sb.DrawString(font, str, new(0, 0), fontColor);
            }

            debugTextList.Clear();
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
            foreach (DebugRect rect in debugRectList.ToList())
            {
                DrawRect(sb, rect.rect, 2, rect.color);
            }
            debugRectList.Clear();
        }

        public void DrawAllMethods(SpriteBatch sb)
        {
            if (visible)
            {
                DrawAllRects(sb);
                DrawText(sb);

            }
        }
    }
}
