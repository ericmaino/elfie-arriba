using System;
using System.Collections.Generic;
using System.Text;

namespace Arriba.Configuration
{
    public interface IArribaConfigurationLoader
    {

        T Bind<T>(string sectionName) where T : IArribaConfiguration, new();
        string GetStringValue(string keyName, string defaultValue = null);
        int GetIntValue(string keyName, int defaultValue = 0);
        bool GetBoolValue(string keyName, bool defaultValue = false);
        bool AddJsonSource(string jsonPath);
        IEnumerator<KeyValuePair<string, string>> GetEnumerator();
    }
}
