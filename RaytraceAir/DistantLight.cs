﻿using System.Numerics;

namespace RaytraceAir
{
    public class DistantLight : Light
    {
        private readonly Vector3 _direction;

        public DistantLight(Vector3 direction, Vector3 color)
            : base(color)
        {
            _direction = direction;
        }

        public override float GetFalloff(float distance)
        {
            return 1;
        }

        public override float GetDistToLight(Vector3 hitPoint)
        {
            return float.MaxValue;
        }

        public override Vector3 GetDirToLight(Vector3 hitPoint)
        {
            return -_direction;
        }
    }
}