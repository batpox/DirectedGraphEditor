using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectedGraphCore.Geometry;

public static class GeometryHelpers
{

    // helper (somewhere in Core, e.g., Geometry.Epsilon)
    public static bool NearEquals(Point3 a, Point3 b, float eps = 1e-3f)
        => MathF.Abs(a.X - b.X) <= eps
        && MathF.Abs(a.Y - b.Y) <= eps
        && MathF.Abs(a.Z - b.Z) <= eps;

}
