using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;
using System.Collections.ObjectModel;
using SafetyBarriers.Models;
using System.IO;
using System.Windows.Controls;
using Autodesk.Revit.DB.Structure;

namespace SafetyBarriers
{
    public class RevitModelForfard
    {
        private UIApplication Uiapp { get; set; } = null;
        private Application App { get; set; } = null;
        private UIDocument Uidoc { get; set; } = null;
        private Document Doc { get; set; } = null;

        public RevitModelForfard(UIApplication uiapp)
        {
            Uiapp = uiapp;
            App = uiapp.Application;
            Uidoc = uiapp.ActiveUIDocument;
            Doc = uiapp.ActiveUIDocument.Document;
        }

        #region Ось барьерного ограждения
        public ParametricPolyLine BarrierAxis { get; set; }

        private string _barrierAxisElemIds;
        public string BarrierAxisElemIds
        {
            get => _barrierAxisElemIds;
            set => _barrierAxisElemIds = value;
        }

        public void GetBarrierAxis()
        {
            var curves = RevitGeometryUtils.GetBarrierAxisCurves(Uiapp, out _barrierAxisElemIds).OfType<Line>();
            BarrierAxis = new ParametricPolyLine(curves);
        }
        #endregion

        #region Граница барьерного ограждения 1
        public Curve BoundCurve1 { get; set; }

        private string _boundCurveId1;
        public string BoundCurveId1
        {
            get => _boundCurveId1;
            set => _boundCurveId1 = value;
        }

        public void GetBoundCurve1()
        {
            BoundCurve1 = RevitGeometryUtils.GetBoundCurve(Uiapp, out _boundCurveId1);
        }
        #endregion

        #region Граница барьерного ограждения 2
        public Curve BoundCurve2 { get; set; }

        private string _boundCurveId2;
        public string BoundCurveId2
        {
            get => _boundCurveId2;
            set => _boundCurveId2 = value;
        }

        public void GetBoundCurve2()
        {
            BoundCurve2 = RevitGeometryUtils.GetBoundCurve(Uiapp, out _boundCurveId2);
        }
        #endregion

        #region Получение названиий семейств для стоек барьерного ограждения
        public ObservableCollection<FamilySymbolSelector> GetPostFamilySymbolNames()
        {
            var familySymbols = RevitFamilyUtils.GetFamilySymbolNames(Doc, BuiltInCategory.OST_GenericModel);
            return familySymbols;
        }
        #endregion

        #region Получение названиий семейств для полотен барьерного ограждения
        public ObservableCollection<FamilySymbolSelector> GetBeamFamilySymbolNames()
        {
            var familySymbols = RevitFamilyUtils.GetFamilySymbolNames(Doc, BuiltInCategory.OST_StructuralFraming);
            return familySymbols;
        }
        #endregion

        #region Параметр стоек границы барьерного ограждения 1
        private double _boundPostParameter1;
        #endregion

        #region Параметр стоек границы барьерного ограждения 2
        private double _boundPostParameter2;
        #endregion

        #region Параметр границ полотна ограждения 1
        private double _boundBeamParameter1;
        #endregion

        #region Параметр границ полотна ограждения 2
        private double _boundBeamParameter2;
        #endregion

        #region Положение стоек барьерного ограждения
        private List<(XYZ Point, double Rotation)> _postLocations = new List<(XYZ Point, double Rotation)>();
        #endregion

        #region Положение полотен барьерного ограждения
        private List<(List<Curve> Lines, FamilySymbolSelector FamilySymbol)> _beamLocations = new List<(List<Curve> Lines, 
                                                                                                        FamilySymbolSelector FamilySymbol)>();
        #endregion

        #region Получение парметров границ барьерного ограждения
        public void GetBoundParameters()
        {
            BarrierAxis.IntersectAndGetPlaneParameter(BoundCurve1, out _boundPostParameter1);
            BarrierAxis.IntersectAndGetPlaneParameter(BoundCurve2, out _boundPostParameter2);

            BarrierAxis.IntersectAndGetParameter(BoundCurve1, out _boundBeamParameter1);
            BarrierAxis.IntersectAndGetParameter(BoundCurve2, out _boundBeamParameter2);
        }
        #endregion

        #region Получение положения стоек барьерного ограждения
        public void GetLocationPostFamilyInstances(bool isRotateOn180,
                                              string alignment,
                                              bool isIncludeStart,
                                              bool isIncludeFinish,
                                              double postStep)
        {
            var pointParameters = GenerateParameters(_boundPostParameter1,
                                                     _boundPostParameter2,
                                                     postStep,
                                                     alignment,
                                                     isIncludeStart,
                                                     isIncludeFinish);

            foreach (double parameter in pointParameters)
            {
                Line targetLine;
                XYZ point = BarrierAxis.GetPointOnPolyLinePlaneParameter(parameter, out targetLine);
                XYZ lineVector = targetLine.GetEndPoint(0) - targetLine.GetEndPoint(1);
                double rotationAngle = lineVector.AngleTo(XYZ.BasisY);
                rotationAngle = RotatePost(rotationAngle, lineVector, isRotateOn180);
                _postLocations.Add((point, rotationAngle));
            }
        }
        #endregion

        #region Получения положения полотна барьерного ограждения
        public void GetLocationBeamFamilyInstances(bool isRotateOn180,
                                                   string alignment,
                                                   IEnumerable<BeamSetup> beamSetups,
                                                   double beamLength)
        {
            var pointParameters = GenerateParameters(_boundBeamParameter1, _boundBeamParameter2, beamLength, alignment, true, true);
            var beamLocationsOnAxis = new List<(XYZ Point, XYZ VectorX)>();

            foreach (double parameter in pointParameters)
            {
                Line targetLine;
                XYZ point = BarrierAxis.GetPointOnPolyLine(parameter, out targetLine);
                XYZ normal = targetLine.GetEndPoint(1) - targetLine.GetEndPoint(0);

                XYZ vectorX = normal.CrossProduct(XYZ.BasisZ).Normalize();
                if(isRotateOn180)
                {
                    vectorX = vectorX.Negate();
                }

                beamLocationsOnAxis.Add((point, vectorX));
            }

            foreach(var beamSetup in beamSetups)
            {
                double offsetX = UnitUtils.ConvertToInternalUnits(beamSetup.OffsetX, UnitTypeId.Meters);
                double offsetZ = UnitUtils.ConvertToInternalUnits(beamSetup.OffsetZ, UnitTypeId.Meters);

                var beamPoints = new List<XYZ>();
                foreach(var location in beamLocationsOnAxis)
                {
                    beamPoints.Add(location.Point + location.VectorX * offsetX + XYZ.BasisZ * offsetZ);
                }

                var pointLines = GetPairs(beamPoints);

                var lines = new List<Curve>();
                foreach(var points in pointLines)
                {
                    Line beamLine = Line.CreateBound(points.Item1, points.Item2);
                    lines.Add(beamLine);
                }

                _beamLocations.Add((lines, beamSetup.FamilyAndSymbolName));
            }
        }
        #endregion

        #region Создание барьерного ограждения
        public void CreateSafetyBarrier(FamilySymbolSelector postFamilyAndSymbolName, bool isReverseBeams)
        {
            FamilySymbol postFSymbol = RevitFamilyUtils.GetFamilySymbolByName(Doc, postFamilyAndSymbolName);

            var level = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Levels).Where(e => e.Name == "Уровень 1").First() as Level;

            using (Transaction trans = new Transaction(Doc, "Created Safety Barrier"))
            {
                trans.Start();
                if (!postFSymbol.IsActive)
                {
                    postFSymbol.Activate();
                }

                foreach (var location in _postLocations)
                {
                    FamilyInstance postFamilyInstance = Doc.Create.NewFamilyInstance(location.Item1, postFSymbol, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                    postFamilyInstance.Location.Rotate(Line.CreateUnbound(location.Point, XYZ.BasisZ), location.Rotation);
                }

                foreach (var location in _beamLocations)
                {
                    FamilySymbol beamFSymbol = RevitFamilyUtils.GetFamilySymbolByName(Doc, location.FamilySymbol);
                    if (!beamFSymbol.IsActive)
                    {
                        beamFSymbol.Activate();
                    }

                    foreach(var line in location.Lines)
                    {
                        FamilyInstance beamFamilyInstance = Doc.Create.NewFamilyInstance(line,
                                                                 beamFSymbol,
                                                                 level,
                                                                 Autodesk.Revit.DB.Structure.StructuralType.Beam);
                        StructuralFramingUtils.DisallowJoinAtEnd(beamFamilyInstance, 0);
                        StructuralFramingUtils.DisallowJoinAtEnd(beamFamilyInstance, 1);

                        if(isReverseBeams)
                        {
                            beamFamilyInstance.flipFacing();
                        }
                    }
                }

                trans.Commit();
            }
        }
        #endregion

        #region Генератор параметров на полилинии
        private List<double> GenerateParameters(double bound1,
                                                double bound2,
                                                double step,
                                                string alignment,
                                                bool isIncludeStart,
                                                bool isIncludeFinish)
        {
            var parameters = new List<double>();

            double postsStep = UnitUtils.ConvertToInternalUnits(step, UnitTypeId.Meters);
            double start = Math.Min(bound1, bound2);
            double finish = Math.Max(bound1, bound2);

            if (alignment == "Начало")
            {
                if (isIncludeStart)
                    parameters.Add(start);

                int count = (int)((finish - start) / postsStep);

                for (int i = 0; i < count; i++)
                {
                    parameters.Add(start + postsStep);
                    start += postsStep;
                }

                if (isIncludeFinish)
                    parameters.Add(finish);
            }

            if (alignment == "Конец")
            {
                if (isIncludeStart)
                    parameters.Add(finish);

                int count = (int)((finish - start) / postsStep);

                for (int i = 0; i < count; i++)
                {
                    parameters.Add(finish - postsStep);
                    finish -= postsStep;
                }

                if (isIncludeFinish)
                    parameters.Add(start);
            }

            if (alignment == "Середина")
            {
                double middleParameter = (finish + start) / 2;
                parameters.Add(middleParameter);

                if (isIncludeStart)
                    parameters.Add(start);

                if (isIncludeFinish)
                    parameters.Add(finish);

                int count = (int)((finish - start) / postsStep / 2);
                double curBeforeMiddle = middleParameter;
                for (int i = 0; i < count; i++)
                {
                    parameters.Add(curBeforeMiddle - postsStep);
                    curBeforeMiddle -= postsStep;
                }

                double curAfterMiddle = middleParameter;
                for (int i = 0; i < count; i++)
                {
                    parameters.Add(curAfterMiddle + postsStep);
                    curAfterMiddle += postsStep;
                }
            }

            parameters = parameters.OrderBy(p => p).ToList();

            return parameters;
        }
        #endregion

        #region Поворот стоек
        private double RotatePost(double rotationAngle, XYZ lineVector, bool isRotateOn180)
        {
            double resultRotationAngle = rotationAngle;

            if (rotationAngle >= Math.PI / 2 && rotationAngle <= Math.PI && lineVector.X > 0)
            {
                resultRotationAngle = -(rotationAngle - Math.PI / 2);
            }
            else if (rotationAngle >= 0 && rotationAngle <= Math.PI / 2 && lineVector.X > 0)
            {
                resultRotationAngle = Math.PI / 2 - rotationAngle;
            }
            else if (rotationAngle >= 0 && rotationAngle <= Math.PI / 2 && lineVector.X < 0)
            {
                resultRotationAngle = Math.PI / 2 + rotationAngle;
            }
            else if (rotationAngle >= Math.PI / 2 && rotationAngle <= Math.PI && lineVector.X < 0)
            {
                resultRotationAngle = rotationAngle + Math.PI / 2;
            }
            else if (lineVector.X == 0)
            {
                resultRotationAngle = rotationAngle + Math.PI / 2;
            }

            if (isRotateOn180)
                return resultRotationAngle + Math.PI;

            return resultRotationAngle;
        }

        #region Положение ограждения
        private XYZ MirrorBeam(Plane plane, double offsetX)
        {
            XYZ vectorX = vectorX = plane.XVec.Normalize() * offsetX;
            XYZ lineVector = plane.Normal;
            double rotationAngle = lineVector.AngleTo(XYZ.BasisY);

            if (rotationAngle >= 0 && rotationAngle <= Math.PI / 2 && lineVector.X > 0)
            {
                vectorX = vectorX.Negate();
            }
            else if (rotationAngle >= 0 && rotationAngle < Math.PI / 2 && lineVector.X < 0)
            {
                vectorX = vectorX.Negate();
            }
            else if (lineVector.X == 0 && rotationAngle != 0)
            {
                vectorX = vectorX.Negate();
            }

            return vectorX;
        }
        #endregion

        private static List<(XYZ, XYZ)> GetPairs(IEnumerable<XYZ> elems)
        {
            var result = new List<(XYZ, XYZ)>();

            for (int i = 0; i < elems.Count() - 1; i++)
            {
                result.Add((elems.ElementAt(i), elems.ElementAt(i + 1)));
            }

            return result;
        }
        #endregion
    }
}
