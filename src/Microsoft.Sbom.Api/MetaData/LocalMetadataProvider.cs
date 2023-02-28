﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Sbom.Extensions;
using Microsoft.Sbom.Extensions.Entities;
using Microsoft.Sbom.Common.Config;
using Microsoft.Sbom.Common.Extensions;
using Microsoft.Sbom.Common.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Sbom.Api.Metadata
{
    /// <summary>
    /// Provides metadata based on the local environment.
    /// </summary>
    public class LocalMetadataProvider : IMetadataProvider, IDefaultMetadataProvider
    {
        private const string ProductName = "Microsoft.SBOMTool";
        private const string BuildEnvironmentNameValue = "local";

        private static readonly Lazy<string> Version = new Lazy<string>(() =>
        {
            return typeof(LocalMetadataProvider).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;
        });

        public string BuildEnvironmentName => BuildEnvironmentNameValue;

        private readonly IConfiguration configuration;

        private IDictionary<MetadataKey, object> metadataDictionary;

        /// <summary>
        /// Gets or sets stores the metadata that is generated by this metadata provider.
        /// </summary>
        public IDictionary<MetadataKey, object> MetadataDictionary
        {
            get
            {
                if (metadataDictionary != null)
                {
                    return metadataDictionary;
                }

                metadataDictionary = new Dictionary<MetadataKey, object>
                {
                    { MetadataKey.SBOMToolName, ProductName },

                    // TODO get tool version from dll manifest.
                    { MetadataKey.SBOMToolVersion, Version.Value }
                };

                // Add the package name if available.
                metadataDictionary.AddIfKeyNotPresentAndValueNotNull(MetadataKey.PackageName, configuration.PackageName?.Value);
                metadataDictionary.AddIfKeyNotPresentAndValueNotNull(MetadataKey.PackageVersion, configuration.PackageVersion?.Value);
                metadataDictionary.AddIfKeyNotPresentAndValueNotNull(MetadataKey.PackageSupplier, configuration.PackageSupplier?.Value);

                // Add generation timestamp
                metadataDictionary.AddIfKeyNotPresentAndValueNotNull(MetadataKey.GenerationTimestamp, configuration.GenerationTimestamp?.Value);
                
                return metadataDictionary;
            }
        }

        public LocalMetadataProvider(IConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public string GetDocumentNamespaceUri()
        {
            // This is used when we can't determine the build environment. So, use a guid and package information
            // to generate the namespace.
            var packageName = Uri.EscapeDataString(MetadataDictionary[MetadataKey.PackageName] as string);
            var packageVersion = Uri.EscapeDataString(MetadataDictionary[MetadataKey.PackageVersion] as string);
            var uniqueNsPart = Uri.EscapeDataString(configuration.NamespaceUriUniquePart?.Value ?? IdentifierUtils.GetShortGuid(Guid.NewGuid()));

            return string.Join("/", configuration.NamespaceUriBase.Value, packageName, packageVersion, uniqueNsPart);
        }
    }

    /// <summary>
    /// Marker interface to indicate that this metadata provider should be the provider of last resort when another cannot be located.
    /// </summary>
    /// <remarks>Only one class should implement this interface, as it defines the one and only default provider.</remarks>
    internal interface IDefaultMetadataProvider
    {
    }
}
