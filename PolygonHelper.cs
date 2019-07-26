// <summary>
//   Provides helper methods for forming polygons from lines and triangulation of polygons
// </summary>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace PolygonHelper.PolygonHelper
{
    class PolygonHelper
    {

        /// <summary>
        /// Form closed polygons from lines
        /// </summary>
        /// <param name="pointsInCut"></param>
        /// <param name="neighbourPoints"></param>

        private List<List<Point3D>> FormSegments(List<Point3D> pointsInCut, List<List<int>> neighbourPoints)
        {
            List<bool> vertexVisited = new List<bool>();
            List<List<Point3D>> segments = new List<List<Point3D>>();

            for (int i = 0; i < pointsInCut.Count; i++)
            {
                vertexVisited.Add(false);
            }

            while (vertexVisited.FindIndex(x => x == false) != -1)
            {
                int firstIndex = vertexVisited.FindIndex(x => x == false);
                int curIndex = firstIndex;
                bool againOnStart = false;
                List<Point3D> pointsInPolygon = new List<Point3D>();

                vertexVisited.RemoveAt(firstIndex);
                vertexVisited.Insert(firstIndex, true);

                while (!againOnStart && neighbourPoints[curIndex].Count > 0)
                {
                    pointsInPolygon.Add(pointsInCut[curIndex]);
                    vertexVisited.RemoveAt(curIndex);
                    vertexVisited.Insert(curIndex, true);
                    int nextIndex = neighbourPoints[curIndex][0];

                    if (curIndex != firstIndex)
                    {
                        for (int i = 0; i < neighbourPoints[curIndex].Count; i++)
                        {
                            neighbourPoints[neighbourPoints[curIndex][i]].Remove(curIndex);
                        }
                        neighbourPoints[curIndex].Clear();
                    }
                    else
                    {
                        neighbourPoints[nextIndex].Remove(curIndex);
                        neighbourPoints[curIndex].Remove(nextIndex);
                    }

                    if (nextIndex == firstIndex)
                    {
                        againOnStart = true;
                    }

                    curIndex = nextIndex;
                }

                curIndex = firstIndex;
                while (!againOnStart && neighbourPoints[curIndex].Count > 0)
                {
                    if (curIndex != firstIndex)
                    {
                        pointsInPolygon.Insert(0, pointsInCut[curIndex]);
                    }
                    vertexVisited.RemoveAt(curIndex);
                    vertexVisited.Insert(curIndex, true);
                    int nextIndex = neighbourPoints[curIndex][0];

                    if (curIndex != firstIndex)
                    {
                        for (int i = 0; i < neighbourPoints[curIndex].Count; i++)
                        {
                            neighbourPoints[neighbourPoints[curIndex][i]].Remove(curIndex);
                        }
                        neighbourPoints[curIndex].Clear();
                    }
                    else
                    {
                        neighbourPoints[nextIndex].Remove(curIndex);
                        neighbourPoints[curIndex].Remove(nextIndex);
                    }

                    if (nextIndex == firstIndex)
                    {
                        againOnStart = true;
                    }

                    curIndex = nextIndex;
                }

                if (pointsInPolygon.Count > 3)
                {
                    segments.Add(pointsInPolygon);
                }
            }

            return segments;
        }

        /// <summary>
        /// Divides polygons on triangles and put them into mesh
        /// </summary>
        /// <param name="segments"></param>
        /// Returns mesh representing polygon. Can be used in 3D model to draw polygon

        private MeshGeometry3D CutPolygons(List<List<Point3D>> segments)
        {
            MeshGeometry3D polygonMesh = new MeshGeometry3D();
            foreach (var segment in segments)
            {
                int direction = 0;

                double min = segment.Min(x => x.X);
                int minIndex = segment.FindIndex(x => x.X == min);
                int curIndex = minIndex;

                double xNormal = -segment[minIndex].Y + segment[(minIndex - 1 + segment.Count) % segment.Count].Y;
                double yNormal = segment[minIndex].X - segment[(minIndex - 1 + segment.Count) % segment.Count].X;
                double c = -(xNormal * segment[minIndex].X) - (yNormal * segment[minIndex].Y);
                Point3D thirdPoint = segment[(minIndex + 1) % segment.Count];

                if ((xNormal * thirdPoint.X + yNormal * thirdPoint.Y + c) > 0)
                {
                    direction = 1;
                }
                else
                {
                    direction = -1;
                }

                while (segment.Count > 3)
                {
                    bool breakWhile = false;
                    for (int i = 0; i < segment.Count; i++)
                    {
                        double xN = -segment[curIndex % segment.Count].Y + segment[(curIndex - direction + segment.Count) % segment.Count].Y;
                        double yN = segment[curIndex % segment.Count].X - segment[(curIndex - direction + segment.Count) % segment.Count].X;
                        double cN = -(xN * segment[curIndex % segment.Count].X) - (yN * segment[curIndex % segment.Count].Y);
                        Point3D third = segment[(curIndex + direction + segment.Count) % segment.Count];

                        if ((xN * third.X + yN * third.Y + cN) > 0)
                        {
                            Point3D[] triangle = new Point3D[3];
                            triangle[0] = segment[(curIndex - direction + segment.Count) % segment.Count];
                            triangle[1] = segment[curIndex % segment.Count];
                            triangle[2] = segment[(curIndex + direction + segment.Count) % segment.Count];

                            bool isInside = false;
                            foreach (Point3D point in segment)
                            {
                                double[] pointValue = new double[3];
                                for (int j = 0; j < 3; j++)
                                {
                                    double triangleNormalX = -triangle[(j + 1) % 3].Y + triangle[j].Y;
                                    double triangleNormalY = triangle[(j + 1) % 3].X - triangle[j].X;
                                    double triangleC = -(triangleNormalX * triangle[j].X) - (triangleNormalY * triangle[j].Y);

                                    pointValue[j] = (triangleNormalX * point.X) + (triangleNormalY * point.Y) + triangleC;
                                }

                                if ((pointValue[0] > 0 && pointValue[1] > 0 && pointValue[2] > 0) ||
                                    (pointValue[0] < 0 && pointValue[1] < 0 && pointValue[2] < 0))
                                {
                                    isInside = true;
                                    break;
                                }
                            }

                            if (!isInside)
                            {
                                polygonMesh.Positions.Add(new Point3D(segment[(curIndex - direction + segment.Count) % segment.Count].X,
                                    segment[(curIndex - direction + segment.Count) % segment.Count].Y, segment[(curIndex - direction + segment.Count) % segment.Count].Z));
                                polygonMesh.Positions.Add(new Point3D(segment[(curIndex) % segment.Count].X,
                                    segment[(curIndex) % segment.Count].Y, segment[(curIndex) % segment.Count].Z));
                                polygonMesh.Positions.Add(new Point3D(segment[(curIndex + direction + segment.Count) % segment.Count].X,
                                    segment[(curIndex + direction + segment.Count) % segment.Count].Y, segment[(curIndex + direction + segment.Count) % segment.Count].Z));

                                segment.Remove(segment[curIndex]);
                                break;
                            }
                        }
                        curIndex += direction;
                        curIndex += segment.Count;
                        curIndex = curIndex % segment.Count;

                        if (i == segment.Count - 1) breakWhile = true;
                    }
                    curIndex = 0;
                    if (breakWhile) break;
                }

                if (segment.Count == 3)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        polygonMesh.Positions.Add(new Point3D(segment[i].X, segment[i].Y, segment[i].Z));
                    }
                }
            }

            return polygonMesh;
        }

    }
}
