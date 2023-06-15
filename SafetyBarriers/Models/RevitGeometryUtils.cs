using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyBarriers.Models
{
    internal class RevitGeometryUtils
    {
        // Метод получения линий оси барьерного ограждения
        public static List<Curve> GetBarrierAxisCurves(UIApplication uiapp, out string elementIds)
        {
            Selection sel = uiapp.ActiveUIDocument.Selection;
            var selectedElements = sel.PickObjects(ObjectType.Element, new Filters.DirectShapeFilter(), "Select Barrier Axis");
            var directShapeElements = selectedElements.Select(r => uiapp.ActiveUIDocument.Document.GetElement(r)).OfType<DirectShape>();
            elementIds = ElementIdToString(directShapeElements);
            var curvesBarrierAxis = GetCurvesByDirectShapes(directShapeElements);

            return curvesBarrierAxis;
        }

        // Получение линии границы
        public static Curve GetBoundCurve(UIApplication uiapp, out string elementIds)
        {
            Selection sel = uiapp.ActiveUIDocument.Selection;
            var boundCurvePicked = sel.PickObject(ObjectType.Element, "Выберете линию границу барьерного ограждения");
            Options options = new Options();
            Element curveElement = uiapp.ActiveUIDocument.Document.GetElement(boundCurvePicked);
            elementIds = "Id" + curveElement.Id.IntegerValue;
            var boundCurve = curveElement.get_Geometry(options).First() as Curve;

            return boundCurve;
        }


        // Метод получения строки с ElementId
        private static string ElementIdToString(IEnumerable<Element> elements)
        {
            var stringArr = elements.Select(e => "Id" + e.Id.IntegerValue.ToString()).ToArray();
            string resultString = string.Join(", ", stringArr);

            return resultString;
        }

        // Получение линий на основе элементов DirectShape
        private static List<Curve> GetCurvesByDirectShapes(IEnumerable<DirectShape> directShapes)
        {
            var curves = new List<Curve>();

            Options options = new Options();
            var geometries = directShapes.Select(d => d.get_Geometry(options)).SelectMany(g => g);

            foreach (var geom in geometries)
            {
                if (geom is PolyLine polyLine)
                {
                    var polyCurve = GetCurvesByPolyline(polyLine);
                    curves.AddRange(polyCurve);
                }
                else
                {
                    curves.Add(geom as Curve);
                }
            }

            return curves;
        }

        // Метод получения списка линий на основе полилинии
        private static List<Line> GetCurvesByPolyline(PolyLine polyLine)
        {
            var curves = new List<Line>();

            for (int i = 0; i < polyLine.NumberOfCoordinates - 1; i++)
            {
                var line = Line.CreateBound(polyLine.GetCoordinate(i), polyLine.GetCoordinate(i + 1));
                curves.Add(line);
            }

            return curves;
        }
    }
}
