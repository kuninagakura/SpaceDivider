using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using FlowMap;
using Noise_Removal;
using System.Drawing;
using System.Diagnostics;
using Utilities;
namespace Test_PCL
{
    class SpaceModel
    {
        private bool[,] background;//keeps track of which pixels are registered as background
        private double[,] backgroundDepth;//keeps track of the background depth pixels for differencing
        private List<PlaneModel> dividors;//list that contains all the plane dividers

        private int threshold = 1000;//threshold that a pixel needs to break in order to be counted as a new pixel
        //private class instances for finding the virtual spaces/faces
        private SpaceGraph qpList = new SpaceGraph();
        private edgeMap edgemap = new edgeMap();
        private List<Face> faces = new List<Face>();
        private Noise_Removal.Remover nr = new Remover();
        private int lastSpace = 0;
        private spaceTraversal traversalHistory;

        //Constructor
        public SpaceModel(int width, int height)
        {
            this.backgroundDepth = new double[width, height];
            this.background = new bool[width, height];
            this.dividors = new List<PlaneModel>();
            this.traversalHistory = new spaceTraversal();
        }

        //custom compararer for comparing two Point3D points
        class Point3DComparer : IComparer<Point3D>
        {
            public int Compare(Point3D p1, Point3D p2)
            {
                if (p1.X != p2.X)
                    return p1.X.CompareTo(p2.X);
                else
                    return p1.Y.CompareTo(p2.Y);
            }
        }

        //clear dividors of Space Model
        public void clearDivisions()
        {
            this.dividors.Clear();
            return;
        }

        //add a new division to the Space Model
        public void addDivision(PlaneModel plane)
        {
            if (!this.dividors.Contains(plane))
                this.dividors.Add(plane);
            return;
        }

        //set the background pixel of (x, y) as depth
        public void setBackground(int x, int y, double depth)
        {
            this.backgroundDepth[x, y] = depth;
            this.background[x, y] = true;
        }

        //get the background pixel of (x, y)
        public double getBackgroundDepth(int x, int y)
        {
            return this.backgroundDepth[x, y];
        }

        //check if (x, y) is part of the background
        public bool isBackground(int x, int y)
        {
            return this.background[x, y];
        }

 

        //creates faces out of the divisions
        //everytime the divisions are rendered, this function should be called to get the virtual spaces 
        public void setVirtualSpaces()
        {
            //add edges of the box
            //the points list will have a list of all the initial points that we will query (not yet query poitns)
            List<Point3D> points = new List<Point3D>();
            //add 4 corners
            points.Add(new Point3D(0, 0, 0));
            points.Add(new Point3D(0, 240, 0));
            points.Add(new Point3D(320, 0, 0));
            points.Add(new Point3D(320, 240, 0));

            int intersectPoints = 0;
            //go through divisions and find all the intersections between lines and the box
            //we get a list of query points
            double[,] planeXYLines = getPlaneXYLines();
            List<planeGeometry> planes = new List<planeGeometry>();

            //add box planes
            Point3DCollection left = new Point3DCollection();
            left.Add(new Point3D(0, 0, 0));
            left.Add(new Point3D(0, 240, 0));
            planes.Add(new planeGeometry(left, double.PositiveInfinity, 0));
            Point3DCollection right = new Point3DCollection();
            right.Add(new Point3D(320, 0, 0));
            right.Add(new Point3D(320, 240, 0));
            planes.Add(new planeGeometry(right, double.PositiveInfinity, 320));
            Point3DCollection top = new Point3DCollection();
            top.Add(new Point3D(0, 240, 0));
            top.Add(new Point3D(320, 240, 0));
            planes.Add(new planeGeometry(top, 0, 240));
            Point3DCollection bottom = new Point3DCollection();
            bottom.Add(new Point3D(0, 0, 0));
            bottom.Add(new Point3D(320, 0, 0));
            planes.Add(new planeGeometry(bottom, 0, 0));

            //go through dividors and add it to list of planes (which contain all segments in the area)
            for (int i = 0; i < this.dividors.Count; i++)
            {
                double x11 = planeXYLines[i, 0];
                double y11 = planeXYLines[i, 1];
                double x12 = planeXYLines[i, 2];
                double y12 = planeXYLines[i, 3];
                double m1 = util.getSlope(x11, y11, x12, y12);
                double b1 = util.getIntersect(planeXYLines[i, 0], planeXYLines[i, 1], m1);

                //add this divider into our list of planes
                Point3DCollection divPoints = new Point3DCollection();
                divPoints.Add(new Point3D(x11, y11, 0));
                divPoints.Add(new Point3D(x12, y12, 0));

                planes.Add(new planeGeometry(divPoints, m1, b1));
                //get the points where this dividor hits the edges of the box
                //condition where this division hits the y of the box
                if (b1 <= 240 && b1 >= 0)
                {
                    //point where it hits the left (x = 0) and right (x = 319) of the box
                    points.Add(new Point3D(0, b1, 0));
                    points.Add(new Point3D(320, (m1 * (320) + b1), 0));

                }
                //otherwise, the line hits the top (y = 239) and bottom  (y = 0) of the box
                else
                {
                    //vertical lines have an infinite slope
                    if (m1 == double.PositiveInfinity || m1 == double.NegativeInfinity)
                    {
                        points.Add(new Point3D(x11, 0, 0));
                        points.Add(new Point3D(x11, 240, 0));
                    }

                    else
                    {
                        points.Add(new Point3D((-b1 / m1), 0, 0));
                        points.Add(new Point3D(((240 - b1) / m1), 240, 0));
                    }
                }

                //compare with other dividors to find intersections
                for (int j = 0; j < this.dividors.Count; j++)
                {
                    double x21 = planeXYLines[j, 0];
                    double y21 = planeXYLines[j, 1];
                    double x22 = planeXYLines[j, 2];
                    double y22 = planeXYLines[j, 3];

                    double x1 = util.lineIntersectionX(x11, y11, x12, y12, x21, y21, x22, y22);
                    double y1 = util.lineIntersectionY(x11, y11, x12, y12, x21, y21, x22, y22);
                    //only include points in the box
                    if (x1 <= 320 && x1 >= 0 && y1 <= 240 && y1 >= 0)
                    {
                        Point3D temp = new Point3D(x1, y1, 0);
                        if (!points.Contains(temp))
                        {
                            points.Add(temp);
                            intersectPoints++;
                        }
                    }
                }
            }

            //sort points based on custom comparator
            points.Sort(new Point3DComparer());

            //this data structure maps a plane to a point3d collection of query points on the line
            planePoints segmentPoints = new planePoints();

            //now we build the plane-segmentpoints list
            for (int i = 0; i < planes.Count; i++)
            {
                //for every plane, examine the sorted points in the box to see if they're on it
                planeGeometry tempPlane = planes.ElementAt(i);

                double m = tempPlane.slope;
                double b = tempPlane.intersect;
                //build an ordered list of poitns along this plane

                //go through all query points that were found 
                for (int j = 0; j < points.Count; j++)
                {
                    Point3D p = points.ElementAt(j);
                    //if the plane is vertical
                    if (m == double.PositiveInfinity || m == double.NegativeInfinity)
                    {
                        //get the x coordinate
                        double planeX = tempPlane.points.ElementAt(0).X;
                        //p lies on the plane of tempPlane
                        if (p.X == planeX)
                        {
                            if (!segmentPoints.contains(tempPlane))
                            {
                                segmentPoints.AddPlane(tempPlane);
                                segmentPoints.AddPointToPlane(tempPlane, p);
                            }
                            else
                            {
                                segmentPoints.AddPointToPlane(tempPlane, p);
                            }
                        }


                    }
                    //nonvertical case
                    else
                    {
                        double s = ((double)m * p.X) + b;
                        // if p is on the plane add it to the segment list, assocaited to plane tempPlane
                        if (p.Y == s)
                        {
                            if (!segmentPoints.contains(tempPlane))
                            {
                                segmentPoints.AddPlane(tempPlane);
                                segmentPoints.AddPointToPlane(tempPlane, p);
                            }
                            else
                            {
                                segmentPoints.AddPointToPlane(tempPlane, p);
                            }
                        }
                    }
                }
            }

            //make querypoint list (qpList maps query points to incident edges
            //clear previous virtual spaces
            this.qpList.clear();

            //j goes through all the plane-segmentpoints pairs and generate the qplist.
            for (int j = 0; j < segmentPoints.plane.Count; j++)
            {
                //get all the points that lie on this plane's segment
                Point3DCollection sPoints = segmentPoints.segmentpoints.ElementAt(j);
                //k goes through all of the points associated with plane j
                for (int k = 0; k < sPoints.Count; k++)
                {
                    queryPoint currentPoint = new queryPoint(sPoints.ElementAt(k));
                    //first, we add this node to query pointn (the class doesn't add if it's already there)
                    this.qpList.addQueryPoint(currentPoint);

                    //add edge of the first node
                    if (k == 0)
                    {
                        //get the next node 
                        queryPoint nextPoint = new queryPoint(sPoints.ElementAt(k + 1));
                        queryEdge newEdge = new queryEdge(currentPoint.points.X, currentPoint.points.Y, nextPoint.points.X, nextPoint.points.Y);
                        //add first node -> second node edge
                        this.qpList.addQueryEdgeToQueryPoint(currentPoint, newEdge);
                        //add this to the edgeMap with default value false
                        this.edgemap.insertEdge(newEdge);
                    }
                    //add edge of the last node
                    else if (k == sPoints.Count - 1)
                    {
                        //get the second to last node
                        queryPoint nextPoint = new queryPoint(sPoints.ElementAt(k - 1));
                        queryEdge newEdge = new queryEdge(currentPoint.points.X, currentPoint.points.Y, nextPoint.points.X, nextPoint.points.Y);
                        //add first node -> second node edge
                        this.qpList.addQueryEdgeToQueryPoint(currentPoint, newEdge);
                        this.edgemap.insertEdge(newEdge);

                    }
                    //add edges of the middle nodes
                    else
                    {
                        //get node behind it
                        queryPoint prevPoint = new queryPoint(sPoints.ElementAt(k - 1));
                        queryEdge frontEdge = new queryEdge(currentPoint.points.X, currentPoint.points.Y, prevPoint.points.X, prevPoint.points.Y);
                        //get next node
                        queryPoint nextPoint = new queryPoint(sPoints.ElementAt(k + 1));
                        queryEdge backEdge = new queryEdge(currentPoint.points.X, currentPoint.points.Y, nextPoint.points.X, nextPoint.points.Y);

                        //add current node -> next node edge
                        this.qpList.addQueryEdgeToQueryPoint(currentPoint, frontEdge);
                        //add current node -> previous node edge
                        this.qpList.addQueryEdgeToQueryPoint(currentPoint, backEdge);

                    }
                }
            }
            //now that qpList is set, we find faces from the graph that qpList forms
            this.faces = findFaces();
            return;
        }

        private List<Face> findFaces()
        {
            //keeps track of found faces
            List<Face> foundFace = new List<Face>();
            int faceCount = 0;
            //go through each point in the qpList
            for (int i = 0; i < this.qpList.qpointsList.Count; i++)
            {
                //get a point from the list
                queryPoint current = this.qpList.qpointsList.ElementAt(i);
                //go through current point's edgelist and recursively find faces
                for (int j = 0; j < current.edges.Count; j++)
                {
                    //make sure that the edge hasn't been walked yet
                    if (!this.edgemap.walked(current.edges[j]))
                    {
                        List<Point3D> facePoints = new List<Point3D>();
                        //add first two points
                        facePoints.Add(current.edges[j].s);
                        facePoints.Add(current.edges[j].t);
                        facePoints = walkface(facePoints, current.edges[j]);
                        if (facePoints != null)
                        {
                            Face potFace = new Face(facePoints, faceCount);
                            if (!equivalentFaceFound(potFace, foundFace))
                            {
                                foundFace.Add(potFace);
                                faceCount++;
                            }
                        }
                    }
                }
            }

            return foundFace;
        }

        //checks the pot (potential face) against all foundFaces, making pointwise comparisons for equality
        private bool equivalentFaceFound(Face pot, List<Face> foundFace)
        {

            //copy points over and sort
            List<Point3D> copyofPotPoints = new List<Point3D>();
            foreach (Point3D p in pot.points)
            {
                copyofPotPoints.Add(new Point3D(p.X, p.Y, p.Z));
            }
            copyofPotPoints.Sort(new Point3DComparer());
            //go through each f. We only need 1 hit to return true, if no hit, we return false
            foreach (Face f in foundFace)
            {

                //copy points over and sort
                List<Point3D> copyofPoints = new List<Point3D>();
                foreach (Point3D p in f.points)
                {
                    copyofPoints.Add(new Point3D(p.X, p.Y, p.Z));
                }
                copyofPoints.Sort(new Point3DComparer());


                //if they have different number of points, we continue to next one
                if (copyofPotPoints.Count != copyofPoints.Count)
                    continue;
                //assume true
                bool eq = true;
                for (int i = 0; i < copyofPotPoints.Count; i++)
                {
                    //if any point is not equal, then 
                    if (!copyofPotPoints[i].Equals(copyofPoints[i]))
                    {
                        eq = false;
                        break;
                    }

                }
                if (eq)
                    return true;

            }
            return false;
        }

        //recursive function that finds faces. terminates when it comes back to the original point
        private List<Point3D> walkface(List<Point3D> facePoints, queryEdge currentEdge)
        {
            Point3D first = facePoints[0];
            Point3D last = facePoints[facePoints.Count - 1];
            this.edgemap.walk(currentEdge);

            //get the list of edges coming out of this last point
            List<queryEdge> next_edges = this.qpList.getEdgesofPoint(last);
            //get the smallest out of these
            queryEdge smallestEdge = getSmallestAngleEdge(currentEdge, next_edges);
            if (smallestEdge == null)
                return null;

            //if the terminating node of this edge is the first node, then we are done.
            if (smallestEdge.t.X == first.X && smallestEdge.t.Y == first.Y)
            {
                this.edgemap.walked(smallestEdge);
                return facePoints;
            }
            //else, we keep walking so we add the new node into our facepoint list and continue recursively
            else
            {
                facePoints.Add(smallestEdge.t);
                return walkface(facePoints, smallestEdge);
            }

        }

        //gets the edge in next_edges that makes the smallest angle CCW with currentEdge
        private queryEdge getSmallestAngleEdge(queryEdge currentEdge, List<queryEdge> next_edges)
        {
            double smallestangle = 360;
            queryEdge smallest = null;

            for (int i = 0; i < next_edges.Count; i++)
            {
                //don't look at edge if it goes back to the current edge's starting point
                if (next_edges[i].t.X == currentEdge.s.X && next_edges[i].t.Y == currentEdge.s.Y)
                    continue;
                //only look at edges ccw to current edge
                if (util.ccw(currentEdge.s.X, currentEdge.s.Y, next_edges[i].s.X, next_edges[i].s.Y, next_edges[i].t.X, next_edges[i].t.Y) > 0)
                {
                    double temp = angle(currentEdge, next_edges[i]);
                    if (temp < smallestangle)
                    {
                        smallestangle = temp;
                        smallest = next_edges[i];
                    }
                }
            }

            return smallest;
        }

        //calculates angle between two query edges
        private double angle(queryEdge currentEdge, queryEdge queryEdge)
        {
            Point3D p1 = currentEdge.s;
            Point3D p2 = queryEdge.s;
            Point3D p3 = queryEdge.t;

            double a = dist(p1, p2);
            double b = dist(p2, p3);
            double c = dist(p3, p1);
            //law of cosines to get angle
            return Math.Acos((c * c - a * a - b * b) / (2 * a * b));
        }

        //distance between two points
        private double dist(Point3D p1, Point3D p2)
        {
            double deltaX = p2.X - p1.X;
            double deltaY = p2.Y - p1.Y;
            //distance formula
            return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        }

 

        public int checkWithMap(float[,] peopleBlobs)
        {
            List<int> occupiedSpaces = new List<int>();
            if (this.dividors.Count > 0)
            {
                int[] spaces = new int[this.faces.Count + 1];
                for (int i = 0; i < peopleBlobs.GetLength(0); i++)
                {
                    float x = peopleBlobs[i, 0];
                    float y = peopleBlobs[i, 1];
                    int sp = whichSpace((int)x, (int)y);
                    if (sp != 0)
                    {
                        this.faces[sp - 1].hit();
                        this.faces[sp - 1].occupy();
                        this.faces[sp - 1].newPerson((int)x, (int)y);
                        occupiedSpaces.Add(sp - 1);

                    }
                    if (this.lastSpace != sp)
                        traversalHistory.AddFace(sp);
                    this.lastSpace = sp;
                    spaces[sp]++;
                }
                for (int i = 0; i < this.faces.Count; i++)
                {
                    if (!occupiedSpaces.Contains(i))
                        this.faces[i].leave();
                }
                int temp = 0;
                int maxIndex = 0;
                for (int j = 0; j < spaces.Length; j++)
                {
                    if (spaces[j] > temp)
                    {
                        maxIndex = j;
                        temp = spaces[j];
                    }
                }
                return maxIndex;
            }
            return 0;

        }
        //Method to call after initialization phase. This method takes in the new depth
        //pixels as foreground and sees which space has the most pixels in it
        public int check(short[] foregroundDepth)
        {
            //only do this if we have dividors
            if (dividors.Count > 0)
            {
                int i = 0;

                //get x1, y1, x2, y2 of planes
                double[,] planeXYLines = getPlaneXYLines();

                int d = this.dividors.Count;//number of divisions

                int[] spaces = new int[this.faces.Count + 1];
                Parallel.For(0, 240, y =>
                {
                    for (int x = 0; x < 320; x++)
                    {
                        int meshZ = ((ushort)foregroundDepth[x + y * 320]) >> 3;
                        double bd = 0;
                        if (this.isBackground(x, y))
                            bd = this.getBackgroundDepth(x, y);

                        if ((meshZ != 0 && bd != 0) && (!this.isBackground(x, y) || (meshZ < bd - this.threshold || meshZ > bd + this.threshold)))
                        {
                            int thisSpace = whichSpace(x, y - 20);
                            spaces[thisSpace]++;

                        }
                        i++;

                    }

                });
                int c = 50; // how many more pixels one side needs to have over to count
                int temp = 0;
                int maxIndex = 0;
                //find the space with highest count
                for (int j = 0; j < spaces.Length; j++)
                {
                    if (spaces[j] > temp)
                    {
                        maxIndex = j;
                        temp = spaces[j];
                    }
                }

                return maxIndex;

            }
            //if no dividors, return 0
            return 0;

        }

        //given a pixel (x,  y) this method returns the space that it lies in 
        public int whichSpace(int x, int y)
        {

            for (int i = 0; i < this.faces.Count; i++)
            {
                Face f = this.faces[i];
                if (pointInFace(x, y, f))
                {
                    return i + 1;
                }
            }
            return 0;
        }
        //given a pixel (x, y) and face, this method indicates if the point is in the face
        private bool pointInFace(int x, int y, Face f)
        {
            //go through all points in the face and check that it's ccw with point x, y 
            for (int i = 0; i < f.points.Count; i++)
            {
                //if it's the second to last point, then check with the first point
                if (i == f.points.Count - 1)
                {
                    if (util.ccw(f.points[i].X, f.points[i].Y, f.points[0].X, f.points[0].Y, x, y) < 0)
                        return false;
                }
                //otherwise, check with the next point and the pixel
                else
                {
                    if (util.ccw(f.points[i].X, f.points[i].Y, f.points[i + 1].X, f.points[i + 1].Y, x, y) < 0)
                        return false;
                }
            }
            //if none of the ccw conditions hit false, then the point is int he face and we return true
            return true;

        }

        //method that returns the xylines of all the divider planes
        private double[,] getPlaneXYLines()
        {
            int d = 0;
            double[,] planeXYLines = new double[this.dividors.Count, 4];
            foreach (PlaneModel planeModel in this.dividors)
            {
                Point3DCollection planePoints = planeModel.getPoints();
                Point3D p1 = planePoints.ElementAt(0);
                Point3D p2 = planePoints.ElementAt(1);
                Point3D p3 = planePoints.ElementAt(2);

                double x1 = p1.X;
                double y1 = p1.Y;
                double x2 = p2.X;
                double y2 = p2.Y;
                //make sure you get unique points
                if ((x1 == x2) && (y1 == y2))
                {
                    x2 = p3.X;
                    y2 = p3.Y;
                }

                planeXYLines[d, 0] = x1;
                planeXYLines[d, 1] = y1;
                planeXYLines[d, 2] = x2;
                planeXYLines[d, 3] = y2;

                d++;

            }
            return planeXYLines;
        }

        public ImageSource showSpaceFacts(int sp)
        {
            Face f = this.faces[sp - 1];

            Font font = new Font("Helvetica", 20);
            SolidBrush brush = new SolidBrush(System.Drawing.Color.BlueViolet);
            System.Drawing.Pen green = new System.Drawing.Pen(System.Drawing.Color.ForestGreen);
            System.Drawing.Pen azure = new System.Drawing.Pen(System.Drawing.Color.Black);
            Bitmap bmp = new Bitmap(320, 240);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawString("Virtual Space: " + sp.ToString(), font, brush, 0, 0);
                //occupied
                String occupied = "Occupied? " + f.isOccupied();
                g.DrawString(occupied, font, brush, 0, 40);
                //average time spent still
                String hits = "Time Spent: " + f.timeSpent();
                g.DrawString(hits, font, brush, 0, 80);
                //isStill
                String movement = "Movement: " + !f.isStill();
                g.DrawString(movement, font, brush, 0, 120);
                //average time spent still
                String stillTime = "Time Spent Still: " + f.timeStill();
                g.DrawString(stillTime, font, brush, 0, 160);
            }
            return this.nr.BitmapSourceFromBitmap(bmp);
        }

        private List<int> getMostRecentPathTraversal(spaceTraversal history)
        {
            int lastZero = 0;
            List<int> curHist = history.getTraversal();
            List<int> ret = new List<int>();
            for (int i = 0; i < curHist.Count; i++)
            {
                if (curHist[i] == 0)
                    lastZero = i;
            }
            for (int i = lastZero; i < curHist.Count; i++)
            {
                ret.Add(curHist[i]);
            }
            return ret;
        }
        public ImageSource showSpace(int sp)
        {
            List<Point3D> points = this.faces[sp - 1].points;
            PointF[] pointFs = new PointF[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                float x = (float)points[i].X;
                float y = (float)points[i].Y;
                if (x == 320) x = 319;
                if (x == 0) x = 1;
                if (y == 0) y = 1;
                if (y == 240) y = 239;

                pointFs[i] = new PointF(x, y);
            }
            Font font = new Font("Helvetica", 20);
            SolidBrush brush = new SolidBrush(System.Drawing.Color.BlueViolet);
            System.Drawing.Pen green = new System.Drawing.Pen(System.Drawing.Color.ForestGreen);
            System.Drawing.Pen azure = new System.Drawing.Pen(System.Drawing.Color.Black);
            Bitmap bmp = new Bitmap(320, 240);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawLines(azure, pointFs);
                g.DrawLine(azure, pointFs[0], pointFs[pointFs.Length - 1]);
                g.DrawString(sp.ToString(), font, brush, 0, 0);
            }
            return this.nr.BitmapSourceFromBitmap(bmp);

        }
    }
}
