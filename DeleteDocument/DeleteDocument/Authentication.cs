using System;
using System.Collections.Generic;
using System.Reflection;

namespace DeleteDocument
{
    public static class Authentication
    {
        /// <summary>
        /// Gets the stored administrative credentials via the Square9.AuthenticationSettings assembly included in the Workflow Engine.
        /// </summary>
        /// <returns>A key value pair containing the username and password</returns>
        public static KeyValuePair<string, string> GetAdminCredentials()
        {
            var authAssembly = Assembly.Load("Square9.AuthenticationSettings");
            if (authAssembly == null)
            {
                throw new Exception("Square9.AuthenticationSettings.dll is missing from the Workflow Engine folder.");
            }
            var authSettingsManagerType = authAssembly.GetType("Square9.AuthenticationSettings.AuthenticationSettingsManager");
            var getStoredAdminUserInfo = authSettingsManagerType.GetMethod("GetStoredAdminUser");
            var getStoredAdminPasswordInfo = authSettingsManagerType.GetMethod("GetStoredAdminPassword");

            if (getStoredAdminUserInfo == null || getStoredAdminPasswordInfo == null)
            {
                throw new Exception("Required credential method not found.");
            }

            var authSettingsManagerInstance = Activator.CreateInstance(authSettingsManagerType);
            var username = getStoredAdminUserInfo.Invoke(authSettingsManagerInstance, null).ToString();
            var password = getStoredAdminPasswordInfo.Invoke(authSettingsManagerInstance, null).ToString();
            authSettingsManagerInstance = null;

            return new KeyValuePair<string, string>(username, password);
        }
    }
}
