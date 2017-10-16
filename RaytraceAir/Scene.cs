﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;

namespace RaytraceAir
{
    public class Scene
    {
        private readonly Camera _camera;
        private readonly List<SceneObject> _sceneObjects;
        private readonly List<Vec3> _lights;

        public Scene(Camera camera, List<SceneObject> sceneObjects, List<Vec3> lights)
        {
            _camera = camera;
            _sceneObjects = sceneObjects;
            _lights = lights;
        }

        public void Render()
        {
           foreach (var pixel in GetPixel())
            {
                var originPrimaryRay = _camera.Position;
                var dir = (_camera.ViewDirection + pixel.X * _camera.RightDirection + pixel.Y * _camera.UpDirection).Normalized();   

                if (Trace(originPrimaryRay, dir, out var hitSceneObject, out var hitPoint))
                {
                    var originShadowRay = hitPoint + hitSceneObject.Normal(hitPoint) * 1e-12;

                    foreach (var light in _lights)
                    {
                        var lightDist = light- hitPoint;
                        var lightDir = lightDist.Normalized();
                        if (!TraceShadow(originShadowRay, lightDir, lightDist.Norm))
                        {
                            var contribution = lightDir.Dot(hitSceneObject.Normal(hitPoint));
                            contribution *= 200;
                            contribution /= 4 * Math.PI * lightDist.Norm * lightDist.Norm;
                            _camera.Pixels[pixel.I, pixel.J] += Vec3.Ones() * Math.Max(0, contribution);
                        }
                    }
                }
                else
                {
                    _camera.Pixels[pixel.I, pixel.J] = new Vec3(0.8, 0.2, 0.3);
                }
            }
        }

        private double deg2rad(double deg)
        {
            return deg * Math.PI / 180;
        }

        private bool Trace(Vec3 origin, Vec3 dir, out SceneObject hitSceneObject, out Vec3 hitPoint)
        {
            hitSceneObject = null;
            hitPoint = null;

            var closestT = double.MaxValue;
            foreach (var sceneObject in _sceneObjects)
            {
                if (sceneObject.Intersects(origin, dir, out var t) && t < closestT)
                {
                    hitSceneObject = sceneObject;
                    hitPoint = origin + t * dir;
                    closestT = t;
                }
            }

            return hitSceneObject != null;
        }

        private bool TraceShadow(Vec3 origin, Vec3 dir, double distToLight)
        {
            SceneObject shadowSphere = null;
            foreach (var sceneObject in _sceneObjects)
            {
                if (sceneObject.Intersects(origin, dir, out var t) && t < distToLight)
                {
                    shadowSphere = sceneObject;
                    break;
                }
            }

            return shadowSphere != null;
        }

        public void Export()
        {
            using (var bmp = new Bitmap(_camera.WidthInPixel, _camera.HeightInPixel, PixelFormat.Format24bppRgb))
            {
                var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                    ImageLockMode.WriteOnly,
                    bmp.PixelFormat);

                for (var j = 0; j < bmp.Height; ++j)
                {
                    for (var i = 0; i < bmp.Width; ++i)
                    {
                        var px = _camera.Pixels[i, j];
                        var col = new[]
                        {
                            (byte) (255 * Clamp(0, 1, px.X)),
                            (byte) (255 * Clamp(0, 1, px.Y)),
                            (byte) (255 * Clamp(0, 1, px.Z))
                        };
                        Marshal.Copy(col, 0, data.Scan0 + (j * bmp.Width + i) * 3, 3);
                    }
                }

                bmp.UnlockBits(data);
                bmp.Save("bmp.bmp");
            }
        }

        private double Clamp(double lo, double hi, double value)
        {
            return Math.Max(lo, Math.Min(hi, value));
        }

        private IEnumerable<Pixel> GetPixel()
        {
            var scale = Math.Tan(deg2rad(_camera.HorizontalFoV * 0.5));
            var aspectRatio = _camera.WidthInPixel / (double) _camera.HeightInPixel;
            for (var j = 0; j < _camera.HeightInPixel; ++j)
            {
                for (var i = 0; i < _camera.WidthInPixel; ++i)
                {
                    var x = (2 * (i + 0.5) / _camera.WidthInPixel - 1) * scale;
                    var y = (1 - 2 * (j + 0.5) / _camera.HeightInPixel) * scale * 1 / aspectRatio;

                    yield return new Pixel {X = x, Y = y, I = i, J = j};
                }
            }
        }
    }

    public struct Pixel
    {
        public double X;
        public double Y;
        public int I;
        public int J;
    }
}