using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SafetyBarriers.Models
{
    internal class RevitFamilyUtils
    {
        #region Список названий типоразмеров семейств
        public ObservableCollection<string> GetFamilySymbolNames(Document doc, BuiltInCategory builtInCategory)
        {
            var familySymbolNames = new ObservableCollection<string>();
            var allFamilies = new FilteredElementCollector(doc).OfClass(typeof(Family)).OfType<Family>();
            var genericModelFamilies = allFamilies.Where(f => f.FamilyCategory.Id.IntegerValue == (int)builtInCategory);
            if (genericModelFamilies.Count() == 0)
                return familySymbolNames;

            foreach (var family in genericModelFamilies)
            {
                foreach (var symbolId in family.GetFamilySymbolIds())
                {
                    var familySymbol = doc.GetElement(symbolId);
                    familySymbolNames.Add($"{family.Name}-{familySymbol.Name}");
                }
            }

            return familySymbolNames;
        }
        #endregion

        #region Получение типоразмера по имени
        public FamilySymbol GetFamilySymbolByName(Document doc, string familyAndSymbolName)
        {
            var familyName = familyAndSymbolName.Split('-').First();
            var symbolName = familyAndSymbolName.Split('-').Last();

            Family family = new FilteredElementCollector(doc).OfClass(typeof(Family)).Where(f => f.Name == familyName).First() as Family;
            var symbolIds = family.GetFamilySymbolIds();
            foreach (var symbolId in symbolIds)
            {
                FamilySymbol fSymbol = (FamilySymbol)doc.GetElement(symbolId);
                if (fSymbol.get_Parameter(BuiltInParameter.SYMBOL_NAME_PARAM).AsString() == symbolName)
                {
                    return fSymbol;
                }
            }
            return null;
        }
        #endregion

        #region Получение семейства по имени
        public Family GetFamilyByName(Document doc, string familyAndSymbolName)
        {
            var familyName = familyAndSymbolName.Split('-').First();
            Family family = new FilteredElementCollector(doc).OfClass(typeof(Family)).Where(f => f.Name == familyName).First() as Family;

            return family;
        }
        #endregion
    }
}
