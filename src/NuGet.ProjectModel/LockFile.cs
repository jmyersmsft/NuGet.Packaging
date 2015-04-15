// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.LibraryModel;

namespace NuGet.ProjectModel
{
    public class LockFile
    {
        public bool Islocked { get; set; }
        public int Version { get; set; }
        public IList<ProjectFileDependencyGroup> ProjectFileDependencyGroups { get; set; } =
            new List<ProjectFileDependencyGroup>();
        public IList<LockFileLibrary> Libraries { get; set; } = new List<LockFileLibrary>();

        public bool IsValidForPackageSpec(PackageSpec spec)
        {
            if (Version != LockFileFormat.Version)
            {
                return false;
            }

            var actualTargetFrameworks = spec.TargetFrameworks;

            // The lock file should contain dependencies for each framework plus dependencies shared by all frameworks
            if (ProjectFileDependencyGroups.Count != actualTargetFrameworks.Count() + 1)
            {
                return false;
            }

            foreach (var group in ProjectFileDependencyGroups)
            {
                IOrderedEnumerable<string> actualDependencies;
                var expectedDependencies = group.Dependencies.OrderBy(x => x);

                // If the framework name is empty, the associated dependencies are shared by all frameworks
                if (string.IsNullOrEmpty(group.FrameworkName))
                {
                    actualDependencies = spec.Dependencies.Select(x => RuntimeStyleLibraryRangeToString(x.LibraryRange)).OrderBy(x => x);
                }
                else
                {
                    var framework = actualTargetFrameworks
                        .FirstOrDefault(f =>
                            string.Equals(f.FrameworkName.ToString(), group.FrameworkName, StringComparison.Ordinal));
                    if (framework == null)
                    {
                        return false;
                    }

                    actualDependencies = framework.Dependencies.Select(d => RuntimeStyleLibraryRangeToString(d.LibraryRange)).OrderBy(x => x);
                }

                if (!actualDependencies.SequenceEqual(expectedDependencies))
                {
                    return false;
                }
            }

            return true;
        }

        // DNU REFACTORING TODO: temp hack to make generated lockfile work with runtime lockfile validation
        private static string RuntimeStyleLibraryRangeToString(LibraryRange libraryRange)
        {
            return string.Format("{0} >= {1}.{2}.{3}{4}",
                libraryRange.Name,
                libraryRange.VersionRange.MinVersion.Version.Major,
                libraryRange.VersionRange.MinVersion.Version.Minor,
                libraryRange.VersionRange.MinVersion.Version.Build,
                libraryRange.VersionRange.IsFloating ? "-*" : string.Empty);
        }
    }
}