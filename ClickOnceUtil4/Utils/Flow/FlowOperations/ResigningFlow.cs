﻿using System;
using System.Collections.Generic;

using ClickOnceUtil4UI.Clickonce;
using ClickOnceUtil4UI.UI.Models;

using Microsoft.Build.Tasks.Deployment.ManifestUtilities;

namespace ClickOnceUtil4UI.Utils.Flow.FlowOperations
{
    /// <summary>
    /// Resigning flow scenario.
    /// </summary>
    internal class ResigningFlow : FlowBase
    {
        /// <summary>
        /// Constructor for <see cref="ResigningFlow"/>.
        /// </summary>
        public ResigningFlow() : base(UserActions.Resigning)
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
            errorString = null;

            var certificate = container.Certificate ?? CertificateUtils.GenerateSelfSignedCertificate();

            var timestamp = !string.IsNullOrEmpty(container.TimestampUrl) ? new Uri(container.TimestampUrl) : null;

            // Signing .manifest
            FlowUtils.SignFile(container.Application.SourcePath, timestamp, certificate);

            // Recompute hash for .manifest file reference in .application
            UpdateApplicationHash(container);

            // Signing .application
            FlowUtils.SignFile(container.Deploy.SourcePath, timestamp, certificate);
            return true;
        }

        /// <inheritdoc/>
        public override IEnumerable<InfoData> GetBuildInformation(Container container)
        {
            string description = container.Certificate == null
                ? "Certificate file will be generated automatically. Publisher name: \"CN = TempCA\""
                : $"Certificate date:{Environment.NewLine}{container.Certificate}";

            yield return new InfoData(nameof(container.Certificate), description);
        }

        private static void UpdateApplicationHash(Container container)
        {
            var deploy = container.Deploy;
            deploy.AssemblyReferences.Clear();

            var manifestReference = new AssemblyReference(container.Application.SourcePath)
            {
                ReferenceType = AssemblyReferenceType.ClickOnceManifest
            };
            deploy.AssemblyReferences.Add(manifestReference);
            deploy.ResolveFiles();
            deploy.UpdateFileInfo();
            ManifestWriter.WriteManifest(deploy);
        }
    }
}