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

        Sprite sprite;
        //Polygon testPolygon = new(new(100, 100), new(150, 200), new(100, 300));
        Polygon testPolygon = new(
            new List<Vector2>
            {
                new(-25, -100),
                new( 25,    0),
                new(-25,  100)
            }   ,

            new Transform(
                new(100, 100),
                new(1000, 200),
                new(0)
            )
        );

        //Polygon playerPrim_X = new(new(0, 100), new(0, 0), new(100, 0));
        //Polygon playerPrim_Y = new(new(0, 100), new(100, 100), new(100, 0));

        public void Load(SceneManager sm)
        {
            this.sm = sm;
            mh = new();
            sprite = new(sm.textureDictionary["kenny"], new(new(0, 0), new(128, 128), new(0, 0)));
        }

        public void Update(float dt)
        {
            time += dt;
            sm.debugManager.AddPolygonToScreen(Color.Green, testPolygon);
            sprite.transform.rotation.degrees += dt * 20;
            MovePlayerPrimitives(dt);
        }

        public void MovePlayerPrimitives(float dt)
        {
            Vector2 dir = new(0, 0);
            int speed = 100;
            KeyboardState ks = Keyboard.GetState();

            if (ks.IsKeyDown(Keys.W)) dir.Y -= 1;
            if (ks.IsKeyDown(Keys.S)) dir.Y += 1;
            if (ks.IsKeyDown(Keys.A)) dir.X -= 1;
            if (ks.IsKeyDown(Keys.D)) dir.X += 1;

            testPolygon.transform.rotation.degrees += dir.X * dt * 50;

            /*
            playerPrim_X.transform.position += (dir * speed * dt);
            playerPrim_X.transform.position += (dir * speed * dt);

            if (playerPrim_X.Intersects(testPolygon)) sm.debugManager.AddPolygonToScreen(Color.Red, playerPrim_X);
            else sm.debugManager.AddPolygonToScreen(Color.Green, playerPrim_X);
            if (playerPrim_Y.Intersects(testPolygon)) sm.debugManager.AddPolygonToScreen(Color.Red, playerPrim_Y);
            else sm.debugManager.AddPolygonToScreen(Color.Green, playerPrim_Y);
            */
        }

        public void Bake(SpriteBatch sb)
        {
        }

        public void Draw(SpriteBatch sb)
        {
            sb.Begin(samplerState: SamplerState.PointClamp);

            sm.debugManager.AddTextToScreen(new Vector2(0, 0), Color.White, $"Draw:{(int)time} @ {(int)sm.fps}");
            sprite.Draw(sb);

            sm.debugManager.DrawAllShapes(sb);
            sm.debugManager.DrawAllText(sb);
            sb.End();
        }
    }
}
