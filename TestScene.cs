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

        SquarePrimitive playerPrimitive = new(new(new(0, 0), new(100, 100), new(0, 0)));
        SquarePrimitive oppPrimitive = new(new(new(500, 500), new(200, 200), new(0, 0)));

        public void Load(SceneManager sm)
        {
            this.sm = sm;
            mh = new();
        }

        public void Update(float dt)
        {
            time += dt;

            MovePlayerPrimitives(dt);

            List<SquarePrimitive> l = new() { playerPrimitive, oppPrimitive };

            foreach(SquarePrimitive prim in l)
            {
                foreach(Polygon poly in prim.polygons)
                {
                    sm.debugManager.AddPolygonToScreen(Color.Green, poly);
                }
            }
        }

        public void MovePlayerPrimitives(float dt)
        {
            Vector2 dir = new(0, 0);
            int rotDir = 0;
            int speed = 500;
            KeyboardState ks = Keyboard.GetState();

            if (ks.IsKeyDown(Keys.Q)) rotDir -= 1;
            if (ks.IsKeyDown(Keys.E)) rotDir += 1;
            if (ks.IsKeyDown(Keys.W)) dir.Y -= 1;
            if (ks.IsKeyDown(Keys.S)) dir.Y += 1;
            if (ks.IsKeyDown(Keys.A)) dir.X -= 1;
            if (ks.IsKeyDown(Keys.D)) dir.X += 1;

            playerPrimitive.transform.position += dir * speed * dt;
            playerPrimitive.transform.rotation.degrees += rotDir * speed * dt;
            
            if (playerPrimitive.Intersects(oppPrimitive.polygons[0]) || playerPrimitive.Intersects(oppPrimitive.polygons[1]))
            {
                if (dir.X > 0)
                {
                    playerPrimitive.transform.position.X = oppPrimitive.Left - playerPrimitive.Width/2; 
                }
                if (dir.X < 0)
                {
                    playerPrimitive.transform.position.X = oppPrimitive.Right + playerPrimitive.Width/2;
                }
                if (dir.Y > 0)
                {
                    playerPrimitive.transform.position.Y = oppPrimitive.Top - playerPrimitive.Height/2;
                }
                if (dir.Y < 0)
                {
                    playerPrimitive.transform.position.Y = oppPrimitive.Bottom + playerPrimitive.Height/2;
                }
            }
        }

        public void Bake(SpriteBatch sb)
        {
        }

        public void Draw(SpriteBatch sb)
        {
            sb.Begin(samplerState: SamplerState.PointClamp);

            sm.debugManager.AddTextToScreen(new Vector2(0, 0), Color.White, $"Draw:{(int)time} @ {(int)sm.frameCounter.CurrentFramerate}");

            sm.debugManager.DrawAllShapes(sb);
            sm.debugManager.DrawAllText(sb);

            sb.End();
        }
    }
}
