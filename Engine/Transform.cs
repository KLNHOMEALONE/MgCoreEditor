using Microsoft.Xna.Framework;

namespace MgCoreEditor.Engine
{
    public class Transform : Component
    {
        public Vector3 Position { get; set; }

        public Quaternion Rotation { get; set; }

        public Vector3 Scale { get; set; }

        public Transform()
        {
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Scale = Vector3.One;
        }
    }
}