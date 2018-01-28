﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpGL.NewLights
{
    /// <summary>
    /// Spot light.
    /// </summary>
    public class SpotLight : LightBase
    {
        /// <summary>
        /// 
        /// </summary>
        public vec3 Target { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public float Angle { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public float Near { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public float Far { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="target"></param>
        /// <param name="angle">in degree.</param>
        /// <param name="near"></param>
        /// <param name="far"></param>
        /// <param name="attenuation"></param>
        public SpotLight(vec3 position, vec3 target, float angle, float near = 0.1f, float far = 50.0f, Attenuation attenuation = null)
            : base(attenuation == null ? new Attenuation(1.0f, 0.0f, 0.0f) : attenuation)
        {
            if (position == target) { throw new ArgumentException(); }

            this.Position = position;
            this.Target = target;
            this.Angle = angle;
            this.Near = near;
            this.Far = far;
        }

    }
}
