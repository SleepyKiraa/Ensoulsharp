using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using EnsoulSharp.SDK.Rendering.Caches;
using Fatality.Utils;
using Fatality.Utils.Oktw;
using SharpDX;

namespace Fatality.Utils
{
    public static class FatalityRenderRing
    {
        private static FontCache TextPen = null;
        private static FontCache TextPen2 = null;
        private static LineCache LinePen = null;
        private static AIHeroClient Me = ObjectManager.Player;

        static FatalityRenderRing()
        {
            TextPen = TextRender.CreateFont(20);
            TextPen2 = TextRender.CreateFont(20, FontCache.DrawFontWeight.Bold, "Tahoma");
            LinePen = LineRender.CreateLine(1, false, true);
        }

        public static void DrawText(String text, float posx, float posy, ColorBGRA color)
        {
            TextPen.Draw(text, new Vector2(posx, posy), color);
        }
        
        public static void DrawText2(String text, Vector2 position, ColorBGRA color)
        {
            TextPen2.Draw(text, position, color);
        }

        private static int getAA(AIBaseClient target)
        {
            return (int)(target.Health / Me.GetAutoAttackDamage(target));
        }

        public static void AALeft()
        {
            foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsVisible && !x.IsDead))
            {
                DrawText2($"{getAA(target)} AA Left", Drawing.WorldToScreen(target.Position), Color.White);
            }
        }

        public static Vector2 To2D(this Vector3 v)
        {
            return new Vector2(v.X, v.Y);
        }

        public static List<Vector2> To2D(this List<Vector3> path)
        {
            return path.Select(point => point.To2D()).ToList();
        }
        
        public static Vector3 To3D(this Vector2 v)
        {
            return new Vector3(v.X, v.Y, ObjectManager.Player.ServerPosition.Z);
        }
        
        public static Vector3 To3D2(this Vector2 v)
        {
            return new Vector3(v.X, v.Y, NavMesh.GetHeightForPosition(v.X, v.Y));
        }

        public static Vector3 To3DWorld(this Vector2 vector)
        {
            return new Vector3(vector.X, vector.Y, NavMesh.GetHeightForPosition(vector.X, vector.Y));
        }
    }
}