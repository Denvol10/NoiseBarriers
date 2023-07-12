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
using NoiseBarriers.Infrastructure;
using NoiseBarriers.Models;

namespace NoiseBarriers.ViewModels
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

        private string _title = "Шумозащитные экраны";

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
        private ObservableCollection<FamilySymbolSelector> _genericModelFamilySymbols;

        public ObservableCollection<FamilySymbolSelector> GenericModelFamilySymbols
        {
            get => _genericModelFamilySymbols;
            set => Set(ref _genericModelFamilySymbols, value);
        }
        #endregion

        #region Выбранный типоразмер семейства стойки
        private FamilySymbolSelector _postFamilySymbol;
        public FamilySymbolSelector PostFamilySymbol
        {
            get => _postFamilySymbol;
            set => Set(ref _postFamilySymbol, value);
        }
        #endregion

        #region Выбранный типоразмер семейства панели
        private FamilySymbolSelector _panelFamilySymbol;
        public FamilySymbolSelector PanelFamilySymbol
        {
            get => _panelFamilySymbol;
            set => Set(ref _panelFamilySymbol, value);
        }
        #endregion

        #region Приподнять панель экрана над землей
        private double _liftPanels = 0.02;
        public double LiftPanels
        {
            get => _liftPanels;
            set => Set(ref _liftPanels, value);
        }
        #endregion

        #region Поворот стоек на 180 градусов
        private bool _isRotatePostOn180;
        public bool IsRotatePostOn180
        {
            get => _isRotatePostOn180;
            set => Set(ref _isRotatePostOn180, value);
        }
        #endregion

        #region Поворот панелей на 180 градусов
        private bool _isRotatePanelOn180;
        public bool IsRotatePanelOn180
        {
            get => _isRotatePanelOn180;
            set => Set(ref _isRotatePanelOn180, value);
        }
        #endregion

        #region Начало построения ограждения
        private ObservableCollection<string> _alignmentSafityBarrier;
        public ObservableCollection<string> AlignmentNoiseBarrier
        {
            get => _alignmentSafityBarrier;
            set => Set(ref _alignmentSafityBarrier, value);
        }
        #endregion

        #region Выбранное начало построения ограждения
        private string _selectedAlignmentSafityBarrier;
        public string SelectedAlignmentNoiseBarrier
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
        private ObservableCollection<FamilySymbolSelector> _beamFamilySymbols;
        public ObservableCollection<FamilySymbolSelector> BeamFamilySymbols
        {
            get => _beamFamilySymbols;
            set => Set(ref _beamFamilySymbols, value);
        }
        #endregion

        #region Выбранное полотно ограждения
        private FamilySymbolSelector _selectedBeamFamilySymbol;
        public FamilySymbolSelector SelectedBeamFamilySymbol
        {
            get => _selectedBeamFamilySymbol;
            set => Set(ref _selectedBeamFamilySymbol, value);
        }
        #endregion

        #region Список полотен ограждения
        private ObservableCollection<BeamSetup> _beamCollection;
        public ObservableCollection<BeamSetup> BeamCollection
        {
            get => _beamCollection;
            set => Set(ref _beamCollection, value);
        }

        #endregion

        #region Выбранное полотно ограждения
        private BeamSetup _selectedBeam;
        public BeamSetup SelectedBeam
        {
            get => _selectedBeam;
            set => Set(ref _selectedBeam, value);
        }
        #endregion

        #region Развернуть балки
        private bool _isReverseBeams;
        public bool IsReverseBeams
        {
            get => _isReverseBeams;
            set => Set(ref _isReverseBeams, value);
        }
        #endregion

        #region Шаг стоек
        private double _postStep = 3.0;
        public double PostStep
        {
            get => _postStep;
            set => Set(ref _postStep, value);
        }
        #endregion

        #region Длина балок
        private double _beamLength = 3.0;
        public double BeamLength
        {
            get => _beamLength;
            set => Set(ref _beamLength, value);
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

        #region Добавление полотна ограждения в список
        public ICommand AddBeamSetupCommand { get; }
        
        private void OnAddBeamSetupCommandExecuted(object parameter)
        {
            var newBeamSetup = new BeamSetup()
            {
                OffsetX = 0.3,
                OffsetZ = 0.5
            };

            BeamCollection.Add(newBeamSetup);
        }

        private bool CanAddBeamSetupCommandExecute(object parameter)
        {
            return true;
        }
        #endregion

        #region Удаление полотна ограждения из списка
        public ICommand DeleteBeamSetupCommand { get; }

        private void OnDeleteBeamSetupCommandExecuted(object parameter)
        {
            var lastBeamSetup = BeamCollection.LastOrDefault();

            if(!(lastBeamSetup is null))
            {
                BeamCollection.Remove(lastBeamSetup);
            }
        }

        private bool CanDeleteBeamSetupCommandExecute(object parameter)
        {
            return true;
        }
        #endregion

        #region Создание барьерного ограждения
        public ICommand CreateSafetyBarrierCommand { get; }

        private void OnCreateSafetyBarrierCommandExecuted(object parameter)
        {
            RevitModel.GetBoundParameters();
            RevitModel.GetLocationPostFamilyInstances(IsRotatePostOn180,
                                                 SelectedAlignmentNoiseBarrier,
                                                 IsIncludeStartPost,
                                                 IsIncludeFinishPost,
                                                 PostStep);
            RevitModel.GetLocationBeamFamilyInstances(IsRotatePostOn180,
                                                      SelectedAlignmentNoiseBarrier,
                                                      BeamCollection,
                                                      BeamLength);
            RevitModel.CreateSafetyBarrier(PostFamilySymbol, IsReverseBeams);
            RevitCommand.mainView.Close();
        }

        public bool CanCreateSafetyBarrierCommandExecute(object parameter)
        {
            return true;
        }
        #endregion

        #region Закрыть окно
        public ICommand CloseWindow { get; }

        private void OnCloseWindowCommandExecuted(object parameter)
        {
            RevitCommand.mainView.Close();
        }

        private bool CanCloseWindowCommandExecute(object parameter)
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

            AlignmentNoiseBarrier = new ObservableCollection<string>
            {
                "Начало",
                "Конец",
                "Середина"
            };

            SelectedAlignmentNoiseBarrier = "Начало";

            BeamCollection = new ObservableCollection<BeamSetup>()
            {
                new BeamSetup() {OffsetX = 0.2, OffsetZ = 0.7}
            };

            #region Команды
            GetBarrierAxisCommand = new LambdaCommand(OnGetBarrierAxisCommandExecuted, CanGetBarrierAxisCommandExecute);

            GetBoundCurve1Command = new LambdaCommand(OnGetBoundCurve1CommandExecuted, CanGetBoundCurve1CommandExecute);

            GetBoundCurve2Command = new LambdaCommand(OnGetBoundCurve2CommandExecuted, CanGetBoundCurve2CommandExecute);

            AddBeamSetupCommand = new LambdaCommand(OnAddBeamSetupCommandExecuted, CanAddBeamSetupCommandExecute);

            DeleteBeamSetupCommand = new LambdaCommand(OnDeleteBeamSetupCommandExecuted, CanDeleteBeamSetupCommandExecute);

            CreateSafetyBarrierCommand = new LambdaCommand(OnCreateSafetyBarrierCommandExecuted, CanCreateSafetyBarrierCommandExecute);

            CloseWindow = new LambdaCommand(OnCloseWindowCommandExecuted, CanCloseWindowCommandExecute);
            #endregion
        }

        public MainWindowViewModel() { }
        #endregion
    }
}
