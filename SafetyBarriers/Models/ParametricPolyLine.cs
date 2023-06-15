using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyBarriers.Models
{
    public class ParametricPolyLine
    {
        public List<Line> Lines { get; set; }
        public List<Line> LinesOnPlane { get; set; }
        public List<(Line Line, double Start, double Finish)> ParametricPlaneLines { get; set; }

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

            double length = LinesOnPlane.Select(l => l.Length).Sum();
            double restOfLength = length;

            ParametricPlaneLines = new List<(Line Line, double Start, double Finish)>();

            foreach (var line in LinesOnPlane)
            {
                ParametricPlaneLines.Add((line, length - restOfLength, length - restOfLength + line.Length));
                restOfLength = restOfLength - line.Length;
            }
        }

        public bool Intersect(Curve curve, out double parameter)
        {
            parameter = 0;
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
                            parameter = planeLine.Start + normalizedParameter * planeLine.Line.Length;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        public XYZ GetPointOnPolyLine(double parameter, out Line targetLine)
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
