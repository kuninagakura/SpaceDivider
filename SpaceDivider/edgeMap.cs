using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_PCL
{
    class edgeMap
    {

        //maps edges to whether or not we've walked it

        public List<queryEdge> edges;
        public List<bool> edgewalked;
        public edgeMap()
        {
            this.edges = new List<queryEdge>();
            this.edgewalked = new List<bool>();
        }

        //add new edge to the edge map
        public void insertEdge(queryEdge e)
        {
            this.edges.Add(e);
            this.edgewalked.Add(false);
        }

        //mark edge e as walked
        public void walk(queryEdge e)
        {
            for (int i = 0; i < this.edges.Count; i++)
            {
                if (this.edges[i].Equals(e))
                    this.edgewalked[i] = true;
            }

        }
        //check if e has been walked
        public bool walked(queryEdge e)
        {
            for (int i = 0; i < this.edges.Count; i++)
            {
                if (this.edges[i].Equals(e))
                    return this.edgewalked[i];
            }
            return false;
        }


    }
}
