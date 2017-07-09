using System;
using System.Threading.Tasks;

namespace Plugin.pbXSettings.Abstractions
{
    /// <summary>
    /// </summary>
    public interface ISettingsStorage
    {
		Task<string> GetStringAsync(string id);
		Task SetStringAsync(string id, string d);
    }
}