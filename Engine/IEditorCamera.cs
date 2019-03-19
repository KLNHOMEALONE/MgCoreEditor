using Microsoft.Xna.Framework;

namespace MgCoreEditor.Engine
{
    public interface IEditorCamera
    {
        Transform Transform { get; set; }

        Vector3 Forward { get; set; }

        Vector3 Up { get; set; }

        float FieldOfView { get; set; }

        Vector3 Lookat { get; set; }
    }
}