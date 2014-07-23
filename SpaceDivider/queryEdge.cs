using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Test_PCL
{
    class queryEdge
    {
        public Point3D s;
        public Point3D t;

        public queryEdge(double sx, double sy, double tx, double ty)
        {
            this.s = new Point3D(sx, sy, 0);
            this.t = new Point3D(tx, ty, 0);
        }
        public Point3D getStart() { return this.s; }
        public Point3D getEnd() { return this.t; }
    }
}
