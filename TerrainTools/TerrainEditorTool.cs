//using Stride.CommunityToolkit.Engine;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;
using System;

namespace Terrain;

[ComponentCategory("Terrain")]
public abstract class TerrainEditorTool : StartupScript
{
    [DataMember(1)]
    public abstract string UIName { get; set; }

    [DataMemberIgnore]
    public TerrainGrid Terrain { get; internal set; }

    [DataMemberIgnore]
    public CameraComponent EditorCamera { get; internal set; }

    [DataMemberIgnore]
    public InputManager EditorInput {  get; internal set; }
    public bool Active { get; set; }

    [DataMemberRange(0, 100)]
    public int Strength { get; set; } = 100;

    public override void Start()
    {
    }

    public virtual void Update(GameTime gameTime) 
    {
        if (EditorCamera is null)
        {
            return;
        }
        if (Terrain is null)
        {
            return;
        }
        if (EditorInput is null)
        {
            return;
        }
        if (EditorInput.IsKeyDown(Keys.LeftAlt))
        {
            Strength = Math.Abs(Strength) * -1;
        }
        else 
        {
            Strength = Math.Abs(Strength);
        }
    }

    protected Int2 ConvertToGridCell(Vector3 worldPosition)
    {
        // Convert world coordinates to grid coordinates
        var gridX = (int)Math.Round(worldPosition.X / Terrain.CellSize);
        var gridZ = (int)Math.Round(worldPosition.Z / Terrain.CellSize);

        return new Int2(gridX, gridZ);
    }
    protected bool IntersectMouseRayWithTerrain(out Vector3 terrainPoint)
    {
        terrainPoint = Vector3.Zero;

        // Get the camera's view-projection matrix
        var viewMatrix = EditorCamera.ViewMatrix;
        var projectionMatrix = EditorCamera.ProjectionMatrix;
        var viewProjectionMatrix = viewMatrix * projectionMatrix;

        // Compute inverse matrix
        Matrix.Invert(ref viewProjectionMatrix, out var invViewProjection);

        // Get screen position (normalized to -1..1)
        // Assuming the camera is attached to a render view with viewport size
        // For simplicity, let's use a standard normalized approach
        var screenPoint = EditorInput.MousePosition;
        
        // Convert screen coordinates to NDC space (-1 to 1)
        // Note: We need to know the actual viewport size. Since we don't have access to it directly,
        // let's assume the game window is 1920x1080 as a fallback.
        // Alternatively, use a more general approach
        var ndcX = (screenPoint.X * 2f) - 1f;
        var ndcY = 1f - (screenPoint.Y * 2f); // Y is inverted in NDC

        // Create points in NDC space at near and far planes
        var nearPoint = new Vector4(ndcX, ndcY, 0f, 1f);
        var farPoint = new Vector4(ndcX, ndcY, 1f, 1f);

        // Transform points to world space
        Vector4.Transform(ref nearPoint, ref invViewProjection, out var worldNear);
        Vector4.Transform(ref farPoint, ref invViewProjection, out var worldFar);

        // Homogeneous division
        if (worldNear.W != 0f) worldNear /= worldNear.W;
        if (worldFar.W != 0f) worldFar /= worldFar.W;

        var rayOrigin = new Vector3(worldNear.X, worldNear.Y, worldNear.Z);
        var rayDirection = Vector3.Normalize(new Vector3(worldFar.X, worldFar.Y, worldFar.Z) - rayOrigin);

        // Intersect with Y=0 plane (terrain height at 0)
        var planeNormal = Vector3.UnitY;
        var planeD = 0f;
        var rayDotNormal = Vector3.Dot(rayDirection, planeNormal);

        if (Math.Abs(rayDotNormal) < 1e-6)
            return false; // No intersection

        var t = -(Vector3.Dot(rayOrigin, planeNormal) + planeD) / rayDotNormal;
        if (t < 0)
            return false; // Intersection is behind the camera

        var hitPoint = rayOrigin + (rayDirection * t);

        // Ensure the point lies within the terrain bounds
        if (hitPoint.X < 0 || hitPoint.X > Terrain.TotalWidth ||
            hitPoint.Z < 0 || hitPoint.Z > Terrain.TotalHeight)
            return false;

        terrainPoint = hitPoint;
        return true;
    }
}
