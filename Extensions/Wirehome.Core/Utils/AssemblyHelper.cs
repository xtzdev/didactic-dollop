﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WWirehome.Core.Utils
{
    public static class AssemblyHelper
    {
        public static IEnumerable<Assembly> GetProjectAssemblies()
        {
            var mainAssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            var applicationNameName = mainAssemblyName.Substring(0, mainAssemblyName.IndexOf("."));
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            return assemblies.Where(x => x.FullName.IndexOf(applicationNameName) > -1);
        }
    }
}