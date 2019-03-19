using System;
using Microsoft.Xna.Framework;

namespace MgCoreEditor.Engine
{
    public class PerspectiveCamera : IEditorCamera
    {
        private Vector3 _up = Vector3.Up; //Vector3.UnitZ;
        private Vector3 _forward =  Vector3.Forward; //Vector3.Up;
        private float _fieldOfView = (float)Math.PI / 4;

        public PerspectiveCamera(Vector3 position, Vector3 lookAt)
        {
            Transform = new Transform {Position = position};
            _forward = lookAt - Transform.Position;
            _forward.Normalize();
        }

        public Transform Transform { get; set; }

        public Vector3 Up
        {
            get
            {
                return _up;
            }
            set
            {
                if (_up != value)
                {
                    _up = value;
                    Transform.HasChanged = true;
                }
            }
        }

        public Vector3 Forward
        {
            get
            {
                return _forward;
            }
            set
            {
                if (_forward != value)
                {
                    _forward = value;
                    Transform.HasChanged = true;
                }
            }
        }

        public float FieldOfView
        {
            get { return _fieldOfView; }
            set
            {
                _fieldOfView = value;
                Transform.HasChanged = true;
            }
        }

        public Vector3 Lookat
        {
            get { return Transform.Position + Forward; }
            set
            {
                Forward = value - Transform.Position;
                Forward.Normalize();
            }
        }
    }
}