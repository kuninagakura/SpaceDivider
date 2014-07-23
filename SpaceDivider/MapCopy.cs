using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Drawing;
using Microsoft.Kinect;
using Noise_Removal;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math;
using System.Drawing.Imaging;
using System.Windows;

namespace Test_PCL
{
    class MapCopy
    {
        private int[,] map;
        private bool[,] background;
        private double[,] backgroundDepth;
        private const int RedIndex = 0;
        private const int GreenIndex = 1;
        private const int BlueIndex = 2;
        private const int AlphaIndex = 3;
        private const double threshold = 100.0;

        private Remover noise_remover;
        public int fCount;
        private int start;
        private List<frameBlob> blobFrames = new List<frameBlob>();
        private int t;
        private listOfPaths paths = new listOfPaths();

        private class frameBlob
        {
            public int t;
            public List<personBlob> blobs = new List<personBlob>();
            public frameBlob(int t)
            {
                this.t = t;
            }
            public void addBlob(personBlob b)
            {
                this.blobs.Add(b);
            }

            internal void removeBlobs(List<personBlob> newBlobs)
            {
                foreach (personBlob remove in newBlobs)
                {
                    for (int i = 0; i < this.blobs.Count; i++)
                    {
                        if (this.blobs[i].x == remove.x && this.blobs[i].y == remove.y)
                        {
                            this.blobs.Remove(this.blobs[i]);
                        }
                    }
                }
            }
        }

        private class listOfPaths
        {
            public List<path> paths;
            public listOfPaths()
            {
                this.paths = new List<path>();
            }

            public List<path> getOpenPaths()
            {
                List<path> ret = new List<path>();
                foreach (path p in this.paths)
                {
                    if (p.open)
                        ret.Add(p);
                }
                return ret;
            }

            public void addPath(path p)
            {
                this.paths.Add(p);
                return;
            }

            public void addPointToPath(int id, PointF point)
            {
                for (int i = 0; i < this.paths.Count; i++)
                {
                    if (this.paths[i].ID == id)
                        this.paths[i].addPoint(point);
                }
                return;
            }
            public void closePath(int i)
            {
                this.paths[i].open = false;
            }
        }
        private class path
        {
            public List<PointF> points;
            public bool open;
            public int ID;
            public personBlob blob;
            public path(int ID, personBlob blob)
            {
                this.ID = ID;
                this.blob = blob;
                this.open = false;
                this.points = new List<PointF>();

            }

            public void addPoint(PointF p)
            {
                this.points.Add(p);
                return;
            }
        }
        private class personBlob
        {
            public float x;
            public float y;
            public int? ID;
            public double size;
            public personBlob(float x, float y, double size)
            {
                this.size = size;
                this.x = x;
                this.y = y;
                this.ID = null;
            }
            public void assignID(int id)
            {
                this.ID = id;
            }
        }


        //Constructor takes in the width and height of the map (usually the screen)
        //setupFrames establishes how long the initial set up time is
        //for background calibration
        public MapCopy(int width, int height, int setUpFrames)
        {
            this.map = new int[width, height];
            this.background = new bool[width, height];
            this.backgroundDepth = new double[width, height];
            this.noise_remover = new Remover();
            this.fCount = 0;
            this.start = setUpFrames;
            this.t = 0;
        }

        //mark this pixel as traversed
        public void hit(int x, int y)
        {
            this.map[x, y] += 1;
        }

        //gets the amount of hits this pixel has gotten
        public int getCount(int x, int y)
        {
            return this.map[x, y];
        }

        //sets the background pixel with depth 
        public void setBackground(int x, int y, double depth)
        {
            this.backgroundDepth[x, y] = depth;
            this.background[x, y] = true;
        }

        //returns the recorded background depth of this pixel
        public double getBackgroundDepth(int x, int y)
        {
            return this.backgroundDepth[x, y];
        }

        //is this pixel part of the background?
        public bool isBackground(int x, int y)
        {
            return this.background[x, y];
        }

        public Bitmap RenderPersonBlobMap(short[] depthArray, int dist, int width, int height)
        {
            this.fCount++;
            double depthDist = dist * 25.4;

            depthArray = noise_remover.applyPixelFilter(depthArray, width, height);

            byte[] colorFrame = new byte[width * height * 4];

            System.Threading.Tasks.Parallel.For(0, height, i =>
            {
                // Process each pixel in the row
                for (int j = 0; j < width; j++)
                {
                    var distanceIndex = j + (i * width);
                    var index = distanceIndex * 4;
                    double depth = depthArray[distanceIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;


                    if (depth < depthDist && depth > 0)
                    {
                        colorFrame[index + BlueIndex] = 255;
                        colorFrame[index + GreenIndex] = 0;
                        colorFrame[index + RedIndex] = 0;
                        colorFrame[index + AlphaIndex] = 255;
                    }
                    else
                    {
                        colorFrame[index + BlueIndex] = 0;
                        colorFrame[index + GreenIndex] = 0;
                        colorFrame[index + RedIndex] = 0;
                        colorFrame[index + AlphaIndex] = 255;
                    }


                }

            });
            BitmapSource bmps = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, colorFrame, width * PixelFormats.Bgr32.BitsPerPixel / 8);
            return noise_remover.BitmapFromSource(bmps);
        }

        public Bitmap detectSpaceBlobs(Bitmap arg)
        {
            BitmapData bmpd = arg.LockBits(new System.Drawing.Rectangle(0, 0, arg.Width, arg.Height), ImageLockMode.ReadWrite, arg.PixelFormat);
            BlobCounter bc = new BlobCounter();
            bc.FilterBlobs = true;
            bc.MinHeight = 5;
            bc.MinWidth = 5;

            bc.ProcessImage(bmpd);
            Blob[] blobs = bc.GetObjectsInformation();
            Bitmap blank = new Bitmap(arg.Width, arg.Height);
            arg.Dispose();
            Graphics g = Graphics.FromImage(blank);

            System.Drawing.Pen bluePen = new System.Drawing.Pen(System.Drawing.Color.Blue);
            foreach (Blob b in blobs)
            {
                List<IntPoint> edgepoints = bc.GetBlobsEdgePoints(b);
                PointF[] renderpoints = new PointF[edgepoints.Count];
                // List<PointF> renderpoints = new List<PointF>();
                if (b.Area > 200)
                {
                    foreach (IntPoint p in edgepoints)
                    {
                        g.DrawEllipse(bluePen, p.X, p.Y, 1, 1);
                    }
                }

            }
            bluePen.Dispose();
            g.Dispose();
            return blank;
        }

        public int numberofPeople(short[] depthArray, int dist, int width, int height)
        {
            Bitmap arg = RenderPersonBlobMap(depthArray, dist, width, height);
            this.t++;
            BitmapData bmpd = arg.LockBits(new System.Drawing.Rectangle(0, 0, arg.Width, arg.Height), ImageLockMode.ReadWrite, arg.PixelFormat);
            BlobCounter bc = new BlobCounter();
            bc.FilterBlobs = true;
            bc.MinHeight = 5;
            bc.MinWidth = 5;
            bc.ProcessImage(bmpd);
            Blob[] blobs = bc.GetObjectsInformation();
            return blobs.Length;
        }
        public float[,] coordinatesofPeople(short[] depthArray, int dist, int width, int height)
        {
            Bitmap arg = RenderPersonBlobMap(depthArray, dist, width, height);
            this.t++;
            BitmapData bmpd = arg.LockBits(new System.Drawing.Rectangle(0, 0, arg.Width, arg.Height), ImageLockMode.ReadWrite, arg.PixelFormat);
            BlobCounter bc = new BlobCounter();
            bc.FilterBlobs = true;
            bc.MinHeight = 5;
            bc.MinWidth = 5;
            bc.ProcessImage(bmpd);
            Blob[] blobs = bc.GetObjectsInformation();
            float[,] ret = new float[blobs.Length, 2];
            for (int i = 0; i < blobs.Length; i++)
            {
                ret[i, 0] = blobs[i].CenterOfGravity.X;
                ret[i, 1] = blobs[i].CenterOfGravity.Y;
            }
            return ret;
        }

        public BitmapSource detectPersonBlob(short[] depthArray, int dist, int width, int height, Bitmap background)
        {
            Bitmap arg = RenderPersonBlobMap(depthArray, dist, width, height);
            this.t++;
            BitmapData bmpd = arg.LockBits(new System.Drawing.Rectangle(0, 0, arg.Width, arg.Height), ImageLockMode.ReadWrite, arg.PixelFormat);
            BlobCounter bc = new BlobCounter();
            bc.FilterBlobs = true;
            bc.MinHeight = 5;
            bc.MinWidth = 5;
            bc.ProcessImage(bmpd);
            Blob[] blobs = bc.GetObjectsInformation();

            arg.Dispose();

            Graphics g = Graphics.FromImage(background);
            System.Drawing.Pen bluePen = new System.Drawing.Pen(System.Drawing.Color.Blue);

            frameBlob fb = new frameBlob(t);
            foreach (Blob b in blobs)
            {
                List<IntPoint> edgepoints = bc.GetBlobsEdgePoints(b);
                PointF[] renderpoints = new PointF[edgepoints.Count];

                if (b.Area > 4000)
                {
//                    fb.addBlob(new personBlob(b.CenterOfGravity.X, b.CenterOfGravity.Y, b.Area));
                    g.DrawEllipse(bluePen, b.CenterOfGravity.X, b.CenterOfGravity.Y, 5, 5);
                }
            }
           // this.blobFrames.Add(fb);
            bluePen.Dispose();
            g.Dispose();

            return this.noise_remover.BitmapSourceFromBitmap(background);
        }
        class blobComparer : IComparer<frameBlob>
        {
            public int Compare(frameBlob b1, frameBlob b2)
            {
                return b1.t.CompareTo(b2.t);
            }
        }
        public Bitmap findPaths()
        {
            Bitmap blank = new Bitmap(640, 480);
            Graphics g = Graphics.FromImage(blank);

            System.Drawing.Pen bluePen = new System.Drawing.Pen(System.Drawing.Color.Blue);
            System.Drawing.Pen greenPen = new System.Drawing.Pen(System.Drawing.Color.Green);
            System.Drawing.Pen redPen = new System.Drawing.Pen(System.Drawing.Color.Red);

            //sort by time
            //this.blobFrames.Sort(new blobComparer());

            int idCount = 1;
            //go through List of blobs b frame and find paths
            for (int i = 1; i < this.blobFrames.Count; i++)
            {
                frameBlob prevFrame = blobFrames[i - 1];
                frameBlob currFrame = blobFrames[i];
                int prevBlobCount = prevFrame.blobs.Count;
                int currBlobCount = currFrame.blobs.Count;
                List<personBlob> prevCopy = new List<personBlob>();

                foreach (personBlob b in prevFrame.blobs)
                {
                    personBlob newb = new personBlob(b.x, b.y, b.size);
                    newb.ID = b.ID;
                    prevCopy.Add(newb);
                }
                //if the last frame has the same amount of blobs as the current one
                if (prevBlobCount == currBlobCount)
                {
                    //if only one person, then we just assign it
                    if (prevBlobCount == 1)
                    {
                        //assign this ID to the blob in the current frame
                        int? id = prevFrame.blobs[0].ID;
                        if (id.HasValue)
                        {
                            currFrame.blobs[0].ID = (int)id;
                            //add this point to the path with this ID
                            this.paths.addPointToPath((int)id, new PointF(currFrame.blobs[0].x, currFrame.blobs[0].y));
                        }
                    }
                    //more than one person so we need to assign
                    else
                    {
                        //get assignment values for each of the blobs in the current frame, mapped by the previous frame
                        for (int j = 0; j < currBlobCount; j++)
                        {
                            if (prevCopy.Count > 0)
                            {
                                int index = getAssignment(currFrame.blobs[j], prevCopy);
                                int? id = prevCopy[index].ID;
                                if (id.HasValue)
                                {
                                    prevCopy.Remove(prevCopy[index]);
                                    currFrame.blobs[j].ID = (int)id;
                                    this.paths.addPointToPath((int)id, new PointF(currFrame.blobs[j].x, currFrame.blobs[j].y));
                                }
                            }
                        }
                    }

                }
                //if there are more blobs in the current frame
                else if (prevBlobCount < currBlobCount)
                {
                    //first, we should check to make sure the blob didn't appear out of nowhere
                    //look at position for now, maybe look at change in size

                    //we need to get the indices of the new blob(s) in the current frame
                    List<int> newBlobIndices = getNewBlobs(prevFrame.blobs, currFrame.blobs);

                    //we need to add a new path for new blobs 
                    for (int j = 0; j < currBlobCount; j++)
                    {
                        //if this blob's index is in the indices of new blobs, 
                        //set the blob's ID to a new id and make a new path with this ID and the new blob
                        if (newBlobIndices.Contains(j))
                        {
                            currFrame.blobs[j].ID = idCount;
                            path newPath = new path(idCount, currFrame.blobs[j]);
                            this.paths.addPath(newPath);
                            this.paths.addPointToPath(idCount, new PointF(currFrame.blobs[j].x, currFrame.blobs[j].y));
                            idCount++;
                        }
                        //else, this blob is in the previous frame, so we find the assignment
                        else
                        {
                            if (prevCopy.Count > 0)
                            {
                                int index = getAssignment(currFrame.blobs[j], prevCopy);
                                int? id = prevCopy[index].ID;
                                if (id.HasValue)
                                {
                                    prevCopy.Remove(prevCopy[index]);
                                    currFrame.blobs[j].ID = (int)id;
                                    this.paths.addPointToPath((int)id, new PointF(currFrame.blobs[j].x, currFrame.blobs[j].y));
                                }
                            }


                        }
                    }
                }
                //if there are less blobs in the current frame, then we just add the ones that haven't left
                //to their paths
                else
                {

                    //with a list of old blobs filtered out, we assign to the current blobs
                    for (int j = 0; j < currBlobCount; j++)
                    {
                        int index = getAssignment(currFrame.blobs[j], prevCopy);
                        int? id = prevCopy[index].ID;
                        if (id.HasValue)
                        {
                            prevCopy.Remove(prevCopy[index]);
                            currFrame.blobs[j].ID = (int)id;
                            this.paths.addPointToPath((int)id, new PointF(currFrame.blobs[j].x, currFrame.blobs[j].y));
                        }
                    }

                }


            }
            this.blobFrames.Clear();
            for (int i = 0; i < this.paths.paths.Count; i++)
            {
                path p = this.paths.paths[i];
                for (int j = 1; j < p.points.Count; j++)
                {
                    if (i % 3 == 0)
                        g.DrawLine(bluePen, p.points[j - 1], p.points[j]);
                    else if (i % 3 == 1)
                        g.DrawLine(greenPen, p.points[j - 1], p.points[j]);
                    else
                        g.DrawLine(redPen, p.points[j - 1], p.points[j]);
                }
            }
            bluePen.Dispose();
            g.Dispose();
            return blank;

        }
        //get new blobs compares the prevBlobList and currBlobList and returns the indices of
        //where in the position of the currBlobList we have new Blobs
        private List<int> getNewBlobs(List<personBlob> prevBlobList, List<personBlob> currBlobList)
        {
            List<int> ret = new List<int>();
            for (int i = 0; i < currBlobList.Count; i++)
            {
                if (!prevBlobList.Contains(currBlobList[i]))
                    ret.Add(i);
            }
            return ret;
        }



        //get the assignment for blob, comparing a distance heuristic against
        //all blobs 
        private int getAssignment(personBlob personBlob, List<personBlob> compBlobs)
        {
            int assign = 0;
            double best = double.PositiveInfinity;
            for (int i = 0; i < compBlobs.Count; i++)
            {
                double sizeDiff = Math.Sqrt(Math.Abs((personBlob.size - compBlobs[i].size)));
                if (sizeDiff < 50)
                {
                    double temp = getDist(personBlob, compBlobs[i]);
                    if (temp < best)
                    {
                        best = temp;
                        assign = i;
                        //   assign = (int) compBlobs[i].ID;
                        // compBlobs.Remove(compBlobs[i]);
                    }
                }
            }

            return assign;

        }

        //get value of distance heuristic which is the distnace + size difference
        private double getDist(personBlob personBlob1, personBlob personBlob2)
        {

            double deltaX = personBlob1.x - personBlob2.x;
            double deltaY = personBlob1.y - personBlob2.y;
            //distance formula
            double val = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));

            return val;
        }

        public Bitmap RenderBlobDetectorMap(short[] depthArray, int dist, int width, int height)
        {

            this.fCount++;
            double depthDist = dist * 25.4;
            depthArray = noise_remover.applyPixelFilter(depthArray, width, height);

            byte[] colorFrame = new byte[width * height * 4];

            System.Threading.Tasks.Parallel.For(0, height, i =>
            {
                // Process each pixel in the row
                for (int j = 0; j < width; j++)
                {
                    var distanceIndex = j + (i * width);
                    var index = distanceIndex * 4;
                    double depth = depthArray[distanceIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;


                    int count = this.getCount(j, i);
                    if (depth < depthDist && depth > 0)
                    {
                        if (this.fCount < this.start)
                            this.setBackground(j, i, depth);
                        else
                            this.hit(j, i);
                    }


                    if (count != 0 && this.fCount > this.start)
                    {
                        double backgroundDepth = 0;
                        if (this.isBackground(j, i))
                            backgroundDepth = this.getBackgroundDepth(j, i);
                        bool a = this.isBackground(j, i);
                        if (!this.isBackground(j, i) || (depth > backgroundDepth + threshold || depth < backgroundDepth - threshold))
                        {
                            int r = 150 + (count / 2);
                            int g = 245;
                            if (r > 150)
                            {
                                g -= (count / 2);
                            }
                            int b = 90;
                            int alpha = 50 + count;
                            byte[] _colors = convert_rgba(r, g, b, alpha);
                            if (r > 200)
                            {
                                colorFrame[index + BlueIndex] = _colors[0];
                                colorFrame[index + GreenIndex] = _colors[1];
                                colorFrame[index + RedIndex] = _colors[2];
                                colorFrame[index + AlphaIndex] = _colors[3];
                            }
                            else
                            {
                                colorFrame[index + BlueIndex] = 0;
                                colorFrame[index + GreenIndex] = 0;
                                colorFrame[index + RedIndex] = 0;
                                colorFrame[index + AlphaIndex] = 255;
                            }
                        }
                    }
                    else
                    {
                        colorFrame[index + BlueIndex] = 0;
                        colorFrame[index + GreenIndex] = 0;
                        colorFrame[index + RedIndex] = 0;
                        colorFrame[index + AlphaIndex] = 255;
                    }
                }

            });
            BitmapSource bmps = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, colorFrame, width * PixelFormats.Bgr32.BitsPerPixel / 8);
            Bitmap ret = noise_remover.BitmapFromSource(bmps);
            return ret;
        }

        //method called to render the flowmap.
        //takes the current frame and a distance threshold (to detect movement above a certain height)
        //returns a bitmapsource, which can be displayed on its own 
        //or overlain on another image. 
        public BitmapSource RenderFlowMap(DepthImageFrame frame, int dist)
        {
            this.fCount++;
            double depthDist = dist * 25.4;
            short[] depthArray = new short[frame.PixelDataLength];
            frame.CopyPixelDataTo(depthArray);
            depthArray = noise_remover.applyPixelFilter(depthArray, frame.Width, frame.Height);

            byte[] colorFrame = new byte[frame.Width * frame.Height * 4];

            System.Threading.Tasks.Parallel.For(0, frame.Height, i =>
            {
                // Process each pixel in the row
                for (int j = 0; j < frame.Width; j++)
                {
                    var distanceIndex = j + (i * frame.Width);
                    var index = distanceIndex * 4;
                    double depth = depthArray[distanceIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;


                    int count = this.getCount(j, i);
                    if (depth < depthDist && depth > 0)
                    {
                        if (this.fCount < this.start)
                            this.setBackground(j, i, depth);
                        else
                            this.hit(j, i);
                    }


                    if (count != 0 && this.fCount > this.start)
                    {
                        double backgroundDepth = 0;
                        if (this.isBackground(j, i))
                            backgroundDepth = this.getBackgroundDepth(j, i);
                        bool a = this.isBackground(j, i);
                        if (!this.isBackground(j, i) || (depth > backgroundDepth + threshold || depth < backgroundDepth - threshold))
                        {
                            int r = 150 + (count / 2);
                            int g = 245;
                            if (r > 150)
                            {
                                g -= (count / 2);
                            }
                            int b = 90;
                            int alpha = 50 + count;
                            byte[] _colors = convert_rgba(r, g, b, alpha);
                            colorFrame[index + BlueIndex] = _colors[0];
                            colorFrame[index + GreenIndex] = _colors[1];
                            colorFrame[index + RedIndex] = _colors[2];
                            colorFrame[index + AlphaIndex] = _colors[3];
                        }
                    }
                    else
                    {
                        colorFrame[index + BlueIndex] = 255;
                        colorFrame[index + GreenIndex] = 255;
                        colorFrame[index + RedIndex] = 255;
                        colorFrame[index + AlphaIndex] = 0;
                    }
                }

            });
            BitmapSource bmps = BitmapSource.Create(frame.Width, frame.Height, 96, 96, PixelFormats.Bgra32, null, colorFrame, frame.Width * PixelFormats.Bgr32.BitsPerPixel / 8);

            return bmps;

        }

        //converts rgba ints into a byte[] with rgba within range (0-255)
        private byte[] convert_rgba(int r, int g, int b, int alpha)
        {
            byte[] vals = new byte[4];
            if (r < 0) vals[0] = 0;
            else if (r > 255) vals[0] = 255;
            else vals[0] = (byte)r;

            if (g < 0) vals[1] = 0;
            else if (g > 255) vals[1] = 255;
            else vals[1] = (byte)g;

            if (b < 0) vals[2] = 0;
            else if (b > 255) vals[2] = 255;
            else vals[2] = (byte)b;

            if (alpha < 0) vals[3] = 0;
            else if (alpha > 200) vals[3] = 200;
            else vals[3] = (byte)alpha;
            return vals;

        }
    }
}
