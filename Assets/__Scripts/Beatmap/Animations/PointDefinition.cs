// Mostly just copied from Heck
using System;
using System.Collections.Generic;
using UnityEngine;

using SimpleJSON;

namespace Beatmap.Animations
{
    public interface IPointDefinition
    {
        public class UntypedParams
        {
            public string key;
            public bool overwrite;
            public JSONNode points;
            public string easing;
            public float time = 0;
            public float transition = 0;
            public float duration = 0;
            public float time_begin;
            public float time_end;
            // TODO: Repeat
        }
    }

    public class PointDefinition<T> : IPointDefinition, IComparable<PointDefinition<T>>
        where T : struct
    {
        public PointData[] Points;
        public float StartTime { get; private set; } = 0;
        // For AnimateTrack
        public float Duration = 0;
        // For AssignPathAnimation
        public float Transition = 0;
        public Func<float, float> Easing;

        public delegate T Parser(JSONArray data, out int i);
        public delegate T InterpolationHandler(PointData[] points, int prev, int next, float time);

        // Used for searching ONLY
        public PointDefinition(float start)
        {
            StartTime = start;
        }

        public PointDefinition(Parser parser, IPointDefinition.UntypedParams p)
        {
            StartTime = p.time;
            Transition = p.transition;
            Duration = p.duration;
            Easing = global::Easing.Named(p.easing ?? "easeLinear");

            var _points = new List<PointData>();
            var data = p.points switch {
                JSONArray arr => arr,
                JSONString pd => (BeatSaberSongContainer.Instance.Map.PointDefinitions.ContainsKey(pd) ? BeatSaberSongContainer.Instance.Map.PointDefinitions[pd] : throw new Exception($"Missing point definition {pd}")),
                _ => new JSONArray(), // TODO: Does this unset properly?
            };

            foreach (var row in data) {
                // WTF, Jevk
                if (row.Value.AsArray == null) {
                    _points.Add(new PointData(parser, data, p.time_begin, p.time_end));
                    break;
                }
                _points.Add(new PointData(parser, row.Value.AsArray, p.time_begin, p.time_end));
            }

            Points = _points.ToArray();
        }

        public T Interpolate(float time)
        {
            var count = Points.Length;

            if (count == 0) {
                return default;
            }

            if (Points[count - 1].Time <= time) {
                return Points[count - 1].Value;
            }

            if (Points[0].Time >= time) {
                return Points[0].Value;
            }

            GetIndexes(time, out int prev, out int next);

            float normalTime;
            float divisor = Points[next].Time - Points[prev].Time;
            if (divisor != 0)
            {
                normalTime = (time - Points[prev].Time) / divisor;
            }
            else
            {
                normalTime = 0;
            }

            normalTime = Points[next].Easing(normalTime);

            return Points[next].Lerp(Points, prev, next, normalTime);
        }

        private void GetIndexes(float time, out int prev, out int next)
        {
            prev = 0;
            next = Points.Length;

            while (prev < next - 1)
            {
                int m = (prev + next) / 2;
                float pointTime = Points[m].Time;

                if (pointTime < time)
                {
                    prev = m;
                }
                else
                {
                    next = m;
                }
            }
        }

        public int CompareTo(PointDefinition<T> other)
        {
            // TODO: might be able to cheese previous/next with this
            return StartTime.CompareTo(other.StartTime);
        }

        public class PointData : IComparable<PointData>
        {
            public PointData(Parser parser, JSONArray data, float tbegin = 0, float tend = 0)
            {
                Value = parser(data, out int i);
                var len = data.Count;
                if (len > i)
                {
                    // Track or Path animation
                    Time = (tend == 0)
                        ? data[i++].AsFloat
                        : Mathf.LerpUnclamped(tbegin, tend, data[i++]);
                }
                else
                {
                    // WTF Jevk
                    Time = 0;
                }
                Easing = global::Easing.Linear;
                Lerp = PointDataInterpolators.LinearLerp<T>;

                for (; i < len; ++i)
                {
                    string str = data[i];
                    if (str[0] == 'e')
                    {
                        Easing = global::Easing.Named(str);
                    }
                    if (str == "splineCatmullRom")
                    {
                        Lerp = PointDataInterpolators.CatmullRomLerp<T>;
                    }
                    if (str == "lerpHSV")
                    {
                        Lerp = PointDataInterpolators.HSVLerp<T>;
                    }
                }
            }

            public T Value { get; }
            public float Time { get; }
            public Func<float, float> Easing { get; }
            public InterpolationHandler Lerp { get; }

            public int CompareTo(PointData other)
            {
                return Time.CompareTo(other.Time);
            }
        }
    }

    public class PointDataInterpolators
    {
        public static T LinearLerp<T>(PointDefinition<T>.PointData[] points, int prev, int next, float time) where T : struct
        {
            return LinearLerpFunc<T>()(points[prev].Value, points[next].Value, time);
        }

        public delegate T LerpFunc<T>(T prev, T next, float time);
        static LerpFunc<float> LinearFloat = new LerpFunc<float>(Mathf.LerpUnclamped);
        static LerpFunc<Color> LinearColor = new LerpFunc<Color>(Color.LerpUnclamped);
        static LerpFunc<Vector3> LinearVector3 = new LerpFunc<Vector3>(Vector3.LerpUnclamped);
        static LerpFunc<Quaternion> LinearQuaternion = new LerpFunc<Quaternion>(Quaternion.SlerpUnclamped);

        public static LerpFunc<T> LinearLerpFunc<T>() where T : struct
        {
            // I hate C#
            return typeof(T) switch
            {
                var n when n == typeof(float) => (LerpFunc<T>)(object)(LinearFloat),
                var n when n == typeof(Color) => (LerpFunc<T>)(object)(LinearColor),
                var n when n == typeof(Vector3) => (LerpFunc<T>)(object)(LinearVector3),
                var n when n == typeof(Quaternion) => (LerpFunc<T>)(object)(LinearQuaternion),
                _ => throw new Exception($"Unhandled LerpFunc for type {typeof(T).Name}"),
            };
        }

        public static T CatmullRomLerp<T>(PointDefinition<T>.PointData[] points, int prev, int next, float time) where T : struct
        {
            return points switch
            {
                PointDefinition<Vector3>.PointData[] v => (T)(object)SmoothVectorLerp(v, prev, next, time),
                _ => LinearLerp<T>(points, prev, next, time),
            };
        }

        public static T HSVLerp<T>(PointDefinition<T>.PointData[] points, int prev, int next, float time) where T : struct
        {
            return points switch
            {
                PointDefinition<Color>.PointData[] colors => (T)(object)HSVColorLerp(colors, prev, next, time),
                _ => LinearLerp<T>(points, prev, next, time),
            };
        }

        public static Vector3 SmoothVectorLerp(PointDefinition<Vector3>.PointData[] points, int a, int b, float time)
        {
            // Catmull-Rom Spline
            Vector3 p0 = a - 1 < 0 ? points[a].Value : points[a - 1].Value;
            Vector3 p1 = points[a].Value;
            Vector3 p2 = points[b].Value;
            Vector3 p3 = b + 1 > points.Length - 1 ? points[b].Value : points[b + 1].Value;

            float tt = time * time;
            float ttt = tt * time;

            float q0 = -ttt + (2.0f * tt) - time;
            float q1 = (3.0f * ttt) - (5.0f * tt) + 2.0f;
            float q2 = (-3.0f * ttt) + (4.0f * tt) + time;
            float q3 = ttt - tt;

            Vector3 c = 0.5f * ((p0 * q0) + (p1 * q1) + (p2 * q2) + (p3 * q3));

            return c;
        }

        public static Color HSVColorLerp(PointDefinition<Color>.PointData[] points, int a, int b, float time)
        {
            Color.RGBToHSV(points[a].Value, out float hl, out float sl, out float vl);
            Color.RGBToHSV(points[b].Value, out float hr, out float sr, out float vr);
            Color lerped = Color.HSVToRGB(Mathf.LerpUnclamped(hl, hr, time), Mathf.LerpUnclamped(sl, sr, time), Mathf.LerpUnclamped(vl, vr, time));
            return new Color(lerped.r, lerped.g, lerped.b, Mathf.LerpUnclamped(points[a].Value.a, points[b].Value.a, time));
        }
    }

    public class PointDataParsers
    {
        public static float ParseFloat(JSONArray data, out int i)
        {
            i = 1;
            return data[0];
        }

        public static Color ParseColor(JSONArray data, out int i)
        {
            i = 4;
            return new Color(data[0], data[1], data[2], data[3]);
        }

        public static Vector3 ParseVector3(JSONArray data, out int i)
        {
            i = 3;
            return new Vector3(data[0], data[1], data[2]);
        }

        public static Quaternion ParseQuaternion(JSONArray data, out int i)
        {
            i = 3;
            return Quaternion.Euler(data[0], data[1], data[2]);
        }
    }
}
