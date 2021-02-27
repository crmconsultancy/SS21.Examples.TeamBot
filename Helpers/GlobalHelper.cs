using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SS21.Examples.TeamBot.Helpers
{
    public static class GlobalHelper
    {
        public static string GetEmbeddedResource(string assemblyName, string embeddedResourceName)
        {
            string resourceData = string.Empty;
            var assembly = Assembly.GetExecutingAssembly();

            try
            {
                string resourceLocation = assemblyName + "." + embeddedResourceName;

                System.IO.Stream s = assembly.GetManifestResourceStream(resourceLocation);

                if (s == null)
                {
                    throw new Exception("Could not read embedded resource for " + resourceLocation);
                }

                using (StreamReader reader = new StreamReader(s))
                {
                    resourceData = reader.ReadToEnd();
                }

                if (resourceData == string.Empty)
                {
                    throw new Exception("Unable to retrieve the Embedded Resource!");
                }
            }
            catch (Exception e)
            {
                throw new Exception("EmailHelper.GetEmbeddedResource :: ERROR :: " + e.Message);
            }

            return resourceData;
        }
    }
}
