﻿using System;
using System.Collections.Generic;
using System.Linq;

using ClickOnceUtil4UI.Clickonce;
using ClickOnceUtil4UI.UI.Models;

using Microsoft.Build.Tasks.Deployment.ManifestUtilities;

namespace ClickOnceUtil4UI.Utils.Flow.FlowOperations
{
    /// <summary>
    /// Update flow scenario.
    /// </summary>
    internal class UpdateFlow : FlowBase
    {
        /// <summary>
        /// Constructor for <see cref="UpdateFlow"/>.
        /// </summary>
        public UpdateFlow() : base(UserActions.Update)
        {
        }

        /// <inheritdoc/>
        public override bool IsFlowApplicable(FolderTypes folderType, string fullPath)
        {
            return folderType == FolderTypes.ClickOnceApplication;
        }

        /// <inheritdoc/>
        public override bool Execute(Container container, out string errorString)
        {
            ApplicationManifest application = container.Application;
            DeployManifest deploy = container.Deploy;
            errorString = null;

            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            if (deploy == null)
            {
                throw new ArgumentNullException(nameof(deploy));
            }

            return UpdateManifestUtils.RecreateReferences(container, out errorString);
        }

        /// <inheritdoc/>
        public override IEnumerable<InfoData> GetBuildInformation(Container container)
        {
            return InfoUtils.GetFullInfoData(container);
        }
    }
}