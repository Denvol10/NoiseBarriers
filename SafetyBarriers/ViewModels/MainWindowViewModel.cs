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
using System.Windows.Input;
using SafetyBarriers.Infrastructure;

namespace SafetyBarriers.ViewModels
{
    internal class MainWindowViewModel : Base.ViewModel
    {
        private RevitModelForfard _revitModel;

        internal RevitModelForfard RevitModel
        {
            get => _revitModel;
            set => _revitModel = value;
        }

        #region Заголовок

        private string _title = "Барьерное ограждение";

        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }

        #endregion

        #region Список семейств категории обобщенной модели
        private ObservableCollection<string> _genericModelFamilySymbols;

        public ObservableCollection<string> GenericModelFamilySymbols
        {
            get => _genericModelFamilySymbols;
            set => Set(ref _genericModelFamilySymbols, value);
        }
        #endregion

        #region Выбранный типоразмер семейства стойки
        private string _postFamilySymbol;
        public string PostFamilySymbol
        {
            get => _postFamilySymbol;
            set => Set(ref _postFamilySymbol, value);
        }
        #endregion

        #region Команды




        #endregion


        #region Конструктор класса MainWindowViewModel
        public MainWindowViewModel(RevitModelForfard revitModel)
        {
            RevitModel = revitModel;
            #region


            #endregion
        }

        public MainWindowViewModel() { }
        #endregion
    }
}
