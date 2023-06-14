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

        #region Элементы оси барьерного ограждения
        private string _barrierAxisElemIds;
        public string BarrierAxisElemIds
        {
            get => _barrierAxisElemIds;
            set => Set(ref _barrierAxisElemIds, value);
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

        #region Получение оси барьерного ограждения
        public ICommand GetBarrierAxisCommand { get; }

        private void OnGetBarrierAxisCommandExecuted(object parameter)
        {
            RevitCommand.mainView.Hide();
            RevitModel.GetBarrierAxis();
            BarrierAxisElemIds = RevitModel.BarrierAxisElemIds;
            RevitCommand.mainView.ShowDialog();
        }

        private bool CanGetBarrierAxisCommandExecute(object parameter)
        {
            return true;
        }
        #endregion


        #endregion


        #region Конструктор класса MainWindowViewModel
        public MainWindowViewModel(RevitModelForfard revitModel)
        {
            RevitModel = revitModel;
            #region
            GetBarrierAxisCommand = new LambdaCommand(OnGetBarrierAxisCommandExecuted, CanGetBarrierAxisCommandExecute);

            #endregion
        }

        public MainWindowViewModel() { }
        #endregion
    }
}
