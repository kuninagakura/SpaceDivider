using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
 
namespace Test_PCL
{
    class queryPoint
    {
                //a queryPoint maps a specific point to a list of its incident edges
        public Point3D points;
        public List<queryEdge> edges;
        public queryPoint(Point3D point)
        {
            this.points = point;
            this.edges = new List<queryEdge>();
        }
        public void addEdge(queryEdge edge)
        {
            this.edges.Add(edge);
        }
        public List<queryEdge> getEdges()
        {
            return this.edges;
        }
        public bool Equals(queryPoint other)
        {
            return (this.points.X == other.points.X && this.points.Y == other.points.Y);
        }
    }
}
