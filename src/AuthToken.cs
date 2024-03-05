/*  This is an optional helper class to store your auth token in local storage
 *
    Example:

    AuthToken.Init(".my_app_name");

    SpacetimeDBClient.CreateInstance(new ConsoleLogger());

    SpacetimeDBClient.instance.onIdentityReceived += (token, identity) =>
    {
        AuthToken.SaveToken(token);

        ...
    };

    SpacetimeDBClient.instance.Connect(AuthToken.Token, "localhost:3000", "basicchat", false);
 */
#if !UNITY_5_3_OR_NEWER
using System;
using System.IO;
using System.Linq;

namespace SpacetimeDB
{
    public static class AuthToken
    {
        private static string? settingsPath;
        private static string? token;

        /// <summary>
        /// Initializes the AuthToken class. This must be called before any other methods.
        /// </summary>
        /// <param name="configFolder">The folder to store the config file in. Default is ".spacetime_csharp_sdk".</param>
        /// <param name="configFile">The name of the config file. Default is "settings.ini".</param>
        /// <param name="configRoot">The root folder to store the config file in. Default is the user's home directory.</param>
        /// </summary>
        public static void Init(string configFolder = ".spacetime_csharp_sdk", string configFile = "settings.ini", string? configRoot = null)
        {
            configRoot ??= Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (Environment.GetCommandLineArgs().Any(arg => arg == "--client"))
            {
                int clientIndex = Array.FindIndex(Environment.GetCommandLineArgs(), arg => arg == "--client");
                var configFileParts = configFile.Split(".");
                configFile = $"{configFileParts[0]}_{Environment.GetCommandLineArgs()[clientIndex + 1]}.{configFileParts[1]}";
            }

            settingsPath = Path.Combine(configRoot, configFolder, configFile);

            if (File.Exists(settingsPath))
            {
                foreach (string line in File.ReadAllLines(settingsPath))
                {
                    if (line.StartsWith("auth_token="))
                    {
                        token = line.Substring("auth_token=".Length);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// This is the auth token that was saved to local storage. Null if not never saved.
        /// When you specify null to the SpacetimeDBClient, SpacetimeDB will generate a new identity for you.
        /// </summary>
        public static string? Token
        {
            get
            {
                if (settingsPath == null)
                {
                    throw new Exception("Token not initialized. Call AuthToken.Init() first.");
                }
                return token;
            }
        }

        /// <summary>
        /// Save the auth token to local storage.
        /// SpacetimeDBClient provides this token to you in the onIdentityReceived callback.
        /// </summary>
        public static void SaveToken(string token)
        {
            if (settingsPath == null)
            {
                throw new Exception("Token not initialized. Call AuthToken.Init() first.");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            var newAuthLine = "auth_token=" + token;
            if (File.Exists(settingsPath))
            {
                var lines = File.ReadLines(settingsPath).ToList();
                var i = lines.FindIndex(line => line.StartsWith("auth_token="));
                if (i >= 0)
                {
                    lines[i] = newAuthLine;
                }
                else
                {
                    lines.Add(newAuthLine);
                }
                File.WriteAllLines(settingsPath, lines);
            }
            else
            {
                File.WriteAllText(settingsPath, newAuthLine);
            }
        }
    }
}
#endif
