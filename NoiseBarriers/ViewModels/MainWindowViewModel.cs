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
using NoiseBarriers.Properties;

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

        private int _postIndex = (int)Properties.Settings.Default["PostIndex"];

        #region Заголовок

        private string _title = "Шумозащитные экраны";

        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }

        #endregion

        #region Элементы оси шумозащитного экрана
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

        #region Выбранный типоразмер семейства секции
        private FamilySymbolSelector _panelFamilySymbol;
        public FamilySymbolSelector PanelFamilySymbol
        {
            get => _panelFamilySymbol;
            set => Set(ref _panelFamilySymbol, value);
        }
        #endregion

        #region Приподнять панель экрана над землей
        private double _liftPanels = (double)Properties.Settings.Default["Lift"];
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

        #region Разворот панелей
        private bool _isReversePanel;
        public bool IsReversePanel
        {
            get => _isReversePanel;
            set => Set(ref _isReversePanel, value);
        }
        #endregion

        #region Начало построения шумозащитных экранов
        private ObservableCollection<string> _alignmentNoiseBarrier;
        public ObservableCollection<string> AlignmentNoiseBarrier
        {
            get => _alignmentNoiseBarrier;
            set => Set(ref _alignmentNoiseBarrier, value);
        }
        #endregion

        #region Выбранное начало построения шумозащитного экрана
        private string _selectedAlignmentNoiseBarrier;
        public string SelectedAlignmentNoiseBarrier
        {
            get => _selectedAlignmentNoiseBarrier;
            set => Set(ref _selectedAlignmentNoiseBarrier, value);
        }
        #endregion

        #region Начальная стойка шумозащитного экрана
        private bool _isIncludeStartPost = true;
        public bool IsIncludeStartPost
        {
            get => _isIncludeStartPost;
            set => Set(ref _isIncludeStartPost, value);
        }
        #endregion

        #region Конечная стойка шумозащитного экрана
        private bool _isIncludeFinishPost = true;
        public bool IsIncludeFinishPost
        {
            get => _isIncludeFinishPost;
            set => Set(ref _isIncludeFinishPost, value);
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
            RevitModel.GetBoundParameters();
            RevitModel.GetLocationFamilyInstances(IsRotatePostOn180,
                                                 SelectedAlignmentNoiseBarrier,
                                                 IsIncludeStartPost,
                                                 IsIncludeFinishPost,
                                                 PostStep,
                                                 LiftPanels);
            RevitModel.CreateSafetyBarrier(PostFamilySymbol, PanelFamilySymbol, IsReversePanel);
            SaveSettings();
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
            SaveSettings();
            RevitCommand.mainView.Close();
        }

        private bool CanCloseWindowCommandExecute(object parameter)
        {
            return true;
        }
        #endregion

        #endregion

        private void SaveSettings()
        {
            Properties.Settings.Default["Lift"] = LiftPanels;
            Properties.Settings.Default["PostIndex"] = GenericModelFamilySymbols.IndexOf(PostFamilySymbol);
            Properties.Settings.Default["ElementIdAxis"] = BarrierAxisElemIds;
            Properties.Settings.Default["ElementIdBound1"] = BoundCurve1;
            Properties.Settings.Default["ElementIdBound2"] = BoundCurve2;
            Properties.Settings.Default.Save();
        }

        #region Конструктор класса MainWindowViewModel
        public MainWindowViewModel(RevitModelForfard revitModel)
        {
            RevitModel = revitModel;

            GenericModelFamilySymbols = RevitModel.GetPostFamilySymbolNames();

            AlignmentNoiseBarrier = new ObservableCollection<string>
            {
                "Начало",
                "Конец",
                "Середина"
            };

            SelectedAlignmentNoiseBarrier = "Начало";

            if(_postIndex >= 0 && _postIndex <= GenericModelFamilySymbols.Count - 1)
            {
                PostFamilySymbol = GenericModelFamilySymbols.ElementAt(_postIndex);
            }

            #region Присваивание значения элементам оси из Settings
            string axisElementIdInSettings = Properties.Settings.Default["ElementIdAxis"].ToString();
            if (RevitModel.IsAxisLinesExistInModel(axisElementIdInSettings) && !string.IsNullOrEmpty(axisElementIdInSettings))
            {
                BarrierAxisElemIds = axisElementIdInSettings;
                RevitModel.GetAxisBySettings(axisElementIdInSettings);
            }
            #endregion

            // TODO Добавить сохранение границы 1
            #region Присваивание значения элементу граница 1 из Settings
            string bound1ElementIdSettings = Properties.Settings.Default["ElementIdBound1"].ToString();
            if (RevitModel.IsBoundLineExistInModel(bound1ElementIdSettings) && !string.IsNullOrEmpty(bound1ElementIdSettings))
            {
                BoundCurve1 = bound1ElementIdSettings;
                RevitModel.GetBound1BySettings(bound1ElementIdSettings);
            }
            #endregion

            
            #region Присваивание значения элементу граница 2 из Settings
            string bound2ElementIdSettings = Properties.Settings.Default["ElementIdBound2"].ToString();

            #endregion

            #region Команды
            GetBarrierAxisCommand = new LambdaCommand(OnGetBarrierAxisCommandExecuted, CanGetBarrierAxisCommandExecute);

            GetBoundCurve1Command = new LambdaCommand(OnGetBoundCurve1CommandExecuted, CanGetBoundCurve1CommandExecute);

            GetBoundCurve2Command = new LambdaCommand(OnGetBoundCurve2CommandExecuted, CanGetBoundCurve2CommandExecute);

            CreateSafetyBarrierCommand = new LambdaCommand(OnCreateSafetyBarrierCommandExecuted, CanCreateSafetyBarrierCommandExecute);

            CloseWindow = new LambdaCommand(OnCloseWindowCommandExecuted, CanCloseWindowCommandExecute);
            #endregion
        }

        public MainWindowViewModel() { }
        #endregion
    }
}
