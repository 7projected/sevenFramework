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
}
