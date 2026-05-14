using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace sevenFramework
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private int displayHeight = 720;
        private int displayWidth = 1280;
        private float framerate = 60;

        private SpriteFont debugFont;
        private SceneManager sm;
        private TestScene scene;
        private Texture2D pixel;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferHeight = displayHeight;
            _graphics.PreferredBackBufferWidth = displayWidth;
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / framerate);

            _graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            debugFont = Content.Load<SpriteFont>("font");

            scene = new();
            sm = new(Content, GraphicsDevice, scene, debugFont);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed) Exit();

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            sm.UpdateScene(dt, gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Transparent);

            sm.BakeScene(_spriteBatch);
            sm.DrawScene(_spriteBatch);

            base.Draw(gameTime);
        }
    }
}
