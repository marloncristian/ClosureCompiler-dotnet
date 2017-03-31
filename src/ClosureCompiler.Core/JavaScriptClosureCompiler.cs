using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace ClosureCompiler.Core
{
    public class JavaScriptClosureCompiler
    {
        #region private methods path

        /// <summary>
        /// Retorna o caminho base da aplicacao
        /// </summary>
        private string path
            => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets");

        /// <summary>
        /// Retorna o caminho base para o cache de aplicacao
        /// </summary>
        private string pathCache
            => Path.Combine(path, "cache");

        /// <summary>
        /// Retorna o caminho para o arquivo jar do closure compiler
        /// </summary>
        private string pathClosureCompiler
            => Path.Combine(path, "closure-compiler.jar");

        #endregion

        #region private methods files

        /// <summary>
        /// Cria um cheksum a partir do arquivo
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private string getChecksum(string source)
        {
            var buffer = Encoding.ASCII.GetBytes(source);
            var hash = new MD5CryptoServiceProvider().ComputeHash(buffer);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Cria o arquivo
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private string saveToCache(string source, string directory)
        {
            cleanCache();

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var checksum = getChecksum(source);
            var filepath = Path.Combine(directory, $"{checksum}.data");

            if (File.Exists(filepath))
                return filepath;

            using (var file = File.CreateText(filepath))
            {
                file.Write(source);
                return filepath;
            }
        }

        /// <summary>
        /// Limpa o cache de todos os arquivos expirados
        /// </summary>
        private void cleanCache()
        {
            if (!Directory.Exists(pathCache))
                return;

            var files = new DirectoryInfo(pathCache).GetFiles();
            foreach (var file in files.Where(x => x.CreationTime < DateTime.Now.AddDays(-1)))
            {
                try
                {
                    file.Delete();
                }
                catch (Exception) { }
            }
        }

        #endregion

        #region private methods processes

        /// <summary>
        /// Cria um processo
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Process createProcess(string filename, string[] args)
        {
            return new Process
            {
                StartInfo =
                {
                    FileName = filename,
                    Arguments = string.Join(" ", args),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };
        }

        /// <summary>
        /// Cria um processo para o java
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private Process createJavaProcess(string[] args)
        {
            var javaPath = new JavaUtils().GetPath();
            return createProcess(Path.Combine(javaPath, "java"), args);
        }

        #endregion

        #region public methods compiler

        /// <summary>
        /// Verifica a validade do script
        /// </summary>
        /// <param name="source"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        public bool Check(string source, out string errors)
        {
            var filePath = saveToCache(source, pathCache);
            var args = new[] {
                $"-jar \"{pathClosureCompiler}\"",
                $"--js \"{filePath}\"",
                "-O ADVANCED",
                "-W DEFAULT"
            };

            using (var process = createJavaProcess(args))
            {
                process.Start();

                if (!process.WaitForExit((int)TimeSpan.FromMinutes(1).TotalMilliseconds))
                    throw new ApplicationException("Closure compiler timeout");

                errors = process.StandardError.ReadToEnd();
                return string.IsNullOrWhiteSpace(errors);
            }
        }

        /// <summary>
        /// Otimiza o script
        /// </summary>
        /// <param name="source"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        public string Optimize(string source, out string errors)
        {
            var filePath = saveToCache(source, pathCache);
            var args = new[] {
                $"-jar \"{pathClosureCompiler}\"",
                $"--js \"{filePath}\"",
                "-O WHITESPACE_ONLY",
                "-W DEFAULT"
            };

            using (var process = createJavaProcess(args))
            {
                process.Start();

                if (!process.WaitForExit((int)TimeSpan.FromMinutes(1).TotalMilliseconds))
                    throw new ApplicationException("Closure compiler timeout");

                errors = process.StandardError.ReadToEnd();
                return process.StandardOutput.ReadToEnd();
            }
        }

        #endregion
    }
}
