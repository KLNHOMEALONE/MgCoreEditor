using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework;
using Shared.Engine;

namespace MgCoreEditor.Engine
{
    public class GameObject : ITransformable
    {
        private const float Length = 5f;
        private static long _sLatestAssignedId = 0;
        private Vector3 _forward = Vector3.Forward;
        private Vector3 _up = Vector3.Up;
        public string Name { get; }
        public ulong ID { get; }
        public Transform Transform { get; }

        public BoundingBox BoundingBox =>
            new BoundingBox(Transform.Position - (Vector3.One * Length) * Transform.Scale,
                Transform.Position + (Vector3.One * Length) * Transform.Scale);

        public float? Select(Ray selectionRay)
        {
            return selectionRay.Intersects(BoundingBox);
        }

        public Vector3 Forward
        {
            get => _forward;
            set
            {
                _forward = value;
                _forward.Normalize();
            }
        }

        public Vector3 Up
        {
            get => _up;
            set
            {
                _up = value;
                _up.Normalize();
            }
        }

        public GameObject() : this(Guid.NewGuid().ToString())
        { }

        public GameObject(string name)
        {
            Transform t = new Transform();
            //t.ParentChanged += OnTransformParentChanged;
            //AddComponent(t);
            Transform = t;
            Name = name;
            //InternalConstructed?.Invoke(this);
            ID = GetNextID();
        }

        private ulong GetNextID()
        {
            ulong newId = unchecked((ulong)Interlocked.Increment(ref _sLatestAssignedId));
            Debug.Assert(newId != 0); // Overflow
            return newId;
        }
    }
}