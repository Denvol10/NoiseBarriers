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
using BridgeDeck.Models;
using SafetyBarriers.Models;
using System.IO;

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
        public ObservableCollection<string> GetPostFamilySymbolNames()
        {
            var familySymbols = RevitFamilyUtils.GetFamilySymbolNames(Doc, BuiltInCategory.OST_GenericModel);
            return familySymbols;
        }
        #endregion

        #region Создание стоек барьерного ограждения
        public void CreatePostFamilyInstances(string familyAndSymbolName,
                                              bool isRotateOn180,
                                              string alignment,
                                              bool isIncludeStart,
                                              bool isIncludeFinish)
        {
            double boundParameter1;
            BarrierAxis.Intersect(BoundCurve1, out boundParameter1);

            double boundParameter2;
            BarrierAxis.Intersect(BoundCurve2, out boundParameter2);

            FamilySymbol fSymbol = RevitFamilyUtils.GetFamilySymbolByName(Doc, familyAndSymbolName);

            var pointParameters = GenerateParameters(boundParameter1, boundParameter2, 2.5, alignment, isIncludeStart, isIncludeFinish);
            var postLocation = new List<(XYZ Point, double Rotation)>();

            foreach (double parameter in pointParameters)
            {
                Line targetLine;
                XYZ point = BarrierAxis.GetPointOnPolyLine(parameter, out targetLine);
                XYZ lineVector = targetLine.GetEndPoint(0) - targetLine.GetEndPoint(1);
                double rotationAngle = lineVector.AngleTo(XYZ.BasisY);
                rotationAngle = RotatePost(rotationAngle, lineVector, isRotateOn180);
                postLocation.Add((point, rotationAngle));
            }

            using (Transaction trans = new Transaction(Doc, "Create Posts"))
            {
                trans.Start();
                if (!fSymbol.IsActive)
                {
                    fSymbol.Activate();
                }
                foreach (var location in postLocation)
                {
                    FamilyInstance familyInstance = Doc.Create.NewFamilyInstance(location.Item1, fSymbol, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                    familyInstance.Location.Rotate(Line.CreateUnbound(location.Point, XYZ.BasisZ), location.Rotation);
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
        #endregion
    }
}
