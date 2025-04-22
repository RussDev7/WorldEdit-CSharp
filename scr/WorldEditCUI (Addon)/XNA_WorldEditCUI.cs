/* 
Copyright (c) 2025 RussDev7

This source is subject to the GNU General Public License v3.0 (GPLv3).
See https://www.gnu.org/licenses/gpl-3.0.html.

THIS PROGRAM IS FREE SOFTWARE: YOU CAN REDISTRIBUTE IT AND/OR MODIFY 
IT UNDER THE TERMS OF THE GNU GENERAL PUBLIC LICENSE AS PUBLISHED BY 
THE FREE SOFTWARE FOUNDATION, EITHER VERSION 3 OF THE LICENSE, OR 
(AT YOUR OPTION) ANY LATER VERSION.

THIS PROGRAM IS DISTRIBUTED IN THE HOPE THAT IT WILL BE USEFUL, 
BUT WITHOUT ANY WARRANTY; WITHOUT EVEN THE IMPLIED WARRANTY OF 
MERCHANTABILITY OR FITNESS FOR A PARTICULAR PURPOSE. SEE THE 
GNU GENERAL PUBLIC LICENSE FOR MORE DETAILS.
*/

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using DNA.Drawing;

using static WorldEdit;

/// <summary>
/// XNA Framework Version
/// 
/// This implementation of WorldEditCUI is specifically designed for the XNA Framework, utilizing 
/// Microsoft.Xna.Framework.Graphics for rendering. The DrawBox method constructs and renders a 
/// wireframe box by calculating its corner points and edges, then leveraging DNA.Drawing's extension 
/// methods to draw lines between points in 3D space.
/// 
/// It Supports:
/// - Toggling the entire overlay on/off  (_enableCLU)
/// - Drawing of a thick outline          (thickLine)
/// - Optional grid overlay on every face (thinLine)
/// 
/// Dependencies:
/// -Microsoft.Xna.Framework
/// -DNA.Drawing
/// </summary>
public class XNA_WorldEditCUI
{
    public static void OutlineSelection(
        GraphicsDevice dev,
        Matrix view,
        Matrix projection,
        Vector3 corner1,
        Vector3 corner2,
        Color? color = null)
    {
        // If the caller didn’t specify a color, use LightCoral.
        var outlineColor = color ?? Color.LightCoral;

        // Draw no outlines if; The CLU is disabled, or if either of the points are invalid.
        if (!_enableCLU || corner1 == null || corner2 == null) return;

        // Find min/max on each axis.
        var min = Vector3.Min(corner1, corner2);
        var max = Vector3.Max(corner1, corner2);

        // Expand max by one whole block so outline is on the outside faces.
        max += Vector3.One; // This +1 pushes it to the far face.

        // Build the 8 corners of the box.
        var corners = new Vector3[8]
        {
            new Vector3(min.X, min.Y, min.Z), // 0: Bottom-near-left.
            new Vector3(max.X, min.Y, min.Z), // 1: Bottom-near-right.
            new Vector3(max.X, min.Y, max.Z), // 2: Bottom-far-right.
            new Vector3(min.X, min.Y, max.Z), // 3: Bottom-far-left.
        
            new Vector3(min.X, max.Y, min.Z), // 4: Top-near-left.
            new Vector3(max.X, max.Y, min.Z), // 5: Top-near-right.
            new Vector3(max.X, max.Y, max.Z), // 6: Top-far-right.
            new Vector3(min.X, max.Y, max.Z), // 7: Top-far-left.
        };

        // List the 12 edges as pairs of indices into the corners array.
        var edges = new (int, int)[]
        {
            // Bottom rectangle.
            (0,1), (1,2), (2,3), (3,0),
            // Top rectangle.
            (4,5), (5,6), (6,7), (7,4),
            // Vertical pillars.
            (0,4), (1,5), (2,6), (3,7)
        };

        // Draw a thicker outline around all edges.
        const float thickLine = 0.06f;
        foreach (var (a, b) in edges)
        {
            var line = new LineF3D(corners[a], corners[b]);
            SolidLineRenderer.DrawSolidLine(dev, view, projection, line, outlineColor, thickLine);
        }
    }

    public static void OutlineSelectionWithGrid(
    GraphicsDevice dev,
    Matrix view,
    Matrix projection,
    Vector3 corner1,
    Vector3 corner2,
    Color? color = null)
    {
        // If the caller didn’t specify a color, use LightCoral.
        var outlineColor = color ?? Color.LightCoral;

        // Draw no outlines if; The CLU is disabled, or if either of the points are invalid.
        if (!_enableCLU || corner1 == null || corner2 == null) return;

        // Find min/max on each axis.
        var min = Vector3.Min(corner1, corner2);
        var max = Vector3.Max(corner1, corner2);

        // Expand max by one whole block so outline is on the outside faces.
        max += Vector3.One; // This +1 pushes it to the far face.

        // Build the 8 corners of the box.
        var corners = new Vector3[8]
        {
            new Vector3(min.X, min.Y, min.Z), // 0: Bottom-near-left.
            new Vector3(max.X, min.Y, min.Z), // 1: Bottom-near-right.
            new Vector3(max.X, min.Y, max.Z), // 2: Bottom-far-right.
            new Vector3(min.X, min.Y, max.Z), // 3: Bottom-far-left.
        
            new Vector3(min.X, max.Y, min.Z), // 4: Top-near-left.
            new Vector3(max.X, max.Y, min.Z), // 5: Top-near-right.
            new Vector3(max.X, max.Y, max.Z), // 6: Top-far-right.
            new Vector3(min.X, max.Y, max.Z), // 7: Top-far-left.
        };

        // List the 12 edges as pairs of indices into the corners array.
        var edges = new (int, int)[]
        {
            // Bottom rectangle.
            (0,1), (1,2), (2,3), (3,0),
            // Top rectangle.
            (4,5), (5,6), (6,7), (7,4),
            // Vertical pillars.
            (0,4), (1,5), (2,6), (3,7)
        };

        // Draw the main thicker outline around all edges.
        const float thickLine = 0.06f;
        foreach (var (a, b) in edges)
        {
            var line = new LineF3D(corners[a], corners[b]);
            SolidLineRenderer.DrawSolidLine(dev, view, projection, line, outlineColor, thickLine);
        }

        // Draw a thinner grid on each wall.
        const float thinLine = 0.02f;
        int x0 = (int)min.X, x1 = (int)max.X;
        int y0 = (int)min.Y, y1 = (int)max.Y;
        int z0 = (int)min.Z, z1 = (int)max.Z;

        // Front/Back faces.
        for (int x = x0; x <= x1; x++)
        {
            // Vertical grid lines.
            SolidLineRenderer.DrawSolidLine(dev, view, projection,
                 new LineF3D(new Vector3(x, y0, z0), new Vector3(x, y1, z0)), outlineColor, thinLine);
            SolidLineRenderer.DrawSolidLine(dev, view, projection,
                 new LineF3D(new Vector3(x, y0, z1), new Vector3(x, y1, z1)), outlineColor, thinLine);
        }
        for (int y = y0; y <= y1; y++)
        {
            // Horizontal grid lines.
            SolidLineRenderer.DrawSolidLine(dev, view, projection,
                 new LineF3D(new Vector3(x0, y, z0), new Vector3(x1, y, z0)), outlineColor, thinLine);
            SolidLineRenderer.DrawSolidLine(dev, view, projection,
                 new LineF3D(new Vector3(x0, y, z1), new Vector3(x1, y, z1)), outlineColor, thinLine);
        }

        // Left/Right faces.
        for (int z = z0; z <= z1; z++)
        {
            // Vertical grid lines.
            SolidLineRenderer.DrawSolidLine(dev, view, projection,
                 new LineF3D(new Vector3(x0, y0, z), new Vector3(x0, y1, z)), outlineColor, thinLine);
            SolidLineRenderer.DrawSolidLine(dev, view, projection,
                 new LineF3D(new Vector3(x1, y0, z), new Vector3(x1, y1, z)), outlineColor, thinLine);
        }
        for (int y = y0; y <= y1; y++)
        {
            // Horizontal grid lines.
            SolidLineRenderer.DrawSolidLine(dev, view, projection,
                 new LineF3D(new Vector3(x0, y, z0), new Vector3(x0, y, z1)), outlineColor, thinLine);
            SolidLineRenderer.DrawSolidLine(dev, view, projection,
                 new LineF3D(new Vector3(x1, y, z0), new Vector3(x1, y, z1)), outlineColor, thinLine);
        }

        // Top/Bottom faces.
        for (int x = x0; x <= x1; x++)
        {
            // Vertical grid lines.
            SolidLineRenderer.DrawSolidLine(dev, view, projection,
                new LineF3D(new Vector3(x, y0, z0), new Vector3(x, y0, z1)), outlineColor, thinLine);
            SolidLineRenderer.DrawSolidLine(dev, view, projection,
                new LineF3D(new Vector3(x, y1, z0), new Vector3(x, y1, z1)), outlineColor, thinLine);
        }
        for (int z = z0; z <= z1; z++)
        {
            // Horizontal grid lines.
            SolidLineRenderer.DrawSolidLine(dev, view, projection,
                new LineF3D(new Vector3(x0, y0, z), new Vector3(x1, y0, z)), outlineColor, thinLine);
            SolidLineRenderer.DrawSolidLine(dev, view, projection,
                new LineF3D(new Vector3(x0, y1, z), new Vector3(x1, y1, z)), outlineColor, thinLine);
        }
    }

    /// <summary>
    /// A drop-in replacement for DNA.Drawing.DrawLine,
    /// but forces both verts to your color.
    /// </summary>
    public static class SolidLineRenderer
    {
        // Reused across calls to avoid reallocating.
        private static readonly VertexPositionColor[] _quadVerts = new VertexPositionColor[6];
        private static BasicEffect _effect;

        /// <summary>
        /// Draws a 3D line with the same color at both endpoints.
        /// </summary>
        public static void DrawSolidLine(
            GraphicsDevice graphicsDevice,
            Matrix view,
            Matrix projection,
            LineF3D line,
            Color color,
            float thickness = 0.06f)
        {
            // Lazy-init effect.
            if (_effect == null)
            {
                _effect = new BasicEffect(graphicsDevice)
                {
                    LightingEnabled = false,
                    TextureEnabled = false,
                    VertexColorEnabled = true
                };
            }

            // Compute camera position from inverse view.
            Matrix invView = Matrix.Invert(view);
            Vector3 camPos = invView.Translation;

            // Find a perp vector: direction of line, and eye-to-line vector.
            Vector3 dir = line.End - line.Start;
            Vector3 toCamera = Vector3.Normalize(camPos - line.Start);
            Vector3 perp = Vector3.Normalize(Vector3.Cross(dir, toCamera));

            // Scale perp by half-thickness.
            Vector3 offset = perp * (thickness * 0.5f);

            // Build quad corner positions.
            Vector3 v0 = line.Start + offset;
            Vector3 v1 = line.Start - offset;
            Vector3 v2 = line.End + offset;
            Vector3 v3 = line.End - offset;

            // Fill out two triangles (6 verts).
            _quadVerts[0] = new VertexPositionColor(v0, color);
            _quadVerts[1] = new VertexPositionColor(v1, color);
            _quadVerts[2] = new VertexPositionColor(v2, color);
            _quadVerts[3] = new VertexPositionColor(v2, color);
            _quadVerts[4] = new VertexPositionColor(v1, color);
            _quadVerts[5] = new VertexPositionColor(v3, color);

            // Set matrices.
            _effect.World = Matrix.Identity;
            _effect.View = view;
            _effect.Projection = projection;

            // Draw.
            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives(
                    PrimitiveType.TriangleList,
                    _quadVerts, 0, 2 // two triangles
                );
            }
        }
    }
}