﻿using System.Collections.Generic;
using System.IO;

namespace NHM.Common
{
    public static class Paths
    {
        public static string Root { get; private set; }  = "";

        public static void SetRoot(string rootPath)
        {
            Root = rootPath;
        }

        // TODO deprecate this one
        public static string MinerPluginsPath()
        {
            return RootPath("miner_plugins");
        }

        public static string MinerPluginsPath(params string[] paths) {
            return RootPath("miner_plugins", paths);
        }

        public static string ConfigsPath(params string[] paths)
        {
            return RootPath("configs", paths);
        }

        public static string InternalsPath(params string[] paths)
        {
            return RootPath("internals", paths);
        }

        public static string RootPath(string subPath, params string[] paths)
        {
            var combine = new List<string> { Root, subPath };
            if (paths.Length > 0) combine.AddRange(paths);
            var path = Path.Combine(combine.ToArray());
            return path;
        }
    }
}
