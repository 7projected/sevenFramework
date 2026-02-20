using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

        public void Load(SceneManager sm)
        {
            this.sm = sm;
        }

        public void Update(float dt)
        {
            time += dt;
        }

        public void Draw(SpriteBatch sb)
        {
            Debug.Print($"Draw:{(int)time} @ {(int)sm.fps}");

            sb.Draw(sm.textureDictionary["pixel"], new Rectangle(0, 0, 32, 32), Color.Red);
        }
    }
}
