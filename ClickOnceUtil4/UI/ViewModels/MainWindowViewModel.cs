﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Windows;

using ClickOnceUtil4UI.Clickonce;
using ClickOnceUtil4UI.UI.Models;
using ClickOnceUtil4UI.UI.Views;
using ClickOnceUtil4UI.Utils;
using ClickOnceUtil4UI.Utils.Flow;
using ClickOnceUtil4UI.Utils.Flow.FlowOperations;
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

        private string _version = "1.0.0.0";

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainWindowViewModel()
        {
            ChooseCommand = new DelegateCommand(ChooseHandler);
            BuildCommand = new DelegateCommand(BuildHandler);

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

        private void FolderUpdated(ClickOnceFolderInfo value)
        {
            if (value != null)
            {
                AvaliableActions.Clear();
                foreach (var action in _flowsContainer.GetActions(value))
                {
                    AvaliableActions.Add(action);
                }
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

        private void BuildHandler(object obj)
        {
            var container = new Container
            {
                FullPath = SelectedFolder.FullPath,
                Application = ApplicationManifest?.Manifest,
                Deploy = DeployManifest?.Manifest,
                Certificate = CertificateUtils.GenerateSelfSignedCertificate(),
                Version = _version,
                EntrypointPath = !string.IsNullOrEmpty(SelectedEntrypoint) ? Path.Combine(SelectedFolder.FullPath, SelectedEntrypoint) : null
            };
            
            string errorString;
            if (
                !_flowsContainer[SelectedAction].Execute(container, out errorString))
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
                SelectedAction = AvaliableActions.FirstOrDefault();
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
                            .Union(rootDirectory.GetFiles($"*.{Constants.DeployFileExtension}")).ToArray();
                    
                    foreach (var entrypointFile in entrypoints)
                    {
                        ApplicationEntryPoints.Add(FlowUtils.GetNormalFilePath(entrypointFile.Name));
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
                var deploy = SelectedFolder.DeployManifest ?? FlowUtils.CreateDeployManifest(SelectedFolder.FullPath, SelectedEntrypoint);
                var application = SelectedFolder.ApplicationManifest ?? FlowUtils.CreateApplicationManifest(SelectedFolder.FullPath, SelectedEntrypoint);

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