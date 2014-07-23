using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Diagnostics;

namespace Test_PCL
{
    class Face
    {
         public List<Point3D> points;
            public int ID;
            public int hits;
            public bool occupied = false;
            private Stopwatch timer;
            private Stopwatch stillnesstimer;
            public bool still = true;
            private int lastX = 0;
            private int lastY = 0;

            public Face(List<Point3D> p, int id)
            {
                this.points = p;
                this.ID = id;
                this.hits = 0;
                timer = new Stopwatch();
                stillnesstimer = new Stopwatch();
            }
            public int getID() { return this.ID; }
            public int getHits() { return this.hits; }
            public void hit() { this.hits++; }


            internal bool isOccupied()
            {
                return this.occupied;
            }

            internal void occupy()
            {
                this.occupied = true;
                this.timer.Start();
            }
            public void leave()
            {
                this.occupied = false;
                this.timer.Stop();
                this.stillnesstimer.Stop();
            }
            public String timeSpent()
            {
                int secs = (int)this.timer.Elapsed.TotalSeconds % 60;
                String secsString = secs.ToString();
                if (secs < 10)
                    secsString = "0" + secsString;
                String min = ((int)this.timer.Elapsed.TotalMinutes).ToString();
                return min + ":" + secsString;
            }

            public String timeStill()
            {

                int secs = (int)this.stillnesstimer.Elapsed.TotalSeconds % 60;
                String secsString = secs.ToString();
                if (secs < 10)
                    secsString = "0" + secsString;
                String min = ((int)this.stillnesstimer.Elapsed.TotalMinutes).ToString();
                return min + ":" + secsString;


            }
            public void newPerson(int x, int y)
            {
                int xDiff = Math.Abs(x - this.lastX);
                int yDiff = Math.Abs(y - this.lastY);
                double dist = Math.Sqrt(xDiff * xDiff + yDiff * yDiff);
                if (dist < 10)
                {
                    this.stillnesstimer.Start();
                    this.still = true;
                }
                else
                {
                    this.stillnesstimer.Stop();
                    this.still = false;
                }
                this.lastX = x;
                this.lastY = y;
            }

            public bool isStill()
            {
                return this.still;
            }
    }
}
