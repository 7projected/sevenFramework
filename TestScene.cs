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


        public void Load(SceneManager sm)
        {
        }

        public void Draw(SpriteBatch sb)
        {
            Debug.Print("Draw");
        }

        public void Update(float dt)
        {
            Debug.Print("Update");
        }
    }
}
