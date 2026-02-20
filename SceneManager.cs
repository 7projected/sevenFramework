using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sevenFramework
{
    internal interface IScene
    {
        public void Load(SceneManager sm);
        public void Update(float dt);
        public void Draw(SpriteBatch sb);
    }

    internal class SceneManager
    {
        public IScene scene;

        public SceneManager(IScene scene)
        {
            LoadScene(scene);
        }

        public void LoadScene(IScene scene)
        {
            this.scene = scene;
            scene.Load(this);
        }

        public void UpdateScene(float dt)
        {
            this.scene.Update(dt);
        }

        public void DrawScene(SpriteBatch sb)
        {
            this.scene.Draw(sb);
        }
    }
}
