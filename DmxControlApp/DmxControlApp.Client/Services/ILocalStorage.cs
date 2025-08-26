namespace DmxControlApp.Client.Services;

public interface ILocalStorage
{
    T? GetItem<T>(string key);
    void SetItem<T>(string key, T value);
    void RemoveItem(string key);
}

