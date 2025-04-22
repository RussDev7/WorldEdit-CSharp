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

using UnityEngine;

using static WorldEdit;

/// <summary>
/// Unity Version
/// 
/// This implementation of WorldEditCUI is designed for Unity, utilizing
/// UnityEngine.GL for rendering quads as thick 3D lines. Attach this script
/// to a GameObject and configure the corners, color, and toggles in the Inspector.
/// 
/// It Supports:
/// - Toggling the entire overlay on/off  (_enableCLU)
/// - Drawing of a thick outline          (thickLine)
/// - Optional grid overlay on every face (thinLine)
/// </summary>
[ExecuteInEditMode]
public class Unity_WorldEditCUI : MonoBehaviour
{
    [Header("Selection Corners")]
    public Vector3 corner1;
    public Vector3 corner2;

    [Header("Rendering Options")]
    public Color lineColor = Color.red;
    public bool drawGrid = true;
    public float thickLine = 0.06f;
    public float thinLine = 0.02f;

    private Material lineMaterial;

    void Awake()
    {
        // Use Unity's built-in colored shader.
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        lineMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        // Enable alpha blending and disable backface culling & depth writes.
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        lineMaterial.SetInt("_ZWrite", 0);
    }

    void OnRenderObject()
    {
        // Draw no outlines if; The CLU is disabled, or if either of the points are invalid.
        if (!_enableCLU || corner1 == null || corner2 == null) return;

        Camera cam = Camera.current;
        if (cam == null) return;

        lineMaterial.SetPass(0);
        GL.PushMatrix();
        GL.MultMatrix(cam.worldToCameraMatrix);
        GL.LoadProjectionMatrix(cam.projectionMatrix);

        DrawOutline(cam);
        if (drawGrid) DrawGrid(cam);

        GL.PopMatrix();
    }

    void DrawOutline(Camera cam)
    {
        // Find min/max on each axis.
        Vector3 min = Vector3.Min(corner1, corner2);
        Vector3 max = Vector3.Max(corner1, corner2);

        // Expand max by one whole block so outline is on the outside faces.
        max += Vector3.one; // This +1 pushes it to the far face.

        // Build 8 box corners.
        Vector3[] corners = new Vector3[8]
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, max.y, max.z),
            new Vector3(min.x, max.y, max.z)
        };

        // 12 edges defined by corner indices.
        int[,] edges = new int[,]
        {
            {0,1},{1,2},{2,3},{3,0},
            {4,5},{5,6},{6,7},{7,4},
            {0,4},{1,5},{2,6},{3,7}
        };

        // Draw a thicker outline around all edges.
        for (int i = 0; i < edges.GetLength(0); i++)
        {
            DrawSolidLineGL(corners[edges[i, 0]], corners[edges[i, 1]], thickLine, lineColor, cam);
        }
    }

    void DrawGrid(Camera cam)
    {
        // Find min/max on each axis.
        Vector3 min = Vector3.Min(corner1, corner2);
        Vector3 max = Vector3.Max(corner1, corner2);

        // Expand max by one whole block so outline is on the outside faces.
        max += Vector3.one; // This +1 pushes it to the far face.

        int x0 = Mathf.FloorToInt(min.x), x1 = Mathf.FloorToInt(max.x);
        int y0 = Mathf.FloorToInt(min.y), y1 = Mathf.FloorToInt(max.y);
        int z0 = Mathf.FloorToInt(min.z), z1 = Mathf.FloorToInt(max.z);

        // Front/Back faces.
        for (int x = x0; x <= x1; x++)
        {
            // Vertical grid lines.
            DrawSolidLineGL(new Vector3(x, y0, z0), new Vector3(x, y1, z0), thinLine, lineColor, cam);
            DrawSolidLineGL(new Vector3(x, y0, z1), new Vector3(x, y1, z1), thinLine, lineColor, cam);
        }
        for (int y = y0; y <= y1; y++)
        {
            // Horizontal grid lines.
            DrawSolidLineGL(new Vector3(x0, y, z0), new Vector3(x1, y, z0), thinLine, lineColor, cam);
            DrawSolidLineGL(new Vector3(x0, y, z1), new Vector3(x1, y, z1), thinLine, lineColor, cam);
        }
        // Left/Right faces.
        for (int z = z0; z <= z1; z++)
        {
            // Vertical grid lines.
            DrawSolidLineGL(new Vector3(x0, y0, z), new Vector3(x0, y1, z), thinLine, lineColor, cam);
            DrawSolidLineGL(new Vector3(x1, y0, z), new Vector3(x1, y1, z), thinLine, lineColor, cam);
        }
        for (int y = y0; y <= y1; y++)
        {
            // Horizontal grid lines.
            DrawSolidLineGL(new Vector3(x0, y, z0), new Vector3(x0, y, z1), thinLine, lineColor, cam);
            DrawSolidLineGL(new Vector3(x1, y, z0), new Vector3(x1, y, z1), thinLine, lineColor, cam);
        }
        // Top/Bottom faces.
        for (int x = x0; x <= x1; x++)
        {
            // Vertical grid lines.
            DrawSolidLineGL(new Vector3(x, y0, z0), new Vector3(x, y0, z1), thinLine, lineColor, cam);
            DrawSolidLineGL(new Vector3(x, y1, z0), new Vector3(x, y1, z1), thinLine, lineColor, cam);
        }
        for (int z = z0; z <= z1; z++)
        {
            // Horizontal grid lines.
            DrawSolidLineGL(new Vector3(x0, y0, z), new Vector3(x1, y0, z), thinLine, lineColor, cam);
            DrawSolidLineGL(new Vector3(x0, y1, z), new Vector3(x1, y1, z), thinLine, lineColor, cam);
        }
    }

    static void DrawSolidLineGL(Vector3 start, Vector3 end, float thickness, Color color, Camera cam)
    {
        Vector3 dir = end - start;
        Vector3 toCam = (cam.transform.position - start).normalized;
        Vector3 perp = Vector3.Cross(dir, toCam).normalized * (thickness * 0.5f);

        // Four corners of the quad.
        Vector3 v0 = start + perp;
        Vector3 v1 = start - perp;
        Vector3 v2 = end + perp;
        Vector3 v3 = end - perp;

        // Draw quad with GL.
        GL.Begin(GL.QUADS);
        GL.Color(color);
        GL.Vertex(v0);
        GL.Vertex(v1);
        GL.Vertex(v3);
        GL.Vertex(v2);
        GL.End();
    }
}