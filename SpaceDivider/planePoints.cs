using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Test_PCL
{
    class planePoints
    {
        public List<planeGeometry> plane;
        public List<Point3DCollection> segmentpoints;
        //constructor
        public planePoints()
        {
            this.plane = new List<planeGeometry>();
            this.segmentpoints = new List<Point3DCollection>();
        }
        //add plane pl to the list
        public void AddPlane(planeGeometry pl)
        {
            this.plane.Add(pl);
            this.segmentpoints.Add(new Point3DCollection());
            return;
        }

        //add point P to plane pl
        public void AddPointToPlane(planeGeometry pl, Point3D p)
        {
            //go through all planes to find the plane
            for (int i = 0; i < this.plane.Count; i++)
            {
                //find the index of this plane
                if (pl.Equals(this.plane.ElementAt(i)))
                {
                    this.segmentpoints.ElementAt(i).Add(p);
                }
            }
        }
        //true if pl has already been added
        public bool contains(planeGeometry pl)
        {
            return this.plane.Contains(pl);
        }
    }
}
