using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sevenFramework
{
    internal class DebugManager
    {
        private SceneManager sm;
        private List<String> debugTextList = new();

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

        public void DrawText(SpriteBatch sb)
        {
            foreach(String str in debugTextList.ToList())
            {
                sb.DrawString(font, str, new(0, 0), fontColor);
            }

            debugTextList.Clear();
        }

        public void DrawAllMethods(SpriteBatch sb)
        {
            if (visible)
            {
                DrawText(sb);
            }
        }
    }
}
