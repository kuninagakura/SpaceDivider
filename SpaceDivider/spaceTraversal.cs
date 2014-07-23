using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_PCL
{
    class spaceTraversal
    {

        List<int> facesTraversed;
        public spaceTraversal()
        {
            this.facesTraversed = new List<int>();
        }
        public void AddFace(int f)
        {
            this.facesTraversed.Add(f);
        }
        public List<int> getTraversal()
        {
            return this.facesTraversed;
        }

    }
}
