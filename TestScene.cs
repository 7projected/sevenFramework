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
        SceneManager sm;
        float time = 0;

        Rectangle testPlayerRect = new(0, 0, 32, 32);
        int testPlayerSpeed = 1000;

        public void Load(SceneManager sm)
        {
            this.sm = sm;
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
        }

        public void Draw(SpriteBatch sb)
        {
            sm.debugManager.AddTextToScreen($"Draw:{(int)time} @ {(int)sm.fps}");
        }
    }
}
