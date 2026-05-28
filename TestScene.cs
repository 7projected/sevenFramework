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

        SquarePrimitive oppPrimitive = new(new(new(500, 500), new(200, 200), new(0, 0)));
        BasicPlayer player;

        public void Load(SceneManager sm)
        {
            this.sm = sm;
            mh = new();

            player = new(new Transform(new Vector2(100, 100), new Vector2i(100, 100), new Rotation(0, 0)), 500);
        }

        public void Update(float dt)
        {
            time += dt;

            player.Update(oppPrimitive.polygons, dt);

            List<SquarePrimitive> l = new() { player.squarePrimitive, oppPrimitive };
            foreach(SquarePrimitive prim in l)
            {
                foreach(Polygon poly in prim.polygons)
                {
                    sm.debugManager.AddPolygonToScreen(Color.Green, poly);
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
