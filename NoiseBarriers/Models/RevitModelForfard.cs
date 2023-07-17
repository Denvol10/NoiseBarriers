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
using NoiseBarriers.Models;
using System.IO;
using System.Windows.Controls;
using Autodesk.Revit.DB.Structure;

namespace NoiseBarriers
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

        #region Ось шумозащитного экрана
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

        #region Проверка на то существуют линии оси в модели
        public bool IsAxisLinesExistInModel(string elemIdsInSettings)
        {
            var elemIds = RevitGeometryUtils.GetIdsByString(elemIdsInSettings);

            return RevitGeometryUtils.IsElemsExistInModel(Doc, elemIds, typeof(DirectShape));
        }
        #endregion

        #region Проверка на то существуют линия границы в модели
        public bool IsBoundLineExistInModel(string elemIdsInSettings)
        {
            var elemIds = RevitGeometryUtils.GetIdsByString(elemIdsInSettings);

            return RevitGeometryUtils.IsElemsExistInModel(Doc, elemIds, typeof(ModelLine));
        }
        #endregion

        #region Получение оси экрана из Settings
        public void GetAxisBySettings(string elemIdsInSettings)
        {
            var elemIds = RevitGeometryUtils.GetIdsByString(elemIdsInSettings);
            var lines = RevitGeometryUtils.GetLinesById(Doc, elemIds);
            BarrierAxis = new ParametricPolyLine(lines);
        }
        #endregion

        #region Получение границы 1 из Settings
        public void GetBound1BySettings(string elemIdInSettings)
        {
            var elemId = RevitGeometryUtils.GetIdsByString(elemIdInSettings).First();
            ElementId modelLineId = new ElementId(elemId);
            Element modelLine = Doc.GetElement(modelLineId);
            Options options = new Options();
            BoundCurve1 = modelLine.get_Geometry(options).First() as Curve;
        }
        #endregion

        #region Граница шумозащитного экрана 1
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

        #region Граница шумозащитного экрана 2
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

        #region Получение названиий семейств для шумозащитного экрана
        public ObservableCollection<FamilySymbolSelector> GetPostFamilySymbolNames()
        {
            var familySymbols = RevitFamilyUtils.GetFamilySymbolNames(Doc, BuiltInCategory.OST_GenericModel);
            return familySymbols;
        }
        #endregion

        #region Параметр стоек границы шумозащитного экрана 1
        private double _boundPostParameter1;
        #endregion

        #region Параметр стоек границы шумозащитного экрана 2
        private double _boundPostParameter2;
        #endregion

        #region Положение стоек шумозащитного экрана
        private List<(XYZ Point, double Rotation)> _postLocations = new List<(XYZ Point, double Rotation)>();
        #endregion

        #region Положение панелей шумозащитного экрана
        private List<(XYZ Point, double Rotation)> _panelLocations = new List<(XYZ Point, double Rotation)>();
        #endregion

        #region Получение парметров границ шумозащитного экрана
        public void GetBoundParameters()
        {
            BarrierAxis.IntersectAndGetPlaneParameter(BoundCurve1, out _boundPostParameter1);
            BarrierAxis.IntersectAndGetPlaneParameter(BoundCurve2, out _boundPostParameter2);
        }
        #endregion

        #region Получение положения шумозащитного экрана
        public void GetLocationFamilyInstances(bool isRotateOn180,
                                              string alignment,
                                              bool isIncludeStart,
                                              bool isIncludeFinish,
                                              double postStep,
                                              double liftPanels)
        {
            var pointParameters = GenerateParameters(_boundPostParameter1,
                                                     _boundPostParameter2,
                                                     postStep,
                                                     alignment,
                                                     true,
                                                     true);

            foreach (double parameter in pointParameters)
            {
                Line targetLine;
                XYZ point = BarrierAxis.GetPointOnPolyLinePlaneParameter(parameter, out targetLine);
                XYZ lineVector = targetLine.GetEndPoint(0) - targetLine.GetEndPoint(1);
                double rotationAngle = lineVector.AngleTo(XYZ.BasisY);
                rotationAngle = RotateFamilyInstance(rotationAngle, lineVector, isRotateOn180);
                _postLocations.Add((point, rotationAngle));
            }

            liftPanels = UnitUtils.ConvertToInternalUnits(liftPanels, UnitTypeId.Meters);

            for (int i = 0; i < _postLocations.Count - 1; i++)
            {
                var startPoint = _postLocations.ElementAt(i).Point;
                var endPoint = _postLocations.ElementAt(i + 1).Point;

                var startPointOnPlane = new XYZ(startPoint.X, startPoint.Y, 0);
                var endPointOnPlane = new XYZ(endPoint.X, endPoint.Y, 0);

                XYZ panelVector = endPointOnPlane - startPointOnPlane;
                double rotationAngle = panelVector.AngleTo(XYZ.BasisY);
                rotationAngle = RotateFamilyInstance(rotationAngle, panelVector, false);
                XYZ panelPoint = new XYZ(startPoint.X, startPoint.Y, startPoint.Z + liftPanels);
                _panelLocations.Add((panelPoint, rotationAngle));
            }


            if (!isIncludeStart)
            {
                _postLocations.RemoveAt(0);
            }

            if(!isIncludeFinish)
            {
                _postLocations.RemoveAt((_postLocations.Count - 1));
            }
        }
        #endregion

        #region Создание шумозащитного экрана
        public void CreateSafetyBarrier(FamilySymbolSelector postFamilyAndSymbolName,
                                        FamilySymbolSelector panelFamilyAndSymbolName,
                                        bool isReversePanel)
        {
            FamilySymbol postFSymbol = RevitFamilyUtils.GetFamilySymbolByName(Doc, postFamilyAndSymbolName);

            FamilySymbol panelFSymbol = RevitFamilyUtils.GetFamilySymbolByName(Doc, panelFamilyAndSymbolName);

            var level = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Levels).Where(e => e.Name == "Уровень 1").First() as Level;

            using (Transaction trans = new Transaction(Doc, "Created Noise Barrier"))
            {
                trans.Start();
                if (!postFSymbol.IsActive)
                {
                    postFSymbol.Activate();
                }

                if (!panelFSymbol.IsActive)
                {
                    panelFSymbol.Activate();
                }

                foreach (var location in _postLocations)
                {
                    FamilyInstance postFamilyInstance = Doc.Create.NewFamilyInstance(location.Item1, postFSymbol, level,
                        Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                    postFamilyInstance.Location.Rotate(Line.CreateUnbound(location.Point, XYZ.BasisZ), location.Rotation);
                }

                foreach (var location in _panelLocations)
                {
                    FamilyInstance panelFamilyInstance = Doc.Create.NewFamilyInstance(location.Point, panelFSymbol, level,
                        Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                    panelFamilyInstance.Location.Rotate(Line.CreateUnbound(location.Point, XYZ.BasisZ), location.Rotation);

                    if(isReversePanel)
                    {
                        panelFamilyInstance.flipFacing();
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
        private double RotateFamilyInstance(double rotationAngle, XYZ lineVector, bool isRotateOn180)
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
