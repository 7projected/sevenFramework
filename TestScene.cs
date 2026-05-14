using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sevenFramework
{
    internal class TestScene : IScene
    {
        MathHelper mh;
        SceneManager sm;
        float time = 0;

        Rectangle testPlayerRect = new(0, 0, 128, 128);
        int testPlayerSpeed = 1000;

        Sprite sprite;
        Polygon testPolygon = new(new(100, 100), new(150, 200), new(100, 300));

        public void Load(SceneManager sm)
        {
            this.sm = sm;
            mh = new();
            sprite = new(sm.textureDictionary["kenny"], new(new(0, 0), new(128, 128), new(0, 0)));
        }

        public void Update(float dt)
        {
            time += dt;
            sm.debugManager.AddRectToScreen(new(Color.Red, testPlayerRect));

            KeyboardState ks = Keyboard.GetState();

            if (ks.IsKeyDown(Keys.W)) testPlayerRect.Y -= (int)(testPlayerSpeed * dt);
            if (ks.IsKeyDown(Keys.S)) testPlayerRect.Y += (int)(testPlayerSpeed * dt);

            if (ks.IsKeyDown(Keys.A)) testPlayerRect.X -= (int)(testPlayerSpeed * dt);
            if (ks.IsKeyDown(Keys.D)) testPlayerRect.X += (int)(testPlayerSpeed * dt);

            sprite.transform.rotation.degrees += dt * 20;
            sprite.transform.position = testPlayerRect.Center.ToVector2();
        }

        public void Bake(SpriteBatch sb)
        {
        }

        public void Draw(SpriteBatch sb)
        {
            sb.Begin(samplerState: SamplerState.PointClamp);

            sm.debugManager.AddTextToScreen($"Draw:{(int)time} @ {(int)sm.fps}");
            sprite.Draw(sb);

            testPolygon.Draw(sb, sm, mh, 50, Color.Red, Color.Green);

            sm.debugManager.DrawText(sb);
            sb.End();
        }
    }
}
