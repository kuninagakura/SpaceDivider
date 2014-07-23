using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Test_PCL
{
    class SpaceGraph
    {
        //main class that is a list of queryPoint objects
        //each queryPoint has an edgelist associated with it
        //so the queryPointList captures the directed graph constructed
        //from line segments


        public List<queryPoint> qpointsList;
        //constructor
        public SpaceGraph()
        {
            this.qpointsList = new List<queryPoint>();
        }

        //add qp to the qpList
        public void addQueryPoint(queryPoint qp)
        {
            if (!custom_Contains(qp))
                this.qpointsList.Add(qp);
        }

        //add query edge qe to query point qp
        public void addQueryEdgeToQueryPoint(queryPoint qp, queryEdge qe)
        {

            for (int i = 0; i < this.qpointsList.Count; i++)
            {
                queryPoint p = this.qpointsList.ElementAt(i);
                if (custom_Equals(p, qp))
                    this.qpointsList.ElementAt(i).addEdge(qe);
            }
            return;
        }

        //custom equals function for thisn object
        private bool custom_Equals(queryPoint p, queryPoint qp)
        {
            return (p.points.X == qp.points.X && p.points.Y == qp.points.Y);
        }

        //custom contains function for this object
        private bool custom_Contains(queryPoint qp)
        {
            foreach (queryPoint p in this.qpointsList)
            {
                if (custom_Equals(p, qp))
                    return true;
            }
            return false;
        }
        //returns the edges incident to qp
        public List<queryEdge> getEdgesofPoint(Point3D qp)
        {
            for (int i = 0; i < this.qpointsList.Count; i++)
            {
                double x = qp.X;
                double y = qp.Y;
                if (x == this.qpointsList[i].points.X && y == this.qpointsList[i].points.Y)
                {
                    return this.qpointsList[i].edges;
                }
            }
            return null;

        }
        //clears the list
        public void clear()
        {
            this.qpointsList.Clear();
            return;
        }


    }
}
