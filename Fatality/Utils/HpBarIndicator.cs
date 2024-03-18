using System;
using EnsoulSharp;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

namespace Fatality.Utils
{
    public class HpBarIndicator
    {
       public static Device dxDevice = Drawing.Direct3DDevice9;
        public static Line dxLine;

        public float hight = 27;
        public float width = 104;


        public HpBarIndicator()
        {
            dxLine = new Line(dxDevice) { Width = 9};

            EnsoulSharp.SDK.Render.OnPreReset += DrawingOnOnPreReset;
            EnsoulSharp.SDK.Render.OnPostReset += DrawingOnOnPostReset;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomainOnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnDomainUnload;
        }

        public AIHeroClient unit { get; set; }

        private Vector2 Offset
        {
            get
            {
                if (unit != null)
                {
                    return unit.IsAlly ? new Vector2(34, 9) : new Vector2(10, 20);
                }

                return new Vector2();
            }
        }

        public Vector2 startPosition
        {
            get { return new Vector2(unit.HPBarPosition.X + Offset.X, unit.HPBarPosition.Y + Offset.Y); }
        }


        private static void CurrentDomainOnDomainUnload(object sender, EventArgs eventArgs)
        {
            dxLine.Dispose();
        }

        private static void DrawingOnOnPostReset(EventArgs args)
        {
            dxLine.OnResetDevice();
        }

        private static void DrawingOnOnPreReset(EventArgs args)
        {
            dxLine.OnLostDevice();
        }


        private float getHpProc(float dmg = 0)
        {
            float health = ((unit.Health - dmg) > 0) ? (unit.Health - dmg) : 0;
            return (health / unit.MaxHealth);
        }

        private Vector2 getHpPosAfterDmg(float dmg)
        {
            float w = getHpProc(dmg) * width;
            return new Vector2(startPosition.X + w, startPosition.Y);
        }

        public void drawDmg(float dmg, ColorBGRA color)
        {
            Vector2 hpPosNow = getHpPosAfterDmg(0);
            Vector2 hpPosAfter = getHpPosAfterDmg(dmg);

            fillHPBar(hpPosNow, hpPosAfter, color);
            
        }

        private void fillHPBar(int to, int from, Color color)
        {
            var sPos = startPosition;
            for (var i = from; i < to; i++)
            {
                Drawing.DrawLine(sPos.X + i, sPos.Y, sPos.X + i, sPos.Y + 9, 1, color);
            }
        }

        private void fillHPBar(Vector2 from, Vector2 to, ColorBGRA color)
        {
            dxLine.Begin();

            dxLine.Draw(new[]
            {
                new Vector2((int)from.X - 55, (int)from.Y - 37),
                new Vector2((int)to.X - 55, (int)to.Y - 37)
            }, color);

            dxLine.End();
        }
    }
}