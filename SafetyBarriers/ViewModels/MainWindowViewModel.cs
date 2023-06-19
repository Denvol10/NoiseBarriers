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

        #region Элемент линии границы 1
        private string _boundCurve1;
        public string BoundCurve1
        {
            get => _boundCurve1;
            set => Set(ref _boundCurve1, value);
        }
        #endregion

        #region Элемент линии границы 2
        private string _boundCurve2;
        public string BoundCurve2
        {
            get => _boundCurve2;
            set => Set(ref _boundCurve2, value);
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

        #region Поворот на 180 градусов
        private bool _isRotateOn180;
        public bool IsRotateOn180
        {
            get => _isRotateOn180;
            set => Set(ref _isRotateOn180, value);
        }
        #endregion

        #region Начало построения ограждения
        private ObservableCollection<string> _alignmentSafityBarrier;
        public ObservableCollection<string> AlignmentSafityBarrier
        {
            get => _alignmentSafityBarrier;
            set => Set(ref _alignmentSafityBarrier, value);
        }
        #endregion

        #region Выбранное начало построения ограждения
        private string _selectedAlignmentSafityBarrier;
        public string SelectedAlignmentSafityBarrier
        {
            get => _selectedAlignmentSafityBarrier;
            set => Set(ref _selectedAlignmentSafityBarrier, value);
        }
        #endregion

        #region Начальная стойка ограждения
        private bool _isIncludeStartPost = true;
        public bool IsIncludeStartPost
        {
            get => _isIncludeStartPost;
            set => Set(ref _isIncludeStartPost, value);
        }
        #endregion

        #region Конечная стойка ограждения
        private bool _isIncludeFinishPost = true;
        public bool IsIncludeFinishPost
        {
            get => _isIncludeFinishPost;
            set => Set(ref _isIncludeFinishPost, value);
        }
        #endregion

        #region Полотно ограждения
        private ObservableCollection<string> _beamFamilySymbols;
        public ObservableCollection<string> BeamFamilySymbols
        {
            get => _beamFamilySymbols;
            set => Set(ref _beamFamilySymbols, value);
        }
        #endregion

        #region Выбранное полотно ограждения
        private string _selectedBeamFamilySymbol;
        public string SelectedBeamFamilySymbol
        {
            get => _selectedBeamFamilySymbol;
            set => Set(ref _selectedBeamFamilySymbol, value);
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

        #region Получение границы барьерного ограждения 1
        public ICommand GetBoundCurve1Command { get; }

        private void OnGetBoundCurve1CommandExecuted(object parameter)
        {
            RevitCommand.mainView.Hide();
            RevitModel.GetBoundCurve1();
            BoundCurve1 = RevitModel.BoundCurveId1;
            RevitCommand.mainView.ShowDialog();
        }

        private bool CanGetBoundCurve1CommandExecute(object parameter)
        {
            return true;
        }
        #endregion

        #region Получение границы плиты барьерного ограждения 2
        public ICommand GetBoundCurve2Command { get; }

        private void OnGetBoundCurve2CommandExecuted(object parameter)
        {
            RevitCommand.mainView.Hide();
            RevitModel.GetBoundCurve2();
            BoundCurve2 = RevitModel.BoundCurveId2;
            RevitCommand.mainView.ShowDialog();
        }

        private bool CanGetBoundCurve2CommandExecute(object parameter)
        {
            return true;
        }
        #endregion

        #region Создание барьерного ограждения
        public ICommand CreateSafetyBarrierCommand { get; }

        private void OnCreateSafetyBarrierCommandExecuted(object parameter)
        {
            RevitModel.GetLocationPostFamilyInstances(IsRotateOn180,
                                                 SelectedAlignmentSafityBarrier,
                                                 IsIncludeStartPost,
                                                 IsIncludeFinishPost);
            RevitModel.CreateSafetyBarrier(PostFamilySymbol);
        }

        public bool CanCreateSafetyBarrierCommandExecute(object parameter)
        {
            return true;
        }
        #endregion

        #endregion


        #region Конструктор класса MainWindowViewModel
        public MainWindowViewModel(RevitModelForfard revitModel)
        {
            RevitModel = revitModel;

            GenericModelFamilySymbols = RevitModel.GetPostFamilySymbolNames();

            BeamFamilySymbols = RevitModel.GetBeamFamilySymbolNames();

            AlignmentSafityBarrier = new ObservableCollection<string>
            {
                "Начало",
                "Конец",
                "Середина"
            };

            SelectedAlignmentSafityBarrier = "Начало";

            #region Команды
            GetBarrierAxisCommand = new LambdaCommand(OnGetBarrierAxisCommandExecuted, CanGetBarrierAxisCommandExecute);

            GetBoundCurve1Command = new LambdaCommand(OnGetBoundCurve1CommandExecuted, CanGetBoundCurve1CommandExecute);

            GetBoundCurve2Command = new LambdaCommand(OnGetBoundCurve2CommandExecuted, CanGetBoundCurve2CommandExecute);

            CreateSafetyBarrierCommand = new LambdaCommand(OnCreateSafetyBarrierCommandExecuted, CanCreateSafetyBarrierCommandExecute);
            #endregion
        }

        public MainWindowViewModel() { }
        #endregion
    }
}
