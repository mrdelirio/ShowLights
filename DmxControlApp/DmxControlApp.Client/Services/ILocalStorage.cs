namespace DmxControlApp.Client.Services;

public interface ILocalStorage
{
    Task<T?> GetItemAsync<T>(string key);
    Task SetItemAsync<T>(string key, T value);
    Task RemoveItemAsync(string key);
    
    // Metodi sincroni per compatibilità (solo per il server)
    T? GetItem<T>(string key);
    void SetItem<T>(string key, T value);
    void RemoveItem(string key);
}

