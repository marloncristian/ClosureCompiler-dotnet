using Microsoft.Win32;
using System;
using System.IO;

namespace ClosureCompiler.Core
{
    public class JavaUtils
    {
        /// <summary>
        /// Retorna o caminho de instalacao do java
        /// </summary>
        /// <returns></returns>
        public string GetPath()
        {
            return getFromEnvironmentVariables()
                ?? getFromRuntimeEnvironmentRegistry()
                ?? getFrom64DevelopmentKitRegistry();
        }

        /// <summary>
        /// Retorna o diretorio a partir da configuracao de variaveis de ambiente
        /// </summary>
        /// <returns></returns>
        private string getFromEnvironmentVariables()
        {
            return Environment.GetEnvironmentVariable("JAVA_HOME");
        }

        /// <summary>
        /// Retorna o diretorio a partir do registro de "runtime environment"
        /// </summary>
        /// <returns></returns>
        private string getFromRuntimeEnvironmentRegistry()
        {
            string javaKey = "SOFTWARE\\JavaSoft\\Java Runtime Environment\\";
            using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(javaKey))
            {
                if (registryKey == null)
                    return null;

                string currentVersion = registryKey.GetValue("CurrentVersion").ToString();
                using (RegistryKey key = registryKey.OpenSubKey(currentVersion))
                {
                    return key.GetValue("JavaHome").ToString();
                }
            }
        }

        /// <summary>
        /// Retorna o diretorio a partir do registro de sdk do java
        /// </summary>
        /// <returns></returns>
        private string getFrom64DevelopmentKitRegistry()
        {
            string javaKey = @"SOFTWARE\WOW6432Node\JavaSoft\Java Development Kit\";
            using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(javaKey))
            {
                if (registryKey == null)
                    return null;

                string currentVersion = registryKey.GetValue("CurrentVersion").ToString();
                using (RegistryKey key = registryKey.OpenSubKey(currentVersion))
                {
                    return Path.Combine(key.GetValue("JavaHome").ToString(), "bin");
                }
            }
        }
    }
}
