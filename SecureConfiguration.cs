using HarmonyLib;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using static System.Net.Mime.MediaTypeNames;


namespace DigitalLogic
{

    public static class SecureWebApplication
    {

        private static Func<string, string>? OnLoad;
        /// <summary>
        /// Customized version of the "CreateBuilder" function that allows using an encrypted appsettings.json.
        /// </summary>
        /// <param name="args">The command-line arguments for your ASP.NET application.</param>
        /// <param name="decryptionFunc">A function that decrypts your appsettings.json file.</param>
        /// <returns>The configured WebApplication instance.</returns>
        public static WebApplicationBuilder CreateBuilder(string[] args, Func<string, string> decryptionFunc)
        {
            Harmony harmony = new Harmony(Assembly.GetEntryAssembly()?.GetName().Name);
            SecureWebApplication.OnLoad = decryptionFunc;
            MethodInfo originalMethod = AccessTools.Method(typeof(FileConfigurationProvider), "Load", new Type[] { typeof(bool) });
            MethodInfo prefixMethod = AccessTools.Method(typeof(SecureWebApplication), "Load");
            harmony.Patch(originalMethod, new HarmonyMethod(prefixMethod));
            return WebApplication.CreateBuilder(args);
        }
        private static bool Load(FileConfigurationProvider __instance, bool reload)
        {

            var Data = typeof(FileConfigurationProvider).GetProperty("Data", BindingFlags.Instance | BindingFlags.NonPublic);
            var OnReload = typeof(FileConfigurationProvider).GetMethod("OnReload", BindingFlags.Instance | BindingFlags.NonPublic);
            var HandleException = typeof(FileConfigurationProvider).GetMethod("HandleException", BindingFlags.Instance | BindingFlags.NonPublic);
            if (__instance.Source.FileProvider == null)
                throw new Exception("FileProvider Is Null");
            IFileInfo file = __instance.Source.FileProvider!.GetFileInfo(__instance.Source.Path);
            if (file == null || !file.Exists)
            {
                if (__instance.Source.Optional || reload) // Always optional on reload
                {
                    Data?.SetValue(__instance, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
                }
                else
                {
                    var error = new StringBuilder($"{__instance.Source.Path} Not Found");
                    if (!string.IsNullOrEmpty(file?.PhysicalPath))
                    {
                        error.Append($"PhysicalPath : ${file.PhysicalPath}");
                    }
                    HandleException?.Invoke(__instance, new[] { ExceptionDispatchInfo.Capture(new FileNotFoundException(error.ToString())) });

                }
            }
            else
            {
                static Stream OpenRead(IFileInfo fileInfo)
                {
                    if (fileInfo.PhysicalPath != null)
                    {
                        return new FileStream(
                            fileInfo.PhysicalPath,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.ReadWrite,
                            bufferSize: 1,
                            FileOptions.SequentialScan);
                    }

                    return fileInfo.CreateReadStream();
                }

                using Stream stream = OpenRead(file);
                try
                {
                    var mod = new StreamReader(stream).ReadToEnd();
                    if (OnLoad != null)
                        mod = OnLoad(mod);
                    __instance.Load(new MemoryStream(Encoding.UTF8.GetBytes(mod)));
                }
                catch (Exception ex)
                {
                    if (reload)
                    {
                        Data?.SetValue(__instance, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

                    }
                    HandleException?.Invoke(__instance, new[] { ExceptionDispatchInfo.Capture(ex) });
                }
            }
            // REVIEW: Should we raise this in the base as well / instead?
            OnReload?.Invoke(__instance, null);
            return false;
        }
    }

}