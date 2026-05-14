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
        SquarePrimitive playerPrimitive = new(new(new(0, 0), new(100, 100), new(0, 0)));

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
            testPolygon.transform.rotation.degrees += dt * 50;
            MovePlayerPrimitives(dt);
        }

        public void MovePlayerPrimitives(float dt)
        {
            Vector2 dir = new(0, 0);
            int rotDir = 0;
            int speed = 100;
            KeyboardState ks = Keyboard.GetState();

            if (ks.IsKeyDown(Keys.Q)) rotDir -= 1;
            if (ks.IsKeyDown(Keys.E)) rotDir += 1;
            if (ks.IsKeyDown(Keys.W)) dir.Y -= 1;
            if (ks.IsKeyDown(Keys.S)) dir.Y += 1;
            if (ks.IsKeyDown(Keys.A)) dir.X -= 1;
            if (ks.IsKeyDown(Keys.D)) dir.X += 1;

            playerPrimitive.transform.position += dir * speed * dt;
            playerPrimitive.transform.rotation.degrees += rotDir * speed * dt;
            
            foreach(Polygon polygon in playerPrimitive.polygons)
            {
                if (polygon.Intersects(testPolygon))
                {
                    sm.debugManager.AddPolygonToScreen(Color.Red, polygon);
                }
                else
                {
                    sm.debugManager.AddPolygonToScreen(Color.Green, polygon);
                }
            }
        }

        public void Bake(SpriteBatch sb)
        {
        }

        public void Draw(SpriteBatch sb)
        {
            sb.Begin(samplerState: SamplerState.PointClamp);

            sm.debugManager.AddTextToScreen(new Vector2(0, 0), Color.White, $"Draw:{(int)time} @ {(int)sm.fps}");

            sm.debugManager.DrawAllShapes(sb);
            sm.debugManager.DrawAllText(sb);

            sb.End();
        }
    }
}
