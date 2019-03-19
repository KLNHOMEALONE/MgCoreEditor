using Microsoft.Xna.Framework;

namespace MgCoreEditor.Engine
{
    public class Transform : Component
    {
        private Vector3 _position;
        private Quaternion _rotation;
        private Vector3 _scale;

        public bool HasChanged = true;
        public bool HasMoved;

        public Vector3 Position
        {
            get => _position;

            set
            {
                _position = value;
                HasChanged = true;
                HasMoved = true;
            }
        }

        public Quaternion Rotation
        {
            get => _rotation;
            set => _rotation = value;
        }

        public Vector3 Scale
        {
            get => _scale;
            set => _scale = value;
        }

        public Transform()
        {
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Scale = Vector3.One;
        }
    }
}