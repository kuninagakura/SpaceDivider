using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Media;

namespace Test_PCL
{
    class PlaneModel
    {
        public GeometryModel3D plane = new GeometryModel3D();
        private Color planeColor = Colors.Aqua;
        private Point3DCollection points = new Point3DCollection();
        private int crosses;
        public PlaneModel(GeometryModel3D plane, Color planeColor, Point3DCollection points)
        {
            this.plane = plane;
            this.planeColor = planeColor;
            this.points = points;
            this.crosses = 0;
        }

        public Point3DCollection getPoints()
        {
            return this.points;
        }
        public void cross()
        {
            this.crosses++;
        }
        public int getCrosses()
        {
            return this.crosses;
        }
    }
}
