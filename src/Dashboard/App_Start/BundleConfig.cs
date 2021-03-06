﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Optimization;

namespace Dashboard
{
    public static class BundleConfig
    {
        // For more information on Bundling, visit http://go.microsoft.com/fwlink/?LinkId=254725
        public static void RegisterBundles(BundleCollection bundles)
        {
            if (bundles == null)
            {
                throw new ArgumentNullException("bundles");
            }

            bundles.Add(new ScriptBundle("~/bundles/js").Include(
                "~/Scripts/jquery-{version}.js",
                "~/Content/bootstrap/js/bootstrap.js",
                "~/Scripts/angular.min.js",
                "~/Scripts/angular-route.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/app") { Orderer = new PriorityFirstOrderer("main.js") }
                .IncludeDirectory("~/app", "*.js")
                .IncludeDirectory("~/app/controllers", "*.js")
                .IncludeDirectory("~/app/models", "*.js")
                .IncludeDirectory("~/app/services", "*.js"));

            bundles.Add(new StyleBundle("~/bundles/css").Include(
                "~/Content/bootstrap/css/bootstrap.css",
                "~/Content/Site.css"));

            // disable bundling optimizations for now
            BundleTable.EnableOptimizations = false;
        }

        private class PriorityFirstOrderer : DefaultBundleOrderer
        {
            private readonly string _filename;

            public PriorityFirstOrderer(string filename)
            {
                _filename = filename;
            }

            public override IEnumerable<BundleFile> OrderFiles(BundleContext context, IEnumerable<BundleFile> files)
            {
                var result = base.OrderFiles(context, files);
                return result.Where(f => f.VirtualFile.Name.Equals(_filename, StringComparison.OrdinalIgnoreCase))
                    .Concat(
                        result.Where(f => !f.VirtualFile.Name.Equals(_filename, StringComparison.OrdinalIgnoreCase)));
            }
        }
    }
}
