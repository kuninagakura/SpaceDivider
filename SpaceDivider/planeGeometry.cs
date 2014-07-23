using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Test_PCL
{
    class planeGeometry
    {
        public Point3DCollection points;
        public double slope;
        public double intersect;
        public planeGeometry(Point3DCollection points, double slope, double intersect)
        {
            this.points = points;
            this.slope = slope;
            this.intersect = intersect;
        }
    }
}
