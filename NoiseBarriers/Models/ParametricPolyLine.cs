using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NoiseBarriers.Models
{
    public class ParametricPolyLine
    {
        public List<Line> Lines { get; set; }
        public List<Line> LinesOnPlane { get; set; }
        public List<(Line Line, double Start, double Finish)> ParametricPlaneLines { get; set; }
        public List<(Line Line, double Start, double Finish)> ParametricLines { get; set; }

        public ParametricPolyLine(IEnumerable<Line> lines)
        {
            int countCurves = lines.Count();
            int countIter = lines.Count();

            if (countCurves == 0)
            {
                throw new Exception("Линии не выбраны");
            }

            if (countCurves > 1)
            {
                Line firstUnreverseCurve = null;
                var firstCurve = GetFirstCurve(lines, out firstUnreverseCurve);

                Lines = new List<Line>()
                {
                    firstCurve
                };

                var unsortedCurves = new List<Line>
                {
                    firstCurve,
                    firstUnreverseCurve
                };

                countIter--;

                Line lastCurve = firstCurve;

                while (countIter != 0)
                {
                    foreach (var line in lines)
                    {
                        if (IsNextCurve(lastCurve, line) && !unsortedCurves.Contains(line))
                        {

                            if (IsNeedReverseCurves(lastCurve, line))
                            {
                                Line reverseCurve = line.CreateReversed() as Line;
                                Lines.Add(reverseCurve);
                                unsortedCurves.Add(reverseCurve);
                                unsortedCurves.Add(line);
                                lastCurve = reverseCurve;
                            }
                            else
                            {
                                Lines.Add(line);
                                unsortedCurves.Add(line);
                                lastCurve = line;
                            }
                        }
                    }
                    countIter--;
                }
            }
            else
            {
                Lines = new List<Line> { lines.First() };
            }


            LinesOnPlane = new List<Line>();

            foreach (var line in Lines)
            {
                XYZ startPoint = line.GetEndPoint(0);
                XYZ finishPoint = line.GetEndPoint(1);

                XYZ startPointOnPlane = new XYZ(startPoint.X, startPoint.Y, 0);
                XYZ finishPointOnPlane = new XYZ(finishPoint.X, finishPoint.Y, 0);

                LinesOnPlane.Add(Line.CreateBound(startPointOnPlane, finishPointOnPlane));
            }

            double lengthPlaneLines = LinesOnPlane.Select(l => l.Length).Sum();
            double restOfLengthPlaneLines = lengthPlaneLines;

            ParametricPlaneLines = new List<(Line Line, double Start, double Finish)>();

            foreach (var line in LinesOnPlane)
            {
                ParametricPlaneLines.Add((line, lengthPlaneLines - restOfLengthPlaneLines, lengthPlaneLines - restOfLengthPlaneLines + line.Length));
                restOfLengthPlaneLines = restOfLengthPlaneLines - line.Length;
            }

            double lengthLines = Lines.Select(l => l.Length).Sum();
            double restOfLength = lengthLines;

            ParametricLines = new List<(Line Line, double Start, double Finish)>();

            foreach (var line in Lines)
            {
                ParametricLines.Add((line, lengthLines - restOfLength, lengthLines - restOfLength + line.Length));
                restOfLength = restOfLength - line.Length;
            }
        }

        public bool IntersectAndGetPlaneParameter(Curve curve, out double planeParameter)
        {
            planeParameter = 0;

            foreach (var planeLine in ParametricPlaneLines)
            {
                var result = new IntersectionResultArray();
                var compResult = planeLine.Line.Intersect(curve, out result);
                if (compResult == SetComparisonResult.Overlap)
                {
                    foreach (var elem in result)
                    {
                        if (elem is IntersectionResult interResult)
                        {
                            double normalizedParameter = planeLine.Line.ComputeNormalizedParameter(interResult.UVPoint.U);
                            planeParameter = planeLine.Start + normalizedParameter * planeLine.Line.Length;
                        }
                    }
                    return true;
                }
            }

            return false;
        }

        public bool IntersectAndGetParameter(Curve curve, out double parameter)
        {
            parameter = 0;
            XYZ basePoint = null;

            foreach (var line in ParametricLines)
            {
                XYZ startPoint = line.Line.GetEndPoint(0);
                XYZ endPoint = line.Line.GetEndPoint(1);

                XYZ baseStartPoint = new XYZ(startPoint.X, startPoint.Y, 0);
                XYZ baseEndPoint = new XYZ(endPoint.X, endPoint.Y, 0);

                Line baseLine = Line.CreateBound(baseStartPoint, baseEndPoint);

                var result = new IntersectionResultArray();
                var compResult = baseLine.Intersect(curve, out result);
                if (compResult == SetComparisonResult.Overlap)
                {
                    foreach (var elem in result)
                    {
                        if (elem is IntersectionResult interResult)
                        {
                            basePoint = interResult.XYZPoint;
                        }
                    }

                    Line verticalLine = Line.CreateUnbound(basePoint, XYZ.BasisZ);

                    var intersectionResult = new IntersectionResultArray();
                    var comparResult = line.Line.Intersect(verticalLine, out intersectionResult);
                    if (comparResult == SetComparisonResult.Overlap)
                    {
                        foreach (var elem in intersectionResult)
                        {
                            if (elem is IntersectionResult res)
                            {
                                double normalizedParameter = line.Line.ComputeNormalizedParameter(res.UVPoint.U);
                                parameter = line.Start + normalizedParameter * line.Line.Length;
                            }
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        public XYZ GetPointOnPolyLinePlaneParameter(double parameter, out Line targetLine)
        {
            XYZ point = null;
            targetLine = null;

            foreach (var items in ParametricPlaneLines.Zip(Lines, Tuple.Create))
            {
                if (items.Item1.Start <= parameter && items.Item1.Finish >= parameter)
                {
                    targetLine = items.Item1.Line;
                    double normalized = (parameter - items.Item1.Start) / (items.Item1.Finish - items.Item1.Start);
                    var pointOnPlane = items.Item1.Line.Evaluate(normalized, true);

                    Line verticalLine = Line.CreateUnbound(pointOnPlane, XYZ.BasisZ);

                    IntersectionResultArray interResult;
                    var compResult = verticalLine.Intersect(items.Item2, out interResult);
                    if (compResult == SetComparisonResult.Overlap)
                    {
                        foreach (var elem in interResult)
                        {
                            if (elem is IntersectionResult result)
                            {
                                point = result.XYZPoint;

                                return point;
                            }
                        }
                    }
                }
            }
            return null;
        }

        public XYZ GetPointOnPolyLine(double parameter, out Line targetLine)
        {
            XYZ point = null;
            targetLine = null;

            foreach (var line in ParametricLines)
            {
                if (line.Start <= parameter && line.Finish >= parameter)
                {
                    targetLine = line.Line;
                    double normalized = (parameter - line.Start) / (line.Finish - line.Start);
                    point = line.Line.Evaluate(normalized, true);

                    return point;
                }
            }

            return null;
        }

        private static bool IsNextCurve(Curve curve1, Curve curve2)
        {

            XYZ start1 = curve1.GetEndPoint(0);
            XYZ finish1 = curve1.GetEndPoint(1);

            XYZ start2 = curve2.GetEndPoint(0);
            XYZ finish2 = curve2.GetEndPoint(1);

            if (start1.IsAlmostEqualTo(start2)
                || start1.IsAlmostEqualTo(finish2)
                || finish1.IsAlmostEqualTo(start2)
                || finish1.IsAlmostEqualTo(finish2))
            {
                return true;
            }
            return false;
        }

        private static bool IsNeedReverseCurves(Line curve1, Line curve2)
        {
            XYZ finishPointCurve1 = curve1.GetEndPoint(1);
            XYZ startPointCurve2 = curve2.GetEndPoint(0);

            if (finishPointCurve1.IsAlmostEqualTo(startPointCurve2))
            {
                return false;
            }

            return true;
        }

        private static Line GetFirstCurve(IEnumerable<Line> curves, out Line unreverseCurve)
        {
            unreverseCurve = null;
            bool isNeedReverse = false;
            foreach (var curve in curves)
            {
                XYZ startPoint = curve.GetEndPoint(0);
                XYZ finishPoint = curve.GetEndPoint(1);
                int jointPoints = 0;

                foreach (var currCurve in curves)
                {
                    if (curve != currCurve)
                    {
                        XYZ startPointCurr = currCurve.GetEndPoint(0);
                        XYZ finishPointCurr = currCurve.GetEndPoint(1);

                        if (startPoint.IsAlmostEqualTo(startPointCurr)
                            || startPoint.IsAlmostEqualTo(finishPointCurr)
                            || finishPoint.IsAlmostEqualTo(startPointCurr)
                            || finishPoint.IsAlmostEqualTo(finishPointCurr))
                        {
                            jointPoints++;
                        }
                        if (startPoint.IsAlmostEqualTo(startPointCurr) || startPoint.IsAlmostEqualTo(finishPointCurr))
                        {
                            isNeedReverse = true;
                        }
                    }
                }
                if (jointPoints == 1)
                {
                    unreverseCurve = curve;
                    if (isNeedReverse)
                    {
                        return curve.CreateReversed() as Line;
                    }

                    return curve;
                }
            }

            return null;
        }
    }
}
