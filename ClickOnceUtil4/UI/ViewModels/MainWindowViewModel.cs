﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

using ClickOnceUtil4UI.Clickonce;
using ClickOnceUtil4UI.UI.Models;
using ClickOnceUtil4UI.UI.Views;
using ClickOnceUtil4UI.Utils;
using ClickOnceUtil4UI.Utils.Flow;
using ClickOnceUtil4UI.Utils.Prism;

using Microsoft.Build.Tasks.Deployment.ManifestUtilities;

namespace ClickOnceUtil4UI.UI.ViewModels
{
    /// <summary>
    /// MainWindow view model.
    /// </summary>
    public class MainWindowViewModel : NotificationObject
    {
        private readonly FlowsContainer _flowsContainer = new FlowsContainer();

        private ClickOnceFolderInfo _selectedFolder;

        private ManifestEditorViewModel<DeployManifest> _deployManifest;

        private ManifestEditorViewModel<ApplicationManifest> _applicationManifest;

        private UserActions _selectedAction = UserActions.None;

        private string _selectedEntrypoint = string.Empty;

        private string _version;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainWindowViewModel()
        {
            ChooseCommand = new DelegateCommand(ChooseHandler);
            BuildCommand = new DelegateCommand(BuildHandler);
            CleanCacheCommand = new DelegateCommand(CleanCacheHandler);

            // TODO DELETE
            var newFolder = new ClickOnceFolderInfo(@"C:\IISRoot\DELME");
            newFolder.Update(true);
            SelectedFolder = newFolder;
        }

        /// <summary>
        /// Choose folder button command.
        /// </summary>
        public DelegateCommand ChooseCommand { get; private set; }

        /// <summary>
        /// Build button command.
        /// </summary>
        public DelegateCommand BuildCommand { get; private set; }

        /// <summary>
        /// Clean cache button command.
        /// </summary>
        public DelegateCommand CleanCacheCommand { get; private set; }

        /// <summary>
        /// Folder source path.
        /// </summary>
        public ClickOnceFolderInfo SelectedFolder
        {
            get
            {
                return _selectedFolder;
            }

            private set
            {
                _selectedFolder = value;
                RaisePropertyChanged(() => SelectedFolder);
                FolderUpdated(value);
            }
        }

        /// <summary>
        /// Available actions for selected folder.
        /// </summary>
        public ObservableCollection<UserActions> AvaliableActions { get; set; } =
            new ObservableCollection<UserActions>();

        /// <summary>
        /// User selected action.
        /// </summary>
        public UserActions SelectedAction
        {
            get
            {
                return _selectedAction;
            }

            set
            {
                _selectedAction = value;
                RaisePropertyChanged(() => SelectedAction);
                SelectedActionChanges(value);
            }
        }

        /// <summary>
        /// Deploy manifest view model.
        /// </summary>
        public ManifestEditorViewModel<DeployManifest> DeployManifest
        {
            get
            {
                return _deployManifest;
            }

            set
            {
                _deployManifest = value;
                RaisePropertyChanged(() => DeployManifest);
            }
        }

        /// <summary>
        /// Application manifest view model.
        /// </summary>
        public ManifestEditorViewModel<ApplicationManifest> ApplicationManifest
        {
            get
            {
                return _applicationManifest;
            }

            set
            {
                _applicationManifest = value;
                RaisePropertyChanged(() => ApplicationManifest);
            }
        }

        /// <summary>
        /// ClickOnce directory executable file names.
        /// </summary> 
        public ObservableCollection<string> ApplicationEntryPoints { get; } = new ObservableCollection<string>();

        /// <summary>
        /// ClickOnce application version
        /// </summary>
        public string Version
        {
            get
            {
                return _version;
            }
            set
            {
                _version = value;
                RaisePropertyChanged(() => Version);
            }
        }

        /// <summary>
        /// Selected entry point value.
        /// </summary>
        public string SelectedEntrypoint
        {
            get
            {
                return _selectedEntrypoint;
            }
            set
            {
                _selectedEntrypoint = value;
                RaisePropertyChanged(() => SelectedEntrypoint);
            }
        }

        private void FolderUpdated(ClickOnceFolderInfo value)
        {
            AvaliableActions.Clear();

            if (value != null)
            {
                foreach (var action in _flowsContainer.GetActions(value))
                {
                    AvaliableActions.Add(action);
                }
            }

            SelectedAction = AvaliableActions.FirstOrDefault();
        }

        private void BuildHandler(object obj)
        {
            var container = new Container
            {
                FullPath = SelectedFolder.FullPath,
                Application = ApplicationManifest?.Manifest,
                Deploy = DeployManifest?.Manifest,
                Certificate = CertificateUtils.GenerateSelfSignedCertificate(),
                Version = _version,
                EntrypointPath =
                    !string.IsNullOrEmpty(SelectedEntrypoint)
                        ? Path.Combine(SelectedFolder.FullPath, SelectedEntrypoint)
                        : null
            };

            var buildInfo = _flowsContainer[SelectedAction].GetBuildInformation(container).ToArray();
            if (buildInfo.Any())
            {
                var buildModel = new BuildInfoViewModel(buildInfo);
                var buildView = new BuildInfoView(buildModel) { Owner = Application.Current.MainWindow };
                if (!buildView.ShowDialog().GetValueOrDefault())
                {
                    return;
                }
            }

            string errorString;
            if (!_flowsContainer[SelectedAction].Execute(container, out errorString))
            {
                MessageBox.Show(errorString, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show(
                    "Operation completed successfully!",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                _selectedFolder.Update(true);
                FolderUpdated(_selectedFolder);
            }
        }

        private void CleanCacheHandler(object obj)
        {
            var appsFolder = $@"C:\Users\{Environment.UserName}\AppData\Local\Apps";
            var buildInfo = new[]
            {
                new InfoData(
                    "Clean cache",
                    $@"You are going to clean local applications cache. Be sure that no deployed programs running now. Cache folder location: [{
                        appsFolder
                        }], anyway next applications launch will make installation back. By the way cleaning can be done manually:{Environment.NewLine}1. By cmd.exe command: ""rundll32 dfshim CleanOnlineAppCache""] {Environment.NewLine}2. Or just remove [{
                        appsFolder}] folder content.")
            };

            var buildModel = new BuildInfoViewModel(buildInfo);
            var buildView = new BuildInfoView(buildModel) { Owner = Application.Current.MainWindow };
            if (buildView.ShowDialog().GetValueOrDefault())
            {
                FlowUtils.CleanOnlineAppCache();
                
                MessageBox.Show(
                    "Operation completed!",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void ChooseHandler(object obj)
        {
            var dataContext = new ChooseFolderDialogViewModel(SelectedFolder?.FullPath);
            var dialog = new ChooseFolderDialog(dataContext) { Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog().GetValueOrDefault())
            {
                SelectedFolder = dataContext.SelectedFolder;
            }
        }

        private void InitEntrypoints(UserActions action)
        {
            ApplicationEntryPoints.Clear();
            switch (action)
            {
                case UserActions.New:
                    var rootDirectory = new DirectoryInfo(SelectedFolder.FullPath);
                    var entrypoints =
                        rootDirectory.GetFiles("*.exe")
                            .Union(rootDirectory.GetFiles($"*.{Constants.DeployFileExtension}"))
                            .ToArray();

                    foreach (var entrypointFile in entrypoints)
                    {
                        ApplicationEntryPoints.Add(ReferenceUtils.GetNormalFilePath(entrypointFile.Name));
                    }

                    SelectedEntrypoint = entrypoints.Select(entry => entry.Name).FirstOrDefault();
                    break;
                case UserActions.Update:
                    var entrypoint = SelectedFolder.ApplicationManifest?.EntryPoint;
                    if (entrypoint != null)
                    {
                        ApplicationEntryPoints.Add(entrypoint.TargetPath);
                        SelectedEntrypoint = entrypoint.TargetPath;
                    }
                    break;
            }
        }

        private void SelectedActionChanges(UserActions action)
        {
            InitEntrypoints(action);
            if (action == UserActions.New || action == UserActions.Update)
            {
                var deploy = SelectedFolder.DeployManifest ??
                             FlowUtils.CreateDeployManifest(SelectedFolder.FullPath, SelectedEntrypoint);
                var application = SelectedFolder.ApplicationManifest ??
                                  FlowUtils.CreateApplicationManifest(SelectedFolder.FullPath, SelectedEntrypoint);

                Version = FlowUtils.ReadApplicationVersion(deploy) ?? Constants.DefaultVersion;

                DeployManifest = new ManifestEditorViewModel<DeployManifest>(deploy);
                ApplicationManifest = new ManifestEditorViewModel<ApplicationManifest>(application);
            }
            else
            {
                DeployManifest = null;
                ApplicationManifest = null;
            }
        }
    }
}