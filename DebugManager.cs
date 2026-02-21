using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace sevenFramework
{
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
            foreach(String str in debugTextList.ToList())
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
